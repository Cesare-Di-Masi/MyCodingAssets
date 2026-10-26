<?php

namespace App\Domain;

interface RequestRepositoryInterface
{
    public function findAll(): array;
}