import requests
from bs4 import BeautifulSoup

# URL della pagina da analizzare
base_url = 'http://web-15.challs.olicyber.it/'

# Funzione per scaricare contenuto dato un URL assoluto o relativo
def download_resource(url):
    try:
        response = requests.get(url)
        if response.status_code == 200:
            return response.text
    except Exception as e:
        print(f"Errore nel download di {url}: {e}")
    return None

# Funzione principale
def find_flag_in_resources():
    # Scarica la pagina HTML principale
    main_page = download_resource(base_url)
    if not main_page:
        print("Errore nel download della pagina principale.")
        return
    
    soup = BeautifulSoup(main_page, 'html.parser')
    
    # Estrai link a risorse esterne da <link href=...>
    links = soup.find_all('link', href=True)
    # Estrai link a script esterni da <script src=...>
    scripts = soup.find_all('script', src=True)
    
    # Costruzione lista URL risorse esterne
    resources = []
    
    # Gestione URL relativi: li convertiamo in assoluti
    from urllib.parse import urljoin
    
    for tag in links:
        href = tag.get('href')
        full_url = urljoin(base_url, href)
        resources.append(full_url)
    
    for tag in scripts:
        src = tag.get('src')
        full_url = urljoin(base_url, src)
        resources.append(full_url)
    
    # Rimuovi duplicati (se presenti)
    resources = list(set(resources))
    
    # Cerca la stringa "flag{" in ogni risorsa
    for url in resources:
        print(f"Controllo risorsa: {url}")
        content = download_resource(url)
        if content and "flag{" in content:
            start = content.find("flag{")
            # estrai flag fino alla prima chiusura graffa "}"
            end = content.find("}", start)
            if end != -1:
                flag = content[start:end+1]
                print(f"FLAG trovata in {url}: {flag}")
                return flag
    
    print("Flag non trovata nelle risorse esterne.")
    return None

if __name__ == "__main__":
    find_flag_in_resources()
