/* =========================================================
   AERODASH v3.3 — app.js
   Global flight tracking + 84k+ airport database
   Airports: sourced from world-airports.csv (comprehensive)
   UI: Minimal & Clean (white + red theme)
   ========================================================= */
'use strict';

/* ── CONFIG ──────────────────────────────────────────────── */
const OPENSKY_URL  = 'https://opensky-network.org/api/states/all';
const METEO_URL    = 'https://api.open-meteo.com/v1/forecast';
const REFRESH_SECS = 30;
const MAX_AC       = 250;   // markers on map

/* ── REGION VIEWS ────────────────────────────────────────── */
const REGIONS = {
  eu: [50,  12,   4],
  na: [42, -98,   3],
  sa: [-15, -56,  3],
  as: [28,  108,  3],
  me: [26,   50,  5],
  af: [  2,  22,  3],
  oc: [-28, 134,  4],
};

/* ════════════════════════════════════════════════════════════
   THEME MANAGER — Light/Dark Mode Toggle
   ════════════════════════════════════════════════════════════ */
const ThemeManager = (() => {
  const STORAGE_KEY = 'aerodash-theme';
  const LIGHT = 'light';
  const DARK = 'dark';

  function getSystemPreference() {
    if (window.matchMedia) {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? DARK : LIGHT;
    }
    return LIGHT;
  }

  function getSavedTheme() {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) return saved;
    return getSystemPreference();
  }

  function applyTheme(theme) {
    const html = document.documentElement;
    html.setAttribute('data-theme', theme);
    localStorage.setItem(STORAGE_KEY, theme);
    updateToggleButton(theme);
  }

  function updateToggleButton(theme) {
    const btn = document.getElementById('theme-toggle');
    if (btn) {
      btn.textContent = theme === DARK ? '☀️' : '🌙';
      btn.setAttribute('aria-label', 
        theme === DARK ? 'Switch to light mode' : 'Switch to dark mode'
      );
    }
  }

  function init() {
    const saved = getSavedTheme();
    applyTheme(saved);
    const btn = document.getElementById('theme-toggle');
    if (btn) btn.addEventListener('click', toggle);
    if (window.matchMedia) {
      window.matchMedia('(prefers-color-scheme: dark)').addListener(e => {
        if (!localStorage.getItem(STORAGE_KEY)) {
          applyTheme(e.matches ? DARK : LIGHT);
        }
      });
    }
  }

  function toggle() {
    const html = document.documentElement;
    const current = html.getAttribute('data-theme') || getSavedTheme();
    const next = current === DARK ? LIGHT : DARK;
    applyTheme(next);
  }

  function current() {
    const html = document.documentElement;
    return html.getAttribute('data-theme') || getSavedTheme();
  }

  function set(theme) {
    if ([LIGHT, DARK].includes(theme)) {
      applyTheme(theme);
    }
  }

  return { init, toggle, current, set, LIGHT, DARK };
})();

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', ThemeManager.init);
} else {
  ThemeManager.init();
}
window.ThemeManager = ThemeManager;

/* ════════════════════════════════════════════════════════════
   DATA LOADER — Robust Flight Data Management
   ════════════════════════════════════════════════════════════ */
const DataLoader = (() => {
  const config = {
    opensky_url: 'https://opensky-network.org/api/states/all',
    timeout_ms: 10000,
    retry_attempts: 3,
    retry_delay_ms: 1000,
    log_level: 'info'
  };

  let loadStats = {
    lastUpdate: null,
    successCount: 0,
    failureCount: 0,
    lastError: null,
    dataSource: 'simulation'
  };

  const Logger = {
    debug: (msg, data) => config.log_level === 'debug' && console.log('🔵 [DEBUG]', msg, data || ''),
    info: (msg, data) => console.log('ℹ️  [INFO]', msg, data || ''),
    warn: (msg, data) => console.warn('⚠️  [WARN]', msg, data || ''),
    error: (msg, data) => console.error('❌ [ERROR]', msg, data || ''),
    success: (msg, data) => console.log('✅ [SUCCESS]', msg, data || '')
  };

  async function fetchWithTimeout(url, timeoutMs = config.timeout_ms) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => {
      controller.abort();
      Logger.warn(`Request timeout after ${timeoutMs}ms`);
    }, timeoutMs);

    try {
      const response = await fetch(url, {
        signal: controller.signal,
        method: 'GET',
        headers: { 'Accept': 'application/json', 'Cache-Control': 'no-cache' }
      });
      clearTimeout(timeoutId);
      return response;
    } catch (err) {
      clearTimeout(timeoutId);
      throw err;
    }
  }

  function parseOpenSkyResponse(json) {
    if (!json || typeof json !== 'object') {
      throw new Error('Response is not a valid object');
    }
    if (!Array.isArray(json.states)) {
      throw new Error('Response missing "states" array');
    }
    const flights = json.states.filter(flight => {
      return flight && flight.length >= 17 && flight[5] !== null && flight[6] !== null;
    });
    if (flights.length === 0) {
      throw new Error('No flights with valid GPS positions');
    }
    Logger.success(`Parsed ${flights.length} flights from ${json.states.length} total`, {
      timestamp: new Date(json.time * 1000).toISOString(),
      coverage: `${Math.round(flights.length / json.states.length * 100)}%`
    });
    return { flights, timestamp: json.time, total: json.states.length, valid: flights.length };
  }

  async function fetchFromOpenSky(attempt = 1) {
    try {
      Logger.info(`Fetching OpenSky API (attempt ${attempt}/${config.retry_attempts})...`);
      const response = await fetchWithTimeout(config.opensky_url, config.timeout_ms);

      if (!response.ok) {
        const error = new Error(`HTTP ${response.status}: ${response.statusText}`);
        error.status = response.status;
        if (response.status === 429) {
          Logger.error('Rate limited! Waiting before retry...', { status: 429, retryAfter: response.headers.get('Retry-After') });
          await new Promise(r => setTimeout(r, config.retry_delay_ms * attempt));
        } else if (response.status >= 500) {
          Logger.error('Server error, will retry...', { status: response.status });
        }
        throw error;
      }

      let json;
      try {
        json = await response.json();
      } catch (parseErr) {
        throw new Error(`Failed to parse JSON: ${parseErr.message}`);
      }

      const data = parseOpenSkyResponse(json);
      loadStats.dataSource = 'opensky-live';
      loadStats.successCount++;
      loadStats.lastUpdate = new Date();
      return data;

    } catch (err) {
      loadStats.lastError = err.message;
      if (attempt < config.retry_attempts) {
        const delay = config.retry_delay_ms * Math.pow(2, attempt - 1);
        Logger.warn(`Attempt ${attempt} failed: ${err.message}. Retrying in ${delay}ms...`);
        await new Promise(r => setTimeout(r, delay));
        return fetchFromOpenSky(attempt + 1);
      } else {
        loadStats.failureCount++;
        throw new Error(`Failed after ${config.retry_attempts} attempts: ${err.message}`);
      }
    }
  }

  function normalizeFlightData(flights) {
    return flights.map(f => {
      const [icao24, callsign, originCountry, timePosition, lastContact, lon, lat, altitude, onGround, velocity, heading, verticalRate, sensors, geoAltitude, squawk, spi, positionSource, category] = f;
      return [
        icao24, callsign?.trim() || icao24, originCountry?.trim(), timePosition, lastContact,
        lon, lat, altitude, onGround, velocity, heading, verticalRate, sensors, geoAltitude, squawk, spi
      ];
    });
  }

  async function load() {
    try {
      Logger.info('═══ Starting flight data load ═══');
      const data = await fetchFromOpenSky();
      const normalized = normalizeFlightData(data.flights);
      Logger.success(`Data load complete: ${normalized.length} flights from OpenSky`);
      return {
        success: true,
        flights: normalized,
        source: 'opensky',
        timestamp: data.timestamp,
        stats: {
          total: data.total,
          valid: data.valid,
          coverage: `${Math.round(data.valid / data.total * 100)}%`
        }
      };
    } catch (err) {
      Logger.error(`OpenSky API failed: ${err.message}`);
      Logger.info('Falling back to simulation data...');
      return {
        success: false,
        flights: null,
        source: 'simulation-fallback',
        error: err.message,
        timestamp: Date.now() / 1000
      };
    }
  }

  function getStats() {
    return {
      ...loadStats,
      uptime: loadStats.lastUpdate ? Math.round((Date.now() - loadStats.lastUpdate) / 1000) + 's ago' : 'never'
    };
  }

  function setConfig(opts) {
    Object.assign(config, opts);
    Logger.info('Configuration updated', config);
  }

  async function testConnectivity() {
    Logger.info('Testing OpenSky API connectivity...');
    try {
      const response = await fetchWithTimeout(config.opensky_url, 5000);
      if (response.ok) {
        Logger.success('✅ OpenSky API is reachable');
        return true;
      } else {
        Logger.error(`API returned HTTP ${response.status}`);
        return false;
      }
    } catch (err) {
      Logger.error(`Connectivity test failed: ${err.message}`);
      return false;
    }
  }

  return { load, getStats, setConfig, testConnectivity, Logger };
})();

