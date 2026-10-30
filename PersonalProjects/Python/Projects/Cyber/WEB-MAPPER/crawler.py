import requests
from bs4 import BeautifulSoup
from urllib.parse import urljoin, urlparse
import tldextract
import re
import networkx as nx
import matplotlib.pyplot as plt
import time

# === CONFIGURAZIONE ===
START_URL = "https://it.wikipedia.org/wiki/Giovanni_De_Min_(pittore)"
HEADERS = {"User-Agent": "Mozilla/5.0 (compatible; FullLinkMapper/1.0)"}
TIMEOUT = 0.1
MAX_DEPTH = 2
SHOW_GRAPH = True
# ======================

visited = set()
graph = nx.DiGraph()
domain_root = None

def normalize_url(base, link):
    link = link.strip()
    if not link:
        return None
    joined = urljoin(base, link)
    parsed = urlparse(joined)
    if not parsed.scheme.startswith("http"):
        return None
    clean = f"{parsed.scheme}://{parsed.netloc}{parsed.path}".rstrip('/')
    return clean

def extract_links_from_html(url, html):
    soup = BeautifulSoup(html, "html.parser")
    links = set()

    # 1. <a href>
    for tag in soup.find_all('a', href=True):
        normalized = normalize_url(url, tag['href'])
        if normalized:
            links.add(normalized)

    # 2. <form action>
    for tag in soup.find_all('form', action=True):
        normalized = normalize_url(url, tag['action'])
        if normalized:
            links.add(normalized)

    # 3. <iframe/src>, <frame/src>
    for tag in soup.find_all(['iframe', 'frame'], src=True):
        normalized = normalize_url(url, tag['src'])
        if normalized:
            links.add(normalized)

    # 4. <object data> if .html
    for tag in soup.find_all('object', data=True):
        normalized = normalize_url(url, tag['data'])
        if normalized and normalized.endswith('.html'):
            links.add(normalized)

    text = soup.decode()

    # 5. meta-refresh
    for tag in soup.find_all('meta', attrs={"http-equiv": lambda v: v and v.lower()=="refresh"}):
        content = tag.get("content", "")
        m = re.search(r'url=([^;]+)', content, re.IGNORECASE)
        if m:
            normalized = normalize_url(url, m.group(1))
            if normalized:
                links.add(normalized)

    # 6. commenti HTML
    for comment in soup.find_all(text=lambda t: isinstance(t, type(soup.comment))):
        for m in re.findall(r'href=["\']([^"\']+)["\']', comment):
            normalized = normalize_url(url, m)
            if normalized:
                links.add(normalized)

    # 7. script inline / JSON / attributi data-*
    for tag in soup.find_all(['script', True]):
        raw = ''
        if tag.name == 'script' and tag.string:
            raw = tag.string
        attrs = ' '.join(f'{k}="{v}"' for k,v in tag.attrs.items())
        raw += attrs
        for m in re.findall(r'["\'](/[^"\']*\.html?)["\']', raw):
            normalized = normalize_url(url, m)
            if normalized:
                links.add(normalized)
        for m in re.findall(r'["\'](https?://[^"\']*\.html?)["\']', raw):
            normalized = normalize_url(url, m)
            if normalized:
                links.add(normalized)

    return links

def is_internal_page(url):
    parsed = tldextract.extract(url)
    return f"{parsed.domain}.{parsed.suffix}" == domain_root

def crawl(url, depth):
    if depth <= 0 or url in visited:
        return
    visited.add(url)
    print(f"Crawling ({len(visited)}): {url}")

    try:
        resp = requests.get(url, headers=HEADERS, timeout=TIMEOUT)
        ctype = resp.headers.get('Content-Type', '')
        if 'text/html' not in ctype:
            return
        links = extract_links_from_html(url, resp.text)
        for link in links:
            graph.add_edge(url, link)
            if is_internal_page(link):
                crawl(link, depth - 1)
    except Exception as e:
        print(f"Errore su {url}: {e}")

def visualize_graph():
    plt.figure(figsize=(20, 14))
    pos = nx.spring_layout(graph, k=0.15)
    nx.draw_networkx_nodes(graph, pos, node_size=60, node_color='lightblue')
    nx.draw_networkx_edges(graph, pos, arrows=True, edge_color='gray', width=0.8)
    nx.draw_networkx_labels(graph, pos, font_size=6)
    plt.title("Mappa completa dei collegamenti HTML interni")
    plt.axis('off')
    plt.tight_layout()
    plt.show()

def main():
    global domain_root
    parsed = tldextract.extract(START_URL)
    domain_root = f"{parsed.domain}.{parsed.suffix}"
    print(f"[INFO] Dominio: {domain_root}")
    start = time.time()
    crawl(START_URL, MAX_DEPTH)
    print(f"\n[INFO] Pagine visitate: {len(visited)}")
    print(f"[INFO] Collegamenti trovati: {len(graph.edges)}")
    print(f"[INFO] Tempo totale: {time.time() - start:.2f}s")
    if SHOW_GRAPH:
        visualize_graph()

if __name__ == "__main__":
    main()
