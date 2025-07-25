import requests

url1 = 'http://web-06.challs.olicyber.it/token'

url2 = 'http://web-06.challs.olicyber.it/flag'

s = requests.Session()

s.get(url1)
r = s.get(url2)

print(r.text)
