<?php

namespace App\Application;

use App\Domain\Request;
use App\Domain\RequestRepositoryInterface;

class GetRequestsListUseCase
{
    public function __construct(
        private RequestRepositoryInterface $repository
    ) {}

    /**
     * @return Request[]
     */
    public function execute(): array
    {
        $requests = $this->repository->findAll();

        // Ordinamento per data decrescente (dalla più recente)
        usort($requests, function (Request $a, Request $b) {
            return strcmp($b->data, $a->data);
        });

        return $requests;
    }
}