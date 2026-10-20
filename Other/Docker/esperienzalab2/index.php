<?php
require_once './vendor/autoload.php'; //carichiamo il file autoload che ha generato composer, così possiamo usare le classi senza fare require di ogni singolo file

use App\Infrastructure\JsonRequestRepository;
use App\Application\GetRequestList;

$dataPath = './data/requests.json';
$repository = new JsonRequestRepository($dataPath);
$getRequestList = new GetRequestList($repository);
$data = $getRequestList->execute();


require_once './views/board.php'; //includiamo la view che visualizza i dati
