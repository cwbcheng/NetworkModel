using IOCPClient;

SocketAsyncClient socket = new SocketAsyncClient("127.0.0.1", 7300);
socket.Connect();
Console.WriteLine(socket.SendReceive("aaa"));
Console.WriteLine(socket.SendReceive("aaa"));
Console.ReadLine();