using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IOCPServer
{
    internal class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            else
            {
                Monitor.Enter(pool);
                pool.Push(item);
                Monitor.Exit(pool);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            Monitor.Enter(pool);
            var temp = pool.Pop();
            Monitor.Exit(pool);
            return temp;
        }

        public int Count
        {
            get { return pool.Count; }
        }
    }
}
