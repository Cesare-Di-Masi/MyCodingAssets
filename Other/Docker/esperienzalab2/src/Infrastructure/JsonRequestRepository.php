<?php 
namespace App\Infrastructure;

use App\Domain\Request;
use App\Domain\RequestRepositoryInterface;
use RuntimeException;

class JsonRequestRepository implements RequestRepositoryInterface
{

    public function __construct(private string $filepath)
    {
    }

    public function findAll(): array
    {
        if (!file_exists($this->filepath)) {
            throw new RuntimeException("File not found: " . $this->filepath);
        }

        $jsonContent = file_get_contents($this->filepath);
        $data = json_decode($jsonContent, true);

        if ($data === null) {
            return[];
        }

        $requests = [];
        foreach($data as $item) //foreach è al contrario rispetto ad altri linguaggi, prima lista e poi item
        {
            $requests[] = new Request( //array[] = new Request(..) --> aggiunge un nuovo oggetto Request all'array $requests
                id: $item['id'],
                titolo: $item['titolo'],
                descrizione: $item['descrizione'],
                data: $item['data'],
                autore: $item['autore']
            );
        }

        return $requests;
    }

}