#include <Arduino.h>
#include "esp_system.h"
#include "esp_crc.h" // Per CRC32

#define OUTPUT_INTERVAL_MS 10  // Output automatico ogni 10ms
#define MODE_AUTO true         // true: continuo | false: su comando
#define OUTPUT_HEX true        // true: stampa hex | false: binario raw

unsigned long lastOutputTime = 0;

void printHexWithCRC(uint32_t rnd) {
  uint32_t crc = esp_crc32_le(UINT32_MAX, (const uint8_t*)&rnd, sizeof(rnd));
  Serial.printf("Random: 0x%08X | CRC32: 0x%08X\n", rnd, crc);
}

void sendRawWithCRC(uint32_t rnd) {
  uint32_t crc = esp_crc32_le(UINT32_MAX, (const uint8_t*)&rnd, sizeof(rnd));
  Serial.write((uint8_t*)&rnd, sizeof(rnd));
  Serial.write((uint8_t*)&crc, sizeof(crc));
}

void setup() {
  Serial.begin(115200);
  while (!Serial); // Attesa opzionale
  Serial.println("[ESP32] TRNG Ready.");
  if (MODE_AUTO) Serial.println("[MODE] Output automatico attivo.");
  else Serial.println("[MODE] Attendi comando 'r' su seriale.");
}

void loop() {
  // Modalità comando
  if (!MODE_AUTO && Serial.available()) {
    char cmd = Serial.read();
    if (cmd == 'r') {
      uint32_t rnd = esp_random();
      if (OUTPUT_HEX) printHexWithCRC(rnd);
      else sendRawWithCRC(rnd);
    }
  }

  // Modalità automatica
  if (MODE_AUTO && millis() - lastOutputTime >= OUTPUT_INTERVAL_MS) {
    lastOutputTime = millis();
    uint32_t rnd = esp_random();
    if (OUTPUT_HEX) printHexWithCRC(rnd);
    else sendRawWithCRC(rnd);
  }
}
