<?php
$jsonPath ='./data/requests.json'; //__DIR__ torniamo alla route del progetto

if(!file_exists($jsonPath)) {
    die("Il file requests.json non esiste. Assicurati che il percorso sia corretto."); // Controllo se il file esiste
    //die --> interrompe l'esecuzione dello script se il file non esiste
}

$jsondata = file_get_contents($jsonPath);
$data = json_decode($jsondata, true); // Decodifica il JSON in un array

if($data === null) {
    die("Errore nella decodifica del JSON. Controlla il formato del file requests.json."); // Controllo se la decodifica è avvenuta correttamente
}

?>

<!DOCTYPE html>
<html lang="it">
<head>
    <meta charset="UTF-8">
    <title>La mia prima pagina PHP in Docker</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f4f6f9;
            margin: 0;
            padding: 30px;
        }
        .container {
            max-width: 800px;
            margin: 0 auto;
        }
        h1 {
            color: #333;
            border-bottom: 2px solid #007bff;
            padding-bottom: 10px;
        }
        .card {
            background-color: #fff;
            border-radius: 6px;
            padding: 20px;
            margin-bottom: 15px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.08);
            border-left: 4px solid #007bff;
        }
        .card h3 {
            margin-top: 0;
            color: #111;
        }
        .card p {
            color: #555;
            line-height: 1.5;
        }
        .meta {
            font-size: 0.85em;
            color: #888;
            margin-top: 15px;
            padding-top: 10px;
            border-top: 1px solid #eee;
        }
    </style>
</head>
<body>

   <div class="container">
        <h1>Elenco delle Richieste</h1>
        <?php if(empty($data)): ?>
            <p> Nessuna Richiesta Disponibile</p>
        <?php else: ?>
            <?php foreach ($data as $request): ?>
                <div class="card">
                    <h3><?php echo htmlspecialchars($request['titolo']); ?></h3>
                    <p><?php echo nl2br(htmlspecialchars($request['descrizione'])); ?></p>
                    <div class="meta">
                        <strong>Data:</strong> <?php echo htmlspecialchars($request['data']); ?><br>
                        <strong>Autore:</strong> <?php echo htmlspecialchars($request['autore']); ?>
                    </div>
                </div>
            <?php endforeach; ?>
        <?php endif; ?>
    </div>

</body>
</html>