import random
from astroquery.gaia import Gaia
import time

def get_random_gaia_star_all_info():
    min_id = int(1e18)
    max_id = int(2e18)
    max_attempts = 20

    for _ in range(max_attempts):
        random_id = random.randint(min_id, max_id)

        query = f"""
        SELECT TOP 1 *
        FROM gaiadr3.gaia_source
        WHERE SOURCE_ID >= {random_id}
        ORDER BY SOURCE_ID ASC
        """

        try:
            job = Gaia.launch_job(query)
            results = job.get_results()
            if len(results) > 0:
                star = results[0]
                # Creiamo un dizionario con tutte le colonne e i relativi valori
                star_info = {col: star[col] for col in results.colnames}
                return star_info
        except Exception as e:
            print(f"Errore query: {e}")
        time.sleep(1)

    return None

if __name__ == "__main__":
    star = get_random_gaia_star_all_info()
    if star:
        print("Informazioni complete sulla stella Gaia trovata:\n")
        for key, value in star.items():
            print(f"{key}: {value}")
    else:
        print("Non è stato possibile trovare una stella casuale dopo vari tentativi.")
