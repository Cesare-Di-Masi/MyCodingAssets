import requests
from bs4 import BeautifulSoup
from urllib.parse import urljoin, urlparse
from collections import deque
import time
import networkx as nx
from pyvis.network import Network
import re
import os
import tldextract

class WebGraphCrawler:
    def __init__(self, start_url, max_pages=50, max_depth=2, delay=1):
        self.start_url = start_url
        self.domain = urlparse(start_url).netloc
        self.base_domain = tldextract.extract(start_url).registered_domain
        self.max_pages = max_pages
        self.max_depth = max_depth
        self.delay = delay
        self.graph = nx.DiGraph()
        self.visited = set()
        self.queue = deque()
        self.session = requests.Session()
        self.session.headers.update({'User-Agent': 'Mozilla/5.0'})
        self.script_dir = os.path.dirname(os.path.abspath(__file__))
        
        # Statistiche per debug
        self.total_links_found = 0
        self.links_skipped = {
            'external': 0,
            'files': 0,
            'special': 0,
            'duplicates': 0,
            'errors': 0
        }
        
    def normalize_url(self, url):
        """Normalizza l'URL per evitare duplicati"""
        url = re.sub(r'([?&])utm_[^&]*', '', url)
        url = re.sub(r'([?&])fbclid[^&]*', '', url)
        url = re.sub(r'([?&])gclid[^&]*', '', url)
        url = url.rstrip('/')
        parsed = urlparse(url)
        netloc = parsed.netloc.lower()
        path = parsed.path.lower()
        query = parsed.query
        normalized = parsed._replace(
            netloc=netloc,
            path=path,
            query=query,
            fragment=''
        ).geturl()
        return normalized
    
    def is_valid_url(self, url):
        try:
            parsed = urlparse(url)
            extracted = tldextract.extract(url)
            if extracted.registered_domain != self.base_domain:
                self.links_skipped['external'] += 1
                return False
            if re.match(r'^.*\.(pdf|jpg|jpeg|png|gif|css|js|zip|tar|gz|mp3|mp4|mov|avi|doc|docx|xls|xlsx|ppt|pptx)$', url.lower()):
                self.links_skipped['files'] += 1
                return False
            if any(x in url.lower() for x in [
                'javascript:', 'mailto:', 'tel:', 'sms:', 'whatsapp:', 
                'special:', 'action=edit', 'action=history', 'redlink=1'
            ]):
                self.links_skipped['special'] += 1
                return False
            if parsed.fragment and not parsed.path:
                return False
            return True
        except Exception as e:
            self.links_skipped['errors'] += 1
            return False

    def extract_links(self, url):
        max_retries = 3
        for attempt in range(max_retries):
            try:
                response = self.session.get(url, timeout=15)
                response.raise_for_status()
                soup = BeautifulSoup(response.text, 'html.parser')
                
                links = set()
                for a_tag in soup.find_all('a', href=True):
                    href = a_tag['href'].strip()
                    if not href or href.startswith('#'):
                        continue
                    full_url = urljoin(url, href)
                    normalized_url = self.normalize_url(full_url)
                    if self.is_valid_url(normalized_url):
                        links.add(normalized_url)
                
                self.total_links_found += len(links)
                return links
            except requests.exceptions.RequestException as e:
                if attempt == max_retries - 1:
                    self.links_skipped['errors'] += 1
                    return set()
                time.sleep(2 ** attempt)
        return set()

    def crawl(self):
        self.queue.append((self.start_url, 0))
        normalized_start = self.normalize_url(self.start_url)
        self.graph.add_node(normalized_start, label=self.start_url.split('/')[-1] or 'Home')
        
        while self.queue and len(self.visited) < self.max_pages:
            current_url, depth = self.queue.popleft()
            normalized_url = self.normalize_url(current_url)
            
            if normalized_url in self.visited or depth > self.max_depth:
                continue
                
            self.visited.add(normalized_url)
            print(f"\n🕷️  Analizzando: {current_url} (Profondità: {depth})")
            print(f"📊 Link trovati finora: {self.total_links_found}")
            
            links = self.extract_links(current_url)
            print(f"✅ Link validi in questa pagina: {len(links)}")
            
            for link in links:
                if link not in self.graph:
                    self.graph.add_node(link, label=link.split('/')[-1] or 'Pagina')
                self.graph.add_edge(normalized_url, link)
                
                if link not in self.visited:
                    self.queue.append((link, depth + 1))
            
            if len(self.visited) % 5 == 0:
                self.print_stats()
            
            time.sleep(self.delay)
            
        return self.graph
    
    def print_stats(self):
        print("\n" + "📈" + "="*48)
        print("📊 STATISTICHE CRAWLING:")
        print(f"📄 Pagine visitate: {len(self.visited)}")
        print(f"🔗 Link totali trovati: {self.total_links_found}")
        print(f"❌ Link saltati:")
        for reason, count in self.links_skipped.items():
            print(f"  - {reason}: {count}")
        print("="*50 + "\n")

    def visualize(self, output_file="web_graph.html"):
        output_path = os.path.join(self.script_dir, output_file)
        self.print_stats()
        
        # Creazione del grafo con configurazione per tema scuro
        net = Network(
            height="900px",
            width="100%",
            directed=True,
            notebook=False,
            cdn_resources='remote',
            bgcolor="#222222",
            font_color="#ffffff"
        )
        
        # Configurazione avanzata per stile Obsidian
        net.set_options("""
        {
          "nodes": {
            "font": {
              "size": 14,
              "face": "Inter, sans-serif",
              "color": "#e0e0e0"
            },
            "shadow": {
              "enabled": true,
              "color": "rgba(0,0,0,0.5)",
              "size": 10,
              "x": 2,
              "y": 2
            },
            "shapeProperties": {
              "borderRadius": 8
            },
            "borderWidth": 2,
            "borderWidthSelected": 3
          },
          "edges": {
            "arrows": {
              "to": {
                "enabled": true,
                "scaleFactor": 0.8,
                "type": "arrow"
              }
            },
            "color": {
              "inherit": false,
              "color": "#555555",
              "highlight": "#888888",
              "hover": "#aaaaaa"
            },
            "smooth": {
              "type": "continuous",
              "roundness": 0.5
            },
            "width": 2,
            "shadow": {
              "enabled": true,
              "color": "rgba(0,0,0,0.3)",
              "size": 5,
              "x": 1,
              "y": 1
            }
          },
          "physics": {
            "hierarchicalRepulsion": {
              "centralGravity": 0.0,
              "springLength": 200,
              "springConstant": 0.01,
              "nodeDistance": 250,
              "damping": 0.09
            },
            "minVelocity": 0.75,
            "solver": "hierarchicalRepulsion",
            "stabilization": {
              "enabled": true,
              "iterations": 1000,
              "updateInterval": 50
            }
          },
          "interaction": {
            "hover": true,
            "tooltipDelay": 200,
            "hideEdgesOnDrag": true,
            "navigationButtons": true,
            "keyboard": true
          }
        }
        """)
        
        # Aggiungi i dati del grafo
        net.from_nx(self.graph)
        
        # Personalizza i nodi con palette Obsidian
        obsidian_colors = {
            'start': '#8b5cf6',  # Violetto per pagina iniziale
            'hub': '#3b82f6',    # Blu per hub
            'normal': '#10b981', # Verde per pagine normali
            'leaf': '#f59e0b'    # Ambra per foglie
        }
        
        for node in net.nodes:
            in_degree = self.graph.in_degree(node['id'])
            out_degree = self.graph.out_degree(node['id'])
            total_degree = in_degree + out_degree
            
            # Dimensione basata sul numero di connessioni
            node['size'] = 20 + min(total_degree * 1.5, 40)
            
            # Colore basato sul tipo di nodo
            if node['id'] == self.normalize_url(self.start_url):
                node['color'] = obsidian_colors['start']
                node['shape'] = 'star'
            elif in_degree > 5:
                node['color'] = obsidian_colors['hub']
                node['shape'] = 'diamond'
            elif out_degree > 10:
                node['color'] = obsidian_colors['hub']
                node['shape'] = 'triangle'
            elif total_degree == 1:
                node['color'] = obsidian_colors['leaf']
                node['shape'] = 'dot'
            else:
                node['color'] = obsidian_colors['normal']
                node['shape'] = 'dot'
                
            # Titolo con design Obsidian
            node['title'] = f"""
                <div style="background: #1e1e1e; padding: 12px; border-radius: 8px; border: 1px solid #333; max-width: 300px;">
                    <div style="font-size: 16px; font-weight: bold; color: #e0e0e0; margin-bottom: 8px;">
                        {node['label']}
                    </div>
                    <div style="font-size: 12px; color: #888; margin-bottom: 4px;">
                        URL: <span style="color: #aaa;">{node['id']}</span>
                    </div>
                    <div style="display: flex; justify-content: space-between; margin-top: 8px;">
                        <div style="color: #8b5cf6;">↩️ {in_degree}</div>
                        <div style="color: #3b82f6;">↪️ {out_degree}</div>
                        <div style="color: #10b981;">🔗 {total_degree}</div>
                    </div>
                </div>
            """
        
        # Salva il file HTML
        try:
            net.save_graph(output_path)
            print(f"\n✅ Grafo salvato con successo in: {output_path}")
            print(f"🌐 Apri il file nel browser per visualizzare il grafo")
        except Exception as e:
            print(f"❌ Errore nel salvataggio del grafo: {str(e)}")
            self.create_obsidian_html(output_path)

    def create_obsidian_html(self, output_path):
        """Crea un HTML con stile Obsidian"""
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <title>Web Graph - Obsidian Style</title>
            <style>
                @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
                
                * {{
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }}
                
                body {{
                    font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
                    background: #1a1a1a;
                    color: #e0e0e0;
                    overflow: hidden;
                }}
                
                .container {{
                    width: 100vw;
                    height: 100vh;
                    display: flex;
                    flex-direction: column;
                }}
                
                .header {{
                    background: #222222;
                    padding: 16px 24px;
                    border-bottom: 1px solid #333;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }}
                
                .title {{
                    font-size: 20px;
                    font-weight: 600;
                    color: #e0e0e0;
                    display: flex;
                    align-items: center;
                    gap: 10px;
                }}
                
                .obsidian-icon {{
                    width: 24px;
                    height: 24px;
                    background: linear-gradient(135deg, #8b5cf6, #3b82f6);
                    border-radius: 6px;
                }}
                
                .stats {{
                    display: flex;
                    gap: 20px;
                }}
                
                .stat {{
                    background: #2a2a2a;
                    padding: 8px 16px;
                    border-radius: 8px;
                    border: 1px solid #333;
                    display: flex;
                    align-items: center;
                    gap: 8px;
                }}
                
                .stat-value {{
                    font-weight: 600;
                    color: #8b5cf6;
                }}
                
                .controls {{
                    background: #222222;
                    padding: 12px 24px;
                    border-bottom: 1px solid #333;
                    display: flex;
                    gap: 12px;
                }}
                
                .btn {{
                    background: #2a2a2a;
                    color: #e0e0e0;
                    border: 1px solid #333;
                    padding: 8px 16px;
                    border-radius: 8px;
                    cursor: pointer;
                    font-size: 14px;
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    transition: all 0.2s;
                }}
                
                .btn:hover {{
                    background: #333;
                    border-color: #555;
                    transform: translateY(-1px);
                }}
                
                #graph {{
                    flex: 1;
                    background: radial-gradient(circle at center, #1a1a1a 0%, #0d0d0d 100%);
                    position: relative;
                }}
                
                .node {{
                    cursor: pointer;
                    transition: all 0.3s ease;
                }}
                
                .node:hover {{
                    filter: brightness(1.2);
                }}
                
                .node.start {{
                    fill: #8b5cf6 !important;
                    stroke: #a78bfa !important;
                }}
                
                .node.hub {{
                    fill: #3b82f6 !important;
                    stroke: #60a5fa !important;
                }}
                
                .node.normal {{
                    fill: #10b981 !important;
                    stroke: #34d399 !important;
                }}
                
                .node.leaf {{
                    fill: #f59e0b !important;
                    stroke: #fbbf24 !important;
                }}
                
                .link {{
                    stroke: #444 !important;
                    stroke-opacity: 0.6 !important;
                    stroke-width: 2px !important;
                    transition: all 0.3s ease;
                }}
                
                .link:hover {{
                    stroke: #666 !important;
                    stroke-opacity: 0.8 !important;
                    stroke-width: 3px !important;
                }}
                
                .tooltip {{
                    position: absolute;
                    padding: 16px;
                    background: #2a2a2a;
                    border: 1px solid #444;
                    border-radius: 12px;
                    box-shadow: 0 10px 25px rgba(0,0,0,0.5);
                    pointer-events: none;
                    opacity: 0;
                    transition: opacity 0.3s ease;
                    max-width: 320px;
                    z-index: 1000;
                }}
                
                .tooltip.visible {{
                    opacity: 1;
                }}
                
                .tooltip-title {{
                    font-size: 16px;
                    font-weight: 600;
                    color: #e0e0e0;
                    margin-bottom: 8px;
                }}
                
                .tooltip-url {{
                    font-size: 12px;
                    color: #888;
                    margin-bottom: 12px;
                    word-break: break-all;
                }}
                
                .tooltip-stats {{
                    display: grid;
                    grid-template-columns: repeat(3, 1fr);
                    gap: 8px;
                }}
                
                .tooltip-stat {{
                    background: #1a1a1a;
                    padding: 8px;
                    border-radius: 6px;
                    text-align: center;
                }}
                
                .tooltip-stat-value {{
                    font-size: 18px;
                    font-weight: 600;
                    margin-bottom: 2px;
                }}
                
                .tooltip-stat-label {{
                    font-size: 11px;
                    color: #666;
                }}
                
                .legend {{
                    position: absolute;
                    bottom: 20px;
                    left: 20px;
                    background: rgba(42, 42, 42, 0.9);
                    padding: 16px;
                    border-radius: 12px;
                    border: 1px solid #333;
                    backdrop-filter: blur(10px);
                }}
                
                .legend-title {{
                    font-size: 14px;
                    font-weight: 600;
                    margin-bottom: 12px;
                    color: #e0e0e0;
                }}
                
                .legend-item {{
                    display: flex;
                    align-items: center;
                    gap: 8px;
                    margin-bottom: 8px;
                }}
                
                .legend-color {{
                    width: 16px;
                    height: 16px;
                    border-radius: 50%;
                }}
                
                .legend-label {{
                    font-size: 13px;
                    color: #bbb;
                }}
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <div class="title">
                        <div class="obsidian-icon"></div>
                        Web Graph Explorer
                    </div>
                    <div class="stats">
                        <div class="stat">
                            <div class="stat-value">{len(self.graph.nodes)}</div>
                            <div>Pagine</div>
                        </div>
                        <div class="stat">
                            <div class="stat-value">{len(self.graph.edges)}</div>
                            <div>Link</div>
                        </div>
                        <div class="stat">
                            <div class="stat-value">{self.total_links_found}</div>
                            <div>Trovati</div>
                        </div>
                    </div>
                </div>
                
                <div class="controls">
                    <button class="btn" onclick="resetZoom()">
                        <span>🔄</span> Reset Zoom
                    </button>
                    <button class="btn" onclick="centerGraph()">
                        <span>🎯</span> Centra Grafo
                    </button>
                    <button class="btn" onclick="togglePhysics()">
                        <span>⚡</span> Toggle Physics
                    </button>
                    <button class="btn" onclick="fitToScreen()">
                        <span>📐</span> Fit Screen
                    </button>
                </div>
                
                <div id="graph"></div>
                
                <div class="legend">
                    <div class="legend-title">Legenda</div>
                    <div class="legend-item">
                        <div class="legend-color" style="background: #8b5cf6;"></div>
                        <div class="legend-label">Pagina iniziale</div>
                    </div>
                    <div class="legend-item">
                        <div class="legend-color" style="background: #3b82f6;"></div>
                        <div class="legend-label">Hub (molte connessioni)</div>
                    </div>
                    <div class="legend-item">
                        <div class="legend-color" style="background: #10b981;"></div>
                        <div class="legend-label">Pagina normale</div>
                    </div>
                    <div class="legend-item">
                        <div class="legend-color" style="background: #f59e0b;"></div>
                        <div class="legend-label">Foglia (pochi link)</div>
                    </div>
                </div>
            </div>
            
            <div class="tooltip" id="tooltip">
                <div class="tooltip-title"></div>
                <div class="tooltip-url"></div>
                <div class="tooltip-stats">
                    <div class="tooltip-stat">
                        <div class="tooltip-stat-value">0</div>
                        <div class="tooltip-stat-label">In</div>
                    </div>
                    <div class="tooltip-stat">
                        <div class="tooltip-stat-value">0</div>
                        <div class="tooltip-stat-label">Out</div>
                    </div>
                    <div class="tooltip-stat">
                        <div class="tooltip-stat-value">0</div>
                        <div class="tooltip-stat-label">Tot</div>
                    </div>
                </div>
            </div>
            
            <script src="https://d3js.org/d3.v7.min.js"></script>
            <script>
                const graphData = {self.get_graph_data()};
                
                const container = document.getElementById('graph');
                const width = container.clientWidth;
                const height = container.clientHeight;
                
                const svg = d3.select("#graph")
                    .append("svg")
                    .attr("width", width)
                    .attr("height", height);
                
                const g = svg.append("g");
                
                // Definiamo i gradienti per un effetto più ricco
                const defs = svg.append("defs");
                
                const gradients = [
                    {id: 'gradient-start', color1: '#8b5cf6', color2: '#a78bfa'},
                    {id: 'gradient-hub', color1: '#3b82f6', color2: '#60a5fa'},
                    {id: 'gradient-normal', color1: '#10b981', color2: '#34d399'},
                    {id: 'gradient-leaf', color1: '#f59e0b', color2: '#fbbf24'}
                ];
                
                gradients.forEach(grad => {{
                    const gradient = defs.append("linearGradient")
                        .attr("id", grad.id)
                        .attr("x1", "0%")
                        .attr("y1", "0%")
                        .attr("x2", "100%")
                        .attr("y2", "100%");
                    
                    gradient.append("stop")
                        .attr("offset", "0%")
                        .attr("stop-color", grad.color1);
                    
                    gradient.append("stop")
                        .attr("offset", "100%")
                        .attr("stop-color", grad.color2);
                }});
                
                // Comportamento di zoom
                const zoom = d3.zoom()
                    .scaleExtent([0.1, 10])
                    .on("zoom", (event) => {{
                        g.attr("transform", event.transform);
                    }});
                
                svg.call(zoom);
                
                // Tooltip
                const tooltip = d3.select("#tooltip");
                
                // Creazione dei link
                const link = g.append("g")
                    .selectAll("line")
                    .data(graphData.edges)
                    .enter().append("line")
                    .attr("class", "link");
                
                // Creazione dei nodi
                const node = g.append("g")
                    .selectAll("circle")
                    .data(graphData.nodes)
                    .enter().append("circle")
                    .attr("class", d => {{
                        if (d.id === "{self.normalize_url(self.start_url)}") return "node start";
                        if (d.in_degree > 5) return "node hub";
                        if (d.total_degree === 1) return "node leaf";
                        return "node normal";
                    }})
                    .attr("r", d => 15 + Math.min(d.total_degree * 0.8, 25))
                    .attr("fill", d => {{
                        if (d.id === "{self.normalize_url(self.start_url)}") return "url(#gradient-start)";
                        if (d.in_degree > 5) return "url(#gradient-hub)";
                        if (d.total_degree === 1) return "url(#gradient-leaf)";
                        return "url(#gradient-normal)";
                    }})
                    .attr("stroke", d => {{
                        if (d.id === "{self.normalize_url(self.start_url)}") return "#a78bfa";
                        if (d.in_degree > 5) return "#60a5fa";
                        if (d.total_degree === 1) return "#fbbf24";
                        return "#34d399";
                    }})
                    .attr("stroke-width", 3)
                    .on("mouseover", (event, d) => {{
                        // Mostra tooltip
                        tooltip.select('.tooltip-title').text(d.label);
                        tooltip.select('.tooltip-url').text(d.id);
                        tooltip.selectAll('.tooltip-stat-value')
                            .nodes()
                            .forEach((el, i) => {{
                                const values = [d.in_degree, d.out_degree, d.total_degree];
                                el.textContent = values[i];
                            }});
                        
                        tooltip.classed('visible', true)
                            .style("left", (event.pageX + 15) + "px")
                            .style("top", (event.pageY - 15) + "px");
                        
                        // Evidenzia connessioni
                        link.style('stroke', '#666')
                            .style('stroke-opacity', l => 
                                l.source.id === d.id || l.target.id === d.id ? 1 : 0.1
                            );
                    }})
                    .on("mouseout", () => {{
                        tooltip.classed('visible', false);
                        link.style('stroke', '#444')
                            .style('stroke-opacity', 0.6);
                    }})
                    .call(d3.drag()
                        .on("start", dragstarted)
                        .on("drag", dragged)
                        .on("end", dragended));
                
                // Simulazione di forza
                const simulation = d3.forceSimulation(graphData.nodes)
                    .force("link", d3.forceLink(graphData.edges).id(d => d.id).distance(100))
                    .force("charge", d3.forceManyBody().strength(-500))
                    .force("center", d3.forceCenter(width / 2, height / 2))
                    .force("collision", d3.forceCollide().radius(30))
                    .force("radial", d3.forceRadial(0, width / 2, height / 2));
                
                simulation.on("tick", () => {{
                    link
                        .attr("x1", d => d.source.x)
                        .attr("y1", d => d.source.y)
                        .attr("x2", d => d.target.x)
                        .attr("y2", d => d.target.y);
                    
                    node
                        .attr("cx", d => d.x)
                        .attr("cy", d => d.y);
                }});
                
                // Funzioni per il drag
                function dragstarted(event, d) {{
                    if (!event.active) simulation.alphaTarget(0.3).restart();
                    d.fx = d.x;
                    d.fy = d.y;
                }}
                
                function dragged(event, d) {{
                    d.fx = event.x;
                    d.fy = d.y;
                }}
                
                function dragended(event, d) {{
                    if (!event.active) simulation.alphaTarget(0);
                    d.fx = null;
                    d.fy = null;
                }}
                
                // Funzioni di controllo
                function resetZoom() {{
                    svg.transition().duration(750).call(
                        zoom.transform,
                        d3.zoomIdentity
                    );
                }}
                
                function centerGraph() {{
                    const bounds = g.node().getBBox();
                    const fullWidth = bounds.width;
                    const fullHeight = bounds.height;
                    const midX = bounds.x + fullWidth / 2;
                    const midY = bounds.y + fullHeight / 2;
                    
                    const scale = 0.8 / Math.max(fullWidth / width, fullHeight / height);
                    const translate = [width / 2 - scale * midX, height / 2 - scale * midY];
                    
                    svg.transition().duration(750).call(
                        zoom.transform,
                        d3.zoomIdentity.translate(translate[0], translate[1]).scale(scale)
                    );
                }}
                
                function togglePhysics() {{
                    if (simulation.alpha() < 0.01) {{
                        simulation.alphaTarget(0.3).restart();
                    }} else {{
                        simulation.stop();
                    }}
                }}
                
                function fitToScreen() {{
                    const bounds = g.node().getBBox();
                    const fullWidth = bounds.width;
                    const fullHeight = bounds.height;
                    const widthScale = width / fullWidth;
                    const heightScale = height / fullHeight;
                    const scale = Math.min(widthScale, heightScale) * 0.9;
                    
                    const translate = [
                        (width - fullWidth * scale) / 2 - bounds.x * scale,
                        (height - fullHeight * scale) / 2 - bounds.y * scale
                    ];
                    
                    svg.transition().duration(750).call(
                        zoom.transform,
                        d3.zoomIdentity.translate(translate[0], translate[1]).scale(scale)
                    );
                }}
                
                // Animazione iniziale
                setTimeout(() => {{
                    fitToScreen();
                }}, 1000);
            </script>
        </body>
        </html>
        """
        
        try:
            with open(output_path, 'w', encoding='utf-8') as f:
                f.write(html_content)
            print(f"✅ Creato HTML stile Obsidian in: {output_path}")
        except Exception as e:
            print(f"❌ Errore nella creazione dell'HTML: {str(e)}")
    
    def get_graph_data(self):
        nodes = []
        for node, data in self.graph.nodes(data=True):
            in_degree = self.graph.in_degree(node)
            out_degree = self.graph.out_degree(node)
            nodes.append({
                'id': node,
                'label': data.get('label', node),
                'in_degree': in_degree,
                'out_degree': out_degree,
                'total_degree': in_degree + out_degree
            })
        
        edges = [{"source": u, "target": v} for u, v in self.graph.edges()]
        return f'{{"nodes": {nodes}, "edges": {edges}}}'

if __name__ == "__main__":
    try:
        import tldextract
    except ImportError:
        print("📦 Installazione dipendenza tldextract...")
        os.system("pip install tldextract")
        import tldextract
    
    START_URL = "https://it.wikipedia.org/wiki/Python"
    MAX_PAGES = 5
    MAX_DEPTH = 2
    DELAY = 1.5
    
    crawler = WebGraphCrawler(
        start_url=START_URL,
        max_pages=MAX_PAGES,
        max_depth=MAX_DEPTH,
        delay=DELAY
    )
    
    graph = crawler.crawl()
    crawler.visualize("web_graph.html")