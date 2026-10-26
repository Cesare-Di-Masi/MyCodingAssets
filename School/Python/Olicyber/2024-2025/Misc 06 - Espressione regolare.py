import re

pattern = r"^flag\{[\D]{2}[0][a-z][^0-9][L][\w]{4}[^a-pr-z]{3}[^\d]{2}[\d][b]\}$"

test_flag = "flag{Q_0g_LA3c_q!1@#8b}"

if re.match(pattern, test_flag):
    print("Flag corretta!")
else:
    print("Flag NON valida.")
