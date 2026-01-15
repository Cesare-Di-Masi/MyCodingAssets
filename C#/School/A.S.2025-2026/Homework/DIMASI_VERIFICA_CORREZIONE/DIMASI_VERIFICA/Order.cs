using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMASI_VERIFICA
{
    public class Order : IComparable<Order>
    {
        private string _id, _clientName;
        private DateOnly _orderDate;
        private Dictionary<Product, int> _productDetails;
        private bool _priority;

        public string ID
        {
            get { return _id; }
            private set
            {
                if (string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("illegal ID for order");
                }
                _id = value;
            }
        }

        public string ClientName
        {
            get { return _clientName; }
            private set
            {
                if (string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("illegal name for client");
                }
                _clientName = value;
            }
        }

        public DateOnly OrderDate
        {
            get { return _orderDate; }
            private set
            {
                //controllo che la data dell'ordine non sia passata
                if (value.CompareTo(DateOnly.FromDateTime(DateTime.Now)) < 0)
                    throw new ArgumentException("illegal orderDate");
                _orderDate = value;
            }
        }

        public Dictionary<Product, int> ProductDetails
        {
            get { return _productDetails; }
            private set { _productDetails = value; }
        }

        public bool Priority
        {
            get { return _priority; }
        }

        public Order(string id, string clientName, DateOnly orderDate, Dictionary<Product, int> productDetail, bool priority)
        {
            ID = id;
            ClientName = clientName;
            OrderDate = orderDate;
            ProductDetails = productDetail;
            _priority = priority;
        }

        public int CompareTo(Order? other)
        {
            if (other == null) return 1;

            if (other.Priority != Priority)
            {
                return Priority.CompareTo(other.Priority);
            }
            else
            {
                var a = OrderDate.CompareTo(other.OrderDate);
                if (a < 0) return 1;
                if (a > 0) return -1;
                return 0;
            }
        }
    }
}