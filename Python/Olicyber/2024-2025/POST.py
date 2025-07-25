import requests

payload = {"username": "admin", "password": "admin"}
url="http://web-08.challs.olicyber.it/login"

r = requests.post(url, data=payload)

print(r.text)