import serial
import struct
import hashlib
import hmac
import base64
import time
import zlib
from Cryptodome.Cipher import AES
from Cryptodome.Util.Padding import pad, unpad
from Cryptodome.Random import get_random_bytes
import os

# ========== CONFIGURAZIONE ==========
SERIAL_PORT = "/dev/ttyUSB0"  # Porta seriale del tuo ESP32
BAUDRATE = 115200
WHITENING_KEY = 0x5A5A5A5A
SALT = b'esp32_salt_value'
AES_BLOCK_SIZE = 16

MASTER_KEY_FILE = "master_key.sec"
MASTER_KEY_PASSWORD = "StrongPasswordHere"  # Cambiare in produzione!

# ========== FUNZIONI CRITTOGRAFICHE ==========

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

def aes_gcm_encrypt(data: bytes, key: bytes) -> bytes:
    cipher = AES.new(key, AES.MODE_GCM)
    ciphertext, tag = cipher.encrypt_and_digest(data)
    return cipher.nonce + tag + ciphertext

def aes_gcm_decrypt(enc_data: bytes, key: bytes) -> bytes:
    nonce = enc_data[:12]
    tag = enc_data[12:28]
    ciphertext = enc_data[28:]
    cipher = AES.new(key, AES.MODE_GCM, nonce=nonce)
    return cipher.decrypt_and_verify(ciphertext, tag)

# ========== FUNZIONI PER LA MASTER KEY ==========

def save_master_key(master_key: bytes, password: str):
    key = hashlib.sha256(password.encode()).digest()
    cipher = AES.new(key, AES.MODE_GCM)
    ciphertext, tag = cipher.encrypt_and_digest(master_key)
    with open(MASTER_KEY_FILE, "wb") as f:
        f.write(cipher.nonce)
        f.write(tag)
        f.write(ciphertext)

def load_master_key(password: str) -> bytes | None:
    if not os.path.exists(MASTER_KEY_FILE):
        return None
    with open(MASTER_KEY_FILE, "rb") as f:
        nonce = f.read(12)
        tag = f.read(16)
        ciphertext = f.read()
    key = hashlib.sha256(password.encode()).digest()
    cipher = AES.new(key, AES.MODE_GCM, nonce=nonce)
    return cipher.decrypt_and_verify(ciphertext, tag)

# ========== FUNZIONI TRNG ==========

def request_random_from_esp32(ser) -> int:
    ser.write(b"r")  # Comando per ricevere TRNG
    data = ser.read(8)  # 4 byte TRNG + 4 byte CRC32
    if len(data) != 8:
        raise ValueError("Errore nella ricezione del TRNG")

    rnd, crc = struct.unpack("<II", data)
    computed_crc = zlib.crc32(struct.pack("<I", rnd)) & 0xFFFFFFFF

    if crc != computed_crc:
        raise ValueError(f"CRC mismatch: ricevuto {crc:08X}, calcolato {computed_crc:08X}")

    return rnd

def generate_master_key_from_esp32(password: str) -> bytes:
    ser = serial.Serial(SERIAL_PORT, BAUDRATE, timeout=1)
    time.sleep(2)  # Attesa inizializzazione ESP32

    try:
        raw = request_random_from_esp32(ser)
    finally:
        ser.close()

    whitened = whitening(raw, WHITENING_KEY)
    hashed = sha256_hash(whitened)

    entropy_pool = bytearray(32)
    entropy_pool = update_entropy_pool(entropy_pool, hashed)

    derived_key = hkdf_extract_expand(entropy_pool, SALT)
    mixed = mix_with_time(whitened)
    final_bytes = struct.pack(">I", mixed)
    encrypted = aes_gcm_encrypt(final_bytes, derived_key)

    master_key = derived_key[:32]
    save_master_key(master_key, password)

    print(f"[DEBUG] TRNG:     0x{raw:08X}")
    print(f"[DEBUG] Whitened: 0x{whitened:08X}")
    print(f"[DEBUG] Mixed:    0x{mixed:08X}")
    print(f"[DEBUG] Master Key salvata (hex): {master_key.hex()}")

    return master_key

# ========== MAIN ==========

if __name__ == "__main__":
    mk = load_master_key(MASTER_KEY_PASSWORD)
    if mk is None:
        print("Master Key non trovata. Generazione da ESP32 in corso...")
        mk = generate_master_key_from_esp32(MASTER_KEY_PASSWORD)
    else:
        print("Master Key caricata da file.")

    print(f"[INFO] Master Key finale (hex): {mk.hex()}")
