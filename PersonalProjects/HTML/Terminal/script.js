/**
 * CYBERDECK OS CORE EXECUTION ENGINE
 * Architecture: Central async processing loops managing decoupled UI and virtual hardware layers.
 */

// Scene Stack Architecture
const stack = [];
function pushScene(s) { stack.push(s); s.selected = 0; if (s.onEnter) s.onEnter(); render(); }
function popScene() { if (stack.length <= 1) return; const s = stack.pop(); if (s.onExit) s.onExit(); render(); }
function current() { return stack[stack.length - 1]; }

// Scene Factories
function menuScene(title, items) { return { type: "menu", title, items, selected: 0 }; }
function appScene(name, desc, cmd, type = "generic") {
  return {
    type: "app", appType: type, title: name, desc, cmdHint: cmd, state: "IDLE",
    onEnter() {
      this.state = "RUNNING";
      logSystem(`systemd`, `Launched telemetry pipeline for: ${cmd}`, "ok");
    }
  };
}
const goto = (s) => () => pushScene(s);

// Real-time Simulation Telemetry Stores
let batteryCapacity = 14000; // 14,000mAh 2S2P pack [cite: 5]
let typicalDraw = 14.9;      // 14.9W continuous consumption [cite: 7]
let runtimeHours = (batteryCapacity * 7.4 / 1000) / typicalDraw; // 

const sensorReadings = {
  "BME280 Temp": "24.2°C", "BME280 Hum": "44.1%", "BME680 Gas": "124 ppm",
  "PMS5003 PM2.5": "12 ug/m3", "Geiger Counter": "0.14 uSv/h", "CCS811 eCO2": "410 ppm",
  "MQ-135 Air": "Optimal", "BMP390 Press": "1013 hPa", "TSL2591 Lux": "340 lx"
};

const aiTelemetry = { "Model Pool": "LLM-Local/Vision-Core", "NPU Load": "14%", "Inference Speed": "28.4 inst/s", "Temp": "41.0°C" };
const rfTelemetry = { "SDR Driver": "SoapySDR/HackRF", "Frequency Range": "1MHz - 6GHz", "LNA Gain": "32dB", "VGA Gain": "20dB" };

// Instantiate Application Views
const subghzApp = appScene("SUB-1GHZ", "Narrowband signal evaluation & capture via CC1101 transceiver.", "subghz scan", "rf");
const sdrApp = appScene("HACKRF ONE", "Wideband software-defined radio analysis framework.", "hackrf analysis", "rf");
const bleApp = appScene("ESP32 BLE/WIFI", "Passive network discovery and tracking agent.", "wifi scan", "rf");
const nfcApp = appScene("PN532 NFC", "High-frequency 13.56MHz contactless target interrogator.", "nfc read", "id");
const rfidApp = appScene("125K RFID", "Low-frequency RFID tag tracking interface.", "rfid read", "id");
const sensorLive = appScene("SENSOR PROTOCOL", "Real-time readout from the 19 environmental sensor bank via I2C Mux.", "sensors live", "sensor");
const aiApp = appScene("HAILO-10H NPU", "Local AI hardware acceleration module inference monitoring.", "ai assist", "ai");
const powerApp = appScene("POWER SUBSYSTEM", "2S2P 18650 Battery management layout and telemetry.", "power status", "sys");

// Build System Menus
const rfMenu = menuScene("RF VAULT", [{ label: "HackRF One", onSelect: goto(sdrApp) }, { label: "SubGHz (CC1101)", onSelect: goto(subghzApp) }, { label: "BLE/WiFi Companion", onSelect: goto(bleApp) }]);
const idMenu = menuScene("ID DECK", [{ label: "PN532 NFC Module", onSelect: goto(nfcApp) }, { label: "125kHz RFID Reader", onSelect: goto(rfidApp) }]);
const sensorMenu = menuScene("SENSORS", [{ label: "Live Core Array", onSelect: goto(sensorLive) }]);
const aiMenu = menuScene("AI CORE", [{ label: "Hailo NPU Metrics", onSelect: goto(aiApp) }]);
const systemMenu = menuScene("SYSTEM POWER", [{ label: "BMS Power Budget", onSelect: goto(powerApp) }]);

const CATS = [
  { label: "RF VAULT", scene: rfMenu }, { label: "ID MODULES", scene: idMenu },
  { label: "SENSOR BANK", scene: sensorMenu }, { label: "AI INFERENCE", scene: aiMenu },
  { label: "POWER TREE", scene: systemMenu }
];
const mainMenu = { type: "home", title: "MAIN MENU", selected: 0 };