if (typeof module !== 'undefined' && module.exports) {
  module.exports = DataLoader;
}

/* ════════════════════════════════════════════════════════════
   AIRPORTS API — Local/Server Database Management
   ════════════════════════════════════════════════════════════ */
const AirportsAPI = (() => {
  const config = { serverUrl: 'http://localhost:3000', useServer: false, cacheEnabled: true, cacheTTL: 600000 };
  const cache = new Map();
  let localDb = null;

  async function loadLocalDatabase() {
    if (localDb) return localDb;
    try {
      let res = await fetch('./data/airports-full.json');
      if (res.status === 404) res = await fetch('./data/airports-enhanced.json');
      localDb = await res.json();
      console.log(`✓ Loaded ${localDb.airports ? localDb.airports.length : (Array.isArray(localDb) ? localDb.length : 0)} airports`);
      return localDb;
    } catch (e) {
      console.error('Failed to load airports database:', e);
      return null;
    }
  }

  async function checkServerAvailability() {
    try {
      let fetchOptions = {};
      if (AbortSignal && AbortSignal.timeout) {
        fetchOptions.signal = AbortSignal.timeout(2000);
      } else {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 2000);
        fetchOptions.signal = controller.signal;
      }
      const res = await fetch(`${config.serverUrl}/api/airports?limit=1`, fetchOptions);
      return res.ok;
    } catch (err) {
      console.warn('Server check failed:', err.message);
      return false;
    }
  }

  function getCached(key) {
    const entry = cache.get(key);
    if (!entry) return null;
    if (Date.now() - entry.timestamp > config.cacheTTL) {
      cache.delete(key);
      return null;
    }
    return entry.value;
  }

  function setCached(key, value) {
    if (config.cacheEnabled) {
      cache.set(key, { value, timestamp: Date.now() });
    }
  }

  function haversine(lat1, lon1, lat2, lon2) {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(dLat / 2) ** 2 + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLon / 2) ** 2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  }

  async function request(endpoint, params = {}) {
    const cacheKey = `${endpoint}:${JSON.stringify(params)}`;
    const cached = getCached(cacheKey);
    if (cached) return cached;

    let url = endpoint;
    if (Object.keys(params).length > 0) {
      const qs = new URLSearchParams(params).toString();
      url += '?' + qs;
    }

    try {
      if (config.useServer) {
        const res = await fetch(`${config.serverUrl}${url}`);
        if (!res.ok) throw new Error(`API Error: ${res.status}`);
        const data = await res.json();
        setCached(cacheKey, data);
        return data;
      }
    } catch (e) {
      console.warn('Server request failed, falling back to local DB:', e.message);
      config.useServer = false;
    }

    return requestLocal(endpoint, params, cacheKey);
  }

  async function requestLocal(endpoint, params, cacheKey) {
    const db = await loadLocalDatabase();
    if (!db) throw new Error('No airport database available');

    let result;
    const airports = Array.isArray(db) ? db : (db.airports || []);

    if (endpoint === '/api/airports') {
      const page = params.page || 1;
      const limit = Math.min(100, params.limit || 20);
      const offset = (page - 1) * limit;
      result = {
        version: db.version,
        pagination: { page, limit, total: airports.length, pages: Math.ceil(airports.length / limit) },
        data: airports.slice(offset, offset + limit)
      };
    } else if (endpoint.startsWith('/api/airports/search')) {
      const q = (params.q || '').toLowerCase();
      if (q.length < 1) {
        result = { query: q, found: 0, data: [] };
      } else {
        result = { query: q, found: 0, data: [] };
        result.data = airports.filter(a => {
          const name = a.name || '';
          const icao = a.icao || '';
          const iata = a.iata || '';
          const country = a.country || '';
          const municipality = a.municipality || '';
          return name.toLowerCase().includes(q) || icao.toLowerCase().includes(q) || iata.toLowerCase().includes(q) ||
                 country.toLowerCase().includes(q) || municipality.toLowerCase().includes(q);
        }).slice(0, 100);
        result.found = result.data.length;
      }
    } else if (endpoint.match(/\/api\/airports\/[A-Z0-9]+$/)) {
      const code = endpoint.split('/').pop().toUpperCase();
      const airport = airports.find(a => 
        (a.icao && a.icao.toUpperCase() === code) || (a.iata && a.iata.toUpperCase() === code)
      );
      if (!airport) throw new Error(`Airport ${code} not found`);
      result = airport;
    } else if (endpoint.startsWith('/api/airports/nearest')) {
      const lat = parseFloat(params.lat);
      const lon = parseFloat(params.lon);
      if (isNaN(lat) || isNaN(lon) || lat < -90 || lat > 90 || lon < -180 || lon > 180) {
        throw new Error('Invalid coordinates');
      }
      const limit = Math.min(50, parseInt(params.limit) || 10);
      result = {
        center: { lat, lon },
        limit,
        found: 0,
        data: airports
          .filter(a => a.lat && a.lon)
          .map(a => ({ ...a, distance: haversine(lat, lon, a.lat, a.lon) }))
          .sort((a, b) => a.distance - b.distance)
          .slice(0, limit)
      };
      result.found = result.data.length;
    } else if (endpoint.match(/\/api\/airports\/by-region\//)) {
      const region = endpoint.split('/').pop().toLowerCase();
      const data = airports.filter(a => {
        const cont = a.continent ? a.continent.toLowerCase() : '';
        return cont === region;
      });
      if (data.length === 0) throw new Error(`No airports in region ${region}`);
      result = { region, found: data.length, data: data.slice(0, 100) };
    } else if (endpoint.includes('filter')) {
      let data = [...airports];
      if (params.type && params.type !== 'all') data = data.filter(a => a.type === params.type);
      if (params.scheduled === 'true') data = data.filter(a => a.scheduled === true);
      if (params.minElevation) data = data.filter(a => (a.elevation_ft || 0) >= parseInt(params.minElevation));
      if (params.country) data = data.filter(a => a.country && a.country.toLowerCase() === params.country.toLowerCase());
      if (params.continent) data = data.filter(a => a.continent && a.continent.toLowerCase() === params.continent.toLowerCase());
      const limit = Math.min(100, parseInt(params.limit) || 20);
      result = { filters: params, found: data.length, limit, data: data.slice(0, limit) };
    } else if (endpoint.includes('stats')) {
      const total = airports.length;
      const byType = {}, byCountry = {}, byContinent = {};
      let totalElevation = 0, elevationCount = 0, maxElevation = 0;
      airports.forEach(a => {
        byType[a.type] = (byType[a.type] || 0) + 1;
        if (a.country) byCountry[a.country] = (byCountry[a.country] || 0) + 1;
        if (a.continent) byContinent[a.continent] = (byContinent[a.continent] || 0) + 1;
        if (a.elevation_ft) {
          totalElevation += a.elevation_ft;
          elevationCount++;
          maxElevation = Math.max(maxElevation, a.elevation_ft);
        }
      });
      result = {
        totals: {
          airports: total,
          countries: Object.keys(byCountry).length,
          continents: Object.keys(byContinent).length,
          scheduled: airports.filter(a => a.scheduled).length
        },
        by_type: byType,
        by_continent: byContinent,
        elevation: { average: elevationCount > 0 ? Math.round(totalElevation / elevationCount) : 0, max: maxElevation },
        top_countries: Object.entries(byCountry).sort((a, b) => b[1] - a[1]).slice(0, 10).map(([country, count]) => ({ country, count }))
      };
    }

    setCached(cacheKey, result);
    return result;
  }

  return {
    init: async () => { const hasServer = await checkServerAvailability(); config.useServer = hasServer; return hasServer; },
    list: (page = 1, limit = 20) => request('/api/airports', { page, limit }),
    search: (query) => request('/api/airports/search', { q: query }),
    get: (code) => request(`/api/airports/${code}`),
    nearest: (lat, lon, limit = 10) => request('/api/airports/nearest', { lat, lon, limit }),
    byRegion: (region) => request(`/api/airports/by-region/${region}`),
    filter: (options) => request('/api/airports/filter', options),
    stats: () => request('/api/airports/stats'),
    clearCache: () => cache.clear(),
    config: (opts) => Object.assign(config, opts),
    getConfig: () => ({ ...config })
  };
})();

