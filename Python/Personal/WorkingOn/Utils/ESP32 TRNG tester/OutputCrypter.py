import serial
import struct
import hashlib
import hmac
import base64
import time
import zlib
from Cryptodome.Cipher import AES
from Cryptodome.Util.Padding import pad

# ========== CONFIGURAZIONE ==========
SERIAL_PORT = "/dev/ttyUSB0"  # Modifica secondo necessità (es. COM3 su Windows)
BAUDRATE = 115200
WHITENING_KEY = 0x5A5A5A5A
SALT = b'esp32_salt_value'
AES_BLOCK_SIZE = 16
MASTER_KEY = hashlib.sha256("TEST".encode()).digest()  # Placeholder. Leggilo da file in prod.

# ========== FUNZIONI BASE ==========

def whitening(raw_value: int, whitening_key: int) -> int:
    return raw_value ^ whitening_key

def sha256_hash(value: int) -> bytes:
    raw_bytes = struct.pack(">I", value)
    return hashlib.sha256(raw_bytes).digest()

def update_entropy_pool(pool: bytearray, new_hash: bytes) -> bytearray:
    combined = hashlib.sha256(pool + new_hash).digest()
    return bytearray(combined)

def hkdf_extract_expand(key_material: bytes, salt: bytes, length: int = 32) -> bytes:
    prk = hmac.new(salt, key_material, hashlib.sha256).digest()
    okm = b""
    previous = b""
    for i in range(1, -(-length // 32) + 1):
        previous = hmac.new(prk, previous + bytes([i]), hashlib.sha256).digest()
        okm += previous
    return okm[:length]

def mix_with_time(value: int) -> int:
    current_millis = int(time.time() * 1000)
    return (value ^ (current_millis & 0xFFFFFFFF)) & 0xFFFFFFFF

def aes_encrypt(data: bytes, key: bytes) -> bytes:
    cipher = AES.new(key, AES.MODE_ECB)
    return cipher.encrypt(pad(data, AES_BLOCK_SIZE))

# ========== FUNZIONE PRINCIPALE ==========

def request_random_from_esp32(ser) -> int:
    ser.write(b"r")  # Invia comando per richiedere valore
    data = ser.read(8)  # 4 byte TRNG + 4 byte CRC32
    if len(data) != 8:
        raise ValueError("Errore nella ricezione dei dati")

    rnd, crc = struct.unpack("<II", data)
    # Verifica CRC32 (little-endian come sull’ESP32)
    computed_crc = 0xFFFFFFFF
    computed_crc = struct.unpack("<I", struct.pack("<I", zlib.crc32(struct.pack("<I", rnd), computed_crc)))[0]
    
    if crc != computed_crc:
        raise ValueError("CRC mismatch")

    return rnd

def generate_secure_output_from_esp32():
    ser = serial.Serial(SERIAL_PORT, BAUDRATE, timeout=1)
    time.sleep(2)  # Per inizializzazione seriale

    raw = request_random_from_esp32(ser)
    ser.close()

    whitened = whitening(raw, WHITENING_KEY)
    hashed = sha256_hash(whitened)
    entropy_pool = bytearray(32)
    entropy_pool = update_entropy_pool(entropy_pool, hashed)
    derived_key = hkdf_extract_expand(entropy_pool, SALT)
    mixed = mix_with_time(whitened)
    final_bytes = struct.pack(">I", mixed)
    encrypted = aes_encrypt(final_bytes, MASTER_KEY)
    encoded_output = base64.b64encode(encrypted).decode('utf-8')

    print("[DEBUG] TRNG:          0x%08X" % raw)
    print("[DEBUG] Whitened:      0x%08X" % whitened)
    print("[DEBUG] Mixed:         0x%08X" % mixed)
    print("[DEBUG] Encrypted HEX: ", encrypted.hex())
    print("[OUTPUT] Base64:       ", encoded_output)

    return encoded_output

# ========== MAIN ==========
if __name__ == "__main__":
    generate_secure_output_from_esp32()
