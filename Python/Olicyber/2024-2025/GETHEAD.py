import requests

url = "http://web-07.challs.olicyber.it"

r = requests.get(url)

r = requests.put(url)

r = requests.delete(url)

r = requests.head(url)
print(r.headers)
r = requests.options(url)