/* ════════════════════════════════════════════════════════════
   AIRPORTS LOADER — Dynamic Loading from JSON Database
   Loads airports from data/airports-enhanced.json with intelligent normalization
   ════════════════════════════════════════════════════════════ */

let AIRPORTS = [];

/* ════════════════════════════════════════════════════════════
   INDEXEDDB CACHE SYSTEM FOR AIRPORTS
   Persistent caching for 55MB+ airport database
   ════════════════════════════════════════════════════════════ */
const AirportCache = (() => {
  const DB_NAME = 'AeroDash-DB';
  const STORE_NAME = 'airports';
  const CACHE_KEY = 'airports-enhanced-v1';
  let db = null;

  async function initDB() {
    if (db) return db;
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, 1);
      
      req.onerror = () => reject(req.error);
      req.onsuccess = () => {
        db = req.result;
        resolve(db);
      };
      
      req.onupgradeneeded = (e) => {
        const idb = e.target.result;
        if (!idb.objectStoreNames.contains(STORE_NAME)) {
          idb.createObjectStore(STORE_NAME, { keyPath: 'key' });
        }
      };
    });
  }

  async function getFromCache() {
    try {
      const database = await initDB();
      return new Promise((resolve, reject) => {
        const tx = database.transaction(STORE_NAME, 'readonly');
        const store = tx.objectStore(STORE_NAME);
        const req = store.get(CACHE_KEY);
        
        req.onerror = () => reject(req.error);
        req.onsuccess = () => {
          if (req.result) {
            console.log(`✅ Loaded ${req.result.airports.length} airports from IndexedDB cache`);
            resolve(req.result.airports);
          } else {
            resolve(null);
          }
        };
      });
    } catch (err) {
      console.warn('⚠️  IndexedDB read failed:', err.message);
      return null;
    }
  }

  async function saveToCache(airports) {
    try {
      const database = await initDB();
      return new Promise((resolve, reject) => {
        const tx = database.transaction(STORE_NAME, 'readwrite');
        const store = tx.objectStore(STORE_NAME);
        const req = store.put({ key: CACHE_KEY, airports, timestamp: Date.now() });
        
        req.onerror = () => reject(req.error);
        req.onsuccess = () => {
          console.log(`💾 Saved ${airports.length} airports to IndexedDB cache`);
          resolve(true);
        };
      });
    } catch (err) {
      console.warn('⚠️  IndexedDB write failed:', err.message);
      return false;
    }
  }

  async function clearCache() {
    try {
      const database = await initDB();
      return new Promise((resolve) => {
        const tx = database.transaction(STORE_NAME, 'readwrite');
        const store = tx.objectStore(STORE_NAME);
        const req = store.delete(CACHE_KEY);
        req.onsuccess = () => {
          console.log('🗑️  Airport cache cleared');
          resolve(true);
        };
      });
    } catch (err) {
      console.warn('Cache clear failed:', err.message);
      return false;
    }
  }

  return { getFromCache, saveToCache, clearCache };
})();

/**
 * Load massive JSON files using ReadableStream without blocking UI
 * Monitors download progress and decodes incrementally
 */
async function loadLargeJSON(url, onProgress) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`HTTP ${response.status}`);

  const contentLength = response.headers.get('content-length');
  const total = parseInt(contentLength, 10);
  let loaded = 0;

  const reader = response.body.getReader();
  const decoder = new TextDecoder('utf-8');
  let chunks = '';

  console.log(`📥 Starting download: ${(total / 1024 / 1024).toFixed(2)} MB`);

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    loaded += value.byteLength;
    chunks += decoder.decode(value, { stream: true });

    if (onProgress && total) {
      const percent = Math.round((loaded / total) * 100);
      onProgress(percent);
      if (percent % 10 === 0) {
        console.log(`  ⏳ ${percent}% downloaded...`);
      }
    }

    await new Promise(r => setTimeout(r, 0));
  }

  console.log('🔄 Parsing JSON...');
  try {
    return JSON.parse(chunks);
  } catch (e) {
    console.error('❌ JSON parse error:', e);
    throw e;
  }
}

/**
 * Normalize airport data from JSON with intelligent field handling
 */
function normalizeAirportData(rawAirport) {
  const apt = rawAirport;
  
  const lat = apt.lat || apt.latitude;
  const lon = apt.lon || apt.longitude;
  
  if (lat == null || lon == null || isNaN(lat) || isNaN(lon)) {
    return null;
  }
  
  const code = apt.icao || apt.iata || apt.code;
  if (!code) return null;
  
  const name = apt.name || `${apt.city || ''} ${apt.airport || ''}`.trim() || 'Unknown Airport';
  const country = apt.country || apt.nation || 'Unknown';
  
  return [code, name, parseFloat(lat), parseFloat(lon), country];
}

/**
 * Load airports from cache or network with progress tracking
 */
async function loadAirportsDatabase() {
  try {
    console.log('📍 Loading airports database...');
    
    const cached = await AirportCache.getFromCache();
    if (cached && cached.length > 0) {
      AIRPORTS = cached;
      console.log(`✅ Loaded ${AIRPORTS.length} airports from cache (instant)`);
      return true;
    }

    console.log('⬇️  Downloading from server...');
    const progressEl = $('loading-progress');
    const statusEl = $('loading-status');
    
    const rawData = await loadLargeJSON('./data/airports-enhanced.json', (percent) => {
      if (progressEl) progressEl.style.width = (15 + percent * 0.5) + '%';
      if (statusEl) statusEl.textContent = `Loading airports ${percent}%...`;
    });

    const airportsList = Array.isArray(rawData) ? rawData : (rawData.airports || []);
    
    if (!Array.isArray(airportsList)) {
      throw new Error('Invalid airports data format');
    }

    console.log(`📦 Processing ${airportsList.length} airports...`);
    
    AIRPORTS = airportsList
      .map(normalizeAirportData)
      .filter(apt => apt !== null);

    await AirportCache.saveToCache(AIRPORTS);
    
    console.log(`✅ Loaded ${AIRPORTS.length} airports with valid GPS positions`);
    return true;
  } catch (err) {
    console.warn('⚠️  Failed to load airports database:', err.message);
    console.log('Using fallback airport list...');
    loadFallbackAirports();
    return false;
  }
}

/**
 * Fallback airport list (when JSON loading fails)
 * Top 50 major airports worldwide
 */
