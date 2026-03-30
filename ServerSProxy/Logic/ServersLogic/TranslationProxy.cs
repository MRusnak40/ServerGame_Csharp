using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ServerSProxy
{
    public class TranslationProxy
    {
        private TcpListener myProxy;
        private bool isRunning;
        private string targetIp;
        private int targetPort;
        private Dictionary<string, string> cache;

        public TranslationProxy(int proxyPort, string targetIp, int targetPort)
        {
            this.targetIp = targetIp;
            this.targetPort = targetPort;
            //neni potreba catche
            cache = new Dictionary<string, string>();

            myProxy = new TcpListener(IPAddress.Any, proxyPort);
            myProxy.Start();
            isRunning = true;

            ServerLoop();
        }

        private async void ServerLoop()
        {
            Console.WriteLine("Proxy byla spustena");

            while (isRunning)
            {
                TcpClient client = await myProxy.AcceptTcpClientAsync();
                _ = Task.Run(() => ClientLoop(client));
            }
        }

        private async Task ClientLoop(TcpClient clientToProxy)
        {
            Console.WriteLine("Klient se pripojil na proxy");

            using (clientToProxy)
            using (StreamReader clientReader = new StreamReader(clientToProxy.GetStream(), Encoding.UTF8))
            using (StreamWriter clientWriter = new StreamWriter(clientToProxy.GetStream(), Encoding.UTF8))
            {
                clientWriter.AutoFlush = true;
                bool clientConnect = true;

                while (clientConnect)
                {
                    string? data = await clientReader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(data))
                    {
                        //clientConnect = false;
                        continue;
                    }

                    data = data.ToLower();
                    Console.WriteLine($"Proxy prijala: {data}");

                    string response;

                    if (data == "exit")
                    {
                        response = await ForwardMessageToServer(data);
                        await clientWriter.WriteLineAsync(response);
                        clientConnect = false;
                        continue;
                    }

                    if (cache.ContainsKey(data))
                    {
                        response = cache[data];
                        Console.WriteLine("Proxy vratila odpoved z cache");
                    }
                    else
                    {
                        response = await ForwardMessageToServer(data);
                        cache[data] = response;
                        Console.WriteLine("Proxy ulozila odpoved do cache");
                    }

                    await clientWriter.WriteLineAsync(response);
                }
            }

            Console.WriteLine("Klient se odpojil od proxy");
        }

        private async Task<string> ForwardMessageToServer(string message)
        {
            using (TcpClient proxyToServer = new TcpClient())
            {
                await proxyToServer.ConnectAsync(targetIp, targetPort);

                using (StreamReader serverReader = new StreamReader(proxyToServer.GetStream(), Encoding.UTF8))
                using (StreamWriter serverWriter = new StreamWriter(proxyToServer.GetStream(), Encoding.UTF8))
                {
                    serverWriter.AutoFlush = true;

                    await serverWriter.WriteLineAsync(message);
                    string? response = await serverReader.ReadLineAsync();

                    return response ?? "Server neodpovedel";
                }
            }
        }
    }
}
