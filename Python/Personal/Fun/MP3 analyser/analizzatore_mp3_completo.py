import librosa
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import os

def analizza_mp3(file_path, time_resolution=0.01):
    """
    Analizza un file MP3 e restituisce dati dettagliati su volume, beat, note e altre features.
    Salva i risultati in un file CSV.

    Args:
        file_path (str): Il percorso del file MP3 da analizzare.
        time_resolution (float): La risoluzione temporale in secondi (default: 0.01).

    Returns:
        tuple: Un DataFrame di pandas con i dati analizzati e il tempo (BPM) rilevato.
    """
    print(f"Caricamento e analisi del file: {file_path}...")
    y, sr = librosa.load(file_path, sr=44100, mono=True)
    hop_length = int(sr * time_resolution)

    # --- Estrazione delle Features ---

    # Volume (RMS Energy)
    rms = librosa.feature.rms(y=y, hop_length=hop_length)[0]

    # Beat e Tempo
    tempo, beats_frames = librosa.beat.beat_track(y=y, sr=sr, hop_length=hop_length)
    beats_times = librosa.frames_to_time(beats_frames, sr=sr, hop_length=hop_length)

    # Note (Pitch) tramite Chroma Feature
    chroma = librosa.feature.chroma_stft(y=y, sr=sr, hop_length=hop_length)

    # Spettroide (Brillantezza)
    spectral_centroids = librosa.feature.spectral_centroid(y=y, sr=sr, hop_length=hop_length)[0]

    # --- NUOVE FEATURES ---
    
    # 1. Onset (Inizio Eventi Musicali)
    onset_frames = librosa.onset.onset_detect(y=y, sr=sr, hop_length=hop_length)
    onset_times = librosa.frames_to_time(onset_frames, sr=sr, hop_length=hop_length)

    # 2. Spectral Rolloff (Frequenza di taglio dell'energia)
    # Usiamo l'85% come soglia standard
    spectral_rolloff = librosa.feature.spectral_rolloff(y=y, sr=sr, hop_length=hop_length, roll_percent=0.85)[0]

    # 3. Zero-Crossing Rate (ZCR)
    zcr = librosa.feature.zero_crossing_rate(y=y, hop_length=hop_length)[0]

    # --- Assemblaggio del DataFrame ---
    num_frames = chroma.shape[1]
    times = librosa.frames_to_time(np.arange(num_frames), sr=sr, hop_length=hop_length)

    data = {
        'Volume (RMS)': rms,
        'Spettroide (Hz)': spectral_centroids,
        'Rolloff Spettrale (Hz)': spectral_rolloff, # Nuova feature
        'Tasso Passaggio Zero (ZCR)': zcr,           # Nuova feature
    }
    
    # Aggiungi le colonne per le 12 note del chroma
    note_names = ['Do', 'Do#', 'Re', 'Re#', 'Mi', 'Fa', 'Fa#', 'Sol', 'Sol#', 'La', 'La#', 'Si']
    for i, note_name in enumerate(note_names):
        data[f'Nota_{note_name}'] = chroma[i, :]

    df = pd.DataFrame(data, index=np.round(times, 2))
    df.index.name = 'Tempo (s)'

    # Aggiungi colonne booleane per beat e onset
    df['is_beat'] = False
    df['is_onset'] = False # Nuova feature
    
    beats_times_rounded = np.round(beats_times, 2)
    onset_times_rounded = np.round(onset_times, 2)
    
    for beat_time in beats_times_rounded:
        if beat_time in df.index:
            df.loc[beat_time, 'is_beat'] = True
            
    for onset_time in onset_times_rounded:
        if onset_time in df.index:
            df.loc[onset_time, 'is_onset'] = True
            
    print("Analisi completata.")
    return df, tempo

def salva_dati_csv(df, input_path):
    """Salva il DataFrame in un file CSV."""
    base_name = os.path.splitext(os.path.basename(input_path))[0]
    output_filename = f"{base_name}_analysis.csv"
    
    try:
        df.to_csv(output_filename)
        print(f"\nDati salvati con successo nel file: '{output_filename}'")
        print("Puoi aprirlo con Excel, Google Sheets o qualsiasi editor di testo.")
    except Exception as e:
        print(f"Errore durante il salvataggio del file CSV: {e}")

