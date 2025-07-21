import random
import hashlib
from typing import List

class Channel:
    def __init__(self, channel_id: int):
        self.id = channel_id
        self.encryption_key = self._generate_key()
        self.status = "secure"  # or "compromised"

    def _generate_key(self) -> str:
        return hashlib.sha256(str(random.random()).encode()).hexdigest()

    def rotate_key(self):
        self.encryption_key = self._generate_key()
        self.status = "secure"

    def compromise(self):
        self.status = "compromised"

class ChannelManager:
    def __init__(self, n_channels: int = 16):
        self.channels: List[Channel] = [Channel(i) for i in range(n_channels)]

    def monitor(self) -> List[int]:
        compromised_ids = []
        for ch in self.channels:
            if random.random() < 0.05:  # 5% chance compromissione simulata
                ch.compromise()
                compromised_ids.append(ch.id)
        return compromised_ids

    def rotate_keys(self, rate: float = 0.5):
        for ch in self.channels:
            if ch.status == "secure" and random.random() < rate:
                ch.rotate_key()
