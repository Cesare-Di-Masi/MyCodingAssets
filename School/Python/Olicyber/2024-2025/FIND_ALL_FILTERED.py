import requests
from bs4 import BeautifulSoup

url = "http://web-13.challs.olicyber.it/"

r = requests.get(url)
soup = BeautifulSoup(r.content, 'html.parser') # Renamed variable for clarity

red_elements = soup.find_all(class_="red")

# Extract and print the text content of each element
for element in red_elements:
    print(element.text)