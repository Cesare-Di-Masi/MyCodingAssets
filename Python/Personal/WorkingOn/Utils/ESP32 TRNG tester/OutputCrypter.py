import hashlib
import hmac
import base64
import time
import struct
import secrets
from Cryptodome.Cipher import AES
from Cryptodome.Util.Padding import pad

# =============================
# CONFIGURAZIONE
# =============================


WHITENING_KEY = 0x5A5A5A5A
MASTER_KEY = hashlib.sha256("TEST".encode()).digest()  # 32-byte key
SALT = b'esp32_salt_value'
AES_BLOCK_SIZE = 16


# =============================
# FUNZIONI PRINCIPALI
# =============================

def get_esp32_random():
    """Simula un valore casuale hardware a 32 bit"""
    return secrets.randbits(32)

def whitening(raw_value: int, whitening_key: int) -> int:
    """XOR tra l'output del TRNG e una chiave fissa per aumentare la dispersione"""
    return raw_value ^ whitening_key

def sha256_hash(value: int) -> bytes:
    """SHA256 sul valore whitened, usato per aggiornare il pool entropico"""
    raw_bytes = struct.pack(">I", value)
    return hashlib.sha256(raw_bytes).digest()

def update_entropy_pool(pool: bytearray, new_hash: bytes) -> bytearray:
    """Pool entropico aggiornato con hash freschi"""
    combined = hashlib.sha256(pool + new_hash).digest()
    return bytearray(combined)

def hkdf_extract_expand(key_material: bytes, salt: bytes, length: int = 32) -> bytes:
    """Deriva una chiave secondaria (usabile per nested encryption)"""
    prk = hmac.new(salt, key_material, hashlib.sha256).digest()
    okm = b""
    previous = b""
    for i in range(1, -(-length // 32) + 1):
        previous = hmac.new(prk, previous + bytes([i]), hashlib.sha256).digest()
        okm += previous
    return okm[:length]

def mix_with_time(value: int) -> int:
    """XOR con il timestamp in ms corrente per introdurre entropia temporale"""
    current_millis = int(time.time() * 1000)
    mixed = value ^ (current_millis & 0xFFFFFFFF)
    return mixed & 0xFFFFFFFF

def aes_encrypt(data: bytes, key: bytes) -> bytes:
    """Cifratura AES con padding (ECB mode semplificato per demo)"""
    cipher = AES.new(key, AES.MODE_ECB)
    return cipher.encrypt(pad(data, AES_BLOCK_SIZE))


# =============================
# LOGICA COMPLETA
# =============================

def generate_secure_output():
    # STEP 0: ESP32 TRNG simulato
    raw = get_esp32_random()

    # STEP 1: Whitening
    whitened = whitening(raw, WHITENING_KEY)

    # STEP 2: Hash SHA-256
    hashed = sha256_hash(whitened)

    # STEP 3: Aggiorna Entropy Pool
    entropy_pool = bytearray(32)
    entropy_pool = update_entropy_pool(entropy_pool, hashed)

    # STEP 4: HKDF su pool
    derived_key = hkdf_extract_expand(entropy_pool, SALT)

    # STEP 5: Mixing con timestamp
    mixed = mix_with_time(whitened)

    # STEP 6: Cifratura AES con MASTER KEY
    final_bytes = struct.pack(">I", mixed)  # 4 byte
    encrypted = aes_encrypt(final_bytes, MASTER_KEY)

    # STEP 7: Output base64 (leggibile da C#)
    encoded_output = base64.b64encode(encrypted).decode('utf-8')

    # DEBUG opzionale
    print("[DEBUG] Raw TRNG:       0x%08X" % raw)
    print("[DEBUG] Whitened:       0x%08X" % whitened)
    print("[DEBUG] SHA256 Hash:    ", hashed.hex())
    print("[DEBUG] Entropy Pool:   ", entropy_pool.hex())
    print("[DEBUG] HKDF Key:       ", derived_key.hex())
    print("[DEBUG] Mixed w/ Time:  0x%08X" % mixed)
    print("[DEBUG] AES Encrypted:  ", encrypted.hex())
    print("[OUTPUT] Base64 Output: ", encoded_output)

    return encoded_output


# =============================
# MAIN (invocabile da C# via subprocess o socket)
# =============================

if __name__ == "__main__":
    generate_secure_output()
