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

        GameWorld world = new();
        ClassTypeListPlayer classTypeLiekEnum = new ClassTypeListPlayer();


        private static SemaphoreSlim _playersLock = new SemaphoreSlim(1, 1);
        public GameServer(int port)
        {
            myServer = new TcpListener(IPAddress.Any, port);
            myServer.Start();
            isRunning = true;

           
            classTypeLiekEnum.LoadFromFile().GetAwaiter().GetResult();
            world.LoadPlayers().GetAwaiter().GetResult();

            
            _ = world.StartAutoSave();

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
        //postarat se o mrtve loopy
        private async Task ClientLoop(TcpClient client)
        {

            Console.WriteLine("Klient se pripojil na server");

            bool isFirstLogin = false;

            //zde se vytvari novy hrac pro klienta
            Player player = new Player();

            using var cts = new CancellationTokenSource();
            try
            {
                using (client)
                using (StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.UTF8))
                {

                    player.Reader = reader;
                    player.Writer = writer;


                    writer.AutoFlush = true;





                    //prihlaseni a nastavni jmena hrace, pokud se nepodari prihlasit, klient se odpoji


                    bool clientConnect = await world.LogInPlayers(reader, writer, player);




                    if (!clientConnect)
                    {
                        Console.WriteLine("Klient se nepodarilo prihlasit");
                        return;
                    }







                    await _playersLock.WaitAsync();

                    try
                    {
                        world.OnlinePlayers.Add(player);
                    }
                    finally
                    {
                        _playersLock.Release();
                    }

                    WriteToConsole.BroadcastAll($"■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■ \n ----\n Hrac {player.Name} se pripojil na server \n ----\n ■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■□■ ", world.OnlinePlayers);



                    isFirstLogin = true;

                    WriteToConsole.TextToPlayer(player, "\n You are now in the game world. Type 'help' for a list of commands.");


                    _ = world.PlayerAutoSave(player, cts.Token);



                    //wait classTypeLiekEnum.LoadFromFile();





                    while (clientConnect)
                    {

                        //main loop betwwen player and game world 

                        await world.GameLoop(player);


                        WriteToConsole.TextToPlayer(player, "☠︎---☠︎---☠︎---☠︎---☠︎---☠︎---☠︎---☠_☠---☠︎---☠︎---☠︎---☠︎---☠︎ \n Back in LOBBY wanna join AGAIN? yes/no");

                        string? answer = await WriteToConsole.TakeInput(player);

                        if (answer.ToLower() == "yes")
                        {



                            continue;
                        }
                        else
                        {

                            clientConnect = false;
                            WriteToConsole.TextToPlayer(player, "You have been disconnected from the server. See you next time!");
                            throw new Exception("Player chose to disconnect");

                        }






                    }

                }


                //doebirat neco z listu co tam nebylo by to hodilo chybu



            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba v komunikaci s {player.Name}  : {ex.Message}");
            }


            finally
            {

                cts.Cancel();
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

                Console.WriteLine($"Klient {player.Name} se odpojil od serveru");
            }
        }
    }
}