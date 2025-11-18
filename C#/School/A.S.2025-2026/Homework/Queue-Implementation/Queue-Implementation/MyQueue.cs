using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Queue_Implementation
{
    public class MyQueue
    {
        private LinkedList<object> _list = new LinkedList<object>();

        public MyQueue()
        {
            _list = new LinkedList<object>();
        }

        public void Enqueue(object item)
        {
            _list.AddLast(item);
        }

        public object Dequeue()
        {
            if (_list.Count == 0) throw new InvalidOperationException("Queue is empty");
            var value = _list.First.Value;
            _list.RemoveFirst();
            return value;
        }

        public object Peek()
        {
            if (_list.Count == 0) throw new InvalidOperationException("Queue is empty");
            return _list.First.Value;
        }

        public bool IsEmpty()
        {
            return _list.Count == 0;
        }

        public int Size()
        {
            return _list.Count;
        }
    }
}