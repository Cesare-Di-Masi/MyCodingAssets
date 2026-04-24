const API_KEY = "83e60d593a839a0a55789e8a946d3c52";

const nome = document.getElementById("nome");
const risultati = document.getElementById("risultati");

nome.addEventListener("input", cerca);
stagione.addEventListener("change", cerca)


function caricaStatistiche(teamId, nomeSquadra, logo, stagione) {
  fetch(
    `https://v3.football.api-sports.io/teams/statistics?league=135&season=${stagione}&team=${teamId}`,
    {
      headers: { "x-apisports-key": API_KEY }
    }
  )
  .then(r => r.json())
  .then(data => {
    const f = data.response.fixtures;
   
    // Creiamo una struttura HTML più pulita
    risultati.innerHTML = `
      <div class="squadra-card">
        <div class="squadra-header">
            <img src="${logo}" alt="${nomeSquadra} logo">
            <h3>${nomeSquadra}</h3>
        </div>
        <div class="stats-grid">
            <p><strong>Vittorie:</strong> ${f.wins.total}</p>
            <p><strong>Pareggi:</strong> ${f.draws.total}</p>
            <p><strong>Sconfitte:</strong> ${f.loses.total}</p>
        </div>
      </div>
    `;
  });
}

function cerca() {
  if (nome.value.length < 3) {
    risultati.innerHTML = "";
    return;
  }

  fetch(`https://v3.football.api-sports.io/teams?search=${nome.value}`, {
    headers: { "x-apisports-key": API_KEY }
  })
  .then(r => r.json())
  .then(data => {
    const squadra = data.response.find(s => s.team.country === "Italy");
    if (!squadra) {
      risultati.innerHTML = "Nessuna squadra italiana trovata";
      return;
    }
    const season = stagione.value;

    caricaStatistiche(squadra.team.id, squadra.team.name, squadra.team.logo, season);
  });
}