function loadFallbackAirports() {
  // Fallback: Top 50 major airports when JSON fails to load
  AIRPORTS = [
    ['EGLL','London Heathrow',51.4706,-0.4619,'United Kingdom'],
    ['EGKK','London Gatwick',51.1481,-0.1903,'United Kingdom'],
    ['LFPG','Paris Charles de Gaulle',49.0097,2.5479,'France'],
    ['EDDF','Frankfurt Main',50.0264,8.5431,'Germany'],
    ['LEMD','Madrid Barajas',40.4936,-3.5669,'Spain'],
    ['LIRF','Rome Fiumicino',41.7999,12.2462,'Italy'],
    ['EHAM','Amsterdam Schiphol',52.3086,4.7639,'Netherlands'],
    ['LOWW','Vienna',48.1103,16.5697,'Austria'],
    ['LSZH','Zurich',47.4647,8.5492,'Switzerland'],
    ['EKCH','Copenhagen Kastrup',55.618,12.6561,'Denmark'],
    ['ENGM','Oslo Gardermoen',60.1939,11.1004,'Norway'],
    ['ESSA','Stockholm Arlanda',59.6519,17.9186,'Sweden'],
    ['EFHK','Helsinki Vantaa',60.3172,24.9633,'Finland'],
    ['UUEE','Moscow Sheremetyevo',55.9726,37.4146,'Russia'],
    ['KLAX','Los Angeles International',33.9425,-118.408,'United States'],
    ['KORD','Chicago O\'Hare',41.9742,-87.9073,'United States'],
    ['KJFK','New York JFK',40.6413,-73.7781,'United States'],
    ['KATL','Atlanta Hartsfield-Jackson',33.6407,-84.4277,'United States'],
    ['KSFO','San Francisco',37.6213,-122.379,'United States'],
    ['KDFW','Dallas Fort Worth',32.8998,-97.0403,'United States'],
    ['CYYZ','Toronto Pearson',43.6777,-79.6248,'Canada'],
    ['CYVR','Vancouver',49.1947,-123.179,'Canada'],
    ['CYUL','Montreal Trudeau',45.4706,-73.7408,'Canada'],
    ['OMDB','Dubai International',25.2528,55.3644,'United Arab Emirates'],
    ['OTBD','Doha Hamad',25.2609,51.6138,'Qatar'],
    ['ZBAA','Beijing Capital',40.0799,116.6031,'China'],
    ['ZSPD','Shanghai Pudong',31.1443,121.8083,'China'],
    ['RJTT','Tokyo Haneda',35.5494,139.7798,'Japan'],
    ['RKSI','Seoul Incheon',37.4602,126.4407,'South Korea'],
    ['WSSS','Singapore Changi',1.3644,103.9915,'Singapore'],
    ['VTBS','Bangkok Suvarnabhumi',13.6811,100.7472,'Thailand'],
    ['VIDP','Delhi Indira Gandhi',28.5665,77.1031,'India'],
    ['VABB','Mumbai Chhatrapati Shivaji',19.0896,72.8656,'India'],
    ['SBGR','São Paulo Guarulhos',-23.4356,-46.4731,'Brazil'],
    ['SBGL','Rio de Janeiro Galeão',-22.8099,-43.2505,'Brazil'],
    ['SAEZ','Buenos Aires Ezeiza',-34.8222,-58.5358,'Argentina'],
    ['FAOR','Johannesburg OR Tambo',-26.1392,28.246,'South Africa'],
    ['HECA','Cairo',30.1219,31.4056,'Egypt'],
    ['YSSY','Sydney Kingsford Smith',-33.9461,151.177,'Australia'],
    ['YMML','Melbourne Tullamarine',-37.6733,144.843,'Australia'],
    ['EGCC','Manchester',53.3537,-2.2750,'United Kingdom']
  ];
  console.log(`✅ Loaded fallback with ${AIRPORTS.length} airports`);
}

/* Initialize airports on page load (will be called in initializeApplication) */

/* ── WMO WEATHER CODES ───────────────────────────────────── */
const WMO = {0:'Clear Sky',1:'Mainly Clear',2:'Partly Cloudy',3:'Overcast',
  45:'Fog',48:'Icy Fog',51:'Lt Drizzle',53:'Drizzle',55:'Hvy Drizzle',
  61:'Lt Rain',63:'Rain',65:'Hvy Rain',71:'Lt Snow',73:'Snow',75:'Hvy Snow',
  80:'Showers',81:'Rain Showers',82:'Violent Showers',
  95:'Thunderstorm',96:'Storm+Hail',99:'Severe Storm'};

/* ── STATE ───────────────────────────────────────────────── */
let allFlights = [], filtered = [];
let trailHistory = {};
let map, markersLayer, trailLayer, lockLayer;
let lockedFlight = null;
let sortKey = null, sortDir = 1;
let cdTimer = null, refreshTimer = null;
let cdRemaining = REFRESH_SECS;

/* ── DOM HELPERS ─────────────────────────────────────────── */
const $ = id => document.getElementById(id);

/* ── CLOCK ───────────────────────────────────────────────── */
let clockInterval = null;
function startClock() {
  if (clockInterval) clearInterval(clockInterval);
  clockInterval = setInterval(() => { 
    const el = $('p-clock');
    if (el) el.textContent = new Date().toISOString().slice(11,19); 
  }, 1000);
}

/* ══════════════════════════════════════════════════════════
   AIRCRAFT SVG ICON
   The plane is drawn pointing UP (north).
   Leaflet rotates the icon container via CSS transform.
   pointer-events:none on SVG so marker catches all clicks.
   ══════════════════════════════════════════════════════════ */
function makeAircraftIcon(color, hdgDeg) {
  // We rotate the wrapping div, not the SVG, to keep icon anchor correct
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="26" height="26" viewBox="0 0 32 32" fill="none" style="pointer-events:none;display:block">
    <filter id="glow-${color.replace('#','')}">
      <feGaussianBlur stdDeviation="1.5" result="blur"/>
      <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
    </filter>
    <g filter="url(#glow-${color.replace('#','')})">
      <!-- fuselage -->
      <ellipse cx="16" cy="16" rx="2.2" ry="11" fill="${color}"/>
      <!-- nose cone -->
      <path d="M13.8 8 Q16 2 18.2 8 Z" fill="${color}"/>
      <!-- main wings -->
      <path d="M13.8 14 L2 20 L3.5 21.5 L13.8 18 L18.2 18 L28.5 21.5 L30 20 L18.2 14 Z" fill="${color}" opacity="0.9"/>
      <!-- tail fins -->
      <path d="M13.8 24 L9.5 29 L12 29.5 L16 27.5 L20 29.5 L22.5 29 L18.2 24 Z" fill="${color}" opacity="0.85"/>
    </g>
  </svg>`;

  return L.divIcon({
    html: `<div style="transform:rotate(${hdgDeg}deg);width:26px;height:26px;transform-origin:13px 13px">${svg}</div>`,
    className: '',
    iconSize: [26, 26],
    iconAnchor: [13, 13],
    popupAnchor: [0, -15],
  });
}

/* ── ALTITUDE → COLOR ────────────────────────────────────── */
function altColor(m) {
  if (!m || m < 10) return '#5566aa';
  const ft = m * 3.28084;
  if (ft < 10000) return '#00ff9d';
  if (ft < 30000) return '#ff9500';
  return '#ff3355';
}

/* ── FLIGHT PHASE ────────────────────────────────────────── */
function flightPhase(f) {
  if (f[8]) return 'ground';
  const vr = f[11] || 0;
  if (vr >  1.5) return 'climb';
  if (vr < -1.5) return 'descent';
  return 'cruise';
}

const PHASE_LABEL = { climb:'↑ CLIMBING', cruise:'— CRUISING', descent:'↓ DESCENDING', ground:'● GROUND' };
const PHASE_COLOR = { climb:'#00ff9d', cruise:'#00d4ff', descent:'#ff9500', ground:'#5566aa' };

/* ── HAVERSINE ───────────────────────────────────────────── */
function haversine(la1, lo1, la2, lo2) {
  const R = 6371, r = Math.PI / 180;
  const dlat = (la2-la1)*r, dlon = (lo2-lo1)*r;
  const a = Math.sin(dlat/2)**2 + Math.cos(la1*r)*Math.cos(la2*r)*Math.sin(dlon/2)**2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
}
window.haversine = haversine;

/* ════════════════════════════════════════════════════════════
   MAP INITIALISATION
   ════════════════════════════════════════════════════════════ */
function initMap() {
  map = L.map('map', { zoomControl: false, preferCanvas: true })
          .setView([48, 11], 3);

  L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
    maxZoom: 18, attribution: '© CARTO'
  }).addTo(map);

  L.control.zoom({ position: 'bottomright' }).addTo(map);

  trailLayer  = L.layerGroup().addTo(map);
  lockLayer   = L.layerGroup().addTo(map);
  markersLayer= L.layerGroup().addTo(map);

  drawAirportDots();

  // Toggle handlers
  const togApts = $('tog-apts');
  const togTrails = $('tog-trails');
  const togLabels = $('tog-labels');
  
  if (togApts) togApts.addEventListener('change', drawAirportDots);
  if (togTrails) togTrails.addEventListener('change', () => {
    if (!$('tog-trails').checked) trailLayer.clearLayers();
    else if (lockedFlight) drawTrail(lockedFlight);
  });
  if (togLabels) togLabels.addEventListener('change', () => renderMarkers());
}

/* ── AIRPORT DOTS ────────────────────────────────────────── */
let aptLayer = null;
function drawAirportDots() {
  if (aptLayer) { map.removeLayer(aptLayer); aptLayer = null; }
  if (!$('tog-apts').checked) return;
  aptLayer = L.layerGroup();
  AIRPORTS.forEach(([icao, name, lt, ln, country]) => {
    L.circleMarker([lt, ln], {
      radius: 3, color: '#00d4ff', fillColor: '#00d4ff',
      fillOpacity: 0.7, weight: 1
    }).bindPopup(`<b>${name}</b><br>${icao} — ${country}`, { maxWidth: 200 })
      .addTo(aptLayer);
  });
  aptLayer.addTo(map);
}

/* ── TRAIL ───────────────────────────────────────────────── */
function recordTrail(f) {
  if (!f[6] || !f[5]) return;
  if (!trailHistory[f[0]]) trailHistory[f[0]] = [];
  trailHistory[f[0]].push([f[6], f[5]]);
  if (trailHistory[f[0]].length > 50) trailHistory[f[0]].shift();
}

function drawTrail(f) {
  trailLayer.clearLayers();
  if (!$('tog-trails').checked) return;
  const pts = trailHistory[f[0]] || [];
  if (pts.length < 2) return;
  L.polyline(pts, { color: '#00ff9d', weight: 1.5, opacity: 0.45, dashArray: '6 4' })
   .addTo(trailLayer);
}

/* ════════════════════════════════════════════════════════════
   TARGET LOCK  — the fix: proper event binding on markers,
   pointer-events:none on SVG, unified lockTarget function
   ════════════════════════════════════════════════════════════ */
function lockTarget(f) {
  lockedFlight = f;
  const lat = f[6], lon = f[5], hdg = f[10] || 0;
  const cs  = (f[1] || f[0] || '---').trim();
  const ph  = flightPhase(f);

  /* ── Lock strip ── */
  $('lock-strip').dataset.locked = 'true';
  $('lock-cs').textContent  = cs;
  $('lock-sub').textContent = `${f[2]||'---'} | ${PHASE_LABEL[ph]} | ${f[0]}`;

  /* ── Show instruments ── */
  $('pfd-idle').classList.add('hidden');
  $('pfd-live').classList.remove('hidden');

  const altFt = f[7] ? Math.round(f[7] * 3.28084) : 0;
  const spdKt = f[9] ? Math.round(f[9] * 1.94384) : 0;
  const vrFpm = f[11]? Math.round(f[11] * 196.85) : 0;

  $('iv-alt').textContent   = altFt ? altFt.toLocaleString() : 'GND';
  $('iv-spd').textContent   = spdKt || '0';
  $('iv-vr').textContent    = (vrFpm > 0 ? '+' : '') + vrFpm;
  $('iv-hdg').textContent   = String(Math.round(hdg)).padStart(3, '0');
  $('iv-icao').textContent  = f[0];
  $('iv-cs').textContent    = cs;
  $('iv-nation').textContent= f[2] || '---';
  $('iv-squawk').textContent= f[14] || '---';
  $('iv-phase').textContent = PHASE_LABEL[ph];
  $('iv-phase').style.color = PHASE_COLOR[ph];
  $('iv-gnd').textContent   = f[8] ? 'YES' : 'NO';
  if (lat && lon) $('iv-pos').textContent = `${lat.toFixed(5)}°  ${lon.toFixed(5)}°`;

  // Progress bars
  $('ibar-alt').style.width = Math.min(100, altFt / 450) + '%';
  $('ibar-spd').style.width = Math.min(100, spdKt / 6) + '%';

  /* ── HSI Compass — rotate the plane icon by heading ──
     The ring stays fixed (cardinal letters fixed).
     The plane inside rotates to show where it's pointing.
  ── */
  const planEl = $('hsi-plane');
  planEl.style.transform = `translate(-50%, -50%) rotate(${hdg}deg)`;

  /* ── Map: pan + lock ring ── */
  if (lat && lon) {
    map.setView([lat, lon], Math.max(map.getZoom(), 6), { animate: true });
    lockLayer.clearLayers();
    // Animated pulse ring
    L.circleMarker([lat, lon], {
      radius: 22, color: '#00ff9d', weight: 1.5,
      fillOpacity: 0, dashArray: '4 4',
      className: 'lock-ring-pulse'
    }).addTo(lockLayer);
    drawTrail(f);
  }

  /* ── Side panels ── */
  if (lat && lon) { fetchWeather(lat, lon); showNearestAirports(lat, lon); }

  /* ── Highlight table row ── */
  document.querySelectorAll('#fms-tbody tr.sel').forEach(r => r.classList.remove('sel'));
  const row = document.querySelector(`#fms-tbody tr[data-id="${f[0]}"]`);
  if (row) { row.classList.add('sel'); row.scrollIntoView({ block: 'nearest', behavior: 'smooth' }); }
}
window.lockTarget = lockTarget;

