using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMASI_VERIFICA
{
    public class Storage
    {
        private Dictionary<Product, int> _storageInventory;

        private PriorityQueue<Order, Order> _orderQueue = new PriorityQueue<Order, Order>(new MyComparer());

        public Dictionary<Product, int> StorageInventory
        {
            get { return _storageInventory; }
            set { _storageInventory = value; }
        }

        public PriorityQueue<Order, Order> OrderQueue
        { get { return _orderQueue; } }

        public Storage(Dictionary<Product, int> storageInventary = null)
        {
            if (storageInventary == null)
                StorageInventory = new Dictionary<Product, int>();
            else
                StorageInventory = storageInventary;
        }

        public void AddNewProduct(Product product, int quantity)
        {
            _storageInventory.Add(product, quantity);
        }

        public void RemoveProduct(Product product)
        {
            _storageInventory.Remove(product);
        }

        public void AddNewOrder(Order order)
        {
            _orderQueue.Enqueue(order, order);
        }

        public Order ElaborateOrder()
        {
            Order order = _orderQueue.Dequeue();

            foreach (var item in order.ProductDetails.Keys)
            {
                if (StorageInventory.TryGetValue(item, out int qu) == false)
                    throw new Exception($"{item} does not exist in storage");

                int quantity = order.ProductDetails[item];
                if (quantity > StorageInventory[item])
                    throw new Exception("invalid item quantity");
                StorageInventory[item] -= quantity;
            }

            return order;
        }
    }
}