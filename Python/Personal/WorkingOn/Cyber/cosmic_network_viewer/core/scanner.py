
import subprocess
from scapy.all import sniff, ARP, IP

def run_nmap_scan(subnet: str, output_path: str = "data/scan.xml"):
    cmd = [
        "nmap",
        "-sS", "-O", "-sV", "-T4",
        "-oX", output_path,
        subnet
    ]
    subprocess.run(cmd, check=True)

def passive_scapy_scan(interface: str = "eth0"):
    devices = set()

    def handler(pkt):
        if pkt.haslayer(ARP):
            devices.add((pkt.psrc, pkt.hwsrc))
        elif pkt.haslayer(IP):
            devices.add((pkt[IP].src, pkt.src))

    sniff(prn=handler, timeout=30, store=False)
    return list(devices)
