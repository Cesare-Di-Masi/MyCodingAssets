<?php

// dirname(__DIR__) risale dalla cartella 'src' alla radice del progetto
$jsonPath = dirname(__DIR__) . '/data/requests.json';

if (!file_exists($jsonPath)) {
    die("Errore: Il file requests.json non è stato trovato in data/requests.json.");
}

$jsonData = file_get_contents($jsonPath);
$richieste = json_decode($jsonData, true);

if ($richieste === null) {
    die("Errore nella lettura del file JSON o formato non valido.");
}
?>
<!DOCTYPE html>
<html lang="it">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Bacheca Richieste - Step 2</title>
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
    <h1>Bacheca Richieste Assistenza</h1>

    <?php if (empty($richieste)): ?>
        <p>Nessuna richiesta presente in bacheca.</p>
    <?php else: ?>
        <?php foreach ($richieste as $richiesta): ?>
            <div class="card">
                <h3><?= htmlspecialchars($richiesta['titolo']) ?></h3>
                <p><?= htmlspecialchars($richiesta['descrizione']) ?></p>
                <div class="meta">
                    <strong>Inviato da:</strong> <?= htmlspecialchars($richiesta['autore']) ?> | 
                    <strong>Data:</strong> <?= htmlspecialchars($richiesta['data']) ?>
                </div>
            </div>
        <?php endforeach; ?>
    <?php endif; ?>
</div>

</body>
</html>