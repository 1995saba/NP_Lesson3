using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NP_Lesson3
{
    public class ChatServerLogic
    {
        private static ManualResetEvent socketEvent = new ManualResetEvent(false);
        List<ClientContext> clientList = new List<ClientContext>();
        Dictionary<Guid, ClientContext> client2Id = new Dictionary<Guid, ClientContext>();
        private ClientContext RegisterClient(Socket socket)
        {
            lock (clientList)
            {
                var client = new ClientContext();
                client.Id = Guid.NewGuid();
                client.Socket = socket;
                clientList.Add(client);
                client2Id[client.Id] = client;
                return client;
            }
        }

        public void Start(string ip, int port)
        {
            Socket listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            listener.Listen(100);

            while (true)
            {
                socketEvent.Reset();
                Console.WriteLine("Waiting for a connection...");
                listener.BeginAccept(BeginAcceptCallBack, listener);
                socketEvent.WaitOne();
            }
        }

        public void BeginAcceptCallBack(IAsyncResult result)
        {
            Console.WriteLine($"Start accept in {Thread.CurrentThread.ManagedThreadId}");
            var client = ((Socket)result.AsyncState).EndAccept(result);
            socketEvent.Set();
            var context = RegisterClient(client);

            client.BeginReceive(context.Buffer, 0, ClientContext.BufferSize, SocketFlags.None,
                new AsyncCallback(ReadCallBack), context);
        }

        public static void ReadCallBack(IAsyncResult result)
        {
            Console.WriteLine($"Start read in {Thread.CurrentThread.ManagedThreadId}");
            ClientContext context = (ClientContext)result.AsyncState;
            Socket socket = context.Socket;
            int read = socket.EndReceive(result);

            if (read > 0)
            {
                context.sb.Append(Encoding.UTF8.GetString(context.Buffer, 0, read));
                socket.BeginReceive(context.Buffer, 0, ClientContext.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallBack), context);
            }
            else
            {
                if (context.sb.Length > 1)
                {
                    string strContent;
                    strContent = context.sb.ToString();
                    Console.WriteLine(String.Format(
                        $"Read {strContent.Length} byte from socket data = {strContent}"));
                }
            }
        }
    }

    public class ClientContext
    {
        public static int BufferSize = 1024;
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Socket Socket { get; set; }
        public byte[] Buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    class Program
    {
        static void Main(string[] args)
        {
            new ChatServerLogic().Start("10.3.4.64", 3080);
        }
    }
}
