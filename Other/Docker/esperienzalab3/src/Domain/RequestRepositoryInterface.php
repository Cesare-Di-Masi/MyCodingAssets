<?php

namespace App\Domain;

interface RequestRepositoryInterface
{
    /**
     * @return Request[]
     */
    public function findAll(): array;
}