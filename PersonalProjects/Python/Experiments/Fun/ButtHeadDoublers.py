#Prendere un numero intero, prendere il su ultimo ultimo valore e metterlo all'inzio: 432 prendi il 2 e diventa 243, 
# l'obiettivo è trovare il primo numero che diventa il doppio del numero iniziale

numero = 0
trovato = False
moltiplicatore = 2
volte_cambio = 2

while not trovato:
    for i in range(volte_cambio):
        numero += 1
        numero_str = str(numero)
        ultimo_valore = numero_str[-1]
        nuovo_numero_str = ultimo_valore + numero_str[:-1]
        nuovo_numero = int(nuovo_numero_str)

    if nuovo_numero == moltiplicatore * numero:
        trovato = True
        print(f"Il primo numero che diventa il {moltiplicatore} volte del numero iniziale è: {numero} cambiando {volte_cambio} volte il numero diventa {nuovo_numero}")

    if(numero % 100000000 == 0):  # Limite per evitare loop infinito
        print(f"Sei arrivato a {numero}")