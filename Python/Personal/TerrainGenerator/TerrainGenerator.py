import io
import os
import sys
import time
import gzip
import math
import threading
import contextlib

import numpy as np
import pandas as pd
import trimesh
import osmnx as ox
import shapely
import requests

from concurrent.futures import ThreadPoolExecutor, as_completed
from shapely.geometry import box


# ============================================================
# CONFIGURAZIONE
# ============================================================

GRID_RES = 200
WIDTH_MM = 150
V_EXAGG = 8
BASE_MM = 5
OUT_STL = "italia_terrain.stl"
OUT_OBJ = "italia_terrain.obj"
OUT_PLY = "italia_terrain.ply"

# Mirror AWS ufficiale (Terrain Tiles / Skadi)
SRTM_AWS_BASE = "https://s3.amazonaws.com/elevation-tiles-prod/skadi"
SRTM_CACHE_DIR = os.path.join(os.path.expanduser("~"), ".cache", "srtm_aws")


# ============================================================
# MODALITÀ DI RICERCA
# ============================================================

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
    "stato":     ["country", "nation", "state"],
    "regione":   ["region", "state", "administrative"],
    "provincia": ["province", "county", "administrative"],
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
            elapsed = time.time() - t0
            sys.stdout.write(
                f"\r  {self.message}... {chars[i % 4]}  {elapsed:6.1f}s"
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


# ============================================================
# SCELTA MODALITÀ E ORDINAMENTO RISULTATI
# ============================================================

def choose_mode():
    print("\nModalità di ricerca:")
    for k, (_, label) in MODES.items():
        print(f"  [{k}] {label}")

    choice = input("Seleziona (1-6) [default: 1]: ").strip() or "1"
    if choice not in MODES:
        raise ValueError("Modalità non valida.")
    return MODES[choice][0]


def sort_by_mode(gdf, mode):
    keywords = MODE_KEYWORDS.get(mode)
    if not keywords or len(gdf) <= 1:
        return gdf

    def score(row):
        haystack = " ".join(
            str(row.get(c, "")).lower()
            for c in ("class", "type", "display_name")
            if c in row.index
        )
        for i, kw in enumerate(keywords):
            if kw in haystack:
                return i
        return len(keywords)

    order = gdf.apply(score, axis=1).values.argsort(kind="stable")
    return gdf.iloc[order]


# ============================================================
# 1. RICERCA DEL TERRITORIO
# ============================================================

def search_place(mode):

    print("=" * 50)
    print("        3D TERRAIN GENERATOR")
    print("=" * 50)

    if mode == "bbox":
        print("\nInserisci il bounding box in coordinate WGS84:")
        try:
            min_lon = float(input("  Min Lon: "))
            min_lat = float(input("  Min Lat: "))
            max_lon = float(input("  Max Lon: "))
            max_lat = float(input("  Max Lat: "))
        except ValueError:
            raise ValueError("Coordinate non valide.")

        if not (min_lon < max_lon and min_lat < max_lat):
            raise ValueError("Bounding box non valido.")

        polygon = box(min_lon, min_lat, max_lon, max_lat)
        bbox = (min_lon, min_lat, max_lon, max_lat)

        print("\nBounding Box manuale:")
        print(f"  Min Lon: {min_lon}")
        print(f"  Min Lat: {min_lat}")
        print(f"  Max Lon: {max_lon}")
        print(f"  Max Lat: {max_lat}")

        return polygon, bbox

    place_name = input(
        "\nInserisci il nome del territorio "
        "(es. Ischia, Italia):\n> "
    ).strip()

    if not place_name:
        raise ValueError("Non hai inserito nessun nome.")

    print("\nRicerca su OpenStreetMap...")
    print(f"Query: {place_name}")

    try:
        gdf = ox.geocode_to_gdf(place_name)
    except Exception as e:
        raise RuntimeError(
            f"Errore durante la ricerca OpenStreetMap:\n{e}"
        )

    if gdf.empty:
        raise ValueError(f"Nessun risultato trovato per '{place_name}'.")

    if gdf.crs is not None and gdf.crs.to_epsg() != 4326:
        gdf = gdf.to_crs(4326)

    gdf = sort_by_mode(gdf, mode)

    print(f"\nRisultati OpenStreetMap ({len(gdf)} trovati):")
    print("-" * 50)

    for pos, (_, row) in enumerate(gdf.iterrows()):
        name = row.get("display_name", "Nome sconosciuto")
        print(f"\n  [{pos}] {name}")
        for col, label in (
            ("osm_type", "osm"),
            ("class", "class"),
            ("type", "type"),
            ("importance", "importance"),
        ):
            if col in row.index and pd.notna(row[col]):
                print(f"        {label}: {row[col]}")

    if len(gdf) > 1:
        choice = input(
            f"\nQuale risultato vuoi utilizzare? (0-{len(gdf)-1}): "
        ).strip()
        try:
            choice = int(choice)
        except ValueError:
            raise ValueError("Scelta non valida.")
        if choice < 0 or choice >= len(gdf):
            raise ValueError("Indice fuori intervallo.")
        result = gdf.iloc[choice]
    else:
        result = gdf.iloc[0]

    polygon = result.geometry
    min_lon, min_lat, max_lon, max_lat = polygon.bounds

    print("\nTerritorio selezionato:")
    print("-" * 50)
    print(result.get("display_name", place_name))
    print("-" * 50)
    print("\nBounding Box:")
    print(f"  Min Lon: {min_lon}")
    print(f"  Min Lat: {min_lat}")
    print(f"  Max Lon: {max_lon}")
    print(f"  Max Lat: {max_lat}")

    return polygon, (min_lon, min_lat, max_lon, max_lat)


# ============================================================
# 2. SCARICAMENTO DEM SRTM da AWS
# ============================================================

def _tile_name(lat, lon):
    ns = "N" if lat >= 0 else "S"
    ew = "E" if lon >= 0 else "W"
    return f"{ns}{abs(lat):02d}{ew}{abs(lon):03d}"


def _tile_url(tile):
    return f"{SRTM_AWS_BASE}/{tile[:3]}/{tile}.hgt.gz"


def _tile_path(tile):
    return os.path.join(SRTM_CACHE_DIR, f"{tile}.hgt")


def _download_tile(tile, timeout=60):
    """Scarica un tile .hgt.gz da AWS (se non già in cache).
    Se il tile non esiste (mare aperto), crea un file di zeri."""
    local = _tile_path(tile)

    if os.path.exists(local):
        return local

    os.makedirs(SRTM_CACHE_DIR, exist_ok=True)
    url = _tile_url(tile)

    try:
        r = requests.get(url, timeout=timeout, stream=True)
    except requests.exceptions.RequestException as e:
        raise RuntimeError(f"Rete: {e}")

    if r.status_code == 404:
        # Tile oceanico: zeri (SRTM1 = 3601 x 3601 x int16)
        data = b"\x00" * (3601 * 3601 * 2)
    else:
        r.raise_for_status()
        data = gzip.decompress(r.content)

    with open(local, "wb") as f:
        f.write(data)

    return local


def _read_hgt(path):
    with open(path, "rb") as f:
        raw = f.read()

    # n = lato del tile (1201 per SRTM3, 3601 per SRTM1)
    n = int(round((len(raw) // 2) ** 0.5))
    if n * n * 2 != len(raw):
        raise ValueError(
            f"Dimensione .hgt inattesa: {len(raw)} byte "
            f"(n calcolato = {n})"
        )

    return np.frombuffer(raw, dtype=">i2").reshape(n, n).astype(float)

def get_real_elevation(bbox, grid_res):

    print("\n[1/3] Recupero altitudini SRTM (mirror AWS)...")

    min_lon, min_lat, max_lon, max_lat = bbox

    lat_min_i = int(np.floor(min_lat))
    lat_max_i = int(np.floor(max_lat))
    lon_min_i = int(np.floor(min_lon))
    lon_max_i = int(np.floor(max_lon))

    tiles = [
        _tile_name(lat, lon)
        for lat in range(lat_min_i, lat_max_i + 1)
        for lon in range(lon_min_i, lon_max_i + 1)
    ]

    print(f"  Tile SRTM: {len(tiles)}")
    print(f"  Cache: {SRTM_CACHE_DIR}")

    # Download parallelo
    tile_data = {}
    with Spinner(f"Download {len(tiles)} tile da AWS"):
        with ThreadPoolExecutor(max_workers=8) as ex:
            futures = {ex.submit(_download_tile, t): t for t in tiles}
            for fut in as_completed(futures):
                t = futures[fut]
                try:
                    path = fut.result()
                    tile_data[t] = _read_hgt(path)
                except Exception as e:
                    raise RuntimeError(f"Errore tile {t}: {e}")

    # Griglia di coordinate WGS84
    lons = np.linspace(min_lon, max_lon, grid_res)
    lats = np.linspace(max_lat, min_lat, grid_res)  # Nord → Sud

    dem = np.zeros((grid_res, grid_res), dtype=float)

    for t, arr in tile_data.items():
        t_lat = int(t[1:3]) * (1 if t[0] == "N" else -1)
        t_lon = int(t[4:7]) * (1 if t[3] == "E" else -1)

        n = arr.shape[0]

        lon_idx = np.where((lons >= t_lon) & (lons < t_lon + 1))[0]
        lat_idx = np.where((lats >= t_lat) & (lats < t_lat + 1))[0]

        if lon_idx.size == 0 or lat_idx.size == 0:
            continue

        lon_frac = (lons[lon_idx] - t_lon)
        lat_frac = (t_lat + 1 - lats[lat_idx])  # Nord → Sud

        col = np.clip((lon_frac * (n - 1)).astype(int), 0, n - 1)
        row = np.clip((lat_frac * (n - 1)).astype(int), 0, n - 1)

        dem[np.ix_(lat_idx, lon_idx)] = arr[np.ix_(row, col)]

    dem = np.nan_to_num(dem, nan=0.0)
    dem[dem < 0] = 0

    print(f"  DEM ricevuto: {dem.shape[0]} x {dem.shape[1]}")
    print(f"  Altitudine minima: {dem.min():.1f} m")
    print(f"  Altitudine massima: {dem.max():.1f} m")

    return dem


# ============================================================
# 3. CREAZIONE MASCHERA TERRITORIO
# ============================================================

def create_mask(dem, bbox, polygon):

    print("\n[2/3] Creazione della maschera geografica...")

    H, W = dem.shape
    min_lon, min_lat, max_lon, max_lat = bbox

    lons = np.linspace(min_lon, max_lon, W)
    lats = np.linspace(max_lat, min_lat, H)

    mask = np.zeros((H, W), dtype=bool)

    n_chunks = 20
    chunk = max(1, math.ceil(H / n_chunks))
    total_chunks = math.ceil(H / chunk)

    for k, j0 in enumerate(range(0, H, chunk)):
        j1 = min(j0 + chunk, H)

        lon_grid, lat_grid = np.meshgrid(lons, lats[j0:j1])
        pts = shapely.points(lon_grid.ravel(), lat_grid.ravel())
        mask[j0:j1] = shapely.contains(polygon, pts).reshape(j1 - j0, W)

        progress_bar(k + 1, total_chunks, prefix="Maschera")

    if not mask.any():
        raise ValueError("La maschera è vuota.")

    print(f"  Area occupata dal territorio: {mask.sum()} pixel")
    return mask


# ============================================================
# 4. GENERAZIONE MESH (BBOX COMPLETO: oceano + terre)
# ============================================================

def build_mesh(
    elevation,
    mask,
    bbox,
    width_mm,
    vertical_exaggeration,
    base_mm,
):

    print("\n[3/3] Generazione mesh 3D...")

    H, W = elevation.shape
    min_lon, min_lat, max_lon, max_lat = bbox

    center_lat = (min_lat + max_lat) / 2
    meters_per_lat = 111320
    meters_per_lon = 111320 * math.cos(math.radians(center_lat))

    real_width = (max_lon - min_lon) * meters_per_lon
    real_height = (max_lat - min_lat) * meters_per_lat

    scale = width_mm / real_width
    z_scale = scale * vertical_exaggeration

    if H < 2 or W < 2:
        raise ValueError("Griglia troppo piccola.")

    progress_bar(1, 3, prefix="Mesh")

    # Tutte le celle sono attive: oceano pieno + terre sopra
    active = np.ones((H - 1, W - 1), dtype=bool)

    uj, ui = np.meshgrid(
        np.arange(H, dtype=np.int64),
        np.arange(W, dtype=np.int64),
        indexing="ij",
    )
    uj = uj.ravel()
    ui = ui.ravel()
    n_used = uj.size

    xs = (ui / (W - 1)) * real_width * scale
    ys = (uj / (H - 1)) * real_height * scale
    zs = np.where(mask[uj, ui], elevation[uj, ui] * z_scale, 0.0)

    vertices = np.empty((n_used * 2, 3), dtype=float)
    vertices[0::2, 0] = xs
    vertices[0::2, 1] = ys
    vertices[0::2, 2] = zs
    vertices[1::2, 0] = xs
    vertices[1::2, 1] = ys
    vertices[1::2, 2] = -base_mm

    idx_top = np.full((H, W), -1, dtype=np.int64)
    idx_bot = np.full((H, W), -1, dtype=np.int64)
    idx_top[uj, ui] = np.arange(0, n_used * 2, 2)
    idx_bot[uj, ui] = np.arange(1, n_used * 2, 2)

    progress_bar(2, 3, prefix="Mesh")

    aj, ai = np.where(active)

    A  = idx_top[aj,     ai    ]
    B  = idx_top[aj,     ai + 1]
    C  = idx_top[aj + 1, ai + 1]
    D  = idx_top[aj + 1, ai    ]
    Ab = idx_bot[aj,     ai    ]
    Bb = idx_bot[aj,     ai + 1]
    Cb = idx_bot[aj + 1, ai + 1]
    Db = idx_bot[aj + 1, ai    ]

    faces_parts = []

    # Superficie
    faces_parts.append(np.column_stack([A, B, C]))
    faces_parts.append(np.column_stack([A, C, D]))

    # Fondo
    faces_parts.append(np.column_stack([Ab, Cb, Bb]))
    faces_parts.append(np.column_stack([Ab, Db, Cb]))

    # Muri laterali (solo sul perimetro esterno)
    padded = np.pad(active, 1, constant_values=False)

    north = ~padded[aj,     ai + 1]
    if north.any():
        faces_parts.append(np.column_stack([A[north], Ab[north], Bb[north]]))
        faces_parts.append(np.column_stack([A[north], Bb[north], B[north]]))

    east = ~padded[aj + 1, ai + 2]
    if east.any():
        faces_parts.append(np.column_stack([B[east], Bb[east], Cb[east]]))
        faces_parts.append(np.column_stack([B[east], Cb[east], C[east]]))

    south = ~padded[aj + 2, ai + 1]
    if south.any():
        faces_parts.append(np.column_stack([C[south], Cb[south], Db[south]]))
        faces_parts.append(np.column_stack([C[south], Db[south], D[south]]))

    west = ~padded[aj + 1, ai]
    if west.any():
        faces_parts.append(np.column_stack([D[west], Db[west], Ab[west]]))
        faces_parts.append(np.column_stack([D[west], Ab[west], A[west]]))

    faces = np.vstack(faces_parts).astype(np.int64)

    mesh = trimesh.Trimesh(
        vertices=vertices,
        faces=faces,
        process=True,
    )

    progress_bar(3, 3, prefix="Mesh")
    return mesh


# ============================================================
# 5. VISUALIZZAZIONE 3D INTERATTIVA
# ============================================================

def _terrain_gradient(t):
    """t in [0,1] → RGB. Verde → marrone → bianco."""
    t = np.clip(t, 0, 1).reshape(-1, 1)
    c0 = np.array([0.20, 0.50, 0.15])  # verde
    c1 = np.array([0.55, 0.40, 0.20])  # marrone
    c2 = np.array([1.00, 1.00, 1.00])  # bianco

    out = np.empty((t.shape[0], 3))
    low = t[:, 0] < 0.5
    s = (t[low] / 0.5)
    out[low] = c0 * (1 - s) + c1 * s
    s = ((t[~low] - 0.5) / 0.5)
    out[~low] = c1 * (1 - s) + c2 * s
    return out


def visualize_mesh(mesh, base_mm):

    print("\nPreparazione visualizzazione...")

    z = mesh.vertices[:, 2]
    colors = np.zeros((len(z), 4), dtype=np.float32)

    bottom_mask = z < -base_mm * 0.5
    sea_mask = (~bottom_mask) & (z < 1e-6)
    land_mask = ~(bottom_mask | sea_mask)

    colors[bottom_mask] = [0.30, 0.30, 0.30, 1.0]
    colors[sea_mask]    = [0.15, 0.45, 0.85, 1.0]

    if land_mask.any():
        z_land = z[land_mask]
        z_max = float(z_land.max()) if z_land.max() > 0 else 1.0
        rgb = _terrain_gradient(z_land / z_max)
        colors[land_mask, :3] = rgb
        colors[land_mask, 3]  = 1.0

    mesh.visual.vertex_colors = (colors * 255).astype(np.uint8)

    print(f"  Vertici:    {len(mesh.vertices)}")
    print(f"  Facce:      {len(mesh.faces)}")
    print(f"  Dimensioni: {mesh.extents}")
    print(f"  Watertight: {mesh.is_watertight}")


def export_mesh(mesh, base_mm):
    """Esporta il modello in STL, OBJ e PLY."""

    print("\n==========================================")
    print("EXPORT MODELLO")
    print("==========================================")

    # STL — per la stampa 3D (geometria pura, no colori)
    mesh.export(OUT_STL)
    stl_size = os.path.getsize(OUT_STL) / (1024 * 1024)
    print(f"  STL  → {OUT_STL}  ({stl_size:.1f} MB)  [stampa 3D]")

    # PLY — con colori per vertice (mantiene il gradiente terra/mare)
    mesh.export(OUT_PLY)
    ply_size = os.path.getsize(OUT_PLY) / (1024 * 1024)
    print(f"  PLY  → {OUT_PLY}  ({ply_size:.1f} MB)  [colori vertici]")

    # OBJ — compatibile con Blender, MeshLab, Three.js, ecc.
    mesh.export(OUT_OBJ)
    obj_size = os.path.getsize(OUT_OBJ) / (1024 * 1024)
    print(f"  OBJ  → {OUT_OBJ}  ({obj_size:.1f} MB)  [editing 3D]")

    print("\n  Dove trovarli:")
    print(f"    {os.path.abspath(OUT_STL)}")
    print(f"    {os.path.abspath(OUT_PLY)}")
    print(f"    {os.path.abspath(OUT_OBJ)}")


def open_viewer(mesh):
    print("\nApertura viewer 3D... (chiudi la finestra per terminare)")


# ============================================================
# MAIN
# ============================================================

if __name__ == "__main__":

    mode = choose_mode()

    polygon, bbox = search_place(mode)

    dem = get_real_elevation(bbox, GRID_RES)

    mask = create_mask(dem, bbox, polygon)

    mesh = build_mesh(
        dem,
        mask,
        bbox,
        WIDTH_MM,
        V_EXAGG,
        BASE_MM,
    )

    visualize_mesh(mesh, BASE_MM)

    export_mesh(mesh, BASE_MM)

    open_viewer(mesh)