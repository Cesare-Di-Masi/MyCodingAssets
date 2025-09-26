import tkinter as tk
from tkinter import ttk, messagebox
import random
from enum import Enum
from PIL import Image, ImageTk, ImageDraw, ImageFont
import os

class Suit(Enum):
    HEARTS = "♥"
    DIAMONDS = "♦"
    CLUBS = "♣"
    SPADES = "♠"

class Rank(Enum):
    TWO = 2
    THREE = 3
    FOUR = 4
    FIVE = 5
    SIX = 6
    SEVEN = 7
    EIGHT = 8
    NINE = 9
    TEN = 10
    JACK = "J"
    QUEEN = "Q"
    KING = "K"
    ACE = "A"

class Card:
    def __init__(self, suit, rank):
        self.suit = suit
        self.rank = rank
        self.hidden = False
    
    def value(self):
        if self.rank in [Rank.JACK, Rank.QUEEN, Rank.KING]:
            return 10
        elif self.rank == Rank.ACE:
            return 11
        else:
            return self.rank.value
    
    def __str__(self):
        if self.hidden:
            return "🂠"
        return f"{self.rank.value if isinstance(self.rank.value, int) else self.rank.value}{self.suit.value}"

class Deck:
    def __init__(self, num_decks=1):
        self.num_decks = num_decks
        self.cards = []
        self.reset()
    
    def reset(self):
        self.cards = []
        for _ in range(self.num_decks):
            for suit in Suit:
                for rank in Rank:
                    self.cards.append(Card(suit, rank))
        self.shuffle()
    
    def shuffle(self):
        random.shuffle(self.cards)
    
    def deal(self):
        if len(self.cards) < 15:
            self.reset()
        return self.cards.pop()

class Hand:
    def __init__(self, bet=0):
        self.cards = []
        self.bet = bet
        self.standing = False
        self.blackjack = False
        self.busted = False
        self.is_split = False
    
    def add_card(self, card):
        self.cards.append(card)
        self.check_blackjack()
    
    def check_blackjack(self):
        if len(self.cards) == 2 and self.value() == 21:
            self.blackjack = True
    
    def value(self):
        total = sum(card.value() for card in self.cards if not card.hidden)
        aces = sum(1 for card in self.cards if card.rank == Rank.ACE and not card.hidden)
        
        while total > 21 and aces > 0:
            total -= 10
            aces -= 1
        
        return total
    
    def is_soft(self):
        total = sum(card.value() for card in self.cards if not card.hidden)
        aces = sum(1 for card in self.cards if card.rank == Rank.ACE and not card.hidden)
        return total <= 21 and aces > 0
    
    def can_split(self):
        return len(self.cards) == 2 and self.cards[0].rank == self.cards[1].rank and not self.is_split

