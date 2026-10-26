import requests

url = "http://web-02.challs.olicyber.it/server-records"
params = {"id": "flag"}  # o un altro valore se specificato dalla challenge

r = requests.get(url, params=params)

print(r.text)
