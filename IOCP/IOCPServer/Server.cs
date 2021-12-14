using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IOCPServer
{
    internal class Server
    {
        private readonly int numConnections;
        private readonly int receiveBufferSize;
        private int totalBytesRead;
        private int numConnectedSockets;
        private BufferManager bufferManager;
        private SocketAsyncEventArgsPool readWritePool;
        private SemaphoreSlim maxNumberAcceptedClinets;
        private Socket? listenSocket;
        const int opsToPreAlloc = 2;

        public Server(int numConnections, int receiveBufferSize)
        {
            this.numConnections = numConnections;
            this.receiveBufferSize = receiveBufferSize;
            this.totalBytesRead = 0;
            this.numConnectedSockets = 0;
            this.bufferManager = new BufferManager(
                receiveBufferSize * numConnections * opsToPreAlloc,
                receiveBufferSize);
            readWritePool = new SocketAsyncEventArgsPool(numConnections);
            maxNumberAcceptedClinets = new SemaphoreSlim(numConnections, numConnections);
        }

        public void Init()
        {
            SocketAsyncEventArgs readWriteEnvetArg;
            for (int i = 0; i < numConnections; i++)
            {
                readWriteEnvetArg = new SocketAsyncEventArgs();
                readWriteEnvetArg.UserToken = new AsyncUserToken();
                readWriteEnvetArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                bufferManager.SetBuffer(readWriteEnvetArg);
                readWritePool.Push(readWriteEnvetArg);
            }
        }

        public void Start(IPEndPoint localEndPoint)
        {
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(100);

            StartAccept(null);

            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }

        private void StartAccept(SocketAsyncEventArgs? acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            maxNumberAcceptedClinets.Wait();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                numConnectedSockets);
            SocketAsyncEventArgs readEventArgs = readWritePool.Pop();
            (readEventArgs.UserToken as AsyncUserToken).Socket = e.AcceptSocket;

            if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(readEventArgs);
            }

            StartAccept(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            var token = e.UserToken as AsyncUserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Interlocked.Add(ref totalBytesRead, e.BytesTransferred);
                Console.WriteLine("The server has read a total of {0} bytes", totalBytesRead);

                Console.WriteLine("The server receive '{0}'", Encoding.ASCII.GetString(e.Buffer, e.Offset, e.BytesTransferred));
                e.SetBuffer(e.Offset, e.BytesTransferred);
                if (!token.Socket.SendAsync(e))
                {
                    ProcessSend(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            var token = e.UserToken as AsyncUserToken;
            if (token == null)
            {
                throw new NullReferenceException();
            }

            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
                token.Socket.Close();
            }

            Interlocked.Decrement(ref numConnectedSockets);
            readWritePool.Push(e);
            maxNumberAcceptedClinets.Release();
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", numConnectedSockets);
        }

        private void AcceptEventArg_Completed(object? sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void IO_Completed(object? sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var token = e.UserToken as AsyncUserToken;
                if (token == null)
                {
                    throw new NullReferenceException();
                }

                if (!token.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
    }
}