def visualizza_dati(df, tempo, mp3_path):
    """Crea un grafico per visualizzare i dati estratti."""
    print("\nGenerazione del grafico...")
    
    fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(15, 10), sharex=True)
    
    # Grafico 1: Volume, Beat e Onset
    ax1.plot(df.index, df['Volume (RMS)'], label='Volume (RMS)', color='tab:blue', alpha=0.8)
    ax1.set_ylabel('Volume (RMS)', color='tab:blue')
    ax1.tick_params(axis='y', labelcolor='tab:blue')
    # CORREZIONE: Convertiamo 'tempo' in float standard prima di formattarlo
    ax1.set_title(f'Analisi Audio di: {os.path.basename(mp3_path)} (Tempo: {float(tempo):.2f} BPM)')
    
    # Evidenzia i beat
    beat_times = df[df['is_beat']].index
    for beat_time in beat_times:
        ax1.axvline(x=beat_time, color='red', linestyle='--', linewidth=0.8, alpha=0.7, label='Beat' if beat_time == beat_times.iloc[0] else "")
        
    # Evidenzia gli onset
    onset_times = df[df['is_onset']].index
    ax1.scatter(onset_times, df.loc[onset_times, 'Volume (RMS)'], color='green', s=20, alpha=0.8, label='Onset', zorder=5)
    
    ax1.legend(loc='upper right')
    ax1.grid(True, linestyle=':', alpha=0.6)

    # Grafico 2: Timbre (Spettroide, Rolloff, ZCR)
    ax2.plot(df.index, df['Spettroide (Hz)'], label='Spettroide (Hz)', color='tab:orange')
    ax2.plot(df.index, df['Rolloff Spettrale (Hz)'], label='Rolloff Spettrale (Hz)', color='tab:purple', linestyle=':')
    ax2.set_ylabel('Frequenza (Hz)', color='tab:orange')
    ax2.tick_params(axis='y', labelcolor='tab:orange')
    
    # Aggiungiamo l'asse Y per lo ZCR sull'altro lato
    ax3 = ax2.twinx()
    ax3.plot(df.index, df['Tasso Passaggio Zero (ZCR)'], label='Tasso Passaggio Zero (ZCR)', color='tab:brown', linestyle='-.')
    ax3.set_ylabel('ZCR', color='tab:brown')
    ax3.tick_params(axis='y', labelcolor='tab:brown')
    
    ax2.set_xlabel('Tempo (s)')
    ax2.legend(loc='upper left')
    ax3.legend(loc='upper right')
    ax2.grid(True, linestyle=':', alpha=0.6)
    
    fig.tight_layout(rect=[0, 0, 1, 0.96])
    plt.show()

# --- Esecuzione Principale ---
if __name__ == "__main__":
    # !!! MODIFICA QUESTA RIGA CON IL PERCORSO DEL TUO FILE MP3 !!!
    MP3_FILE_PATH = 'test.mp3' 

    try:
        dati_analisi, bpm_rilevato = analizza_mp3(MP3_FILE_PATH)

        # Stampa i risultati
        print(f"\n--- RIEPILOGO ---")
        # CORREZIONE: Convertiamo 'bpm_rilevato' in float standard prima di formattarlo
        print(f"Tempo rilevato: {float(bpm_rilevato):.2f} BPM")
        print(f"Risoluzione temporale: 0.01 secondi (10 ms)")
        # CORREZIONE: Convertiamo il risultato di .max() in float standard prima di formattarlo
        print(f"Durata totale analizzata: {float(dati_analisi.index.max()):.2f} secondi")
        print(f"Numero di Onset rilevati: {dati_analisi['is_onset'].sum()}")
        
        print("\n--- PRIMI 10 RIGHE DI DATI ---")
        print(dati_analisi.head(10).to_string())

        # Salva i dati in un file CSV
        salva_dati_csv(dati_analisi, MP3_FILE_PATH)
        
        # Visualizza i dati graficamente
        visualizza_dati(dati_analisi, bpm_rilevato, MP3_FILE_PATH)

    except FileNotFoundError:
        print(f"Errore: Il file non è stato trovato a '{MP3_FILE_PATH}'.")
        print("Per favore, modifica la variabile MP3_FILE_PATH nel codice.")
    except Exception as e:
        print(f"Si è verificato un errore durante l'analisi: {e}")
