<?php

namespace App\Infrastructure;

use App\Domain\Request;
use App\Domain\RequestRepositoryInterface;
use RuntimeException;

class JsonRequestRepository implements RequestRepositoryInterface
{
    public function __construct(
        private string $filePath
    ) {}

    /**
     * @return Request[]
     */
    public function findAll(): array
    {
        if (!file_exists($this->filePath)) {
            throw new RuntimeException("Impossibile trovare il file dei dati: {$this->filePath}");
        }

        $content = file_get_contents($this->filePath);
        $data = json_decode($content, true);

        if ($data === null) {
            return [];
        }

        $requests = [];
        foreach ($data as $item) {
            $requests[] = new Request(
                id: $item['id'],
                titolo: $item['titolo'],
                descrizione: $item['descrizione'],
                autore: $item['autore'],
                data: $item['data']
            );
        }

        return $requests;
    }
}