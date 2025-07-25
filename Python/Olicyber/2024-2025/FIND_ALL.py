import requests
from bs4 import BeautifulSoup
url = " http://web-12.challs.olicyber.it/"

r=requests.get(url)
r=BeautifulSoup(r.content, 'html.parser')
print(r.find_all("p"))

