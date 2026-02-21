using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DIMASI_VERIFICA;

namespace VerificaTest
{
    [TestClass]
    public sealed class StorageTests
    {
        [TestMethod]
        public void AggiungiProdotto_ProdottoNonEsistente_AggiungeNuovoProdotto()
        {
            Storage storage = new Storage();
            Product product = new Product("pro01", "gelato", 2.0);
            storage.AddNewProduct(product, 2);
            Assert.IsTrue(storage.StorageInventory.ContainsKey(product));
        }

        [TestMethod]
        public void AggiungiProdotto_ProdottoDuplicato_LanciaArgumentException()
        {
            Storage storage = new Storage();
            Product product = new Product("pro01", "gelato", 2.0);
            storage.AddNewProduct(product, 2);
            // Storage.AddNewProduct usa Dictionary.Add internamente: se la chiave esiste viene sollevata ArgumentException
            Assert.ThrowsException<ArgumentException>(() => storage.AddNewProduct(product, 3));
        }

        [TestMethod]
        public void AggiungiOrdine_OrdineValido_VieneMessoInCoda()
        {
            Storage storage = new Storage();
            Product product = new Product("pro01", "gelato", 2.0);
            var dettaglioOrdine = new Dictionary<Product, int> { { product, 2 } };
            DateOnly dataOrdine = DateOnly.FromDateTime(DateTime.Now);
            Order order = new Order("ord01", "Anna", dataOrdine, dettaglioOrdine, true);

            storage.AddNewOrder(order);

            // Poiché è il solo elemento in coda, rimuovendolo otteniamo l'istanza inserita
            Order dequeued = storage.OrderQueue.Dequeue();
            Assert.AreEqual(order, dequeued);
        }

        [TestMethod]
        public void ElaboraProssimoOrdine_OrdineSoddisfatto_RiduceInventarioERitornaOrdine()
        {
            Storage storage = new Storage();
            Product product = new Product("pro01", "gelato", 2.0);
            storage.AddNewProduct(product, 5);

            var dettaglioOrdine = new Dictionary<Product, int> { { product, 2 } };
            Order order = new Order("ord001", "Anna", DateOnly.FromDateTime(DateTime.Now), dettaglioOrdine, true);

            storage.AddNewOrder(order);
            Order risultato = storage.ElaborateOrder();

            Assert.AreEqual(order, risultato);
            Assert.AreEqual(3, storage.StorageInventory[product]);
        }
    }
}