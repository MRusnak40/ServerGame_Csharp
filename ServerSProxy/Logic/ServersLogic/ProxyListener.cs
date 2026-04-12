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
        await server.ConnectAsync(targetIp, targetPort);

        StreamReader odKlienta = new StreamReader(clientToProxy.GetStream(), Encoding.UTF8);
        StreamWriter KeKlientovi = new StreamWriter(clientToProxy.GetStream(), Encoding.UTF8);
        KeKlientovi.AutoFlush = true;

        StreamReader odServeru = new StreamReader(server.GetStream(), Encoding.UTF8);
        StreamWriter KeServeru = new StreamWriter(server.GetStream(), Encoding.UTF8);
        KeServeru.AutoFlush = true;

        // Cteme od klienta a posilame serveru
        Task smer1 = Task.Run(async () =>
        {
            string? zprava;
            while ((zprava = await odKlienta.ReadLineAsync()) != null)
            {
                Console.WriteLine($"Klient rekl: {zprava}");
                await KeServeru.WriteLineAsync(zprava);
            }
        });

        // Cteme od serveru a posilame klientovi
        Task smer2 = Task.Run(async () =>
        {
            string? zprava;
            while ((zprava = await odServeru.ReadLineAsync()) != null)
            {
                Console.WriteLine($"Server rekl: {zprava}");
                await KeKlientovi.WriteLineAsync(zprava);
            }
        });

        // Cekame dokud se nekdo neodpoji
        await Task.WhenAny(smer1, smer2);

        Console.WriteLine("Klient se odpojil od proxy");
    }
}