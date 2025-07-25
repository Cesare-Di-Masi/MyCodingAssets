import requests
from bs4 import BeautifulSoup, Comment

url = "http://web-14.challs.olicyber.it/"

r = requests.get(url)
soup = BeautifulSoup(r.content, 'html.parser')

# Find all comments in the HTML
comments = soup.find_all(string=lambda text: isinstance(text, Comment))

# Print each comment found
for comment in comments:
    print(comment.strip()) # .strip() to remove leading/trailing whitespace