import serial
import time
import matplotlib.pyplot as plt
from collections import Counter

PORT = '/dev/ttyUSB0'     # Cambia con la tua porta, es: 'COM4' su Windows
BAUDRATE = 115200
NUM_BYTES = 10000         # Quanti byte vuoi leggere

def read_bytes_from_esp32(port, baudrate, num_bytes):
    with serial.Serial(port, baudrate, timeout=5) as ser:
        data = bytearray()
        print(f"[+] Reading {num_bytes} bytes from ESP32...")
        while len(data) < num_bytes:
            if ser.in_waiting:
                data.extend(ser.read(ser.in_waiting))
        print("[+] Done.")
        return data[:num_bytes]

def analyze_distribution(data):
    counter = Counter(data)
    values = list(range(256))
    counts = [counter.get(i, 0) for i in values]

    plt.bar(values, counts, width=1.0, edgecolor='black')
    plt.title('Byte Frequency Distribution (0–255)')
    plt.xlabel('Byte Value')
    plt.ylabel('Frequency')
    plt.grid(True)
    plt.show()

    entropy = -sum((c / len(data)) * (c / len(data)).bit_length() for c in counts if c > 0)
    print(f"[i] Estimated entropy: {entropy:.4f} bits/byte (ideal = 8.000)")

if __name__ == "__main__":
    data = read_bytes_from_esp32(PORT, BAUDRATE, NUM_BYTES)
    analyze_distribution(data)