const bodyEl = document.getElementById("active-body");
const breadcrumbEl = document.getElementById("breadcrumb");
const consoleEl = document.getElementById("console");
const termInput = document.getElementById("term-input");
const ribbon = document.getElementById("ribbon");

// Core Asynchronous Logging Subsystem (Simulating central Redis/systemd bus)
function log(html) {
  const d = document.createElement("div");
  d.innerHTML = html;
  consoleEl.appendChild(d);
  consoleEl.scrollTop = consoleEl.scrollHeight;
}
function logSystem(unit, msg, status = "out") {
  log(`[<span class="prompt">${unit}</span>] <span class="${status}">${msg}</span>`);
}

// Interactive Screen Renderer
function render() {
  const scene = current();
  breadcrumbEl.textContent = stack.map(s => s.title).join("/");

  if (scene.type === "home") {
    bodyEl.innerHTML = `<div class="cat-grid" id="grid"></div>`;
    const grid = document.getElementById("grid");
    CATS.forEach((c, i) => {
      const t = document.createElement("div");
      t.className = "cat-tile" + (i === scene.selected ? " sel" : "");
      t.textContent = c.label;
      t.onclick = () => { scene.selected = i; pushScene(c.scene); };
      grid.appendChild(t);
    });
  } 
  else if (scene.type === "menu") {
    bodyEl.innerHTML = "";
    scene.items.forEach((it, i) => {
      const r = document.createElement("div");
      r.className = "row" + (i === scene.selected ? " sel" : "");
      r.innerHTML = `<span>${it.label}</span><span>&gt;</span>`;
      r.onclick = () => { scene.selected = i; it.onSelect(); };
      bodyEl.appendChild(r);
    });
  } 
  else if (scene.type === "app") {
    let telemetryHtml = "";
    
    if (scene.appType === "sensor") {
      telemetryHtml = `<div class="sensor-data-grid">` + 
        Object.entries(sensorReadings).map(([k,v]) => `<div class="sensor-line"><span>${k}</span><span class="sensor-val">${v}</span></div>`).join("") + 
        `</div>`;
    } else if (scene.appType === "power") {
      telemetryHtml = `
        <div class="sensor-line"><span>Pack Struct</span><span class="sensor-val">2S2P Li-ion</span></div>
        <div class="sensor-line"><span>True Capacity</span><span class="sensor-val">14,000 mAh</span></div>
        <div class="sensor-line"><span>Target Load</span><span class="sensor-val">${typicalDraw}W</span></div>
        <div class="sensor-line"><span>Est. Runtime</span><span class="sensor-val">${runtimeHours.toFixed(1)} Hours</span></div>
        <div class="sensor-line"><span>BMS Balance</span><span class="sensor-val">Optimal (Cell 1/2 Balanced)</span></div>
      `;
    } else if (scene.appType === "ai") {
      telemetryHtml = Object.entries(aiTelemetry).map(([k,v]) => `<div class="sensor-line"><span>${k}</span><span class="sensor-val">${v}</span></div>`).join("");
    } else if (scene.appType === "rf") {
      telemetryHtml = Object.entries(rfTelemetry).map(([k,v]) => `<div class="sensor-line"><span>${k}</span><span class="sensor-val">${v}</span></div>`).join("");
    } else {
      telemetryHtml = `<div class="state">PROCESS RUNNING... TELEMETRY NOMINAL</div>`;
    }

    bodyEl.innerHTML = `
      <div class="app-box">
        <div class="name">${scene.title}</div>
        <div class="desc">${scene.desc}</div>
        <div style="margin-top:4px; border-top:1px solid var(--border-light); padding-top:4px;">
          ${telemetryHtml}
        </div>
      </div>`;
  }
}

// Asynchronous Hardware Simulation Loops
setInterval(() => {
  // Drift environmental sensor metrics slightly
  sensorReadings["BME280 Temp"] = (24.0 + Math.random() * 0.5).toFixed(1) + "°C";
  sensorReadings["Geiger Counter"] = (0.12 + Math.random() * 0.04).toFixed(2) + " uSv/h";
  // Drift AI Metrics
  aiTelemetry["NPU Load"] = Math.floor(10 + Math.random() * 15) + "%";
  
  if (current().type === "app") render();
}, 2000);

