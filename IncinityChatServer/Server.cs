using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using IncityChatServer.Util;
using System.Threading;

class Server
{

    private static AutoResetEvent connectionWaitHandle = new AutoResetEvent(false);

    public static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 80);
        listener.Start();

        while (true)
        {
            IAsyncResult result = listener.BeginAcceptTcpClient(HandleAsyncConnection, listener);
            connectionWaitHandle.WaitOne(); // Wait until a client has begun handling an event
            connectionWaitHandle.Reset(); // Reset wait handle or the loop goes as fast as it can (after first request)
        }
    }

    private static void HandleAsyncConnection(IAsyncResult result)
    {
        TcpListener listener = (TcpListener)result.AsyncState;
        TcpClient client = listener.EndAcceptTcpClient(result);
        connectionWaitHandle.Set(); //Inform the main thread this connection is now handled

        Thread chatThread = new Thread(new ParameterizedThreadStart(DoChat));
        chatThread.Start(client);

    }

    private static void DoChat(object param)
    {
        TcpClient client = (TcpClient)param;

        NetworkStream stream = client.GetStream();

        while (true)
        {
            while (!stream.DataAvailable) ;

            Byte[] bytes = new Byte[client.Available];

            stream.Read(bytes, 0, bytes.Length);

            //translate bytes of request to string
            String data = Encoding.UTF8.GetString(bytes);

            if (new Regex("^GET").IsMatch(data))
            {
                Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                    + "Connection: Upgrade" + Environment.NewLine
                    + "Upgrade: websocket" + Environment.NewLine
                    + "Sec-WebSocket-Protocol: chat" + Environment.NewLine
                    + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                        SHA1.Create().ComputeHash(
                            Encoding.UTF8.GetBytes(
                                new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                            )
                        )
                    ) + Environment.NewLine
                    + Environment.NewLine);

                stream.Write(response, 0, response.Length);
            }
            else
            {
                String message = CommunicationUtil.DecodeMessage(bytes);

                Console.WriteLine("Decoded message: {0}{1}", message, Environment.NewLine);

                String messageToClient = "Ahoj";

                Byte[] response = CommunicationUtil.EncodeMessage(messageToClient);
                stream.Write(response, 0, response.Length);

            }
        }
    }
}