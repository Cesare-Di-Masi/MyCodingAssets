import serial
import struct
import sys

def is_valid_utf16_char(code_point):
    # Escludi area surrogate D800-DFFF
    return 0 <= code_point <= 0xD7FF or 0xE000 <= code_point <= 0xFFFF

def uint32_to_utf16_chars(num):
    # Dividi il uint32 in due uint16 (due caratteri UTF-16)
    high = (num >> 16) & 0xFFFF
    low = num & 0xFFFF
    chars = []
    if is_valid_utf16_char(high):
        chars.append(chr(high))
    if is_valid_utf16_char(low):
        chars.append(chr(low))
    return chars

def read_esp32_serial(port="/dev/ttyUSB0", baudrate=115200, count=1000, output_file="sequenza_esp32_utf16.txt"):
    ser = serial.Serial(port, baudrate, timeout=1)
    print(f"Connected to {port} at {baudrate} baud. Reading {count} random numbers...")
    collected_chars = []

    try:
        numbers_read = 0
        while numbers_read < count:
            # ESP32 invia 4 byte uint32 random + 4 byte CRC32 (tot 8 byte)
            data = ser.read(8)
            if len(data) < 8:
                continue  # timeout o dati insufficienti

            rnd, crc = struct.unpack('<II', data)  # little endian uint32, uint32

            # Non controllo CRC qui, presuppongo correttezza o implementare controllo se serve

            chars = uint32_to_utf16_chars(rnd)
            collected_chars.extend(chars)

            numbers_read += 1

        # Scrivi su file UTF-16 con BOM
        with open(output_file, 'w', encoding='utf-16') as f:
            f.write(''.join(collected_chars))

        print(f"Generati {len(collected_chars)} caratteri UTF-16 e salvati su {output_file}")

    except KeyboardInterrupt:
        print("Interrotto dall'utente.")
    finally:
        ser.close()

if __name__ == "__main__":
    # Modifica porta seriale e numero di numeri da leggere se serve
    serial_port = "COM3"  # es. Windows, su Linux /dev/ttyUSB0 o /dev/ttyACM0
    read_count = 1000
    output_filename = "sequenza_esp32_utf16.txt"

    read_esp32_serial(serial_port, 115200, read_count, output_filename)
