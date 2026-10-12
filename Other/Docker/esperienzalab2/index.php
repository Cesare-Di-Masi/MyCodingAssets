<!DOCTYPE html>
<html lang="it">
<head>
    <meta charset="UTF-8">
    <title>La mia prima pagina PHP in Docker</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 700px; margin: 40px auto; padding: 20px; }
        h1 { color: #2c3e50; }
        .info { background: #ecf0f1; padding: 15px; border-radius: 8px; margin: 20px 0; }
        form { margin: 20px 0; }
        input, button { padding: 8px; margin: 5px 0; font-size: 1em; }
        button { background: #3498db; color: white; border: none; border-radius: 4px; cursor: pointer; }
        .result { background: #d4edda; padding: 10px; border-radius: 4px; margin-top: 10px; }
    </style>
</head>
<body>

    <h1>🐳 Ciao dal container Docker!</h1>

    <!-- Sezione 1: informazioni sul server -->
    <div class="info">
        <p><strong>Versione PHP:</strong> <?php echo phpversion(); ?></p>
        <p><strong>Sistema operativo:</strong> <?php echo php_uname(); ?></p>
        <p><strong>Ora del server:</strong> <?php echo date('d/m/Y H:i:s'); ?></p>
    </div>

    <!-- Sezione 2: form che invia dati a se stesso (stesso file) -->
    <h2>Prova il form</h2>
    <form method="POST" action="">
        <label for="nome">Il tuo nome:</label><br>
        <input type="text" name="nome" id="nome" required>
        <br>
        <button type="submit" name="invia">Invia</button>
    </form>

    <?php
    // Sezione 3: elaborazione del form (solo se è stato inviato)
    if (isset($_POST['invia'])) {
        // htmlspecialchars previene attacchi XSS
        $nome = htmlspecialchars($_POST['nome']);
        echo "<div class='result'>";
        echo "Ciao, <strong>$nome</strong>! La pagina è stata generata dal container PHP.";
        echo "</div>";
    }
    ?>

</body>
</html>