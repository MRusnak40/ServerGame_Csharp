using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
namespace ServerSProxy
{

    public class TranslationServer
    {
        private TcpListener myServer;
        private bool isRunning;
        private Dictionary<string, string> dictionary;

        public TranslationServer(int port)
        {
            //vraci to do konzole hraci
            dictionary = new Dictionary<string, string>()
        {
            { "dog", "pes" },
            { "cat", "kocka" },
            { "house", "dum" },
            { "car", "auto" },
            { "school", "skola" }
        };

            myServer = new TcpListener(IPAddress.Any, port);
            myServer.Start();
            isRunning = true;

            ServerLoop();
        }

        private async void ServerLoop()
        {
            Console.WriteLine("Prekladovy server byl spusten");

            while (isRunning)
            {
                TcpClient client = await myServer.AcceptTcpClientAsync();
                _ = Task.Run(() => ClientLoop(client));
            }
        }

        private async Task ClientLoop(TcpClient client)
        {
            Console.WriteLine("Klient se pripojil na server");

            using (client)
            using (StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.UTF8))
            {
                writer.AutoFlush = true;
                bool clientConnect = true;

                while (clientConnect)
                {
                    string? data = await reader.ReadLineAsync();

                    //pri nefungovani musis zapnout nebo vypnout jak u proxy tak u serveru
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        //clientConnect = false;
                        continue;
                    }

                    data = data.ToLower();
                    Console.WriteLine($"Server prijal: {data}");

                    if (data == "exit")
                    {
                        await writer.WriteLineAsync("Komunikace ukoncena");
                        clientConnect = false;
                        continue;
                    }

                    if (dictionary.ContainsKey(data))
                    {
                        await writer.WriteLineAsync(dictionary[data]);
                    }
                    else
                    {
                        await writer.WriteLineAsync("Slovo neznam");
                    }
                }
            }

            Console.WriteLine("Klient se odpojil od serveru");
        }
    }
}