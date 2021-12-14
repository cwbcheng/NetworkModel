using IOCPServer;
using System.Net;

var server = new Server(100, 1024 * 1024);
server.Init();
IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
server.Start(new IPEndPoint(iPAddress, 7300));