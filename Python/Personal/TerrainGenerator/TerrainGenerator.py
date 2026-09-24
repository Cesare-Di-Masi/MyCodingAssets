#!/usr/bin/env python3
"""
3D TERRAIN GENERATOR v2
=======================
Territorio (OSM/Nominatim) + quote (SRTM o Terrarium) -> modello 3D stampabile.

Dipendenze obbligatorie:  numpy  shapely>=2  trimesh  requests
Opzionali (le funzioni collegate si disattivano con un avviso se mancano):
    pyproj             proiezione corretta (LAEA)      pip install pyproj
    pillow             sorgente Terrarium (globale)    pip install pillow
    manifold3d         spine di incastro + incisioni   pip install manifold3d
    fast-simplification  decimazione della mesh        pip install fast-simplification
    pyglet<2           viewer 3D                       pip install "pyglet<2"

Esempi
------
  python terrain_generator_v2.py                                   # interattivo
  python terrain_generator_v2.py --place Fuerteventura --mode isola --res 300
  python terrain_generator_v2.py --place Italia --mode stato --plate 220x220
  python terrain_generator_v2.py --place Tenerife --bathy 0.2 --simplify 0.4
  python terrain_generator_v2.py --bbox -14.6 28.0 -13.7 28.8 --res 250
  python terrain_generator_v2.py --config mio.json                 # opzioni da file
"""

import argparse
import gzip
import hashlib
import io
import json
import math
import os
import re
import sys
import threading
import time
import unicodedata
import warnings
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass

import numpy as np
import requests
import shapely
import trimesh
from shapely.geometry import shape

try:
    import pyproj
except ImportError:
    pyproj = None

try:
    from PIL import Image
except ImportError:
    Image = None

try:
    import manifold3d  # noqa: F401  (backend booleano di trimesh)
    HAVE_MANIFOLD = True
except ImportError:
    HAVE_MANIFOLD = False

try:
    import fast_simplification
except ImportError:
    fast_simplification = None


# ============================================================
# COSTANTI E DEFAULT
# ============================================================

CACHE_ROOT = os.path.join(os.path.expanduser("~"), ".cache", "terrain_generator")
NOMINATIM_CACHE = os.path.join(CACHE_ROOT, "nominatim")
TERRARIUM_CACHE = os.path.join(CACHE_ROOT, "terrarium")
# stessa cache della versione 1: i tile già scaricati vengono riutilizzati
SRTM_CACHE_DIR = os.path.join(os.path.expanduser("~"), ".cache", "srtm_aws")

SRTM_AWS_BASE = "https://s3.amazonaws.com/elevation-tiles-prod/skadi"
TERRARIUM_URL = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png"
NOMINATIM_URL = "https://nominatim.openstreetmap.org/search"
USER_AGENT = "terrain-generator-stl/2.0 (personal project)"
NOMINATIM_TTL_S = 30 * 24 * 3600

SRTM_PIXEL_M = 30.9           # 1 arcsec
MAX_SRTM_TILES_AUTO = 12      # oltre, "auto" passa a Terrarium
MAX_SRTM_TILES_WARN = 60
TERRARIUM_MAX_TILES = 600
TERRARIUM_MAX_ZOOM = 14

PIN_RADIUS = 1.5
PIN_LENGTH = 4.0
PIN_CLEARANCE = 0.15
ENGRAVE_DEPTH = 0.6

DEFAULTS = dict(
    mode=None, place=None, pick=None, bbox=None, name=None,
    res=200, size=150.0,
    vexag="auto", relief_mm=None, max_auto_vexag=25.0,
    base=5.0, land_min=0.4,
    peaks=0.0, max_slope=0.8, smooth=0, ss=0,
    source="auto", bathy=0.0, bathy_max_mm=10.0,
    proj="auto", margin=0.03,
    simplify=0.0,
    plate=None, split=None, pins=True, label=True,
    out_dir=".", formats="stl,ply,obj",
    view=False, yes=False,
)

MODES = {
    "1": ("territorio", "Territorio (qualsiasi risultato OSM)"),
    "2": ("isola",      "Isola / Arcipelago"),
    "3": ("stato",      "Stato / Nazione"),
    "4": ("regione",    "Regione"),
    "5": ("provincia",  "Provincia / Città metropolitana"),
    "6": ("bbox",       "Bounding box manuale (no OSM)"),
}

MODE_KEYWORDS = {
    "isola":     ["island", "islet", "archipelago"],
    "stato":     ["country", "nation"],
    "regione":   ["region", "state"],
    "provincia": ["province", "county", "metropolitan"],
}


# ============================================================
# HELPERS DI PROGRESSO
# ============================================================

class Spinner:
    """Spinner con tempo trascorso, per fasi di durata ignota."""

    def __init__(self, message="Lavorazione"):
        self.message = message
        self._stop = threading.Event()
        self._thread = None

    def _spin(self):
        chars = "|/-\\"
        t0 = time.time()
        i = 0
        while not self._stop.is_set():
            sys.stdout.write(
                f"\r  {self.message}... {chars[i % 4]}  {time.time() - t0:6.1f}s"
            )
            sys.stdout.flush()
            i += 1
            time.sleep(0.1)

    def __enter__(self):
        self._thread = threading.Thread(target=self._spin, daemon=True)
        self._thread.start()
        return self

    def __exit__(self, *exc):
        self._stop.set()
        if self._thread is not None:
            self._thread.join()
        sys.stdout.write("\r" + " " * 78 + "\r")
        sys.stdout.flush()


def progress_bar(done, total, prefix="", width=30):
    if total <= 0:
        return
    frac = min(max(done / total, 0.0), 1.0)
    filled = int(round(width * frac))
    bar = "#" * filled + "-" * (width - filled)
    sys.stdout.write(
        f"\r  {prefix} [{bar}] {int(frac * 100):3d}%  ({done}/{total})"
    )
    sys.stdout.flush()
    if done >= total:
        sys.stdout.write("\n")


def slugify(text):
    text = unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode()
    text = re.sub(r"[^a-zA-Z0-9]+", "_", text).strip("_").lower()
    return text or "terrain"


def ask_yes(prompt, assume_yes=False):
    if assume_yes:
        return True
    return input(prompt).strip().lower() in ("s", "si", "sì", "y", "yes")


