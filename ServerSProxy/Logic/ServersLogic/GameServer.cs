using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace ServerSProxy
{

    public class GameServer
    {
        private TcpListener myServer;
        private bool isRunning;

        gameWorld world = new();


        private static SemaphoreSlim _playersLock = new SemaphoreSlim(1, 1);
        public GameServer(int port)
        {
            myServer = new TcpListener(IPAddress.Any, port);
            myServer.Start();
            isRunning = true;


            ServerLoop();
        }

        private async void ServerLoop()
        {
            Console.WriteLine("Herni server byl spusten");
            //zde se vytvari nove vlakno pro hrace
            while (isRunning)
            {
                TcpClient client = await myServer.AcceptTcpClientAsync();
                _ = Task.Run(() => ClientLoop(client));
            }
        }


        //hracsky loop
        private async Task ClientLoop(TcpClient client)
        {

            Console.WriteLine("Klient se pripojil na server");

            bool isFirstLogin = false;

            Player player = new Player();

            using (client)
            using (StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.UTF8))
            {


                writer.AutoFlush = true;


                //zde se vytvari novy hrac pro klienta


                //prihlaseni a nastavni jmena hrace, pokud se nepodari prihlasit, klient se odpoji
                bool clientConnect = await world.LogInPlayers(reader, writer, player);

                if (!clientConnect)
                {
                    Console.WriteLine("Klient se nepodarilo prihlasit");
                    return;
                }


                WriteToConsole.BroadcastAll($"■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■ \n ----\n Hrac {player.Name} se pripojil na server \n ----\n ■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■ ", world.OnlinePlayers);




                await _playersLock.WaitAsync();
                try
                {
                    world.OnlinePlayers.Add(player);
                }
                finally
                {
                    _playersLock.Release();
                }




                isFirstLogin = true;

                while (clientConnect)
                {

                    //main loop betwwen player and game world 
                    player.LastActive = DateTime.Now;
                    await world.GameLoop(player);



                }

            }
            //doebirat neco z listu co tam nebylo by to hodilo chybu


            if (player.Name != null && isFirstLogin)
            {
                await _playersLock.WaitAsync();
                try
                {
                    world.OnlinePlayers.Remove(player);
                }
                finally
                {
                    _playersLock.Release();
                }
            }

            Console.WriteLine("Klient se odpojil od serveru");
        }
    }
}