function clearLock() {
  lockedFlight = null;
  
  const lockStrip = $('lock-strip');
  const lockCs = $('lock-cs');
  const lockSub = $('lock-sub');
  const pfdIdle = $('pfd-idle');
  const pfdLive = $('pfd-live');
  const wxBody = $('wx-body');
  const aptBody = $('apt-body');
  
  if (lockStrip) lockStrip.dataset.locked = 'false';
  if (lockCs) lockCs.textContent = 'NO TARGET';
  if (lockSub) lockSub.textContent = 'Click on any aircraft';
  if (pfdIdle) pfdIdle.classList.remove('hidden');
  if (pfdLive) pfdLive.classList.add('hidden');
  
  if (lockLayer) lockLayer.clearLayers();
  if (trailLayer) trailLayer.clearLayers();
  
  if (wxBody) wxBody.innerHTML = '<div class="info-idle">Lock a target<br>to fetch live weather</div>';
  if (aptBody) aptBody.innerHTML = '<div class="info-idle">Lock a target<br>to compute proximity</div>';
  
  document.querySelectorAll('#fms-tbody tr.sel').forEach(r => r.classList.remove('sel'));
}
window.clearLock = clearLock;

/* ════════════════════════════════════════════════════════════
   RENDER MARKERS  — fix: bind click on marker directly,
   SVG has pointer-events:none so the div captures clicks
   ════════════════════════════════════════════════════════════ */
function renderMarkers() {
  markersLayer.clearLayers();
  const showLabels = $('tog-labels').checked;

  allFlights.slice(0, MAX_AC).forEach(f => {
    if (!f[6] || !f[5]) return;
    recordTrail(f);

    const color  = altColor(f[7]);
    const hdg    = f[10] || 0;
    const icon   = makeAircraftIcon(color, hdg);
    const cs     = (f[1] || f[0] || 'UNKNOWN').trim();
    const altFt  = f[7] ? Math.round(f[7] * 3.28084).toLocaleString() + ' ft' : 'GND';
    const spdKt  = f[9] ? Math.round(f[9] * 1.94384) + ' kt' : '—';
    const ph     = flightPhase(f);

    const marker = L.marker([f[6], f[5]], { icon });

    // ── THE FIX: bind click directly, no onclick="" in HTML ──
    marker.on('click', (e) => {
      L.DomEvent.stopPropagation(e);
      lockTarget(f);
    });

    // Popup (info only, no lock button needed — click already locks)
    marker.bindPopup(
      `<div style="font-family:'Share Tech Mono',monospace;min-width:150px">
        <div style="font-size:0.9rem;font-weight:700;color:#fff;letter-spacing:1px;margin-bottom:5px">${cs}</div>
        <div style="color:#00d4ff">ALT: ${altFt}</div>
        <div style="color:#aaa">SPD: ${spdKt}</div>
        <div style="color:#aaa">HDG: ${Math.round(hdg)}°</div>
        <div style="color:#aaa">PHASE: ${PHASE_LABEL[ph]}</div>
        <div style="color:#aaa;margin-top:3px">NATION: ${f[2]||'---'}</div>
      </div>`,
      { maxWidth: 200 }
    );

    if (showLabels) {
      marker.bindTooltip(cs, {
        permanent: true, direction: 'top', offset: [0, -14],
        opacity: 1, className: 'ac-label'
      });
    }

    marker.addTo(markersLayer);
  });

  // Re-draw lock ring if locked
  if (lockedFlight?.[ 6] && lockedFlight?.[5]) {
    lockLayer.clearLayers();
    L.circleMarker([lockedFlight[6], lockedFlight[5]], {
      radius: 22, color: '#00ff9d', weight: 1.5,
      fillOpacity: 0, dashArray: '4 4',
      className: 'lock-ring-pulse'
    }).addTo(lockLayer);
  }
}

