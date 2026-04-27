using System.Net;
using System.Net.Sockets;
using System.Text;

public class ProxyListener
{
    private TcpListener myProxy;
    private bool isRunning;
    private string targetIp;
    private int targetPort;

    public ProxyListener(int proxyPort, string targetIp, int targetPort)
    {
        this.targetIp = targetIp;
        this.targetPort = targetPort;
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

        TcpClient server = new TcpClient();
        try
        {
            await server.ConnectAsync(targetIp, targetPort);
        }
        catch
        {
            Console.WriteLine("Nepodařilo se připojit na server");
            clientToProxy.Close();
            return;
        }

        try
        {
            using (var odKlienta = new StreamReader(clientToProxy.GetStream(), Encoding.UTF8))
            using (var KeKlientovi = new StreamWriter(clientToProxy.GetStream(), Encoding.UTF8))
            using (var odServeru = new StreamReader(server.GetStream(), Encoding.UTF8))
            using (var KeServeru = new StreamWriter(server.GetStream(), Encoding.UTF8))
            {
                KeServeru.AutoFlush = true;
                KeKlientovi.AutoFlush = true;

                Task smer1 = Task.Run(async () =>
                {
                    try
                    {
                        string? zprava;
                        while ((zprava = await odKlienta.ReadLineAsync()) != null)
                        {
                            Console.WriteLine($"Klient rekl: {zprava}");
                            await KeServeru.WriteLineAsync(zprava);
                        }
                    }
                    catch { }
                });

                Task smer2 = Task.Run(async () =>
                {
                    try
                    {
                        string? zprava;
                        while ((zprava = await odServeru.ReadLineAsync()) != null)
                        {
                            Console.WriteLine($"Server rekl: {zprava}");
                            await KeKlientovi.WriteLineAsync(zprava);
                        }
                    }
                    catch { }
                });

                await Task.WhenAny(smer1, smer2);
            }
        }
        finally
        {
            clientToProxy.Close();
            server.Close();
            Console.WriteLine("Klient se odpojil od proxy");
        }
    }
}