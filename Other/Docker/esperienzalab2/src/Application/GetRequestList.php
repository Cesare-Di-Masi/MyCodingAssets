<?php


namespace App\Application;

use App\Domain\RequestRepositoryInterface;
use App\Domain\Request;

class GetRequestList
{
    public function __construct(
        private RequestRepositoryInterface $requestRepository
    ){}


    //buona norma dare il nome execute a tutti i metodi che eseguono un'azione
    // in questo caso recuperare la lista delle richieste
    public function execute():array
    {
        //sort --> sort crescente
        //usort -->sort decrescente
        return $this->requestRepository->findAll();
    }

}