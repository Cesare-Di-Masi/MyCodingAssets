import requests

BASE_URL = "http://web-11.challs.olicyber.it"

# Credenziali di login
login_payload = {
    "username": "admin",
    "password": "admin"
}

# Sessione per gestire cookie
session = requests.Session()

# Effettua il login
login_response = session.post(f"{BASE_URL}/login", json=login_payload)
if login_response.status_code != 200:
    raise Exception(f"Login fallito con status {login_response.status_code}")

login_json = login_response.json()
csrf_token = login_json.get("csrf")
if not csrf_token:
    raise Exception("Token CSRF mancante nella risposta di login")

print(f"[INFO] Login riuscito. CSRF token iniziale: {csrf_token}")
flag=''
flag_pieces = []

for i in range(4):
    # Prova POST con token CSRF nel body JSON
    print(f"[INFO] Tentativo POST a /flag_piece?index={i} con CSRF nel JSON body")
    post_resp = session.post(f"{BASE_URL}/flag_piece?index={i}", json={"csrf": csrf_token})

    if post_resp.status_code == 200:
        piece = post_resp.text.strip()
        print(f"[SUCCESS] Pezzo {i} ricevuto via POST: {piece}")
        flag_pieces.append(piece)
        try:
            new_csrf = post_resp.json().get("csrf")
            if new_csrf:
                csrf_token = new_csrf
                print(f"[INFO] Token CSRF aggiornato: {csrf_token}")
        except Exception:
            pass
        continue

    # Se POST fallisce, prova GET con token CSRF come query param
    print(f"[INFO] Tentativo GET a /flag_piece con index={i} e CSRF come query param")
    get_resp = session.get(f"{BASE_URL}/flag_piece", params={"index": i, "csrf": csrf_token})

    if get_resp.status_code == 200:
        piece = get_resp.text.strip()
        print(f"[SUCCESS] Pezzo {i} ricevuto via GET: {piece}")
        flag_pieces.append(piece)
        flag += piece
        try:
            new_csrf = get_resp.json().get("csrf")
            if new_csrf:
                csrf_token = new_csrf
                print(f"[INFO] Token CSRF aggiornato: {csrf_token}")
        except Exception:
            pass
        continue

    raise Exception(f"Errore nel recupero del pezzo {i}: status {post_resp.status_code}, {get_resp.status_code}")

print("\nFLAG COMPLETA:", flag)
