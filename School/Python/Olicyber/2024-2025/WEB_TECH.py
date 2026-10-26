import requests
from bs4 import BeautifulSoup
from collections import deque
from urllib.parse import urljoin
import re

base_url = "http://web-16.challs.olicyber.it/"
start_url = base_url

visited_urls = set()
urls_to_visit = deque([start_url])

headers = {"User-Agent": "Mozilla/5.0"}

while urls_to_visit:
    current_url = urls_to_visit.popleft()

    if current_url in visited_urls:
        continue

    print(f"Visiting: {current_url}")
    visited_urls.add(current_url)

    try:
        response = requests.get(current_url, headers=headers, timeout=5)
        response.raise_for_status()

        soup = BeautifulSoup(response.content, 'html.parser')

        h1_tag = soup.find('h1')
        if h1_tag:
            match = re.search(r'flag\{.*?\}', h1_tag.text)
            if match:
                print("\n--- FLAG TROVATA ---")
                print(f"URL: {current_url}")
                print(f"FLAG: {match.group(0)}")
                break

        for a_tag in soup.find_all('a', href=True):
            next_url = urljoin(current_url, a_tag['href'])
            if next_url not in visited_urls:
                urls_to_visit.append(next_url)

    except requests.RequestException as e:
        print(f"[!] Errore durante la richiesta a {current_url}: {e}")
