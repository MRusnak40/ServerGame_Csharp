using ServerSProxy.Logic.Commands;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.PlayerCode.Items;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;

namespace ServerSProxy.Logic.GameWorldCode
{
    internal class GameWorld
    {



        List<Map>? _mapsInGameWorld;

        private int _numberOfMapsInGameWorld;

        private List<Player> _onlinePlayers;
        Login login = new Login();


        private string pathToJsonPlayerList = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "JSON", "Players.json");
        private string pathToJsonGameWorld = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "JSON", "GameWorld.json");

        // public bool gameRunning = true;

        private List<Player> _accounts;

        public GameWorld()
        {
            _onlinePlayers = new List<Player>();
            _accounts = new List<Player>();
            _mapsInGameWorld = new List<Map>();
        }
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

        private Dictionary<string, Command> CreateCommands(Player player)
        {
            return new Dictionary<string, Command>()
            {
                { "exit",new ExitComm(player, this) },
                { "chat", new Chat(player, this) },
                { "help", new Help(player, this) },
            };
        }


        public async Task LoadGameWorld()
        {
            // Implementace načítání světa z JSON souboru
            // Cesta k JSON souboru

            if (File.Exists(pathToJsonGameWorld))
            {
                string jsonData = await File.ReadAllTextAsync(pathToJsonGameWorld);
                GameWorld loadedGameWorld = System.Text.Json.JsonSerializer.Deserialize<GameWorld>(jsonData);
                if (loadedGameWorld != null)
                {
                    MapsInGameWorld = loadedGameWorld.MapsInGameWorld;
                    NumberOfMapsInGameWorld = loadedGameWorld.NumberOfMapsInGameWorld;
                    OnlinePlayers = loadedGameWorld.OnlinePlayers;
                }
            }
        }

        //Gameworld list=> mapsInGame world je to cela mapa cela hra
        public async Task SaveGameWorld()
        {
            // Implementace ukládání světa do JSON souboru
            // Cesta k JSON souboru

            string jsonData = System.Text.Json.JsonSerializer.Serialize(MapsInGameWorld);
            await File.WriteAllTextAsync(pathToJsonGameWorld, jsonData);
        }






        //save players je postarano jeste to pridat ke smrti hrace
        public async Task LoadPlayers()
        {
            if (!File.Exists(pathToJsonPlayerList))
            {
                Accounts = new List<Player>();
                return;
            }

            string jsonData = await File.ReadAllTextAsync(pathToJsonPlayerList);

            if (string.IsNullOrWhiteSpace(jsonData))
            {
                Accounts = new List<Player>();
                return;
            }

            try
            {
                Accounts = System.Text.Json.JsonSerializer.Deserialize<List<Player>>(jsonData)
                           ?? new List<Player>();
            }
            catch
            {
                Accounts = new List<Player>();
            }
        }

        public async Task SavePlayersList()
        {
            if (Accounts == null) Accounts = new List<Player>();

            string jsonData = System.Text.Json.JsonSerializer.Serialize(Accounts);
            await File.WriteAllTextAsync(pathToJsonPlayerList, jsonData);
        }


        public async Task StartAutoSave()
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            // Smyčka běží navždy na pozadí
            while (await timer.WaitForNextTickAsync())
            {
                try
                {
                    await SavePlayersList();
                    Console.WriteLine($"[AUTOSAVE] {DateTime.Now:HH:mm:ss} - Data uložena.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CHYBA] Autosave selhal: {ex.Message}");
                }
            }
        }

        public async Task PlayerAutoSave(Player player, CancellationToken token)
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            try
            {

                while (!token.IsCancellationRequested && await timer.WaitForNextTickAsync(token))
                {
                    try
                    {
                        await UpadateVluesForPlayerTOList(player);
                        Console.WriteLine($"[AUTOSAVE-{player.Name}] {DateTime.Now:HH:mm:ss} - Data uložena.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CHYBA] Autosave selhal: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {

                Console.WriteLine($"[SYSTEM] Autosave pro hráče {player.Name} byl korektně ukončen.");
            }
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
                    //player.LastActive = p.LastActive;
                    break;
                }
            }
        }


        public async Task UpadateVluesForPlayerTOList(Player player)
        {
            foreach (Player p in Accounts)
            {
                if (p.Name == player.Name)
                {
                    p.Level = player.Level;
                    p.Experience = player.Experience;
                    p.Health = player.Health;
                    p.MaxHealth = player.MaxHealth;
                    p.Shield = player.Shield;
                    p.MaxShield = player.MaxShield;
                    p.Stamina = player.Stamina;
                    p.MaxStamina = player.MaxStamina;
                    p.Strength = player.Strength;
                    p.AttackSpeed = player.AttackSpeed;
                    p.Coins = player.Coins;
                    p.Class = player.Class;
                    p.Inventory = player.Inventory;
                    p.ActiveQuests = player.ActiveQuests;
                    p.IsAlive = player.IsAlive;
                    p.LastActive = player.LastActive;
                    break;
                }
            }
        }


        //napojeni na connected pro hrace jinak ho to vyhodi



        public async Task<bool> LogInPlayers(StreamReader reader, StreamWriter writer, Player player)
        {





            player.Reader = reader;
            player.Writer = writer;


            await LoadPlayers();


            WriteToConsole.TextToPlayer(player, "\n Welcome back to the game! Please enter your game name:");

            string name = await WriteToConsole.TakeInput(player);

            WriteToConsole.TextToPlayer(player, "*-----------------------------* \n Please enter your login password:");
            string password = await WriteToConsole.TakeInput(player);

            if (await login.VerifyPassword(name, password))
            {
                WriteToConsole.TextToPlayer(player, "*----------------------------------*\n Login successful! Welcome back," + name + " ! \n *---------------------------------------------* ");

                player.Name = name;
                await SetVluesForPlayer(player);

                // data missing save
                if (string.IsNullOrEmpty(player.Class))
                {
                    await ChooseClassForPlayer(player);
                }

                return true;
            }
            else
            {























                WriteToConsole.TextToPlayer(player, "\n Incorrect name or password for LOGIN. Do you want to create a account? (yes/no)");

                string response = await WriteToConsole.TakeInput(player);

                if (response.ToLower() == "yes")
                {







                    bool correct = false;


                    while (!correct)
                    {
                        await WriteToConsole.TextToPlayer(player, $"Do you want to change your name from {name} ? (yes/no)");

                        string change = await WriteToConsole.TakeInput(player);


                        if (change.ToLower() == "yes")
                        {


                            WriteToConsole.TextToPlayer(player, $"Zadej jine jmeno:");

                            name = await WriteToConsole.TakeInput(player);

                            await WriteToConsole.TextToPlayer(player, "Zadej heslo:");
                            password = await WriteToConsole.TakeInput(player);

                        }



                        string accIn = await login.CreateAcc(name, password);

                        await WriteToConsole.TextToPlayer(player, accIn);





                        if (accIn == "ok")
                        {








                            //PRIDAT SEM EFAULTNI HODNOTY NEBO POMOCI METODY VYBRAT CLASS KTEROU BUDE
                            //ulozit do seznamu muze nastat chybe ze hrac se odpoji driv nez se ulozi do seznamu i hrac v tom pripade udelat moznost odstrannei uctu pokud zna jmeno i heslo

                            /*
                            player.Name = name;
                            player.Health = 100;
                            player.MaxHealth = 100;
                            player.Shield = 50;
                            player.MaxShield = 50;
                            player.Stamina = 100;
                            player.MaxStamina = 100;
                            player.Strength = 10;
                            player.AttackSpeed = 10;
                            player.IsAlive = true;
                            player.Coins = 0;
                            */
                            player.Name = name;

                            await ChooseClassForPlayer(player);




                            await WriteToConsole.TextToPlayer(player, $"SUSCESSFULLY REGISTERED IN {name}");





                            correct = true;

                            Accounts.Add(player);

                            await SavePlayersList();


                            return true;

                        }
                        /*
                        WriteToConsole.TextToPlayer(player, $"Zkus jinou nez {name}");

                        name = await WriteToConsole.TakeInput(player);

                        await WriteToConsole.TextToPlayer(player, "Zadej heslo:");
                        password = await WriteToConsole.TakeInput(player);
                        */
                    }


                    return true;




















                }

                return false;
            }





        }
        private async Task<bool> CheckIfPlayerIsConnected(Player player)
        {
            try
            {
                await player.Writer.WriteLineAsync("ping");
                await player.Writer.FlushAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task RemoveDisconnectedPlayer(Player player)
        {
            if (!await CheckIfPlayerIsConnected(player))
            {
                OnlinePlayers.Remove(player);
                WriteToConsole.TextToPlayer(player, "You have been disconnected from the server.");
            }
        }


        public async Task ChooseClassForPlayer(Player player)
        {


            bool validSelection = false;


            if (ClassTypeListPlayer.AvailableClasses.Count == 0)
            {
                Console.WriteLine("No classes available. Please add classes to the ClassTypeListPlayer.AvailableClasses list.");
                WriteToConsole.TextToPlayer(player, "Bro cant cook even easy code. Please contact the administrator .");
                return;
            }
            while (!validSelection)
            {

                StringBuilder menu = new StringBuilder();
                menu.AppendLine("\n╔════════════════════════════════════════════════════════════╗");
                menu.AppendLine("║                ✨ Choose the class ✨                      ║");
                menu.AppendLine("╚════════════════════════════════════════════════════════════╝");
                menu.AppendLine("  Available classes:");
                menu.AppendLine("  ------------------------------------------------------------");

                foreach (var cls in ClassTypeListPlayer.AvailableClasses.Values)
                {
                    menu.AppendLine($"  ▶ [{cls.ClassId.ToUpper()}] {cls.DisplayName}");
                    menu.AppendLine($"    \"{cls.Description}\"");
                    menu.AppendLine($"    💪 Strength: {cls.BaseStrength} | ❤️ HP: {cls.BaseHealth} | 🛡️ Shields: {cls.BaseShield} | ⚡ Stamina: {cls.BaseStamina} | ⏱️ Attack Speed: {cls.BaseAttackSpeed}");
                    menu.AppendLine("  ------------------------------------------------------------");
                }

                menu.AppendLine("\n  Type the ID of the class you want to choose:");

                await WriteToConsole.TextToPlayer(player, menu.ToString());


                string choice = await WriteToConsole.TakeInput(player);
                choice = choice?.Trim().ToLower();


                if (!string.IsNullOrEmpty(choice) && ClassTypeListPlayer.AvailableClasses.TryGetValue(choice, out var template))
                {





                    player.IsAlive = true;
                    player.Coins = 0;
                    player.Level = 1;
                    player.IsInCombat = false;
                    player.Experience = 0;

                    player.Inventory = new Inventory(new List<Item>(), new List<Item>(), 5);

                    player.ActiveQuests = new List<Quest>();

                    player.IsKillable = true;

                    player.Class = template.DisplayName;
                    player.MaxHealth = template.BaseHealth;
                    player.Health = template.BaseHealth;
                    player.MaxShield = template.BaseShield;
                    player.Shield = template.BaseShield;
                    player.MaxStamina = template.BaseStamina;
                    player.Stamina = template.BaseStamina;
                    player.Strength = template.BaseStrength;
                    player.AttackSpeed = template.BaseAttackSpeed;



                    string confirmMsg = $"\n✨ Nice choice! From now on, you are: {template.DisplayName} ✨\n";
                    await WriteToConsole.TextToPlayer(player, confirmMsg);

                    validSelection = true;
                }
                else
                {
                    await WriteToConsole.TextToPlayer(player, "\n❌ Invalid choice! Please try again and enter the class ID correctly.");
                }



            }
        }

        public async Task GameLoop(Player player)
        {

            bool gameRunning = true;

            string input;

            var commands = CreateCommands(player);




            while (gameRunning)
            {
                player.LastActive = DateTime.Now;
                if (!player.IsAlive)
                {
                    WriteToConsole.TextToPlayer(player, "You are dead. Please wait for retturning to lobby...");
                    await Task.Delay(5000);
                    player.IsAlive = true;
                    player.Health = player.MaxHealth;
                    WriteToConsole.TextToPlayer(player, "You are in lobby. Be careful next time!");

                    gameRunning = false;
                    return;
                }

                if (player == null)
                {


                    WriteToConsole.TextToPlayer(player, "YOUR ACCOUNT IS BROKEN CHOOSE YOUR CLASS AGAIN");

                    ChooseClassForPlayer(player);

                }


                if (player.Health <= 0)
                {
                    player.IsAlive = false;
                }


                WriteToConsole.TextToPlayer(player, "\n---------------------------------------------");
                WriteToConsole.TextToPlayer(player, $"Player: {player.Name} | Class: {player.Class} \n" +
                    $" Level: {player.Level} | HP: {player.Health}/{player.MaxHealth} \n" +
                    $" Shields: {player.Shield}/{player.MaxShield} | Stamina: {player.Stamina}/{player.MaxStamina} \n " +
                    $" Strength: {player.Strength} | Attack Speed: {player.AttackSpeed} | Coins: {player.Coins}");


                WriteToConsole.TextToPlayer(player, "\n---------------------------------------------");


                WriteToConsole.TextToPlayer(player, "\nEnter command (type 'help' for a list of commands): ");

                input = await WriteToConsole.TakeInput(player);

                string commandKey = input.Split(' ')[0].ToLower();



                if (commands.ContainsKey(commandKey))
                {
                    await commands[commandKey].Execute();
                }
                else
                {
                    WriteToConsole.TextToPlayer(player, "Unknown command. Please try again.");
                }




            }






















        }



    }
}
