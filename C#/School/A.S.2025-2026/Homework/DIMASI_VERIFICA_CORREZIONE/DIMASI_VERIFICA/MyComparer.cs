using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMASI_VERIFICA
{
    public class MyComparer : IComparer<Order>
    {
        public int Compare(Order? x, Order? y)
        {
            return y.CompareTo(x);
        }
    }
}