/* ── WEATHER ─────────────────────────────────────────────── */
async function fetchWeather(lat, lon) {
  $('wx-body').innerHTML = '<div class="info-idle">Fetching…</div>';
  try {
    const url = `${METEO_URL}?latitude=${lat.toFixed(3)}&longitude=${lon.toFixed(3)}&current_weather=true&hourly=cloudcover,visibility,precipitation_probability&forecast_days=1`;
    const d = await (await fetch(url)).json();
    const w = d.current_weather;
    const cond = WMO[w.weathercode] || `CODE ${w.weathercode}`;
    const times = d.hourly?.time || [];
    const idx = times.indexOf((w.time||'').slice(0,16));
    const vis = idx >= 0 && d.hourly.visibility ? (d.hourly.visibility[idx]/1000).toFixed(1)+' km' : '—';
    const cld = idx >= 0 && d.hourly.cloudcover  ? d.hourly.cloudcover[idx]+'%' : '—';

    $('wx-body').innerHTML = [
      ['CONDITION', cond],
      ['TEMPERATURE', `${w.temperature}°C`],
      ['WIND', `${w.windspeed} km/h @ ${w.winddirection}°`],
      ['CLOUD COVER', cld],
      ['VISIBILITY', vis],
      ['OBS TIME', (w.time||'').replace('T',' ')],
    ].map(([k,v]) =>
      `<div class="wx-row"><span class="wxk">${k}</span><span class="wxv">${v}</span></div>`
    ).join('');
  } catch {
    $('wx-body').innerHTML = '<div class="info-idle" style="color:#ff3355">METEO UNAVAILABLE</div>';
  }
}

/* ── NEAREST AIRPORTS ────────────────────────────────────── */
function showNearestAirports(lat, lon) {
  const sorted = AIRPORTS
    .map(([c,n,lt,ln,co]) => ({ c, n, lt, ln, co, d: haversine(lat,lon,lt,ln) }))
    .sort((a,b) => a.d - b.d)
    .slice(0, 5);

  $('apt-body').innerHTML = sorted.map(a =>
    `<div class="apt-card">
       <span class="apt-dist">${Math.round(a.d)} km</span>
       <div class="apt-name">${a.n}</div>
       <div class="apt-sub">${a.c} · ${a.co}</div>
     </div>`
  ).join('');
}

/* ── FLEET STATS ─────────────────────────────────────────── */
function renderFleetStats(flights) {
  const air = flights.filter(f => !f[8] && f[7] > 10);
  const avg = arr => arr.length ? Math.round(arr.reduce((a,b)=>a+b,0)/arr.length) : 0;
  const avgA = avg(air.map(f => f[7]*3.28084).filter(Boolean));
  const avgS = avg(air.map(f => f[9]*1.94384).filter(Boolean));

  $('p-airborne').textContent = air.length;
  $('p-avgalt').textContent   = avgA ? avgA.toLocaleString()+' ft' : '—';
  $('p-avgspd').textContent   = avgS ? avgS+' kt' : '—';

  const cnt = {};
  flights.forEach(f => { if(f[2]) cnt[f[2]] = (cnt[f[2]]||0)+1; });
  const top = Object.entries(cnt).sort((a,b)=>b[1]-a[1]).slice(0,7);
  const max = top[0]?.[1] || 1;

  $('fleet-body').innerHTML =
    `<div class="wx-row"><span class="wxk">TOTAL</span><span class="wxv">${flights.length}</span></div>
     <div class="wx-row"><span class="wxk">AIRBORNE</span><span class="wxv">${air.length}</span></div>
     <div class="wx-row"><span class="wxk">GROUNDED</span><span class="wxv">${flights.length-air.length}</span></div>
     <div class="wx-row"><span class="wxk">AVG ALT</span><span class="wxv">${avgA ? avgA.toLocaleString()+' ft':'—'}</span></div>
     <div class="wx-row"><span class="wxk">AVG SPEED</span><span class="wxv">${avgS ? avgS+' kt':'—'}</span></div>
     <div style="margin:8px 0 4px;font-size:0.58rem;color:#3d5568;letter-spacing:1px">TOP NATIONS</div>
     ${top.map(([c,n])=>`
       <div class="wx-row"><span class="wxk" style="flex:1">${c}</span><span class="wxv">${n}</span></div>
       <div style="height:2px;background:#0c1f33;margin:1px 0 4px;border-radius:1px">
         <div style="height:100%;width:${Math.round(n/max*100)}%;background:#00d4ff;border-radius:1px"></div>
       </div>`).join('')}`;
}

/* ════════════════════════════════════════════════════════════
   FMS TABLE
   ════════════════════════════════════════════════════════════ */
function filterTable() {
  const q   = ($('fms-q').value || '').toLowerCase();
  const nat = ($('fms-nat').value || '').toLowerCase();
  const ph  = $('fms-ph').value || '';

  filtered = allFlights.filter(f => {
    const cs  = ((f[1]||f[0])||'').trim().toLowerCase();
    const co  = (f[2]||'').toLowerCase();
    const fph = flightPhase(f);
    return (!q || cs.includes(q)) && (!nat || co === nat) && (!ph || fph === ph);
  });

  if (sortKey) applySort();
  renderTable();
}
window.filterTable = filterTable;

const SORTERS = {
  cs:  f => ((f[1]||f[0])||'').trim(),
  nat: f => (f[2]||''),
  alt: f => (f[7]||0),
  spd: f => (f[9]||0),
  hdg: f => (f[10]||0),
  vr:  f => (f[11]||0),
};

function applySort() {
  const fn = SORTERS[sortKey];
  if (!fn) return;
  filtered.sort((a,b) => { const av=fn(a),bv=fn(b); return av>bv?sortDir:av<bv?-sortDir:0; });
}

function sortBy(k) {
  sortDir = sortKey === k ? -sortDir : 1;
  sortKey = k;
  filterTable();
}
window.sortBy = sortBy;

function renderTable() {
  $('fc-show').textContent = Math.min(filtered.length, 200);
  $('fc-tot').textContent  = filtered.length;

  const rows = filtered.slice(0, 200);
  if (!rows.length) {
    $('fms-tbody').innerHTML = '<tr><td colspan="8" class="fms-empty">No aircraft match filter</td></tr>';
    return;
  }

  $('fms-tbody').innerHTML = rows.map(f => {
    const cs   = (f[1]||f[0]||'—').trim();
    const color= altColor(f[7]);
    const alt  = f[7] ? Math.round(f[7]*3.28084).toLocaleString()+' ft' : 'GND';
    const spd  = f[9] ? Math.round(f[9]*1.94384)+' kt' : '—';
    const hdg  = f[10] ? Math.round(f[10])+'°' : '—';
    const vr   = f[11] ? (f[11]>0?'+':'')+Math.round(f[11]*196.85)+' fpm' : '—';
    const lat  = f[6]?.toFixed(3) || '—';
    const lon  = f[5]?.toFixed(3) || '—';
    const ph   = flightPhase(f);
    const isSel= lockedFlight?.[0] === f[0];

    return `<tr class="${isSel?'sel':''}" data-id="${f[0]}" onclick="rowClick('${f[0]}')">
      <td style="color:${color};font-weight:700">${cs}</td>
      <td>${f[2]||'—'}</td>
      <td>${alt}</td>
      <td>${spd}</td>
      <td>${hdg}</td>
      <td style="color:${PHASE_COLOR[ph]}">${PHASE_LABEL[ph]}</td>
      <td style="color:${vr.startsWith('+')?'#00ff9d':vr.startsWith('-')?'#ff9500':'#5566aa'}">${vr}</td>
      <td style="color:#3d5568;font-size:0.65rem">${lat} / ${lon}</td>
    </tr>`;
  }).join('');
}

window.rowClick = function(icao) {
  const f = filtered.find(x => x[0] === icao);
  if (f) lockTarget(f);
};

function populateNationFilter() {
  const sel = $('fms-nat');
  const existing = [...sel.options].map(o => o.value);
  [...new Set(allFlights.map(f=>f[2]).filter(Boolean))].sort()
    .forEach(n => {
      if (!existing.includes(n)) {
        const o = document.createElement('option');
        o.value = n; o.textContent = n; sel.appendChild(o);
      }
    });
}

/* ── TABLE HEADER SORT CLICK ─────────────────────────────── */
document.querySelectorAll('#fms-table th[data-k]').forEach(th => {
  th.addEventListener('click', () => sortBy(th.dataset.k));
  th.style.cursor = 'pointer';
});

/* ════════════════════════════════════════════════════════════
   REFRESH SYSTEM — fix: single source of truth for timer,
   proper cleanup before each new cycle
   ════════════════════════════════════════════════════════════ */
