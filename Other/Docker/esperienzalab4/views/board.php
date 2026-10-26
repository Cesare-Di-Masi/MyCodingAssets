<!DOCTYPE html>
<html lang="it">
<head>
    <meta charset="UTF-8">
    <title>La mia prima pagina PHP in Docker</title>
    <link rel="stylesheet" href="public/css/style.css">
</head>
<body>

   <div class="container">
        <h1>Elenco delle Richieste</h1>
        <?php if(empty($data)): ?>
            <p> Nessuna Richiesta Disponibile</p>
        <?php else: ?>
            <?php foreach ($data as $request): ?>
                <div class="card">
                    <h3><?php echo htmlspecialchars($request -> titolo); ?></h3>
                    <p><?php echo nl2br(htmlspecialchars($request->descrizione)); ?></p>
                    <div class="meta">
                        <strong>Data:</strong> <?php echo htmlspecialchars($request->data); ?><br>
                        <strong>Autore:</strong> <?php echo htmlspecialchars($request->autore); ?>
                    </div>
                </div>
            <?php endforeach; ?>
        <?php endif; ?>
    </div>

</body>
</html>