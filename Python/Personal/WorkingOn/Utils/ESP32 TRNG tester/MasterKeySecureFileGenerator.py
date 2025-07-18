from Cryptodome.Cipher import AES
from Cryptodome.Random import get_random_bytes
import hashlib

# === INPUT ===
password = "Test"
master_key = get_random_bytes(32)  # oppure leggi da file, o imposta a mano

# === DERIVAZIONE CHIAVE DA PASSWORD ===
key = hashlib.sha256(password.encode()).digest()

# === CIFRATURA AES-GCM ===
cipher = AES.new(key, AES.MODE_GCM)
ciphertext, tag = cipher.encrypt_and_digest(master_key)

# === COSTRUISCI FILE BINARIO ===
with open("master_key.sec", "wb") as f:
    f.write(cipher.nonce)      # 12 byte
    f.write(tag)               # 16 byte
    f.write(ciphertext)        # 32 byte

print("Master Key salvata in 'master_key.sec' (cifrata)")
print("Master Key (hex):", master_key.hex())