function startCountdown() {
  const cdFill = $('cd-fill');
  const pCd = $('p-cd');
  
  if (!cdFill || !pCd) {
    console.warn('⚠️  Countdown timer elements not found');
    return;
  }
  
  // Clear any existing timers
  if (cdTimer) clearInterval(cdTimer);
  if (refreshTimer) clearTimeout(refreshTimer);

  cdRemaining = REFRESH_SECS;
  cdFill.style.transition = 'none';
  cdFill.style.width = '100%';
  pCd.textContent = REFRESH_SECS + 's';

  // Force reflow so transition reset takes effect
  cdFill.getBoundingClientRect();
  cdFill.style.transition = 'width 1s linear';

  cdTimer = setInterval(() => {
    cdRemaining--;
    const el = $('p-cd');
    const fillEl = $('cd-fill');
    if (el) el.textContent = cdRemaining + 's';
    if (fillEl) fillEl.style.width = (cdRemaining / REFRESH_SECS * 100) + '%';
    if (cdRemaining <= 0) {
      clearInterval(cdTimer);
      cdTimer = null;
    }
  }, 1000);

  refreshTimer = setTimeout(() => {
    refreshTimer = null;
    loadData();
  }, REFRESH_SECS * 1000);
}

function manualRefresh() {
  if (cdTimer)     { clearInterval(cdTimer); cdTimer = null; }
  if (refreshTimer){ clearTimeout(refreshTimer); refreshTimer = null; }
  loadData();
}
window.manualRefresh = manualRefresh;

function jumpRegion(v) {
  if (!v || !REGIONS[v]) return;
  const [lat, lon, z] = REGIONS[v];
  map.setView([lat, lon], z, { animate: true });
  $('sel-region').value = '';
}
window.jumpRegion = jumpRegion;

/* ════════════════════════════════════════════════════════════
   LOAD DATA
   ════════════════════════════════════════════════════════════ */
async function loadData() {
  return new Promise(async (resolve) => {
    try {
      const statusEl = $('p-status');
      const totalEl = $('p-total');
      
      if (statusEl) {
        statusEl.textContent = 'SCANNING…';
        statusEl.className = 'pv scanning';
      }

      console.log('🔄 Loading aircraft data using DataLoader...');
      
      // Try to load from OpenSky API
      const result = await DataLoader.load();
      
      if (result.success && result.flights && result.flights.length > 0) {
        // Use live OpenSky data
        allFlights = result.flights;
        console.log(`✅ Loaded ${allFlights.length} aircraft from OpenSky API`);
        console.log(`   Coverage: ${result.stats.coverage} (${result.stats.valid}/${result.stats.total} valid)`);
        
        if (statusEl) {
          statusEl.textContent = '● ONLINE';
          statusEl.className = 'pv online';
        }
      } else {
        // Fallback to simulation
        console.warn('⚠️  OpenSky API failed, using simulation data');
        console.warn(`   Error: ${result.error}`);
        allFlights = SIMULATION_DATA.slice();
        console.log(`✅ Loaded ${allFlights.length} aircraft from simulation`);
        
        if (statusEl) {
          statusEl.textContent = '◉ SIMULATION';
          statusEl.className = 'pv sim';
        }
      }

      if (totalEl) {
        totalEl.textContent = allFlights.length;
      }

      // Render UI
      renderFleetStats(allFlights);
      populateNationFilter();
      filterTable();
      renderMarkers();

      // Re-lock active target with fresh data
      if (lockedFlight) {
        const fresh = allFlights.find(f => f[0] === lockedFlight[0]);
        if (fresh) lockTarget(fresh);
      }

      // Start refresh timer
      startCountdown();
      resolve(true);
      
    } catch (err) {
      console.error('❌ Critical error in loadData():', err);
      allFlights = SIMULATION_DATA.slice();
      renderFleetStats(allFlights);
      populateNationFilter();
      filterTable();
      renderMarkers();
      startCountdown();
      resolve(false);
    }
  });
}

/* ── SIMULATION DATA (30 global flights) ─────────────────── */
const SIMULATION_DATA = [
  ['3c6544','DLH456 ','Germany',  0,0, 11.54, 48.45,11200,false,240, 93, 2.5,null,null,'2347'],
  ['3005f2','ITA111 ','Italy',    0,0, 12.10, 41.90, 8800,false,180, 46,-1.2,null,null,'2000'],
  ['440081','BAW99  ','United Kingdom',0,0,-0.45,51.30,12500,false,280,270,0.1,null,null,'1234'],
  ['4ca723','RYR502 ','Ireland',  0,0,  2.35, 48.86, 9000,false,300,184,-2.0,null,null,'4521'],
  ['3944ee','AFR720 ','France',   0,0,  3.10, 49.10,11500,false,220, 96, 3.1,null,null,'6400'],
  ['34618e','VLG123 ','Spain',    0,0, -3.70, 40.45, 3500,false,170,186,-4.0,null,null,'5012'],
  ['4b1805','SWR451 ','Switzerland',0,0,8.55, 47.45,10000,false,250,260, 0.0,null,null,'2456'],
  ['484161','KLM876 ','Netherlands',0,0,4.76, 52.30,10500,false,330,  5, 1.5,null,null,'3300'],
  ['3e1b45','DLH999 ','Germany',  0,0, 13.40, 52.54,    0, true, 90,  0, 0.0,null,null,'0000'],
  ['0101ab','UAE401 ','United Arab Emirates',0,0,55.36,25.25,12000,false, 90,331,0.0,null,null,'4500'],
  ['710102','SIA321 ','Singapore',0,0,104.00,  1.36,11500,false,110, 96, 0.5,null,null,'7654'],
  ['780455','ANA007 ','Japan',    0,0,139.78, 35.55,10800,false,270,230,-0.8,null,null,'3210'],
  ['4d2233','QTR815 ','Qatar',    0,0, 51.61, 25.27,12000,false,310, 26, 1.0,null,null,'6543'],
  ['a12301','AAL100 ','United States',0,0,-73.77,40.64,3000,false,200,271,4.0,null,null,'1111'],
  ['a98abc','UAL500 ','United States',0,0,-87.90,41.97,11000,false,260,91,0.0,null,null,'2222'],
  ['c02345','ACA870 ','Canada',   0,0,-79.62, 43.67, 9500,false,230,181, 0.3,null,null,'3333'],
  ['e01f00','TAM001 ','Brazil',   0,0,-46.47,-23.44, 7500,false,190,356,-1.5,null,null,'4444'],
  ['7c4444','QFA455 ','Australia',0,0,151.17,-33.94,10000,false,270,311, 0.0,null,null,'5555'],
  ['5002c1','CSN001 ','China',    0,0,116.60, 40.07,11200,false, 80,271,-0.3,null,null,'6666'],
  ['502aaa','AFR999 ','France',   0,0,  2.20, 47.00, 5000,false,310, 31, 2.8,null,null,'7777'],
  ['f88001','ETH502 ','Ethiopia', 0,0, 38.80,  8.97,10500,false,150, 46, 0.0,null,null,'8888'],
  ['b22222','THA456 ','Thailand', 0,0,100.74, 13.68, 9800,false,200,276, 0.4,null,null,'9001'],
  ['d33333','EIN888 ','Ireland',  0,0, -6.27, 53.42,    0, true,  0,  0, 0.0,null,null,'0000'],
  ['888def','AMX777 ','Mexico',   0,0,-99.07, 19.43, 8000,false,340, 91,-2.0,null,null,'1001'],
  ['555aaa','LAN500 ','Chile',    0,0,-70.78,-33.39,11000,false, 70,181, 0.0,null,null,'2001'],
  ['cc9999','SAA001 ','South Africa',0,0,28.24,-26.13,10200,false,310,1,1.2,null,null,'3001'],
  ['dd1111','PIA445 ','Pakistan', 0,0, 67.16, 24.90,11000,false, 50, 91, 0.0,null,null,'4001'],
  ['ee2222','SVO301 ','Russia',   0,0, 37.41, 55.97,10800,false,180,271,-0.5,null,null,'5001'],
  ['ff3333','NZL222 ','New Zealand',0,0,174.79,-37.0,9500,false,320,181,0.0,null,null,'6001'],
  ['aa4444','JAL007 ','Japan',    0,0,139.78, 35.76,12000,false,210, 46, 0.0,null,null,'7001'],
];

/* ════════════════════════════════════════════════════════════
   ADVANCED AIRPORT SEARCH & MANAGEMENT
   ════════════════════════════════════════════════════════════ */
async function initAirportSearch() {
  return new Promise(async (resolve) => {
    try {
      console.log('🔄 Initializing airport database…');
      const hasServer = await AirportsAPI.init();
      const mode = hasServer ? '🔗 SERVER' : '📱 CLIENT';
      const dbStatus = '🌍 Airport database ready';
      console.log(`✈️  ${mode} | ${dbStatus}`);
      resolve(true);
    } catch (err) {
      console.error('❌ Airport database initialization failed:', {
        message: err.message,
        stack: err.stack
      });
      console.log('⚠️  Continuing without airport database');
      resolve(false);
    }
  });
}

