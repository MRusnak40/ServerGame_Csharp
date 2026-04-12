using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ServerSProxy.Logic.GameWorldCode
{
    internal class gameWorld
    {

        List<Map>? _mapsInGameWorld;

        private int _numberOfMapsInGameWorld;

        private List<Player> _onlinePlayers;
        Login login = new Login();


        private string pathToJsonPlayerList = "";
        private string pathToJsonGameWorld = "";


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
            // Cesta k JSON souboru
            string jsonData = System.Text.Json.JsonSerializer.Serialize(this);
            await File.WriteAllTextAsync(pathToJsonGameWorld, jsonData);
        }

        public async Task SetVluesForPlayer(Player player)
        {
            foreach (Player p in Accounts)
            {
                if (p.Name == player.Name)
                {
                    player.Level = p.Level;
                    player.Experience = p.Experience;
                    player.Health = p.Health;
                    player.MaxHealth = p.MaxHealth;
                    player.Shield = p.Shield;
                    player.MaxShield = p.MaxShield;
                    player.Stamina = p.Stamina;
                    player.MaxStamina = p.MaxStamina;
                    player.Strength = p.Strength;
                    player.AttackSpeed = p.AttackSpeed;
                    player.Coins = p.Coins;
                    player.Class = p.Class;
                    player.Inventory = p.Inventory;
                    player.ActiveQuests = p.ActiveQuests;
                    player.IsAlive = p.IsAlive;
                    player.LastActive = p.LastActive;
                    break;
                }
            }
        }


        //napojeni na connected pro hrace jinak ho to vyhodi
        public async Task<bool> LogInPlayers(StreamReader reader, StreamWriter writer,Player player)
        {
            Task loadTask = LoadPlayers();


            

            player.Reader = reader;
            player.Writer = writer;

            WriteToConsole.TextToPlayer(player, "\n Welcome back to the game! Please enter your game name:");

            string name = await WriteToConsole.TakeInput(player);

            WriteToConsole.TextToPlayer(player, "*-----------------------------* \n Please enter your login password:");
            string password = await WriteToConsole.TakeInput(player);

            if (await login.VerifyPassword(name, password))
            {
                WriteToConsole.TextToPlayer(player, "*----------------------------------*\n Login successful! Welcome back,"+ name +" ! \n *---------------------------------------------* " );

                player.Name = name;


                await loadTask;


               await SetVluesForPlayer(player);


                return true;
            }
            else
            {
                WriteToConsole.TextToPlayer(player, "\n Incorrect name or password. Do you want to create a new account? (yes/no)");
                string response = await WriteToConsole.TakeInput(player);

                if (response.ToLower() == "yes")
                {
                    bool? correct = false;


                    while (correct == false)
                    {
                        string accIn = await login.CreateAcc(name, password);

                        await WriteToConsole.TextToPlayer(player, accIn);

                        if (accIn == "ok")
                        {
                            await WriteToConsole.TextToPlayer(player, "SUSCESSFULLY REGISTERED IN");
                            correct = true;

                            player.Name = name;
                            
                        }
                    }
                    await loadTask;
                    return true;
                }

                //odhlasit pokud nechce vytvorit ucet
                return false;
            }




        }






        public async Task LoadPlayers()
        {

            if (File.Exists(pathToJsonPlayerList))
            {
                string jsonData = await File.ReadAllTextAsync(pathToJsonPlayerList);
                Accounts = System.Text.Json.JsonSerializer.Deserialize<List<Player>>(jsonData);

            }


        }


        public async Task GameLoop(Player player)
        {





        }



    }
}
