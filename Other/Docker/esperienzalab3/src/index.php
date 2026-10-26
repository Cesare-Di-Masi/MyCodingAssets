<?php

require_once __DIR__ . '/Domain/Request.php';
require_once __DIR__ . '/Domain/RequestRepositoryInterface.php';
require_once __DIR__ . '/Infrastructure/JsonRequestRepository.php';
require_once __DIR__ . '/Application/GetRequestsListUseCase.php';

use App\Infrastructure\JsonRequestRepository;
use App\Application\GetRequestsListUseCase;

// 1. Setup Infrastruttura
$dataPath = dirname(__DIR__) . '/data/requests.json';
$repository = new JsonRequestRepository($dataPath);

// 2. Setup ed Esecuzione dello Use Case
$useCase = new GetRequestsListUseCase($repository);
$richieste = $useCase->execute();

// 3. Renderizzazione HTML
?>
<!DOCTYPE html>
<html lang="it">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Bacheca Richieste - Pure Clean Architecture</title>
    <style>
        body { font-family: Arial, sans-serif; background: #f4f6f9; margin: 0; padding: 30px; }
        .container { max-width: 800px; margin: 0 auto; }
        h1 { color: #333; border-bottom: 2px solid #007bff; padding-bottom: 10px; }
        .card { background: #fff; padding: 20px; border-radius: 6px; margin-bottom: 15px; border-left: 4px solid #007bff; box-shadow: 0 2px 4px rgba(0,0,0,0.05); }
        .card h3 { margin-top: 0; color: #111; }
        .meta { color: #777; font-size: 0.85em; margin-top: 10px; border-top: 1px solid #eee; padding-top: 8px; }
    </style>
</head>
<body>

<div class="container">
    <h1>Bacheca Richieste (Clean Architecture)</h1>

    <?php if (empty($richieste)): ?>
        <p>Nessuna richiesta presente.</p>
    <?php else: ?>
        <?php foreach ($richieste as $richiesta): ?>
            <div class="card">
                <h3><?= htmlspecialchars($richiesta->titolo) ?></h3>
                <p><?= htmlspecialchars($richiesta->descrizione) ?></p>
                <div class="meta">
                    <strong>Inviato da:</strong> <?= htmlspecialchars($richiesta->autore) ?> | 
                    <strong>Data:</strong> <?= htmlspecialchars($richiesta->data) ?>
                </div>
            </div>
        <?php endforeach; ?>
    <?php endif; ?>
</div>

</body>
</html>