// Clock and Battery Status Header Loop
setInterval(() => {
  const d = new Date();
  document.getElementById("topbar-clock").textContent = d.toTimeString().substring(0, 5);
  
  // Fake slow discharge calculation
  let chargePct = Math.max(10, Math.floor(84 + (d.getSeconds() / 10)));
  document.getElementById("topbar-batt").textContent = `BATT ${chargePct}%`;
}, 1000);

// Shell Interactive Input Handler Engine
const COMMANDS = {
  "help": () => logSystem("shell", "Commands: clear | help | hackrf analysis | sensors live | ai assist | power status | esp32 verify", "ok"),
  "clear": () => { consoleEl.innerHTML = ""; },
  "hackrf analysis": () => pushScene(sdrApp),
  "sensors live": () => pushScene(sensorLive),
  "ai assist": () => pushScene(aiApp),
  "power status": () => pushScene(powerApp),
  "esp32 verify": () => {
    logSystem("esp32-mcu", "Initializing recursive MicroSD offline test...", "out"); // [cite: 44, 45]
    setTimeout(() => logSystem("esp32-mcu", "Scanning sector clusters for blacklisted file extensions...", "out"), 800); // [cite: 46, 47]
    setTimeout(() => logSystem("esp32-mcu", "VERDICT: [SAFE] - Threat signatures zero. Core solid high.", "ok"), 2000); // [cite: 48, 49]
  }
};

termInput.addEventListener("keydown", (e) => {
  if (e.key === "Enter") {
    const rawInput = termInput.value.trim();
    if (!rawInput) return;
    
    log(`<span class="prompt">&gt;</span> <span class="cmd">${rawInput}</span>`);
    const sanitized = rawInput.toLowerCase();
    
    if (COMMANDS[sanitized]) {
      COMMANDS[sanitized]();
    } else {
      logSystem("systemd", `command not found: "${rawInput}". Type 'help'.`, "err");
    }
    termInput.value = "";
  }
});

// Dynamic Ribbon Tab Trigger
let ribbonOpen = false;
function toggleRibbon() {
  ribbonOpen = !ribbonOpen;
  ribbon.classList.toggle("open", ribbonOpen);
}

// Core Physical Inputs Mapper (Analog Pad / Keyboard Simulation Interface) 
document.addEventListener("keydown", (e) => {
  if (document.activeElement === termInput) {
    if (e.key === "Tab") { e.preventDefault(); toggleRibbon(); }
    return;
  }
  
  const scene = current();
  if (e.key === "Tab") { e.preventDefault(); toggleRibbon(); return; }
  if (e.key === "Backspace") { e.preventDefault(); popScene(); return; }

  if (scene.type === "home") {
    if (e.key === "ArrowRight") { scene.selected = (scene.selected + 1) % CATS.length; render(); }
    if (e.key === "ArrowLeft")  { scene.selected = (scene.selected - 1 + CATS.length) % CATS.length; render(); }
    if (e.key === "ArrowDown")  { scene.selected = (scene.selected + 2) % CATS.length; render(); }
    if (e.key === "ArrowUp")    { scene.selected = (scene.selected - 2 + CATS.length) % CATS.length; render(); }
    if (e.key === "Enter")      { pushScene(CATS[scene.selected].scene); }
  } 
  else if (scene.type === "menu") {
    if (e.key === "ArrowDown") { scene.selected = (scene.selected + 1) % scene.items.length; render(); }
    if (e.key === "ArrowUp")   { scene.selected = (scene.selected - 1 + scene.items.length) % scene.items.length; render(); }
    if (e.key === "Enter")     { scene.items[scene.selected].onSelect(); }
  }
});

// Bind UI Ribbon Controls to Execution Layer
document.querySelectorAll(".ribbon .pill").forEach((pill, idx) => {
  pill.onclick = () => {
    document.querySelectorAll(".ribbon .pill").forEach(p => p.classList.remove("active"));
    pill.classList.add("active");
    toggleRibbon();
    
    // Quick route mapping based on tabs
    if (idx === 0) pushScene(powerApp);
    if (idx === 1) pushScene(rfMenu);
    if (idx === 2) pushScene(sensorLive);
    if (idx === 3) pushScene(aiApp);
  };
});

// Bootstrap Initialization Sequences
logSystem("kernel", "Boot sequence validated. Mounting virtual framework paths...", "out");
logSystem("systemd", "Core daemon pipelines online, message network mapped via internal memory bus.", "ok");
pushScene(mainMenu);