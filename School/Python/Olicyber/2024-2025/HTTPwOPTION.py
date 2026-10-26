import requests
#requests mette a disposizione funzioni analoghe alla funzione get anche per i verbi meno comuni, come put e patch.
url = "http://web-10.challs.olicyber.it"

r = requests.put(url)
print(r.headers)