async function searchAirports(query) {
  try {
    const result = await AirportsAPI.search(query);
    return result.data || [];
  } catch (e) {
    console.error('Airport search failed:', e);
    return [];
  }
}

async function getAirportStats() {
  try {
    return await AirportsAPI.stats();
  } catch (e) {
    console.error('Failed to fetch airport stats:', e);
    return null;
  }
}

async function getNearestAirports(lat, lon, limit = 10) {
  try {
    const result = await AirportsAPI.nearest(lat, lon, limit);
    return result.data || [];
  } catch (e) {
    console.error('Failed to fetch nearest airports:', e);
    return [];
  }
}

async function showAirportSearchResults() {
  const query = document.getElementById('apt-search-input').value.trim();
  if (!query) {
    $('apt-results').innerHTML = '<div class="info-idle">Enter airport code, name, or country…</div>';
    return;
  }

  $('apt-results').innerHTML = '<div class="info-idle">Searching…</div>';
  
  try {
    const result = await searchAirports(query);
    const airports = Array.isArray(result) ? result : (result.data || []);

    if (airports.length === 0) {
      $('apt-results').innerHTML = '<div class="info-idle">No results for "<strong>' + query + '</strong>"</div>';
      return;
    }

    const limitedResults = airports.slice(0, 50);
    const showMore = airports.length > 50;

    const html = limitedResults.map(a => `
      <div class="apt-card" onclick="lockAirport('${a.icao}')">
        <div class="apt-dist">${a.iata || a.icao}</div>
        <div style="flex:1;min-width:0">
          <div class="apt-name">${a.name}</div>
          <div class="apt-sub">${a.country}${a.municipality ? ' · ' + a.municipality : ''}</div>
        </div>
      </div>
    `).join('');

    const moreText = showMore ? `<div class="info-idle" style="color:var(--text-tertiary);padding:8px;text-align:center;font-size:10px">+${(airports.length - 50).toLocaleString()} more results…</div>` : '';

    $('apt-results').innerHTML = html + moreText;
  } catch (e) {
    console.error('Search error:', e);
    $('apt-results').innerHTML = '<div class="info-idle" style="color:var(--accent-danger)">Search error. Try again.</div>';
  }
}

async function showAirportStats() {
  $('apt-results').innerHTML = '<div class="info-idle">Loading statistics…</div>';
  const stats = await getAirportStats();

  if (!stats) {
    $('apt-results').innerHTML = '<div class="info-idle" style="color:#e63946">Failed to load statistics</div>';
    return;
  }

  const html = `
    <div style="padding:12px;font-size:0.75rem">
      <div style="color:#e63946;font-weight:700;margin-bottom:8px">📊 GLOBAL STATISTICS</div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:12px">
        <div style="background:#f5f5f5;padding:8px;border-left:3px solid #e63946;border-radius:4px">
          <div style="color:#666;font-size:10px">TOTAL</div>
          <div style="color:#e63946;font-weight:700">${(stats.totals.airports || 0).toLocaleString()}</div>
        </div>
        <div style="background:#f5f5f5;padding:8px;border-left:3px solid #457b9d;border-radius:4px">
          <div style="color:#666;font-size:10px">COUNTRIES</div>
          <div style="color:#457b9d;font-weight:700">${stats.totals.countries || 0}</div>
        </div>
        <div style="background:#f5f5f5;padding:8px;border-left:3px solid #06a77d;border-radius:4px">
          <div style="color:#666;font-size:10px">SCHEDULED</div>
          <div style="color:#06a77d;font-weight:700">${(stats.totals.scheduled || 0).toLocaleString()}</div>
        </div>
        <div style="background:#f5f5f5;padding:8px;border-left:3px solid #f77f00;border-radius:4px">
          <div style="color:#666;font-size:10px">CONTINENTS</div>
          <div style="color:#f77f00;font-weight:700">${stats.totals.continents || 0}</div>
        </div>
      </div>
      <div style="color:#666;font-size:10px;font-weight:600;margin-bottom:4px">TOP COUNTRIES:</div>
      ${(stats.top_countries || []).map(c => `
        <div style="color:#666;padding:2px 0;font-size:10px;display:flex;justify-content:space-between">
          <span>${c.country}</span><span style="color:#e63946;font-weight:600">${c.count}</span>
        </div>
      `).join('')}
    </div>
  `;

  $('apt-results').innerHTML = html;
}

function toggleAirportPanel() {
  const panel = document.getElementById('airport-search');
  if (!panel) return;
  
  if (panel.classList.contains('show')) {
    panel.classList.remove('show');
  } else {
    panel.classList.add('show');
    const input = document.getElementById('apt-search-input');
    if (input) {
      input.focus();
      input.select();
    }
  }
}

function lockAirport(icao) {
  if (icao && icao.length > 0) {
    console.log('🔒 Airport locked:', icao);
  }
}

function showAirportDBStatus() {
  const status = AirportsAPI.getConfig();
  console.log('📦 Database Status:', {
    source: status.useServer ? 'Server API' : 'Local JSON',
    cached: status.cacheEnabled,
    ttl: status.cacheTTL + 'ms'
  });
}

window.searchAirports = searchAirports;
window.getAirportStats = getAirportStats;
window.getNearestAirports = getNearestAirports;
window.showAirportSearchResults = showAirportSearchResults;
window.showAirportStats = showAirportStats;
window.toggleAirportPanel = toggleAirportPanel;
window.lockAirport = lockAirport;
window.showAirportDBStatus = showAirportDBStatus;

/* ── DEBUG FUNCTIONS ─────────────────────────────────────────── */
window.testAPIConnection = async function() {
  console.log('🧪 Testing API connectivity...');
  const result = await DataLoader.testConnectivity();
  console.log(result ? '✅ API is reachable' : '❌ API is not reachable');
  return result;
};

window.reloadData = async function() {
  console.log('🔄 Reloading aircraft data...');
  await loadData();
  console.log('✅ Data reloaded');
};

window.getDataStats = function() {
  console.table(DataLoader.getStats());
  return DataLoader.getStats();
};

window.getLoadInfo = function() {
  return {
    aircraftCount: allFlights.length,
    source: allFlights === SIMULATION_DATA ? 'simulation' : 'opensky',
    flightFilters: {
      total: allFlights.length,
      filtered: filtered.length,
      locked: lockedFlight ? 1 : 0
    }
  };
};

/* ────────────────────────────────────────────────────────────
   LOADING SYSTEM
   ──────────────────────────────────────────────────────────── */

const LoadingManager = (() => {
  let progress = 0;
  const maxProgress = 100;
  let isComplete = false;

  function updateProgress(percentage, status) {
    progress = Math.min(percentage, maxProgress);
    const progEl = $('loading-progress');
    const statusEl = $('loading-status');
    if (progEl) progEl.style.width = progress + '%';
    if (status && statusEl) statusEl.textContent = status;
  }

  function hideLoading() {
    if (isComplete) return;
    isComplete = true;
    
    updateProgress(100, 'READY');
    
    setTimeout(() => {
      const overlay = $('loading-overlay');
      if (overlay) {
        overlay.classList.add('hidden');
        setTimeout(() => {
          overlay.style.display = 'none';
        }, 500);
      }
    }, 300);
  }

  return {
    updateProgress,
    hideLoading,
    isComplete: () => isComplete
  };
})();

async function initializeApplication() {
  try {
    startClock();
    
    LoadingManager.updateProgress(0, 'Initializing map…');
    initMap();
    
    LoadingManager.updateProgress(15, 'Loading airport database…');
    // Load airports from JSON, fallback to hardcoded list if fails
    const airportsLoaded = await loadAirportsDatabase();
    if (!airportsLoaded) {
      console.log('⚠️  Will use fallback airport list');
    }
    
    LoadingManager.updateProgress(30, 'Initializing airport search…');
    await initAirportSearch();
    
    LoadingManager.updateProgress(60, 'Fetching aircraft data…');
    await loadData();
    
    LoadingManager.updateProgress(100, 'Ready…');
    setTimeout(() => {
      LoadingManager.hideLoading();
    }, 300);
  } catch (err) {
    console.error('Initialization error:', err);
    LoadingManager.updateProgress(100, 'ERROR — Retry');
  }
}

/* ── BOOT ────────────────────────────────────────────────── */
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initializeApplication);
} else {
  initializeApplication();
}
