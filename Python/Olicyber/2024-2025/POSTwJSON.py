import requests

url ='http://web-09.challs.olicyber.it/login'
payload = {"username": "admin", "password": "admin"}
r = requests.post(url, json=payload)
print(r.text)