def atomic_write(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = f"{path}.{threading.get_ident()}.tmp"
    with open(tmp, "wb") as f:
        f.write(data)
    os.replace(tmp, path)


# ============================================================
# TERRITORIO (Nominatim con cache, antimeridiano)
# ============================================================

@dataclass
class Territory:
    name: str
    polygon: object          # shapely geometry nel "frame" longitudine, o None (bbox)
    bbox: tuple              # (min_lon, min_lat, max_lon, max_lat) nel frame
    wrapped: bool = False    # True: longitudini in 0..360 (attraversa l'antimeridiano)


def frame_lon(lon, wrapped):
    return np.mod(lon, 360.0) if wrapped else lon


def norm_lon(lon):
    return ((np.asarray(lon, dtype=float) + 180.0) % 360.0) - 180.0


def unwrap_antimeridian(geom):
    """Se la geometria attraversa ±180°, porta le longitudini in 0..360."""
    minx, _, maxx, _ = geom.bounds
    if maxx - minx < 180:
        return geom, False

    def shift(c):
        c = np.array(c, dtype=float)
        c[:, 0] = np.where(c[:, 0] < 0, c[:, 0] + 360.0, c[:, 0])
        return c

    shifted = shapely.transform(geom, shift)
    if not shifted.is_valid:
        shifted = shifted.buffer(0)
    sminx, _, smaxx, _ = shifted.bounds
    if (smaxx - sminx) < 0.8 * (maxx - minx):
        return shifted, True
    return geom, False


_last_nominatim = [0.0]


def _nominatim_raw(query, limit=15):
    key = hashlib.sha1(f"{query.strip().lower()}|{limit}".encode()).hexdigest()
    path = os.path.join(NOMINATIM_CACHE, key + ".json")

    if os.path.exists(path) and time.time() - os.path.getmtime(path) < NOMINATIM_TTL_S:
        with open(path, encoding="utf-8") as f:
            return json.load(f), True

    # rispetta il limite di 1 richiesta/secondo
    wait = 1.1 - (time.time() - _last_nominatim[0])
    if wait > 0:
        time.sleep(wait)

    r = requests.get(
        NOMINATIM_URL,
        params={"q": query, "format": "jsonv2", "polygon_geojson": 1, "limit": limit},
        headers={"User-Agent": USER_AGENT},
        timeout=60,
    )
    _last_nominatim[0] = time.time()
    r.raise_for_status()
    data = r.json()
    if data:
        atomic_write(path, json.dumps(data).encode("utf-8"))
    return data, False


def nominatim_search(query, limit=15):
    """Tutti i risultati poligonali di Nominatim (con cache su disco)."""
    data, cached = _nominatim_raw(query, limit)
    if cached:
        print("  (risultati dalla cache locale)")

    results = []
    for item in data:
        geom = item.get("geojson")
        if not geom or geom.get("type") not in ("Polygon", "MultiPolygon"):
            continue
        poly = shape(geom)
        if not poly.is_valid:
            poly = poly.buffer(0)
        if poly.is_empty:
            continue
        results.append({
            "display_name": item.get("display_name", "Nome sconosciuto"),
            "osm_type": item.get("osm_type"),
            "class": item.get("category") or item.get("class"),
            "type": item.get("type"),
            "addresstype": item.get("addresstype"),
            "importance": item.get("importance"),
            "geometry": poly,
        })
    return results


def sort_by_mode(results, mode):
    keywords = MODE_KEYWORDS.get(mode)
    if not keywords or len(results) <= 1:
        return results

    def score(r):
        hay = " ".join(
            str(r.get(k) or "").lower()
            for k in ("addresstype", "class", "type", "display_name")
        )
        for i, kw in enumerate(keywords):
            if kw in hay:
                return i
        return len(keywords)

    return sorted(results, key=score)


def choose_mode():
    print("\nModalità di ricerca:")
    for k, (_, label) in MODES.items():
        print(f"  [{k}] {label}")
    choice = input("Seleziona (1-6) [default: 1]: ").strip() or "1"
    if choice not in MODES:
        raise ValueError("Modalità non valida.")
    return MODES[choice][0]


def territory_from_bbox(min_lon, min_lat, max_lon, max_lat, name=None):
    wrapped = False
    if max_lon < min_lon:          # es. 176 -> -178: attraversa l'antimeridiano
        wrapped = True
        min_lon = float(np.mod(min_lon, 360.0))
        max_lon = float(np.mod(max_lon, 360.0))
        if max_lon <= min_lon:
            max_lon += 360.0
    if not (min_lon < max_lon and min_lat < max_lat):
        raise ValueError("Bounding box non valido.")
    if not (-90 <= min_lat and max_lat <= 90):
        raise ValueError("Latitudini fuori dai limiti.")
    return Territory(
        name=name or f"bbox_{min_lat:.2f}_{min_lon:.2f}",
        polygon=None,
        bbox=(min_lon, min_lat, max_lon, max_lat),
        wrapped=wrapped,
    )


def territory_from_place(place, mode, pick, assume_yes):
    print("\nRicerca su OpenStreetMap...")
    print(f"Query: {place}")

    try:
        results = nominatim_search(place)
    except Exception as e:
        raise RuntimeError(f"Errore durante la ricerca OpenStreetMap:\n{e}")

    if not results:
        raise ValueError(f"Nessun risultato con un'area per '{place}'.")

    results = sort_by_mode(results, mode)

    print(f"\nRisultati OpenStreetMap ({len(results)} trovati):")
    print("-" * 50)
    for pos, r in enumerate(results):
        print(f"\n  [{pos}] {r['display_name']}")
        for key, label in (("osm_type", "osm"), ("class", "class"),
                           ("type", "type"), ("addresstype", "addresstype"),
                           ("importance", "importance")):
            if r.get(key) is not None:
                print(f"        {label}: {r[key]}")

    if pick is None:
        if len(results) > 1 and not assume_yes:
            raw = input(
                f"\nQuale risultato vuoi utilizzare? (0-{len(results)-1}) [0]: "
            ).strip() or "0"
            try:
                pick = int(raw)
            except ValueError:
                raise ValueError("Scelta non valida.")
        else:
            pick = 0
    if pick < 0 or pick >= len(results):
        raise ValueError("Indice fuori intervallo.")

    result = results[pick]
    polygon, wrapped = unwrap_antimeridian(result["geometry"])
    if wrapped:
        print("  (territorio a cavallo dell'antimeridiano: longitudini 0..360)")

    print("\nTerritorio selezionato:")
    print("-" * 50)
    print(result["display_name"])
    print("-" * 50)

    return Territory(
        name=result["display_name"].split(",")[0],
        polygon=polygon,
        bbox=polygon.bounds,
        wrapped=wrapped,
    )


def get_territory(cfg):
    """Da argomenti CLI/config, oppure in modo interattivo."""
    if cfg.bbox:
        return territory_from_bbox(*cfg.bbox, name=cfg.name)

    if cfg.place:
        return territory_from_place(
            cfg.place, cfg.mode or "territorio", cfg.pick, cfg.yes
        )

    mode = cfg.mode or choose_mode()

    if mode == "bbox":
        print("\nInserisci il bounding box in coordinate WGS84")
        print("(per attraversare l'antimeridiano: Max Lon < Min Lon, es. 176 e -178)")
        try:
            vals = [float(input(f"  {lbl}: ")) for lbl in
                    ("Min Lon", "Min Lat", "Max Lon", "Max Lat")]
        except ValueError:
            raise ValueError("Coordinate non valide.")
        return territory_from_bbox(*vals, name=cfg.name)

    place = input(
        "\nInserisci il nome del territorio (es. Ischia, Italia):\n> "
    ).strip()
    if not place:
        raise ValueError("Non hai inserito nessun nome.")
    return territory_from_place(place, mode, cfg.pick, cfg.yes)


# ============================================================
# PROIEZIONE
# ============================================================

class Projection:
    """LAEA (pyproj) oppure equirettangolare con cos(lat0) come ripiego."""

    def __init__(self, kind, lon0, lat0):
        self.lon0 = float(norm_lon(lon0))
        self.lat0 = float(lat0)
        self.kind = kind

        if kind == "laea":
            crs = pyproj.CRS.from_proj4(
                f"+proj=laea +lat_0={self.lat0} +lon_0={self.lon0} "
                "+x_0=0 +y_0=0 +datum=WGS84 +units=m +no_defs"
            )
            self._fwd = pyproj.Transformer.from_crs("EPSG:4326", crs, always_xy=True)
            self._inv = pyproj.Transformer.from_crs(crs, "EPSG:4326", always_xy=True)
        else:
            self._k = 111320.0 * math.cos(math.radians(self.lat0))

    def forward(self, lon, lat):
        lon = norm_lon(lon)
        lat = np.asarray(lat, dtype=float)
        if self.kind == "laea":
            return self._fwd.transform(lon, lat)
        dl = norm_lon(lon - self.lon0)
        return dl * self._k, (lat - self.lat0) * 111320.0

    def inverse(self, x, y):
        x = np.asarray(x, dtype=float)
        y = np.asarray(y, dtype=float)
        if self.kind == "laea":
            lon, lat = self._inv.transform(x, y)
            return norm_lon(lon), np.asarray(lat)
        return norm_lon(self.lon0 + x / self._k), self.lat0 + y / 111320.0


def make_projection(kind, terr):
    min_lon, min_lat, max_lon, max_lat = terr.bbox
    lon0 = (min_lon + max_lon) / 2
    lat0 = (min_lat + max_lat) / 2

    if kind == "auto":
        kind = "laea" if pyproj is not None else "equirect"
        if pyproj is None:
            print("  pyproj non installato: uso proiezione equirettangolare "
                  "(pip install pyproj per LAEA).")
    elif kind == "laea" and pyproj is None:
        print("  pyproj non installato: ripiego su equirettangolare.")
        kind = "equirect"

    return Projection(kind, lon0, lat0)


# ============================================================
# GRIGLIA
# ============================================================

@dataclass
class Grid:
    x: np.ndarray     # (W,) metri
    y: np.ndarray     # (H,) metri
    dx: float
    dy: float

    @property
    def W(self):
        return len(self.x)

    @property
    def H(self):
        return len(self.y)


def territory_extent(terr, proj, margin_frac):
    """Rettangolo (xmin, ymin, xmax, ymax) in metri che contiene il territorio."""
    if terr.polygon is not None:
        coords = shapely.get_coordinates(terr.polygon)
        lon, lat = coords[:, 0], coords[:, 1]
    else:
        min_lon, min_lat, max_lon, max_lat = terr.bbox
        t = np.linspace(0, 1, 200)
        lon = np.concatenate([min_lon + t * (max_lon - min_lon),
                              np.full_like(t, max_lon),
                              max_lon - t * (max_lon - min_lon),
                              np.full_like(t, min_lon)])
        lat = np.concatenate([np.full_like(t, min_lat),
                              min_lat + t * (max_lat - min_lat),
                              np.full_like(t, max_lat),
                              max_lat - t * (max_lat - min_lat)])

    x, y = proj.forward(lon, lat)
    xmin, xmax = float(np.min(x)), float(np.max(x))
    ymin, ymax = float(np.min(y)), float(np.max(y))
    m = margin_frac * max(xmax - xmin, ymax - ymin)
    return xmin - m, ymin - m, xmax + m, ymax + m


def make_grid(extent, grid_res):
    """Griglia rettangolare con celle ~quadrate; il lato lungo ha grid_res vertici."""
    xmin, ymin, xmax, ymax = extent
    w, h = xmax - xmin, ymax - ymin

    if w >= h:
        W = grid_res
        dx = w / (W - 1)
        H = max(2, int(round(h / dx)) + 1)
        dy = h / (H - 1)
    else:
        H = grid_res
        dy = h / (H - 1)
        W = max(2, int(round(w / dy)) + 1)
        dx = w / (W - 1)

    return Grid(x=xmin + dx * np.arange(W), y=ymin + dy * np.arange(H), dx=dx, dy=dy)


def grid_lonlat_bounds(grid, proj, wrapped):
    """Estensione lon/lat (nel frame) coperta dalla griglia, con un po' di margine."""
    xs = np.linspace(grid.x[0] - 2 * grid.dx, grid.x[-1] + 2 * grid.dx, 80)
    ys = np.linspace(grid.y[0] - 2 * grid.dy, grid.y[-1] + 2 * grid.dy, 80)
    X, Y = np.meshgrid(xs, ys)
    lon, lat = proj.inverse(X.ravel(), Y.ravel())
    lon = frame_lon(lon, wrapped)
    return float(lon.min()), float(lat.min()), float(lon.max()), float(lat.max())


def pick_supersampling(n_cells, requested):
    if requested and requested > 0:
        return int(requested)
    if n_cells <= 300_000:
        return 4
    if n_cells <= 700_000:
        return 3
    if n_cells <= 2_000_000:
        return 2
    return 1


# ============================================================
# SORGENTE 1: SRTM (Skadi su AWS)
# ============================================================

def _tile_name(lat, lon):
    ns = "N" if lat >= 0 else "S"
    ew = "E" if lon >= 0 else "W"
    return f"{ns}{abs(lat):02d}{ew}{abs(lon):03d}"


def _tile_path(tile):
    return os.path.join(SRTM_CACHE_DIR, f"{tile}.hgt")


def _empty_marker(tile):
    return os.path.join(SRTM_CACHE_DIR, f"{tile}.none")


def srtm_tiles_for_bounds(bounds):
    """Lista di (lat, lon) dei tile SRTM (lon normalizzata) per un'estensione."""
    min_lon, min_lat, max_lon, max_lat = bounds
    out = []
    for lat in range(int(np.floor(min_lat)), int(np.floor(max_lat)) + 1):
        for L in range(int(np.floor(min_lon)), int(np.floor(max_lon)) + 1):
            out.append((lat, int(norm_lon(L))))
    return out


def _download_srtm_tile(tile, timeout=60, retries=3):
    local = _tile_path(tile)
    if os.path.exists(local):
        return local
    if os.path.exists(_empty_marker(tile)):
        return None

    os.makedirs(SRTM_CACHE_DIR, exist_ok=True)
    url = f"{SRTM_AWS_BASE}/{tile[:3]}/{tile}.hgt.gz"
    last_err = None

    for attempt in range(retries):
        try:
            r = requests.get(url, timeout=timeout)
            # S3 risponde 403 (non 404) per le chiavi inesistenti
            if r.status_code in (403, 404):
                open(_empty_marker(tile), "wb").close()
                return None
            r.raise_for_status()
            atomic_write(local, gzip.decompress(r.content))
            return local
        except Exception as e:
            last_err = e
            time.sleep(1.5 * (attempt + 1))

    raise RuntimeError(f"Download fallito dopo {retries} tentativi: {last_err}")


def _open_hgt(path):
    size = os.path.getsize(path)
    n = int(round(math.sqrt(size // 2)))
    if n * n * 2 != size:
        raise ValueError(f"Dimensione .hgt inattesa: {size} byte (n = {n})")
    return np.memmap(path, dtype=">i2", mode="r", shape=(n, n))


def _reduce_blocks(arr, f, strip_rows=64):
    """Riduzione a blocchi f x f: restituisce (media, massimo) float32 >= 0."""
    n = arr.shape[0]
    m = n // f
    mean = np.empty((m, m), dtype=np.float32)
    mx = np.empty((m, m), dtype=np.float32)
    for r0 in range(0, m, strip_rows):
        r1 = min(r0 + strip_rows, m)
        blk = np.asarray(arr[r0 * f:r1 * f, :m * f], dtype=np.float32)
        blk = np.maximum(blk, 0.0)          # vuoti (-32768) e negativi -> 0
        blk = blk.reshape(r1 - r0, f, m, f)
        mean[r0:r1] = blk.mean(axis=(1, 3))
        mx[r0:r1] = blk.max(axis=(1, 3))
    return mean, mx


def _bilinear(arr, fy, fx, clean=False):
    h, w = arr.shape
    fy = np.clip(fy, 0, h - 1)
    fx = np.clip(fx, 0, w - 1)
    y0 = np.floor(fy).astype(np.int64)
    x0 = np.floor(fx).astype(np.int64)
    y1 = np.minimum(y0 + 1, h - 1)
    x1 = np.minimum(x0 + 1, w - 1)
    wy = (fy - y0).astype(np.float32)
    wx = (fx - x0).astype(np.float32)

    def g(yy, xx):
        v = np.asarray(arr[yy, xx], dtype=np.float32)
        return np.maximum(v, 0.0) if clean else v

    return (g(y0, x0) * (1 - wx) * (1 - wy) + g(y0, x1) * wx * (1 - wy)
            + g(y1, x0) * (1 - wx) * wy + g(y1, x1) * wx * wy)


class SrtmSource:
    """SRTM 1 arcsec (30 m), copertura ±60°. I tile grandi vengono ridotti a
    blocchi in streaming (media + massimo), quindi la RAM non cresce con l'area."""

    name = "SRTM (AWS Skadi)"

    def __init__(self, spacing_m):
        self.f = max(1, int(spacing_m / SRTM_PIXEL_M))
        self.tiles = {}

    def prepare(self, bounds):
        if max(abs(bounds[1]), abs(bounds[3])) >= 60.0:
            raise RuntimeError(
                "SRTM non copre oltre ±60° di latitudine: usa --source terrarium."
            )

        wanted = srtm_tiles_for_bounds(bounds)
        names = {_tile_name(la, lo): (la, lo) for la, lo in wanted}

        print(f"  Sorgente: {self.name}, tile: {len(names)}, riduzione: {self.f}x")
        print(f"  Cache: {SRTM_CACHE_DIR}")

        done = 0
        progress_bar(0, len(names), prefix="Tile")
        with ThreadPoolExecutor(max_workers=8) as ex:
            futs = {ex.submit(_download_srtm_tile, t): t for t in names}
            for fut in as_completed(futs):
                t = futs[fut]
                try:
                    path = fut.result()
                except Exception as e:
                    raise RuntimeError(f"Errore tile {t}: {e}")
                done += 1
                progress_bar(done, len(names), prefix="Tile")
                if path is None:      # mare aperto
                    continue

                arr = _open_hgt(path)
                n = arr.shape[0]
                if self.f == 1:
                    self.tiles[names[t]] = (arr, None, 1, n)
                else:
                    mean, mx = _reduce_blocks(arr, self.f)
                    self.tiles[names[t]] = (mean, mx, self.f, n)
                    del arr

    def sample(self, lonf, lat):
        """-> (media, massimo) per ogni punto; lonf nel frame continuo."""
        lon = norm_lon(lonf)
        mean = np.zeros(lon.shape, dtype=np.float32)
        mx = np.zeros(lon.shape, dtype=np.float32)

        tlat = np.floor(lat).astype(np.int64)
        tlon = np.floor(lon).astype(np.int64)
        key = (tlat + 90) * 360 + (tlon + 180)

        for k in np.unique(key):
            t_lat = int(k // 360) - 90
            t_lon = int(k % 360) - 180
            info = self.tiles.get((t_lat, t_lon))
            if info is None:
                continue
            marr, xarr, f, n = info

            sel = np.nonzero(key == k)[0]
            ux = (lon[sel] - t_lon) * (n - 1)
            uy = ((t_lat + 1) - lat[sel]) * (n - 1)
            fx = (ux - (f - 1) / 2.0) / f
            fy = (uy - (f - 1) / 2.0) / f

            if f == 1:
                v = _bilinear(marr, fy, fx, clean=True)
                mean[sel] = v
                mx[sel] = v
            else:
                mean[sel] = _bilinear(marr, fy, fx)
                mx[sel] = _bilinear(xarr, fy, fx)

        return mean, mx


# ============================================================
# SORGENTE 2: TERRARIUM (globale, con batimetria)
# ============================================================

def _fetch_terrarium_tile(z, x, y, timeout=60, retries=3):
    """Ritorna il PNG come bytes, oppure None se il tile non esiste."""
    path = os.path.join(TERRARIUM_CACHE, str(z), str(x), f"{y}.png")
    if os.path.exists(path):
        with open(path, "rb") as f:
            return f.read()

    url = TERRARIUM_URL.format(z=z, x=x, y=y)
    last_err = None
    for attempt in range(retries):
        try:
            r = requests.get(url, timeout=timeout)
            if r.status_code in (403, 404):
                return None
            r.raise_for_status()
            atomic_write(path, r.content)
            return r.content
        except Exception as e:
            last_err = e
            time.sleep(1.5 * (attempt + 1))
    raise RuntimeError(f"Download fallito dopo {retries} tentativi: {last_err}")


def _decode_terrarium(png_bytes):
    rgb = np.asarray(Image.open(io.BytesIO(png_bytes)).convert("RGB"), dtype=np.float32)
    return rgb[..., 0] * 256.0 + rgb[..., 1] + rgb[..., 2] / 256.0 - 32768.0


def _mercator_px(lon, lat, z):
    n = 256.0 * (2 ** z)
    lat = np.clip(lat, -85.0511, 85.0511)
    s = np.sin(np.radians(lat))
    px = (lon + 180.0) / 360.0 * n
    py = (0.5 - np.log((1 + s) / (1 - s)) / (4 * np.pi)) * n
    return px, py


class TerrariumSource:
    """Mapzen/AWS Terrarium: copertura globale, terraferma + batimetria.
    Lo zoom è scelto in base alla risoluzione della griglia, quindi i download
    dipendono da --res e non dall'area del territorio."""

    name = "Terrarium (AWS)"

    def __init__(self, spacing_m):
        if Image is None:
            raise RuntimeError("Serve Pillow per Terrarium: pip install pillow")
        self.spacing_m = spacing_m
        self.z = None
        self.mosaic = None

    def _choose_zoom(self, bounds):
        min_lon, min_lat, max_lon, max_lat = bounds
        lat_c = (min_lat + max_lat) / 2
        z = math.ceil(math.log2(
            156543.03392 * max(math.cos(math.radians(lat_c)), 0.05) / self.spacing_m
        ))
        z = int(min(max(z, 0), TERRARIUM_MAX_ZOOM))

        while z > 0:
            x0, y0 = _mercator_px(min_lon, max_lat, z)
            x1, y1 = _mercator_px(max_lon, min_lat, z)
            nt = (int(x1 // 256) - int(x0 // 256) + 1) * (int(y1 // 256) - int(y0 // 256) + 1)
            if nt <= TERRARIUM_MAX_TILES:
                break
            z -= 1
        return z

    def prepare(self, bounds):
        min_lon, min_lat, max_lon, max_lat = bounds
        self.z = self._choose_zoom(bounds)
        z = self.z
        n_tiles_axis = 2 ** z

        x0, y0 = _mercator_px(min_lon, max_lat, z)
        x1, y1 = _mercator_px(max_lon, min_lat, z)
        self.tx0 = int(x0 // 256) - 1
        self.tx1 = int(x1 // 256) + 1
        self.ty0 = max(0, int(y0 // 256) - 1)
        self.ty1 = min(n_tiles_axis - 1, int(y1 // 256) + 1)

        nx = self.tx1 - self.tx0 + 1
        ny = self.ty1 - self.ty0 + 1
        total = nx * ny
        px_m = 156543.03392 * math.cos(math.radians((min_lat + max_lat) / 2)) / (2 ** z)
        print(f"  Sorgente: {self.name}, zoom {z} (~{px_m:.0f} m/px), tile: {total}")
        print(f"  Cache: {TERRARIUM_CACHE}")

        self.mosaic = np.zeros((ny * 256, nx * 256), dtype=np.float32)
        missing = 0
        done = 0
        progress_bar(0, total, prefix="Tile")

        jobs = {}
        with ThreadPoolExecutor(max_workers=8) as ex:
            for ty in range(self.ty0, self.ty1 + 1):
                for tx in range(self.tx0, self.tx1 + 1):
                    fut = ex.submit(_fetch_terrarium_tile, z, tx % n_tiles_axis, ty)
                    jobs[fut] = (tx, ty)
            for fut in as_completed(jobs):
                tx, ty = jobs[fut]
                data = fut.result()
                done += 1
                progress_bar(done, total, prefix="Tile")
                if data is None:
                    missing += 1
                    continue
                r = (ty - self.ty0) * 256
                c = (tx - self.tx0) * 256
                self.mosaic[r:r + 256, c:c + 256] = _decode_terrarium(data)

        if missing:
            print(f"  ATTENZIONE: {missing} tile mancanti (trattati come livello del mare)")

    def sample(self, lonf, lat):
        px, py = _mercator_px(lonf, lat, self.z)
        fx = px - 0.5 - self.tx0 * 256
        fy = py - 0.5 - self.ty0 * 256
        v = _bilinear(self.mosaic, fy, fx)
        return v, v


def make_source(cfg, bounds, spacing_m):
    src = cfg.source
    if src == "auto":
        n_srtm = len(srtm_tiles_for_bounds(bounds))
        lat_ok = max(abs(bounds[1]), abs(bounds[3])) < 59.9
        if lat_ok and n_srtm <= MAX_SRTM_TILES_AUTO and cfg.bathy <= 0:
            src = "srtm"
        else:
            src = "terrarium"
            why = ("batimetria richiesta" if cfg.bathy > 0
                   else "oltre ±60°" if not lat_ok
                   else f"{n_srtm} tile SRTM sarebbero troppi")
            print(f"  Sorgente auto -> Terrarium ({why})")

    if src == "srtm":
        if cfg.bathy > 0:
            print("  --bathy ignorato: SRTM non contiene batimetria (usa --source terrarium)")
            cfg.bathy = 0.0
        n_srtm = len(srtm_tiles_for_bounds(bounds))
        if n_srtm > MAX_SRTM_TILES_WARN:
            print(f"\nATTENZIONE: {n_srtm} tile SRTM (~10 MB ciascuno in cache).")
            print("Con --source terrarium si scarica molto meno.")
            if not ask_yes("Continuare comunque? [s/N]: ", cfg.yes):
                raise SystemExit("Interrotto.")
        return SrtmSource(spacing_m)

    return TerrariumSource(spacing_m)


# ============================================================
# CAMPI DEL TERRENO: media d'area, copertura anti-alias della costa
# ============================================================

@dataclass
class Fields:
    land: np.ndarray      # quota media (m) della sola terraferma (con blend picchi)
    cov: np.ndarray       # copertura terra [0,1] per vertice
    sea: np.ndarray       # profondità media (m, <= 0) dei sub-campioni marini


def smooth_field(field, weight, passes):
    """Media pesata 3x3 (convoluzione normalizzata), ripetuta `passes` volte."""
    if passes <= 0:
        return field
    k = np.array([1.0, 2.0, 1.0], dtype=np.float32)

    def blur(a):
        p = np.pad(a, 1, mode="edge")
        p = k[0] * p[:, :-2] + k[1] * p[:, 1:-1] + k[2] * p[:, 2:]
        p = k[0] * p[:-2, :] + k[1] * p[1:-1, :] + k[2] * p[2:, :]
        return p / 16.0

    f = field.astype(np.float32)
    w = weight.astype(np.float32)
    for _ in range(passes):
        num = blur(f * w)
        den = blur(w)
        f = np.where(den > 1e-6, num / np.maximum(den, 1e-6), 0.0)
        w = np.where(w > 0, w, 0)
    return f


def compute_fields(grid, terr, proj, source, ss, peaks, max_slope):
    """Campiona ss x ss sotto-punti per cella (quadrati in metri, proiettati
    all'indietro in lon/lat) e ne ricava:
      - quota media della terraferma (robusta: i sotto-campioni che superano la
        mediana della cella di più di max_slope * cella vengono riportati al limite,
        perché entro una cella il dislivello reale non può essere maggiore)
      - copertura terra per vertice (costa anti-alias)
      - profondità media del mare."""
    H, W = grid.H, grid.W
    offs = ((np.arange(ss) + 0.5) / ss) - 0.5
    xs = (grid.x[:, None] + offs[None, :] * grid.dx).ravel()
    max_rise = max_slope * min(grid.dx, grid.dy) if max_slope > 0 else None

    land = np.zeros((H, W), dtype=np.float32)
    cov = np.zeros((H, W), dtype=np.float32)
    sea = np.zeros((H, W), dtype=np.float32)

    if terr.polygon is not None:
        shapely.prepare(terr.polygon)

    rows_per = max(1, math.ceil(H / 24))
    total = math.ceil(H / rows_per)

    for k, j0 in enumerate(range(0, H, rows_per)):
        j1 = min(j0 + rows_per, H)
        hr = j1 - j0
        ys = (grid.y[j0:j1, None] + offs[None, :] * grid.dy).ravel()
        X, Y = np.meshgrid(xs, ys)

        lon, lat = proj.inverse(X.ravel(), Y.ravel())
        lonf = frame_lon(lon, terr.wrapped)

        vm, vx = source.sample(lonf, lat)

        if terr.polygon is not None:
            inland = shapely.contains_xy(terr.polygon, lonf, lat)
        else:
            inland = vm > 0.0

        shp = (hr, ss, W, ss)
        vm = vm.reshape(shp)
        vx = vx.reshape(shp)
        inl = inland.reshape(shp)

        def cell_axis(a):                      # (hr, ss, W, ss) -> (hr, W, ss*ss)
            return a.transpose(0, 2, 1, 3).reshape(hr, W, ss * ss)

        inl_c = cell_axis(inl)
        cnt = inl_c.sum(axis=2).astype(np.float32)

        pos = np.where(inl_c, np.maximum(cell_axis(vm), 0.0), np.nan).astype(np.float32)
        mxs = np.where(inl_c, np.maximum(cell_axis(vx), 0.0), np.nan).astype(np.float32)

        with warnings.catch_warnings():
            warnings.simplefilter("ignore", category=RuntimeWarning)
            if max_rise is not None and ss > 1:
                med = np.nanmedian(pos, axis=2)[..., None]
                pos = np.clip(pos, med - max_rise, med + max_rise)
                mxs = np.minimum(mxs, med + max_rise)
            mean = np.nan_to_num(np.nanmean(pos, axis=2))
            mxv = np.nan_to_num(np.nanmax(np.where(np.isnan(mxs), -1.0, mxs), axis=2), nan=0.0)
            mxv = np.maximum(mxv, mean)

        n_sea = np.maximum(ss * ss - cnt, 1.0)
        neg = np.minimum(vm, 0.0)
        sea_mean = (neg * (~inl)).sum(axis=(1, 3)) / n_sea

        land[j0:j1] = mean + peaks * (mxv - mean)
        cov[j0:j1] = cnt / float(ss * ss)
        sea[j0:j1] = sea_mean

        progress_bar(k + 1, total, prefix="Campi")

    if not (cov > 0).any():
        raise ValueError(
            "Il territorio non copre nessuna cella: prova ad aumentare --res."
        )

    return Fields(land=land, cov=cov, sea=sea)


def despike_field(land, cov, cell_m, max_slope, passes=2):
    """Elimina i picchi irreali (artefatti radar, pixel anomali dei DEM).
    Un vertice non può superare la mediana dei suoi vicini di terra di più di
    max_slope * cella (pendenza rise/run: 1.5 ~ 56°). La mediana usa solo celle
    di terra, così le falesie costiere non vengono tagliate. Le vette vere, che
    si alzano gradualmente, non vengono toccate."""
    if max_slope <= 0:
        return land, 0

    H, W = land.shape
    valid = cov > 0.5
    f = land.astype(np.float32).copy()
    tol = np.float32(max_slope * cell_m)
    clipped_total = 0

    for _ in range(passes):
        a = np.where(valid, f, np.nan).astype(np.float32)
        pad = np.pad(a, 1, mode="constant", constant_values=np.nan)
        stack = np.stack([pad[dj:dj + H, di:di + W]
                          for dj in range(3) for di in range(3)])
        with warnings.catch_warnings():
            warnings.simplefilter("ignore", category=RuntimeWarning)
            med = np.nanmedian(stack, axis=0)
        cap = med + tol
        spike = valid & (f > cap)
        n = int(spike.sum())
        if n == 0:
            break
        f = np.where(spike, cap, f)
        clipped_total += n

    return f, clipped_total


# ============================================================
# ALTEZZE E MESH
# ============================================================

def build_heights(fields, scale, vexag, land_min_mm, bathy, bathy_max_mm):
    """Quota Z (mm) per vertice. La costa è una rampa di una cella pesata dalla
    copertura, quindi il contorno segue il poligono con precisione sub-cella."""
    z_scale = scale * vexag
    land_z = np.maximum(fields.land * z_scale, land_min_mm)

    if bathy > 0:
        raw = -fields.sea * z_scale * bathy                    # mm positivi
        sea_z = -bathy_max_mm * np.tanh(raw / bathy_max_mm)   # compressione dolce
    else:
        sea_z = np.zeros_like(land_z)

    c = fields.cov
    return (c * land_z + (1.0 - c) * sea_z).astype(np.float64)


def build_piece(X, Y, Z, z_bottom):
    """Solido chiuso: superficie a griglia + pareti perimetrali + fondo a ventaglio
    (niente griglia sul fondo: meno della metà delle facce rispetto alla v1)."""
    h, w = Z.shape
    nTop = h * w

    XX, YY = np.meshgrid(X, Y)
    top_v = np.column_stack([XX.ravel(), YY.ravel(), Z.ravel()])

    idx = np.arange(nTop).reshape(h, w)

    # perimetro in senso antiorario visto dall'alto
    loop = np.concatenate([
        idx[0, :],                 # sud: ovest -> est
        idx[1:, -1],               # est: sud -> nord
        idx[-1, -2::-1],           # nord: est -> ovest
        idx[-2:0:-1, 0],           # ovest: nord -> sud
    ])
    P = len(loop)

    bot_v = top_v[loop].copy()
    bot_v[:, 2] = z_bottom
    centre = np.array([[X.mean(), Y.mean(), z_bottom]])
    vertices = np.vstack([top_v, bot_v, centre])
    c_idx = nTop + P

    A = idx[:-1, :-1].ravel()
    B = idx[:-1, 1:].ravel()
    C = idx[1:, 1:].ravel()
    D = idx[1:, :-1].ravel()
    faces = [np.column_stack([A, B, C]), np.column_stack([A, C, D])]

    k = np.arange(P)
    k1 = (k + 1) % P
    t, t1 = loop[k], loop[k1]
    b, b1 = nTop + k, nTop + k1
    faces.append(np.column_stack([t, b, b1]))
    faces.append(np.column_stack([t, b1, t1]))
    faces.append(np.column_stack([np.full(P, c_idx), b1, b]))

    return trimesh.Trimesh(
        vertices=vertices,
        faces=np.vstack(faces).astype(np.int64),
        process=False,
    )


def simplify_mesh(mesh, keep):
    """Decimazione quadrica (fast-simplification). Prova aggressività decrescenti
    finché la mesh resta chiusa; altrimenti restituisce l'originale."""
    if fast_simplification is None or not (0 < keep < 1.0):
        return mesh
    for agg in (5, 3, 1):
        try:
            pts, fcs = fast_simplification.simplify(
                np.asarray(mesh.vertices), np.asarray(mesh.faces),
                target_reduction=1.0 - keep, agg=agg,
            )
        except Exception:
            continue
        cand = trimesh.Trimesh(pts, fcs, process=False)
        if cand.is_watertight and cand.is_winding_consistent:
            return cand
    print("  Decimazione scartata: la mesh non sarebbe più chiusa.")
    return mesh


def plan_pieces(W, H, size_x_mm, size_y_mm, plate, split):
    if split:
        nx, ny = split
    elif plate:
        nx = max(1, math.ceil(size_x_mm / plate[0] - 1e-9))
        ny = max(1, math.ceil(size_y_mm / plate[1] - 1e-9))
    else:
        nx = ny = 1

    bi = np.unique(np.round(np.linspace(0, W - 1, nx + 1)).astype(int))
    bj = np.unique(np.round(np.linspace(0, H - 1, ny + 1)).astype(int))
    nc, nr = len(bi) - 1, len(bj) - 1

    pieces = []
    for r in range(nr):                      # r = 0 -> riga più a nord
        jr = nr - 1 - r
        for c in range(nc):
            pieces.append(dict(
                i0=int(bi[c]), i1=int(bi[c + 1]),
                j0=int(bj[jr]), j1=int(bj[jr + 1]),
                row=r, col=c, nrows=nr, ncols=nc,
            ))
    return pieces


# ============================================================
# BOOLEANI: spine di incastro e incisioni (richiedono manifold3d)
# ============================================================

def _boolean(kind, meshes):
    fn = trimesh.boolean.union if kind == "union" else trimesh.boolean.difference
    return fn(meshes, engine="manifold")


def _cylinder(center, axis, length, radius):
    cyl = trimesh.creation.cylinder(radius=radius, height=length, sections=32)
    if axis == "x":
        cyl.apply_transform(trimesh.transformations.rotation_matrix(np.pi / 2, [0, 1, 0]))
    elif axis == "y":
        cyl.apply_transform(trimesh.transformations.rotation_matrix(np.pi / 2, [1, 0, 0]))
    cyl.apply_translation(center)
    return cyl


def add_alignment_pins(mesh, piece, geom, z_bottom, base_mm):
    """Spine sulle facce ovest e sud, fori sulle facce est e nord: i pezzi
    adiacenti combaciano. Posizionate nello spessore della base."""
    x0, x1, y0, y1 = geom["x0"], geom["x1"], geom["y0"], geom["y1"]
    zc = z_bottom + base_mm / 2.0
    L, R = PIN_LENGTH, PIN_RADIUS
    pins, holes = [], []

    fr = (0.25, 0.75)
    r, c, nr, nc = piece["row"], piece["col"], piece["nrows"], piece["ncols"]

    if c > 0:                                   # spine sul lato ovest
        for f in fr:
            y = y0 + f * (y1 - y0)
            pins.append(_cylinder([x0 + (0.6 - L) / 2, y, zc], "x", L + 0.6, R))
    if c < nc - 1:                              # fori sul lato est
        for f in fr:
            y = y0 + f * (y1 - y0)
            depth = L + PIN_CLEARANCE * 2
            holes.append(_cylinder([x1 + (0.5 - depth) / 2, y, zc], "x",
                                   depth + 0.5, R + PIN_CLEARANCE))
    if r < nr - 1:                              # spine sul lato sud
        for f in fr:
            x = x0 + f * (x1 - x0)
            pins.append(_cylinder([x, y0 + (0.6 - L) / 2, zc], "y", L + 0.6, R))
    if r > 0:                                   # fori sul lato nord
        for f in fr:
            x = x0 + f * (x1 - x0)
            depth = L + PIN_CLEARANCE * 2
            holes.append(_cylinder([x, y1 + (0.5 - depth) / 2, zc], "y",
                                   depth + 0.5, R + PIN_CLEARANCE))
    return pins, holes


# --- font bitmap 5x7 minimale (maiuscole, cifre, qualche simbolo) -------------

_FONT_SRC = {
    "A": ".###. #...# #...# ##### #...# #...# #...#",
    "B": "####. #...# #...# ####. #...# #...# ####.",
    "C": ".###. #...# #.... #.... #.... #...# .###.",
    "D": "####. #...# #...# #...# #...# #...# ####.",
    "E": "##### #.... #.... ####. #.... #.... #####",
    "F": "##### #.... #.... ####. #.... #.... #....",
    "G": ".###. #...# #.... #.### #...# #...# .###.",
    "H": "#...# #...# #...# ##### #...# #...# #...#",
    "I": "##### ..#.. ..#.. ..#.. ..#.. ..#.. #####",
    "J": "..### ...#. ...#. ...#. ...#. #..#. .##..",
    "K": "#...# #..#. #.#.. ##... #.#.. #..#. #...#",
    "L": "#.... #.... #.... #.... #.... #.... #####",
    "M": "#...# ##.## #.#.# #.#.# #...# #...# #...#",
    "N": "#...# ##..# #.#.# #..## #...# #...# #...#",
    "O": ".###. #...# #...# #...# #...# #...# .###.",
    "P": "####. #...# #...# ####. #.... #.... #....",
    "Q": ".###. #...# #...# #...# #.#.# #..#. .##.#",
    "R": "####. #...# #...# ####. #.#.. #..#. #...#",
    "S": ".#### #.... #.... .###. ....# ....# ####.",
    "T": "##### ..#.. ..#.. ..#.. ..#.. ..#.. ..#..",
    "U": "#...# #...# #...# #...# #...# #...# .###.",
    "V": "#...# #...# #...# #...# #...# .#.#. ..#..",
    "W": "#...# #...# #...# #.#.# #.#.# ##.## #...#",
    "X": "#...# #...# .#.#. ..#.. .#.#. #...# #...#",
    "Y": "#...# #...# .#.#. ..#.. ..#.. ..#.. ..#..",
    "Z": "##### ....# ...#. ..#.. .#... #.... #####",
    "0": ".###. #...# #..## #.#.# ##..# #...# .###.",
    "1": "..#.. .##.. ..#.. ..#.. ..#.. ..#.. .###.",
    "2": ".###. #...# ....# ...#. ..#.. .#... #####",
    "3": "##### ...#. ..#.. ...#. ....# #...# .###.",
    "4": "...#. ..##. .#.#. #..#. ##### ...#. ...#.",
    "5": "##### #.... ####. ....# ....# #...# .###.",
    "6": "..##. .#... #.... ####. #...# #...# .###.",
    "7": "##### ....# ...#. ..#.. .#... .#... .#...",
    "8": ".###. #...# #...# .###. #...# #...# .###.",
    "9": ".###. #...# #...# .#### ....# ...#. .##..",
    ".": "..... ..... ..... ..... ..... .##.. .##..",
    "-": "..... ..... ..... ##### ..... ..... .....",
    "/": "....# ...#. ...#. ..#.. .#... .#... #....",
    " ": "..... ..... ..... ..... ..... ..... .....",
}
FONT = {ch: v.split() for ch, v in _FONT_SRC.items()}


def _clean_text(text):
    t = unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode().upper()
    return "".join(ch if ch in FONT else " " for ch in t)


def _text_pixels(text):
    """Rettangoli (u, v, w, h) in unità-pixel; u verso destra, v verso l'alto;
    le sequenze orizzontali di pixel sono fuse in un solo rettangolo."""
    rects = []
    for ci, ch in enumerate(text):
        rows = FONT.get(ch, FONT[" "])
        for ri, row in enumerate(rows):
            v = 6 - ri
            start = None
            for xi in range(6):
                on = xi < 5 and row[xi] == "#"
                if on and start is None:
                    start = xi
                if not on and start is not None:
                    rects.append((ci * 6 + start, v, xi - start, 1))
                    start = None
    return rects


def _nice_length_m(max_m):
    best = 1.0
    for e in range(-3, 8):
        for m in (1, 2, 5):
            v = m * 10.0 ** e
            if v <= max_m:
                best = v
    return best


def _fmt_km(m):
    km = m / 1000.0
    return f"{km:g} KM" if km >= 1 else f"{m:g} M"


def engrave_bottom(mesh, lines, scale_bar, cx, cy, z_bottom, piece_w, piece_h):
    """Incide sul fondo (leggibile capovolgendo il modello sull'asse Nord-Sud,
    cioè girandolo 'come una pagina'): testo + barra della scala."""
    boxes = []
    z0 = z_bottom - 0.2
    depth = ENGRAVE_DEPTH + 0.2
    eps = 0.02

    def add_rect(x_center, y_bot, w, h):
        boxes.append(trimesh.creation.box(
            extents=[w + eps, h + eps, depth],
            transform=trimesh.transformations.translation_matrix(
                [x_center, y_bot + h / 2.0, z0 + depth / 2.0]),
        ))

    texts = [_clean_text(t) for t in lines]
    texts = [t for t in texts if t.strip()]
    if not texts:
        return mesh

    # dimensione del pixel: il testo più largo deve stare nell'85% della larghezza
    p = min(1.0, min(0.85 * piece_w / (6 * len(t)) for t in texts))
    # ... e tutte le righe nell'85% dell'altezza
    p = min(p, 0.85 * piece_h / (8 * len(texts)))
    if p < 0.35:
        return mesh                      # troppo piccolo per essere inciso bene

    line_h = 8 * p
    bar_block = 13.5 * p                 # spazio: gap + barra + tacche + etichetta
    show_bar = bool(scale_bar) and scale_bar[0] <= 0.8 * piece_w \
        and len(texts) * line_h + bar_block <= 0.85 * piece_h

    total_h = len(texts) * line_h + (bar_block if show_bar else 0.0)
    y_cursor = cy + total_h / 2.0

    for text in texts:
        y_cursor -= line_h
        tw = 6 * len(text) * p - p
        for (u, v, w, h) in _text_pixels(text):
            # da sotto l'asse x è specchiato: il primo pixel sta a destra
            x_center = cx + tw / 2.0 - (u + w / 2.0) * p
            add_rect(x_center, y_cursor + v * p, w * p, h * p)

    if show_bar:
        L_mm, label = scale_bar
        y_cursor -= 2 * p
        bar_h = 1.2 * p
        add_rect(cx, y_cursor - bar_h, L_mm, bar_h)                          # barra
        add_rect(cx + L_mm / 2 - p / 2, y_cursor - 3.5 * p, p, 3.5 * p)      # tacche
        add_rect(cx - L_mm / 2 + p / 2, y_cursor - 3.5 * p, p, 3.5 * p)
        lbl = _clean_text(label)
        tw = 6 * len(lbl) * p - p
        y_lbl = y_cursor - 3.5 * p - 7 * p - p
        for (u, v, w, h) in _text_pixels(lbl):
            x_center = cx + tw / 2.0 - (u + w / 2.0) * p
            add_rect(x_center, y_lbl + v * p, w * p, h * p)

    if not boxes:
        return mesh

    cutter = _boolean("union", boxes) if len(boxes) > 1 else boxes[0]
    return _boolean("difference", [mesh, cutter])


# ============================================================
# COLORI (ipsometrici + ombreggiatura + batimetria)
# ============================================================

PAL_M = np.array([0, 150, 400, 800, 1500, 2500, 3500], dtype=float)
PAL_RGB = np.array([
    [0.16, 0.45, 0.20],
    [0.42, 0.62, 0.25],
    [0.72, 0.72, 0.35],
    [0.66, 0.52, 0.30],
    [0.52, 0.40, 0.32],
    [0.62, 0.60, 0.58],
    [0.97, 0.97, 0.98],
])
LIGHT = np.array([-0.5, 0.5, 0.7071])       # da nord-ovest, 45° di elevazione
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def hypsometric(elev_m):
    return np.column_stack(
        [np.interp(elev_m, PAL_M, PAL_RGB[:, c]) for c in range(3)]
    )


def apply_colors(mesh, z_bottom, base_mm, z_scale, land_min_mm):
    z = mesh.vertices[:, 2]
    colors = np.zeros((len(z), 4), dtype=np.float32)
    colors[:, 3] = 1.0

    base = z < z_bottom + 0.999 * base_mm
    top = ~base
    sea = top & (z <= 0.5 * land_min_mm)
    land = top & ~sea

    colors[base, :3] = [0.30, 0.30, 0.30]

    try:
        vn = np.asarray(mesh.vertex_normals)
        ndl = np.clip(vn @ LIGHT, 0.0, 1.0)
        shade = (0.45 + 0.55 * ndl) / (0.45 + 0.55 * LIGHT[2])
    except Exception:
        shade = np.ones(len(z))
    shade = np.clip(shade, 0.55, 1.3)

    if sea.any():
        zs = z[sea]
        zmin = float(zs.min())
        if zmin < -0.05:
            t = np.clip(zs / zmin, 0, 1).reshape(-1, 1)      # 0 = costa, 1 = fondo
            shallow = np.array([0.32, 0.65, 0.92])
            deep = np.array([0.04, 0.18, 0.48])
            rgb = shallow * (1 - t) + deep * t
        else:
            rgb = np.tile([0.15, 0.45, 0.85], (int(sea.sum()), 1))
        colors[sea, :3] = np.clip(rgb * (0.8 + 0.2 * shade[sea, None]), 0, 1)

    if land.any():
        elev_m = z[land] / max(z_scale, 1e-12)
        rgb = hypsometric(elev_m) * shade[land, None]
        colors[land, :3] = np.clip(rgb, 0, 1)

    mesh.visual.vertex_colors = (colors * 255).astype(np.uint8)


# ============================================================
# EXPORT / VIEWER
# ============================================================

def export_meshes(pieces_meshes, name, cfg):
    print("\n==========================================")
    print("EXPORT MODELLO")
    print("==========================================")

    os.makedirs(cfg.out_dir, exist_ok=True)
    formats = [f.strip().lower() for f in cfg.formats.split(",") if f.strip()]
    notes = {"stl": "stampa 3D", "ply": "colori vertici", "obj": "editing 3D",
             "3mf": "slicer moderni", "glb": "web/AR"}

    written = []
    for tag, mesh in pieces_meshes:
        base = os.path.join(cfg.out_dir, f"{slugify(name)}_terrain{tag}")
        for ext in formats:
            path = f"{base}.{ext}"
            with Spinner(f"Scrittura {ext.upper()}"):
                mesh.export(path)
            size = os.path.getsize(path) / (1024 * 1024)
            print(f"  {ext.upper()}  → {path}  ({size:.1f} MB)  [{notes.get(ext, '')}]")
            written.append(os.path.abspath(path))

    print("\n  Dove trovarli:")
    for p in written:
        print(f"    {p}")


def open_viewer(pieces_meshes):
    print("Apertura viewer 3D... (chiudi la finestra per terminare)")
    try:
        trimesh.Scene([m for _, m in pieces_meshes]).show()
    except Exception as e:
        print(f"  Viewer non disponibile ({e}).")
        print('  Serve:  pip install "pyglet<2"   (oppure apri i file in MeshLab/Blender)')


# ============================================================
# PIPELINE
# ============================================================

def parse_wxh(text, what):
    m = re.fullmatch(r"\s*(\d+(?:\.\d+)?)\s*[xX×]\s*(\d+(?:\.\d+)?)\s*", str(text))
    if not m:
        raise ValueError(f"{what}: formato atteso AxB (es. 220x220)")
    return float(m.group(1)), float(m.group(2))


def run(cfg, terr):
    t_start = time.time()

    # ---- proiezione e griglia ---------------------------------------------------
    proj = make_projection(cfg.proj, terr)
    margin = 0.0 if terr.polygon is None else cfg.margin
    extent = territory_extent(terr, proj, margin)
    grid = make_grid(extent, cfg.res)
    W, H = grid.W, grid.H

    ext_w, ext_h = extent[2] - extent[0], extent[3] - extent[1]
    scale = cfg.size / max(ext_w, ext_h)                 # mm per metro
    ss = pick_supersampling(W * H, cfg.ss)
    spacing_m = min(grid.dx, grid.dy) / ss

    print(f"\n  Proiezione: {proj.kind}   Griglia: {H} x {W}   "
          f"Cella: {grid.dx:.0f} m   Sotto-campioni/cella: {ss}x{ss}")
    print(f"  Modello: {ext_w * scale:.1f} x {ext_h * scale:.1f} mm  "
          f"(1 mm = {1 / scale / 1000:.2f} km)")

    # ---- quote -----------------------------------------------------------------
    print("\n[1/4] Recupero altitudini...")
    bounds = grid_lonlat_bounds(grid, proj, terr.wrapped)
    source = make_source(cfg, bounds, spacing_m)
    source.prepare(bounds)

    print("\n[2/4] Campionamento (media d'area, copertura della costa)...")
    fields = compute_fields(grid, terr, proj, source, ss, cfg.peaks, cfg.max_slope)

    fields.land, n_spikes = despike_field(
        fields.land, fields.cov, min(grid.dx, grid.dy), cfg.max_slope)
    if n_spikes:
        print(f"  Picchi anomali corretti: {n_spikes} vertici")

    if cfg.smooth > 0:
        fields.land = smooth_field(fields.land, fields.cov, cfg.smooth)

    max_land = float(fields.land[fields.cov > 0.5].max()) if (fields.cov > 0.5).any() \
        else float(fields.land.max())
    print(f"  Quota massima: {max_land:.0f} m   "
          f"Terra: {int((fields.cov > 0.5).sum())} vertici")

    # ---- esagerazione verticale --------------------------------------------------
    if str(cfg.vexag).lower() == "auto":
        relief = cfg.relief_mm if cfg.relief_mm else 0.10 * cfg.size
        vexag = relief / max(max_land * scale, 1e-9)
        vexag = float(np.clip(vexag, 1.0, cfg.max_auto_vexag))
        print(f"  Esagerazione verticale automatica: x{vexag:.1f} "
              f"(rilievo ~{max_land * scale * vexag:.1f} mm)")
    else:
        vexag = float(cfg.vexag)
        print(f"  Esagerazione verticale: x{vexag:.1f}")
    z_scale = scale * vexag

    # ---- altezze e pezzi ---------------------------------------------------------
    print("\n[3/4] Generazione mesh 3D...")
    Z = build_heights(fields, scale, vexag, cfg.land_min, cfg.bathy, cfg.bathy_max_mm)
    z_bottom = min(float(Z.min()), 0.0) - cfg.base

    Xmm = (grid.x - grid.x[0]) * scale
    Ymm = (grid.y - grid.y[0]) * scale

    plate = parse_wxh(cfg.plate, "--plate") if cfg.plate else None
    split = tuple(int(v) for v in parse_wxh(cfg.split, "--split")) if cfg.split else None
    pieces = plan_pieces(W, H, Xmm[-1], Ymm[-1], plate, split)
    multi = len(pieces) > 1

    if multi:
        print(f"  Suddivisione in {len(pieces)} pezzi "
              f"({pieces[0]['ncols']} x {pieces[0]['nrows']})")

    do_booleans = HAVE_MANIFOLD
    if (multi and cfg.pins) or cfg.label:
        if not HAVE_MANIFOLD:
            print("  manifold3d non installato: niente spine/incisioni "
                  "(pip install manifold3d).")

    if cfg.simplify and cfg.simplify < 1.0 and fast_simplification is None:
        print("  fast-simplification non installato: nessuna decimazione "
              "(pip install fast-simplification).")

    results = []
    for n, pc in enumerate(pieces, 1):
        js = slice(pc["j0"], pc["j1"] + 1)
        is_ = slice(pc["i0"], pc["i1"] + 1)
        Xp, Yp, Zp = Xmm[is_], Ymm[js], Z[js, is_]

        mesh = build_piece(Xp, Yp, Zp, z_bottom)

        # decimazione quadrica: il mare piatto collassa per primo
        if cfg.simplify and 0 < cfg.simplify < 1.0 and fast_simplification is not None:
            before = len(mesh.faces)
            mesh = simplify_mesh(mesh, cfg.simplify)
            if len(mesh.faces) != before:
                print(f"  Pezzo {n}: {before} -> {len(mesh.faces)} facce")

        geom = dict(x0=Xp[0], x1=Xp[-1], y0=Yp[0], y1=Yp[-1])

        if do_booleans:
            try:
                if multi and cfg.pins and cfg.base >= 3.5:
                    pins, holes = add_alignment_pins(mesh, pc, geom, z_bottom, cfg.base)
                    if pins:
                        mesh = _boolean("union", [mesh] + pins)
                    if holes:
                        mesh = _boolean("difference", [mesh] + holes)

                if cfg.label:
                    lines = [terr.name]
                    tag_txt = f"V X{vexag:.0f}"
                    if multi:
                        tag_txt = f"R{pc['row'] + 1}C{pc['col'] + 1} " + tag_txt
                    lines.append(tag_txt)

                    pw = geom["x1"] - geom["x0"]
                    ph = geom["y1"] - geom["y0"]
                    bar = None
                    if scale > 0:
                        Lm = _nice_length_m(0.5 * pw / scale)
                        bar = (Lm * scale, _fmt_km(Lm))
                    mesh = engrave_bottom(
                        mesh, lines, bar,
                        (geom["x0"] + geom["x1"]) / 2, (geom["y0"] + geom["y1"]) / 2,
                        z_bottom, pw, ph,
                    )
            except Exception as e:
                print(f"  Booleani falliti sul pezzo {n} ({e}): pezzo lasciato semplice.")

        apply_colors(mesh, z_bottom, cfg.base, z_scale, cfg.land_min)

        tag = f"_r{pc['row'] + 1}c{pc['col'] + 1}" if multi else ""
        results.append((tag, mesh))

        progress_bar(n, len(pieces), prefix="Pezzi")

    # ---- report ------------------------------------------------------------------
    print("\n[4/4] Verifica...")
    for tag, m in results:
        e = m.extents
        print(f"  {tag.strip('_') or 'modello':<8} {e[0]:6.1f} x {e[1]:6.1f} x {e[2]:5.1f} mm   "
              f"vertici {len(m.vertices):>8}  facce {len(m.faces):>8}  "
              f"watertight: {m.is_watertight}")
        if plate and (e[0] > plate[0] + 0.5 or e[1] > plate[1] + 0.5):
            print(f"    ATTENZIONE: supera il piatto {plate[0]:g}x{plate[1]:g} mm")

    export_meshes(results, terr.name, cfg)
    print(f"\n  Tempo totale: {time.time() - t_start:.1f}s")

    return results


# ============================================================
# CLI / CONFIG
# ============================================================

def build_parser():
    p = argparse.ArgumentParser(
        description="Genera modelli 3D stampabili di territori reali.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    p.add_argument("--config", help="file JSON con le stesse opzioni (senza --)")

    g = p.add_argument_group("territorio")
    g.add_argument("--place", help="nome da cercare su OpenStreetMap")
    g.add_argument("--mode", choices=[m[0] for m in MODES.values()],
                   help="tipo di ricerca (ordina i risultati)")
    g.add_argument("--pick", type=int, help="indice del risultato OSM (evita il prompt)")
    g.add_argument("--bbox", nargs=4, type=float,
                   metavar=("MINLON", "MINLAT", "MAXLON", "MAXLAT"),
                   help="bounding box manuale (MAXLON < MINLON = antimeridiano)")
    g.add_argument("--name", help="nome per file e incisione")

    g = p.add_argument_group("modello")
    g.add_argument("--res", type=int, help="vertici sul lato lungo (default 200)")
    g.add_argument("--size", type=float, help="lato lungo in mm (default 150)")
    g.add_argument("--vexag", help="'auto' oppure un numero (default auto)")
    g.add_argument("--relief-mm", type=float, help="rilievo massimo voluto con vexag auto")
    g.add_argument("--max-auto-vexag", type=float, help="tetto per vexag auto (default 25)")
    g.add_argument("--base", type=float, help="spessore base in mm (default 5)")
    g.add_argument("--land-min", type=float, help="rilievo minimo della terra in mm (0.4)")
    g.add_argument("--peaks", type=float,
                   help="0 = media d'area pura (default), 1 = massimo di cella: rende le vette "
                        "più aguzze ma anche più rumorose (valori tipici 0.1-0.3)")
    g.add_argument("--max-slope", type=float,
                   help="pendenza massima (rise/run) per eliminare i picchi anomali; "
                        "0 = disattivato (default 0.8, cioè ~39°)")
    g.add_argument("--smooth", type=int, help="passate di lisciatura del rilievo (0)")
    g.add_argument("--ss", type=int, help="sotto-campioni per lato di cella (0 = auto)")
    g.add_argument("--margin", type=float, help="cornice di mare attorno al territorio (0.03)")

    g = p.add_argument_group("dati")
    g.add_argument("--source", choices=["auto", "srtm", "terrarium"])
    g.add_argument("--bathy", type=float,
                   help="fondale marino: fattore rispetto alla scala verticale (es. 0.2)")
    g.add_argument("--bathy-max-mm", type=float, help="profondità massima in mm (10)")
    g.add_argument("--proj", choices=["auto", "laea", "equirect"])

    g = p.add_argument_group("stampa")
    g.add_argument("--simplify", type=float,
                   help="decimazione: frazione di facce da TENERE (es. 0.4)")
    g.add_argument("--plate", help="piatto di stampa LxH in mm (es. 220x220): divide in pezzi")
    g.add_argument("--split", help="divisione manuale NxM (es. 2x3)")
    g.add_argument("--pins", action=argparse.BooleanOptionalAction,
                   help="spine/fori di allineamento tra i pezzi")
    g.add_argument("--label", action=argparse.BooleanOptionalAction,
                   help="incisione con nome, scala e barra km sul fondo")

    g = p.add_argument_group("output")
    g.add_argument("--out-dir", help="cartella di output")
    g.add_argument("--formats", help="es. stl,ply,obj,3mf (default stl,ply,obj)")
    g.add_argument("--view", action="store_true", default=None, help="apri il viewer")
    g.add_argument("--yes", "-y", action="store_true", default=None,
                   help="non chiedere conferme")
    return p


def parse_args(argv=None):
    parser = build_parser()
    pre, _ = parser.parse_known_args(argv)

    defaults = dict(DEFAULTS)
    if pre.config:
        with open(pre.config, encoding="utf-8") as f:
            cfg_file = json.load(f)
        for k, v in cfg_file.items():
            k = k.replace("-", "_")
            if k not in defaults:
                raise SystemExit(f"Opzione sconosciuta nel config: {k}")
            defaults[k] = v

    parser.set_defaults(**defaults)
    args = parser.parse_args(argv)

    if args.view is None:
        args.view = defaults["view"]
    if args.yes is None:
        args.yes = defaults["yes"]

    args.formats = str(args.formats)
    return args


def main(argv=None):
    cfg = parse_args(argv)

    print("=" * 50)
    print("        3D TERRAIN GENERATOR v2")
    print("=" * 50)

    terr = get_territory(cfg)
    if cfg.name:
        terr.name = cfg.name

    b = terr.bbox
    print(f"\nBounding box: lon {b[0]:.4f}..{b[2]:.4f}   lat {b[1]:.4f}..{b[3]:.4f}")

    results = run(cfg, terr)

    interactive = not (cfg.place or cfg.bbox)
    if cfg.view or (interactive and ask_yes("\nAprire il viewer 3D? [s/N]: ")):
        open_viewer(results)


if __name__ == "__main__":
    try:
        main()
    except (ValueError, RuntimeError) as e:
        sys.exit(f"\nErrore: {e}")
    except KeyboardInterrupt:
        sys.exit("\nInterrotto.")