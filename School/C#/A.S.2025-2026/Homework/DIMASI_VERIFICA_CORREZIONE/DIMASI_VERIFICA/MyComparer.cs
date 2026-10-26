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
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;
            return y.CompareTo(x);
        }
    }
}