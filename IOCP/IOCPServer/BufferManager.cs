using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IOCPServer
{
    internal class BufferManager
    {
        private readonly int numBytes;
        private readonly int bufferSize;
        private int currentIndex;
        private byte[] buffer;
        private readonly Stack<int> freeIndexPool;

        public BufferManager(int totalBytes, int bufferSize)
        {
            this.numBytes = totalBytes;
            this.bufferSize = bufferSize;
            this.currentIndex = 0;
            this.freeIndexPool = new Stack<int>();
            this.buffer = new byte[totalBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (freeIndexPool.Count > 0)
            {
                args.SetBuffer(buffer, freeIndexPool.Pop(), bufferSize);
                return true;
            }
            else if ((numBytes - bufferSize) < currentIndex)
            {
                return false;
            }
            else
            {
                args.SetBuffer(buffer, currentIndex, bufferSize);
                currentIndex += bufferSize;
                return true;
            }
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
