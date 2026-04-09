using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.GameWorldCode
{
    internal class gameWorld
    {

        List<Map>? _mapsInGameWorld;

        private int _numberOfMapsInGameWorld;

        private List<Player> _onlinePlayers;
        Login login = new Login();
        private string pathToJsonPlayerList = "";
        private List<Player> _accounts;


        public List<Player> Accounts
        {
            get { return _accounts; }
            set { _accounts = value; }
        }

        public List<Map>? MapsInGameWorld
        {
            get { return _mapsInGameWorld; }
            set { _mapsInGameWorld = value; }
        }

        public int NumberOfMapsInGameWorld
        {
            get { return _numberOfMapsInGameWorld; }
            set { _numberOfMapsInGameWorld = value; }
        }

        public List<Player> OnlinePlayers
        {
            get { return _onlinePlayers; }
            set { _onlinePlayers = value; }
        }


        public async Task LoadGameWorld()
        {
            // Implementace načítání světa z JSON souboru
            string jsonFilePath = "gameWorld.json"; // Cesta k JSON souboru
            if (File.Exists(jsonFilePath))
            {
                string jsonData = await File.ReadAllTextAsync(jsonFilePath);
                gameWorld loadedGameWorld = System.Text.Json.JsonSerializer.Deserialize<gameWorld>(jsonData);
                if (loadedGameWorld != null)
                {
                    MapsInGameWorld = loadedGameWorld.MapsInGameWorld;
                    NumberOfMapsInGameWorld = loadedGameWorld.NumberOfMapsInGameWorld;
                    OnlinePlayers = loadedGameWorld.OnlinePlayers;
                }
            }
        }


        public async Task SaveGameWorld()
        {
            // Implementace ukládání světa do JSON souboru
            string jsonFilePath = "gameWorld.json"; // Cesta k JSON souboru
            string jsonData = System.Text.Json.JsonSerializer.Serialize(this);
            await File.WriteAllTextAsync(jsonFilePath, jsonData);
        }

        //napojeni na connected pro hrace jinak ho to vyhodi
        public async Task<bool> LogInPlayers(StreamReader reader, StreamWriter writer)
        {   
            Task loadTask=LoadPlayers();


            Player player = new Player();

            player.Reader = reader;
            player.Writer = writer;

            WriteToConsole.TextToPlayer(player, "\n Welcome to the game! Please enter your name:");

            string name = await WriteToConsole.TakeInput(player);

            WriteToConsole.TextToPlayer(player, "\n Please enter your password:");
            string password = await WriteToConsole.TakeInput(player);

            if (await login.VerifyPassword(name, password))
            {
                WriteToConsole.TextToPlayer(player, "\n Login successful! Welcome back, " + name + "!");
                return true;
            }
            else
            {
                WriteToConsole.TextToPlayer(player, "\n Incorrect name or password. Do you want to create a new account? (yes/no)");
                string response = await WriteToConsole.TakeInput(player);

                if (response.ToLower() == "yes")
                {
                    await login.CreateAcc(name, password);
                    return true;
                }

                //odhlasit pokud nechce vytvorit ucet
                return false;
            }

            await loadTask;





            //tato metoda bude vracet hrace ktereho najdu podle jmena 



        }



        public async Task LoadPlayers()
        {

            if (File.Exists(pathToJsonPlayerList))
            {
                string jsonData = await File.ReadAllTextAsync(pathToJsonPlayerList);
                Accounts = System.Text.Json.JsonSerializer.Deserialize<List<Player>>(jsonData);

            }


        }



    }
}
