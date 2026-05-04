using ServerSProxy.Logic.Commands;
using ServerSProxy.Logic.NPCs;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.PlayerCode.Items;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private List<Player> _accounts;

        
        private readonly object _accountsLock = new object();
        private static readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);   // pro soubor
        private readonly SemaphoreSlim _roomSemaphore = new SemaphoreSlim(1, 1); //pristup k mkstnsotem
        private static SemaphoreSlim _playersLock = new SemaphoreSlim(1, 1);
        public GameWorld()
        {
            _onlinePlayers = new List<Player>();
            _accounts = new List<Player>();
            _mapsInGameWorld = new List<Map>();
        }

        // Vlastnosti 
        public List<Player> Accounts
        {
            get { lock (_accountsLock) return _accounts; }
            set { lock (_accountsLock) _accounts = value; }
        }

        public List<Map>? MapsInGameWorld
        {
            get => _mapsInGameWorld;
            set => _mapsInGameWorld = value;
        }

        public int NumberOfMapsInGameWorld
        {
            get => _numberOfMapsInGameWorld;
            set => _numberOfMapsInGameWorld = value;
        }

        public List<Player> OnlinePlayers
        {
            get => _onlinePlayers;
            set => _onlinePlayers = value;
        }

        private Dictionary<string, Command> CreateCommands(Player player)
        {
            return new Dictionary<string, Command>()
            {
                { "exit", new ExitComm(player, this) },
                { "chat", new Chat(player, this) },
                { "help", new Help(player, this) },
                { "stats", new ShowStats(player, this) },

                
                { "move", new Move(player, this) },
                { "fight", new FightPlayer(player, this) },
            };
        }


        // --------------------------------------------------
        //  SPRAVA SVETA 
        // --------------------------------------------------


        public async Task LoadGameWorld()
        {
            try
            {
                if (!File.Exists(pathToJsonGameWorld))
                {
                    Console.WriteLine(" GameWorld.json nenalezen. Používám prázdný svět.");
                    MapsInGameWorld = new List<Map>();
                    NumberOfMapsInGameWorld = 0;
                    return;
                }

                string jsonData = await File.ReadAllTextAsync(pathToJsonGameWorld);
                var maps = System.Text.Json.JsonSerializer.Deserialize<List<Map>>(jsonData);

                if (maps == null)
                {
                    Console.WriteLine(" GameWorld.json je prázdný. Používám prázdný svět.");
                    MapsInGameWorld = new List<Map>();
                    NumberOfMapsInGameWorld = 0;
                    return;
                }

                //inicilize
                foreach (var map in maps)
                {
                    if (map.RoomsInMap != null)
                    {
                        foreach (var room in map.RoomsInMap)
                        {
                            room.PlayersInRoom ??= new List<Player>();
                            room.NpcsInRoom ??= new List<NPC>();
                            room.EnemiesInRoom ??= new List<Enemy>();
                            room.DropedItems ??= new List<Item>();
                            room.ConnectedRooms ??= new List<string>();
                        }
                    }
                }

                MapsInGameWorld = maps;
                NumberOfMapsInGameWorld = maps.Count;
                Console.WriteLine($"✓ Svět načten: {maps.Count} map");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Chyba při načítání GameWorld.json: {ex.Message}");
                MapsInGameWorld = new List<Map>();
                NumberOfMapsInGameWorld = 0;
            }
        }

        //vicemene k nicemu   
        public async Task SaveGameWorld()
        {
            string jsonData = System.Text.Json.JsonSerializer.Serialize(MapsInGameWorld);
            await File.WriteAllTextAsync(pathToJsonGameWorld, jsonData);
        }

        // --------------------------------------------------
        //  SPRÁVA HRACU
        // --------------------------------------------------
        public async Task LoadPlayers()
        {
            string jsonData = null;
            if (File.Exists(pathToJsonPlayerList))
            {
                jsonData = await File.ReadAllTextAsync(pathToJsonPlayerList);
            }

            if (string.IsNullOrWhiteSpace(jsonData))
            {
                lock (_accountsLock) _accounts = new List<Player>();
                return;
            }

            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<List<Player>>(jsonData)
                           ?? new List<Player>();
                lock (_accountsLock) _accounts = list;
            }
            catch
            {
                lock (_accountsLock) _accounts = new List<Player>();
            }
        }

        public async Task SavePlayersList()
        {
            List<Player> snapshot;
            lock (_accountsLock)
            {
                snapshot = new List<Player>(_accounts);   // kopie pro bezpecnost serializace
            }

            string jsonData = System.Text.Json.JsonSerializer.Serialize(snapshot);

            // Asynchronní zápis do souboru s použitím semaforu, aby nedošlo k souběhu
            await _fileSemaphore.WaitAsync();
            try
            {
                await File.WriteAllTextAsync(pathToJsonPlayerList, jsonData);
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }



        // --------------------------------------------------
        //  SPRÁVA SPAWNUTÍ A ODCHODU Z MÍSTNOSTÍ
        // --------------------------------------------------


        //uprava anhrani hlavbni vec se deje v game world current room je jen informacni v hracovy 
        //nezapomenout ho pri msrti dat a opdpojit z current room jak pri odhlaseni tak pri smrti a pak ho dat do lobby
        public async Task LeaveRoomAsync(Player player)
        {
            if (MapsInGameWorld == null) return;
            await _roomSemaphore.WaitAsync();   
            try
            {
                MapsInGameWorld.ForEach(map =>
                {
                    map.RoomsInMap?.ForEach(room =>
                    {
                        room.PlayersInRoom?.Remove(player);
                    });
                });


                player.CurrentRoom = null;
            }
            finally
            {
                _roomSemaphore.Release();
            }
        }


        public async Task SpawnPlayerAsync(Player player)
        {
            if (MapsInGameWorld == null) return;
            // 1. remove player z aktualni mistnosti, pokud je v ni
            if (player.CurrentRoom != null)
            {
                await _roomSemaphore.WaitAsync();
                try
                {
                    MapsInGameWorld.ForEach(map =>
                    {
                        map.RoomsInMap?.ForEach(room =>
                        {
                            room.PlayersInRoom?.Remove(player);
                        });
                    });


                    player.CurrentRoom = null;
                }
                finally
                {
                    _roomSemaphore.Release();
                }
            }

            // 2. lowes level ve svete
            int minLevel = int.MaxValue;
            foreach (var map in MapsInGameWorld)
            {
                if (map.RoomsInMap == null) continue;
                foreach (var room in map.RoomsInMap)
                {
                    if (room.LevelOfRoom < minLevel)
                        minLevel = room.LevelOfRoom;
                }
            }

            // 3. list s nejmensim levelem
            var candidates = new List<Room>();
            foreach (var map in MapsInGameWorld)
            {
                if (map.RoomsInMap == null) continue;
                foreach (var room in map.RoomsInMap)
                {
                    if (room.LevelOfRoom == minLevel)
                        candidates.Add(room);
                }
            }

            // 4. mistnost s nejmene hraci in
            int minPlayers = int.MaxValue;
            foreach (var room in candidates)
            {
                int count = room.PlayersInRoom?.Count ?? 0;
                if (count < minPlayers)
                    minPlayers = count;
            }

            var bestRooms = candidates.Where(r => (r.PlayersInRoom?.Count ?? 0) == minPlayers).ToList();

           
            Room chosen = bestRooms[new Random().Next(bestRooms.Count)];

            
            await _roomSemaphore.WaitAsync();
            try
            {
                player.CurrentRoom = chosen;
                if (chosen.PlayersInRoom == null)
                    chosen.PlayersInRoom = new List<Player>();
                chosen.PlayersInRoom.Add(player);
            }
            finally
            {
                _roomSemaphore.Release();
            }
        }

        // --------------------------------------------------
        //  AUTOSAVE
        // --------------------------------------------------
        public async Task StartAutoSave()
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
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
            finally
            {
                timer.Dispose();
            }
        }

        // --------------------------------------------------
        //  POMOCNE METODY PRO PRENSO DAT HRACE
        // --------------------------------------------------
        public async Task SetVluesForPlayer(Player player)
        {
            lock (_accountsLock)
            {
                foreach (Player p in _accounts)
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
                        player.IsInCombat = p.IsInCombat;
                        player.IsKillable = p.IsKillable;
                        player.LastActive = p.LastActive;

                        break;
                    }
                }
            }
        }

        public async Task UpadateVluesForPlayerTOList(Player player)
        {
            lock (_accountsLock)
            {
                foreach (Player p in _accounts)
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
        }

        // --------------------------------------------------
        //  REGISTRACE A LOGIN
        // --------------------------------------------------
        public async Task<bool> LogInPlayers(StreamReader reader, StreamWriter writer, Player player)
        {
            player.Reader = reader;
            player.Writer = writer;

            WriteToConsole.TextToPlayer(player, "\n Welcome back to the game! Please enter your game name:");
            string name = await WriteToConsole.TakeInput(player);

            WriteToConsole.TextToPlayer(player, "*-----------------------------* \n Please enter your login password:");
            string password = await WriteToConsole.TakeInput(player);

            if (await login.VerifyPassword(name, password))
            {
                WriteToConsole.TextToPlayer(player, "*----------------------------------*\n Login successful! Welcome back," + name + " ! \n *---------------------------------------------* ");
                player.Name = name;
                await SetVluesForPlayer(player);

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
                            player.Name = name;
                            await ChooseClassForPlayer(player);
                            await WriteToConsole.TextToPlayer(player, $"SUSCESSFULLY REGISTERED IN {name}");
                            correct = true;
                            lock (_accountsLock) _accounts.Add(player);
                            await SavePlayersList();
                            return true;
                        }
                    }
                    return true;
                }
                return false;
            }
        }

        // --------------------------------------------------
        // WIFI CHECK A ODPOJOVANI HRACE
        // --------------------------------------------------
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

        // --------------------------------------------------
        //  CLASS SELECTION
        // --------------------------------------------------
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

                    string confirmMsg = $"\n Nice choice! From now on, you are: {template.DisplayName} ✨\n";
                    await WriteToConsole.TextToPlayer(player, confirmMsg);

                    await UpadateVluesForPlayerTOList(player);
                    await SavePlayersList();

                    validSelection = true;
                }
                else
                {
                    await WriteToConsole.TextToPlayer(player, "\n Invalid choice! Please try again and enter the class ID correctly.");
                }
            }
        }

        // --------------------------------------------------
        //  GAME LOOP
        // --------------------------------------------------
        public async Task GameLoop(Player player)
        {
            bool gameRunning = true;
            var commands = CreateCommands(player);

            while (gameRunning)
            {
                player.LastActive = DateTime.Now;

                if (!player.IsAlive)
                {
                    WriteToConsole.TextToPlayer(player, "You are dead. Please wait for returning to lobby...");


                    await LeaveRoomAsync(player);
                    await Task.Delay(5000);

                    await SetVluesForPlayer(player);

                    if (string.IsNullOrEmpty(player.Class))
                    {
                        await ChooseClassForPlayer(player);
                    }

                    player.IsAlive = true;
                    player.Health = player.MaxHealth;
                    player.Shield = player.MaxShield;
                    player.Stamina = player.MaxStamina;

                    await UpadateVluesForPlayerTOList(player);

                    WriteToConsole.TextToPlayer(player, "You are in lobby. Be careful next time!");
                    gameRunning = false;
                    return;
                }

                if (player.Health <= 0)
                {
                    player.IsAlive = false;
                    continue;
                }

                /*

                WriteToConsole.TextToPlayer(player, "\n---------------------------------------------");
                WriteToConsole.TextToPlayer(player, $"Player: {player.Name} | Class: {player.Class} \n" +
                    $" Level: {player.Level} | HP: {player.Health}/{player.MaxHealth} \n" +
                    $" Shields: {player.Shield}/{player.MaxShield} | Stamina: {player.Stamina}/{player.MaxStamina} \n " +
                    $" Strength: {player.Strength} | Attack Speed: {player.AttackSpeed} | Coins: {player.Coins}");
                WriteToConsole.TextToPlayer(player, "\n---------------------------------------------");
                WriteToConsole.TextToPlayer(player, "\nEnter command (type 'help' for a list of commands): ");


                */






                //command processing

                string input = await WriteToConsole.TakeInput(player);
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