class BlackjackGUI:
    def __init__(self, root):
        self.root = root
        self.root.title("Blackjack Professionale")
        self.root.geometry("1200x800")
        self.root.configure(bg="#0d5d2e")
        
        # Game variables
        self.bankroll = 1000
        self.bet = 10
        self.deck = Deck()
        self.dealer_hand = Hand()
        self.player_hands = []
        self.current_hand_index = 0
        self.game_active = False
        
        # House rules
        self.rules = {
            'soft17': 'stand',
            'surrender': True,
            'double_after_split': True,
            'blackjack_payout': 1.5,
            'num_decks': 6,
            'max_splits': 3
        }
        
        # Card images
        self.card_images = {}
        self.card_back = None
        self.load_card_images()
        
        # Create GUI
        self.create_widgets()
        self.update_display()
    
    def load_card_images(self):
        # Create card images programmatically
        card_width, card_height = 100, 140
        
        # Create card back
        img = Image.new('RGB', (card_width, card_height), color='#000080')
        draw = ImageDraw.Draw(img)
        draw.rectangle([(5, 5), (card_width-5, card_height-5)], outline='gold', width=3)
        draw.text((card_width//2, card_height//2), "🂠", fill='white', anchor='mm', font=ImageFont.truetype("arial.ttf", 60))
        self.card_back = ImageTk.PhotoImage(img)
        
        # Create card fronts
        for suit in Suit:
            for rank in Rank:
                img = Image.new('RGB', (card_width, card_height), color='white')
                draw = ImageDraw.Draw(img)
                
                # Card border
                draw.rectangle([(0, 0), (card_width, card_height)], outline='black', width=2)
                
                # Card content
                rank_str = str(rank.value) if isinstance(rank.value, int) else rank.value
                color = 'red' if suit in [Suit.HEARTS, Suit.DIAMONDS] else 'black'
                
                # Top-left rank and suit
                draw.text((10, 10), rank_str, fill=color, font=ImageFont.truetype("arial.ttf", 20))
                draw.text((10, 30), suit.value, fill=color, font=ImageFont.truetype("arial.ttf", 20))
                
                # Center suit symbol
                draw.text((card_width//2, card_height//2), suit.value, fill=color, anchor='mm', font=ImageFont.truetype("arial.ttf", 60))
                
                # Bottom-right rank and suit (upside down)
                draw.text((card_width-10, card_height-10), rank_str, fill=color, anchor='rd', font=ImageFont.truetype("arial.ttf", 20))
                draw.text((card_width-10, card_height-30), suit.value, fill=color, anchor='rd', font=ImageFont.truetype("arial.ttf", 20))
                
                self.card_images[(suit, rank)] = ImageTk.PhotoImage(img)
    
    def create_widgets(self):
        # Title
        title_frame = tk.Frame(self.root, bg="#0d5d2e")
        title_frame.pack(pady=10)
        
        title_label = tk.Label(
            title_frame, 
            text="BLACKJACK PROFESSIONALE", 
            font=("Arial", 24, "bold"),
            bg="#0d5d2e",
            fg="gold"
        )
        title_label.pack()
        
        # Info panel
        info_frame = tk.Frame(self.root, bg="#0d5d2e")
        info_frame.pack(pady=5)
        
        self.bankroll_label = tk.Label(
            info_frame,
            text=f"Bankroll: ${self.bankroll}",
            font=("Arial", 16),
            bg="#0d5d2e",
            fg="white"
        )
        self.bankroll_label.pack(side=tk.LEFT, padx=20)
        
        self.bet_label = tk.Label(
            info_frame,
            text=f"Puntata: ${self.bet}",
            font=("Arial", 16),
            bg="#0d5d2e",
            fg="white"
        )
        self.bet_label.pack(side=tk.LEFT, padx=20)
        
        # Bet controls
        bet_frame = tk.Frame(self.root, bg="#0d5d2e")
        bet_frame.pack(pady=5)
        
        tk.Label(
            bet_frame,
            text="Modifica Puntata:",
            font=("Arial", 12),
            bg="#0d5d2e",
            fg="white"
        ).pack(side=tk.LEFT, padx=5)
        
        tk.Button(
            bet_frame,
            text="-10",
            command=lambda: self.change_bet(-10),
            bg="#d32f2f",
            fg="white",
            font=("Arial", 12, "bold"),
            width=3
        ).pack(side=tk.LEFT, padx=2)
        
        tk.Button(
            bet_frame,
            text="-1",
            command=lambda: self.change_bet(-1),
            bg="#f57c00",
            fg="white",
            font=("Arial", 12, "bold"),
            width=3
        ).pack(side=tk.LEFT, padx=2)
        
        tk.Button(
            bet_frame,
            text="+1",
            command=lambda: self.change_bet(1),
            bg="#388e3c",
            fg="white",
            font=("Arial", 12, "bold"),
            width=3
        ).pack(side=tk.LEFT, padx=2)
        
        tk.Button(
            bet_frame,
            text="+10",
            command=lambda: self.change_bet(10),
            bg="#1976d2",
            fg="white",
            font=("Arial", 12, "bold"),
            width=3
        ).pack(side=tk.LEFT, padx=2)
        
        # Game area
        self.game_frame = tk.Frame(self.root, bg="#0d5d2e")
        self.game_frame.pack(expand=True, fill=tk.BOTH, padx=20, pady=10)
        
        # Dealer area
        self.dealer_frame = tk.Frame(self.game_frame, bg="#0d5d2e")
        self.dealer_frame.pack(pady=20)
        
        self.dealer_label = tk.Label(
            self.dealer_frame,
            text="DEALER",
            font=("Arial", 16, "bold"),
            bg="#0d5d2e",
            fg="white"
        )
        self.dealer_label.pack()
        
        self.dealer_cards_frame = tk.Frame(self.dealer_frame, bg="#0d5d2e")
        self.dealer_cards_frame.pack()
        
        self.dealer_value_label = tk.Label(
            self.dealer_frame,
            text="Valore: 0",
            font=("Arial", 14),
            bg="#0d5d2e",
            fg="white"
        )
        self.dealer_value_label.pack()
        
        # Player area
        self.player_frame = tk.Frame(self.game_frame, bg="#0d5d2e")
        self.player_frame.pack(pady=20)
        
        self.player_label = tk.Label(
            self.player_frame,
            text="GIOCATORE",
            font=("Arial", 16, "bold"),
            bg="#0d5d2e",
            fg="white"
        )
        self.player_label.pack()
        
        self.player_cards_frame = tk.Frame(self.player_frame, bg="#0d5d2e")
        self.player_cards_frame.pack()
        
        self.player_value_label = tk.Label(
            self.player_frame,
            text="Valore: 0",
            font=("Arial", 14),
            bg="#0d5d2e",
            fg="white"
        )
        self.player_value_label.pack()
        
        # Control buttons
        self.control_frame = tk.Frame(self.root, bg="#0d5d2e")
        self.control_frame.pack(pady=10)
        
        self.new_game_btn = tk.Button(
            self.control_frame,
            text="Nuova Partita",
            command=self.new_game,
            bg="#4caf50",
            fg="white",
            font=("Arial", 14, "bold"),
            width=12,
            height=2
        )
        self.new_game_btn.pack(side=tk.LEFT, padx=10)
        
        self.hit_btn = tk.Button(
            self.control_frame,
            text="Pesca",
            command=self.player_hit,
            bg="#2196f3",
            fg="white",
            font=("Arial", 14, "bold"),
            width=10,
            height=2,
            state=tk.DISABLED
        )
        self.hit_btn.pack(side=tk.LEFT, padx=5)
        
        self.stand_btn = tk.Button(
            self.control_frame,
            text="Stai",
            command=self.player_stand,
            bg="#ff9800",
            fg="white",
            font=("Arial", 14, "bold"),
            width=10,
            height=2,
            state=tk.DISABLED
        )
        self.stand_btn.pack(side=tk.LEFT, padx=5)
        
        self.double_btn = tk.Button(
            self.control_frame,
            text="Raddoppia",
            command=self.player_double,
            bg="#9c27b0",
            fg="white",
            font=("Arial", 14, "bold"),
            width=10,
            height=2,
            state=tk.DISABLED
        )
        self.double_btn.pack(side=tk.LEFT, padx=5)
        
        self.split_btn = tk.Button(
            self.control_frame,
            text="Dividi",
            command=self.player_split,
            bg="#e91e63",
            fg="white",
            font=("Arial", 14, "bold"),
            width=10,
            height=2,
            state=tk.DISABLED
        )
        self.split_btn.pack(side=tk.LEFT, padx=5)
        
        self.surrender_btn = tk.Button(
            self.control_frame,
            text="Arrenditi",
            command=self.player_surrender,
            bg="#f44336",
            fg="white",
            font=("Arial", 14, "bold"),
            width=10,
            height=2,
            state=tk.DISABLED
        )
        self.surrender_btn.pack(side=tk.LEFT, padx=5)
        
        # Rules button
        self.rules_btn = tk.Button(
            self.control_frame,
            text="Regole",
            command=self.show_rules,
            bg="#607d8b",
            fg="white",
            font=("Arial", 12),
            width=8
        )
        self.rules_btn.pack(side=tk.LEFT, padx=5)
    
    def change_bet(self, amount):
        if not self.game_active:
            new_bet = self.bet + amount
            if new_bet >= 1 and new_bet <= self.bankroll:
                self.bet = new_bet
                self.bet_label.config(text=f"Puntata: ${self.bet}")
    
    def new_game(self):
        if self.bet > self.bankroll:
            messagebox.showwarning("Puntata non valida", "La puntata supera il tuo bankroll!")
            return
        
        self.bankroll -= self.bet
        self.game_active = True
        self.dealer_hand = Hand()
        self.player_hands = [Hand(self.bet)]
        self.current_hand_index = 0
        
        # Reset deck if needed
        self.deck.num_decks = self.rules['num_decks']
        self.deck.reset()
        
        # Deal initial cards
        for _ in range(2):
            self.player_hands[0].add_card(self.deck.deal())
            dealer_card = self.deck.deal()
            if len(self.dealer_hand.cards) == 0:
                dealer_card.hidden = False
            else:
                dealer_card.hidden = True
            self.dealer_hand.add_card(dealer_card)
        
        # Check for blackjack
        if self.player_hands[0].blackjack:
            self.dealer_hand.cards[1].hidden = False
            if self.dealer_hand.blackjack:
                self.bankroll += self.bet
                messagebox.showinfo("Pareggio", "Entrambi avete fatto Blackjack! Pareggio.")
            else:
                payout = self.bet * (1 + self.rules['blackjack_payout'])
                self.bankroll += payout
                messagebox.showinfo("Blackjack!", f"Hai fatto Blackjack! Hai vinto ${payout - self.bet:.2f}!")
            self.game_active = False
        else:
            self.update_buttons()
        
        self.update_display()
    
    def player_hit(self):
        if not self.game_active:
            return
        
        current_hand = self.player_hands[self.current_hand_index]
        current_hand.add_card(self.deck.deal())
        
        if current_hand.value() > 21:
            current_hand.busted = True
            messagebox.showinfo("Sballato!", f"Hai sballato con {current_hand.value()}!")
            self.check_next_hand()
        
        self.update_display()
        self.update_buttons()
    
    def player_stand(self):
        if not self.game_active:
            return
        
        current_hand = self.player_hands[self.current_hand_index]
        current_hand.standing = True
        self.check_next_hand()
        self.update_display()
        self.update_buttons()
    
    def player_double(self):
        if not self.game_active:
            return
        
        current_hand = self.player_hands[self.current_hand_index]
        if self.bankroll < current_hand.bet:
            messagebox.showwarning("Fondi insufficienti", "Non hai abbastanza soldi per raddoppiare!")
            return
        
        self.bankroll -= current_hand.bet
        current_hand.bet *= 2
        current_hand.add_card(self.deck.deal())
        
        if current_hand.value() > 21:
            current_hand.busted = True
            messagebox.showinfo("Sballato!", f"Hai sballato con {current_hand.value()}!")
        
        current_hand.standing = True
        self.check_next_hand()
        self.update_display()
        self.update_buttons()
    
    def player_split(self):
        if not self.game_active:
            return
        
        current_hand = self.player_hands[self.current_hand_index]
        if not current_hand.can_split():
            return
        
        if len(self.player_hands) >= self.rules['max_splits']:
            messagebox.showwarning("Limite split", f"Hai raggiunto il limite massimo di split ({self.rules['max_splits']})!")
            return
        
        if self.bankroll < current_hand.bet:
            messagebox.showwarning("Fondi insufficienti", "Non hai abbastanza soldi per dividere!")
            return
        
        self.bankroll -= current_hand.bet
        
        # Create new hand
        new_hand = Hand(current_hand.bet)
        new_hand.is_split = True
        new_hand.add_card(current_hand.cards.pop())
        
        # Add new card to both hands
        current_hand.add_card(self.deck.deal())
        new_hand.add_card(self.deck.deal())
        
        # Add new hand to list
        self.player_hands.append(new_hand)
        
        self.update_display()
        self.update_buttons()
    
    def player_surrender(self):
        if not self.game_active:
            return
        
        current_hand = self.player_hands[self.current_hand_index]
        if not self.rules['surrender']:
            messagebox.showinfo("Arrenditi non disponibile", "Questo casinò non permette l'arrendersi!")
            return
        
        self.bankroll += current_hand.bet / 2
        current_hand.standing = True
        messagebox.showinfo("Arrenditi", "Ti sei arreso. Perdi metà della tua puntata.")
        self.check_next_hand()
        self.update_display()
        self.update_buttons()
    
    def check_next_hand(self):
        # Check if all hands are finished
        all_finished = True
        for hand in self.player_hands:
            if not hand.standing and not hand.busted:
                all_finished = False
                break
        
        if all_finished:
            self.dealer_play()
            self.settle_bets()
            self.game_active = False
        else:
            # Move to next active hand
            self.current_hand_index = (self.current_hand_index + 1) % len(self.player_hands)
            while (self.player_hands[self.current_hand_index].standing or 
                   self.player_hands[self.current_hand_index].busted):
                self.current_hand_index = (self.current_hand_index + 1) % len(self.player_hands)
    
    def dealer_play(self):
        # Reveal hidden card
        if len(self.dealer_hand.cards) >= 2:
            self.dealer_hand.cards[1].hidden = False
        
        while True:
            dealer_value = self.dealer_hand.value()
            
            if dealer_value > 21:
                break
            elif dealer_value == 17:
                if self.dealer_hand.is_soft() and self.rules['soft17'] == 'hit':
                    self.dealer_hand.add_card(self.deck.deal())
                else:
                    break
            elif dealer_value < 17:
                self.dealer_hand.add_card(self.deck.deal())
            else:
                break
    
    def settle_bets(self):
        dealer_value = self.dealer_hand.value()
        dealer_busted = dealer_value > 21
        
        results = []
        
        for hand in self.player_hands:
            if hand.busted:
                results.append(f"Mano sballata! Perdi ${hand.bet:.2f}")
            elif hand.blackjack:
                if self.dealer_hand.blackjack:
                    self.bankroll += hand.bet
                    results.append(f"Blackjack vs Blackjack! Pareggio.")
                else:
                    payout = hand.bet * (1 + self.rules['blackjack_payout'])
                    self.bankroll += payout
                    results.append(f"Blackjack! Vinci ${payout - hand.bet:.2f}")
            elif dealer_busted:
                self.bankroll += hand.bet * 2
                results.append(f"Dealer sballato! Vinci ${hand.bet:.2f}")
            elif hand.value() > dealer_value:
                self.bankroll += hand.bet * 2
                results.append(f"Vinci! {hand.value()} vs {dealer_value} (+${hand.bet:.2f})")
            elif hand.value() == dealer_value:
                self.bankroll += hand.bet
                results.append(f"Pareggio! {hand.value()} vs {dealer_value}")
            else:
                results.append(f"Perdi! {hand.value()} vs {dealer_value} (-${hand.bet:.2f})")
        
        messagebox.showinfo("Risultati", "\n".join(results))
    
    def update_display(self):
        # Update bankroll and bet
        self.bankroll_label.config(text=f"Bankroll: ${self.bankroll}")
        self.bet_label.config(text=f"Puntata: ${self.bet}")
        
        # Update dealer hand
        for widget in self.dealer_cards_frame.winfo_children():
            widget.destroy()
        
        for card in self.dealer_hand.cards:
            if card.hidden:
                img_label = tk.Label(self.dealer_cards_frame, image=self.card_back, bg="#0d5d2e")
            else:
                img_label = tk.Label(
                    self.dealer_cards_frame, 
                    image=self.card_images[(card.suit, card.rank)], 
                    bg="#0d5d2e"
                )
            img_label.pack(side=tk.LEFT, padx=5)
        
        # Update dealer value label - FIX HERE
        if not self.game_active:
            self.dealer_value_label.config(text=f"Valore: {self.dealer_hand.value()}")
        else:
            if len(self.dealer_hand.cards) == 0:
                self.dealer_value_label.config(text="Valore: 0")
            elif len(self.dealer_hand.cards) == 1:
                self.dealer_value_label.config(text=f"Valore: {self.dealer_hand.cards[0].value()}")
            else:
                if self.dealer_hand.cards[1].hidden:
                    self.dealer_value_label.config(text=f"Valore: {self.dealer_hand.cards[0].value()} + ?")
                else:
                    self.dealer_value_label.config(text=f"Valore: {self.dealer_hand.value()}")
        
        # Update player hand
        for widget in self.player_cards_frame.winfo_children():
            widget.destroy()
        
        if self.player_hands:
            current_hand = self.player_hands[self.current_hand_index]
            for card in current_hand.cards:
                img_label = tk.Label(
                    self.player_cards_frame, 
                    image=self.card_images[(card.suit, card.rank)], 
                    bg="#0d5d2e"
                )
                img_label.pack(side=tk.LEFT, padx=5)
            
            self.player_value_label.config(text=f"Valore: {current_hand.value()}")
            
            # Show current hand indicator
            if len(self.player_hands) > 1:
                self.player_label.config(text=f"MANO {self.current_hand_index + 1}/{len(self.player_hands)}")
            else:
                self.player_label.config(text="GIOCATORE")
        else:
            self.player_value_label.config(text="Valore: 0")
            self.player_label.config(text="GIOCATORE")
    
    def update_buttons(self):
        if not self.game_active:
            self.new_game_btn.config(state=tk.NORMAL)
            self.hit_btn.config(state=tk.DISABLED)
            self.stand_btn.config(state=tk.DISABLED)
            self.double_btn.config(state=tk.DISABLED)
            self.split_btn.config(state=tk.DISABLED)
            self.surrender_btn.config(state=tk.DISABLED)
            return
        
        self.new_game_btn.config(state=tk.DISABLED)
        current_hand = self.player_hands[self.current_hand_index]
        
        # Always available
        self.hit_btn.config(state=tk.NORMAL)
        self.stand_btn.config(state=tk.NORMAL)
        
        # Double (only with 2 cards and enough money)
        if len(current_hand.cards) == 2 and self.bankroll >= current_hand.bet:
            self.double_btn.config(state=tk.NORMAL)
        else:
            self.double_btn.config(state=tk.DISABLED)
        
        # Split (only with pair and under max splits)
        if (current_hand.can_split() and 
            len(self.player_hands) < self.rules['max_splits'] and 
            self.bankroll >= current_hand.bet):
            self.split_btn.config(state=tk.NORMAL)
        else:
            self.split_btn.config(state=tk.DISABLED)
        
        # Surrender (only before any action and if allowed)
        if (len(current_hand.cards) == 2 and 
            not current_hand.is_split and 
            self.rules['surrender']):
            self.surrender_btn.config(state=tk.NORMAL)
        else:
            self.surrender_btn.config(state=tk.DISABLED)
    
    def show_rules(self):
        rules_text = f"""REGOLE DEL BLACKJACK

OBIETTIVO:
Battere il dealer ottenendo un punteggio più vicino a 21 senza superarlo.

VALORI DELLE CARTE:
- Carte numeriche: Valore nominale
- Figure (J, Q, K): 10 punti
- Asso: 1 o 11 punti (a seconda di convenga)

REGOLE DI GIOCO:
- Il dealer deve pescare fino a 17+
- Blackjack (21 con 2 carte) paga {self.rules['blackjack_payout']}:1
- Puoi raddoppiare la puntata dopo aver ricevuto 2 carte
- Puoi dividere coppie di carte dello stesso valore
- Puoi arrenderti e perdere metà della puntata (se permesso)

REGOLE DEL CASINÒ:
- Mazzi in uso: {self.rules['num_decks']}
- Dealer su Soft 17: {"Pesca" if self.rules['soft17'] == 'hit' else "Sta"}
- Resa permessa: {"Sì" if self.rules['surrender'] else "No"}
- Raddoppio dopo split: {"Sì" if self.rules['double_after_split'] else "No"}
- Massimo split: {self.rules['max_splits']}

BUONA FORTUNA!"""
        
        messagebox.showinfo("Regole del Gioco", rules_text)

if __name__ == "__main__":
    root = tk.Tk()
    game = BlackjackGUI(root)
    root.mainloop()