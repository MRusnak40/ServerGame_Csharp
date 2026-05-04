using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{
    internal class FightPlayer : Command
    {
        private const int ROUND_TIMEOUT_MS = 15000;   // 15 s na každé kolo
        private const int CLEANUP_TIMEOUT_MS = 2000;  // 2 s na vyčištění vstupu druhého hráče

        public FightPlayer(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            if (_player.IsInCombat)
            {
                await SafeWrite(_player, ">> You are already in combat!");
                return "error";
            }

            Room room = FindPlayerRoom(_player);
            if (room == null)
            {
                await SafeWrite(_player, ">> Room not found!");
                return "error";
            }

            // snapshot hráčů v místnosti
            var playersSnapshot = room.PlayersInRoom?.ToList() ?? new List<Player>();

            // debug (odkomentujte pokud chcete)
            // await SafeWrite(_player, "DEBUG: players in room: " + string.Join(", ", playersSnapshot.Select(x => $"{x.Name}(InCombat={x.IsInCombat},Killable={x.IsKillable})")));

            var opponents = playersSnapshot
                .Where(p => p.Name != _player.Name && !p.IsInCombat && p.IsKillable)
                .ToList();

            if (opponents.Count == 0)
            {
                await SafeWrite(_player, ">> No opponents available.");
                return "error";
            }

            await ShowOpponents(opponents);
            await SafeWrite(_player, "\n>> Choose opponent (1-" + opponents.Count + "): ");
            string choice = await ReadLineSafe(_player);
            if (!int.TryParse(choice, out int idx) || idx < 1 || idx > opponents.Count)
            {
                await SafeWrite(_player, ">> Invalid choice.");
                return "error";
            }

            Player opponent = opponents[idx - 1];

            await RunFight(_player, opponent);
            return "fight";
        }

        public override bool Exit() => false;

        // ------------------------------------------------------------------------
        //  Hlavní bojová smyčka
        // ------------------------------------------------------------------------
        private async Task RunFight(Player p1, Player p2)
        {
            // Inicializace
            p1.Stamina = p1.MaxStamina;
            p2.Stamina = p2.MaxStamina;
            p1.IsInCombat = true;
            p2.IsInCombat = true;

            await ShowFightStart(p1, p2);

            while (p1.Health > 0 && p2.Health > 0)
            {
                await ShowFightStatus(p1, p2);

                // Spustíme asynchronní čtení pro oba hráče
                Task<string> task1 = GetTimedInputAsync(p1, "Your attack (fast/heavy/careful): ", ROUND_TIMEOUT_MS);
                Task<string> task2 = GetTimedInputAsync(p2, "Your attack (fast/heavy/careful): ", ROUND_TIMEOUT_MS);

                Task completedTask = await Task.WhenAny(task1, task2);
                Player first = (completedTask == task1) ? p1 : p2;
                Player second = (completedTask == task1) ? p2 : p1;
                string firstMove = await (completedTask == task1 ? task1 : task2);

                // Pokud první hráč nestihl timeout → okamžitě prohrává
                if (string.IsNullOrEmpty(firstMove))
                {
                    first.Health = 0;
                    if (!await SafeWrite(first, "\n⏰ Time's up! You lost the fight."))
                    {
                        await HandleDisconnectDuringFight(first, second);
                        break;
                    }
                    if (!await SafeWrite(second, $"\n🏆 {first.Name} didn't respond. You win!"))
                    {
                        await HandleDisconnectDuringFight(second, first);
                        break;
                    }
                    break;
                }

                // První útok
                bool ok = await ApplyAttack(first, second, firstMove);
                if (!ok) { await HandleDisconnectDuringFight(first, second); break; }
                if (second.Health <= 0) break;

                // Teď zkusíme získat útok od druhého hráče (krátký timeout pro vyčištění bufferu)
                Task<string> secondTask = (completedTask == task1) ? task2 : task1;
                string secondMove = await GetRemainingInputAsync(secondTask, CLEANUP_TIMEOUT_MS);

                if (!string.IsNullOrEmpty(secondMove))
                {
                    ok = await ApplyAttack(second, first, secondMove);
                    if (!ok) { await HandleDisconnectDuringFight(second, first); break; }
                    if (first.Health <= 0) break;
                }

                // Regenerace staminy
                RegenerateStamina(p1);
                RegenerateStamina(p2);
            }

            // Konec boje – určit vítěze (pokud někdo zůstal naživu)
            Player winner = p1.Health > 0 ? p1 : p2;
            Player loser = p1.Health > 0 ? p2 : p1;

            ApplyResults(winner, loser);

            p1.IsInCombat = false;
            p2.IsInCombat = false;

            // Pokus o uložení stavu (neblokovat server při chybě)
            try
            {
                await _gameWorld.UpadateVluesForPlayerTOList(winner);
                await _gameWorld.UpadateVluesForPlayerTOList(loser);
                await _gameWorld.SavePlayersList();
            }
            catch { }

            await ShowFightEnd(winner, loser);
        }

        // ------------------------------------------------------------------------
        //  Pomocné metody
        // ------------------------------------------------------------------------

        /// <summary> Vrátí vstup hráče do timeoutu, jinak null. </summary>
        private async Task<string> GetTimedInputAsync(Player player, string prompt, int timeoutMs)
        {
            try
            {
                if (!await SafeWrite(player, prompt)) return null;
            }
            catch { return null; }

            var readTask = player.Reader.ReadLineAsync();
            var delayTask = Task.Delay(timeoutMs);

            if (await Task.WhenAny(readTask, delayTask) == readTask)
                return await readTask;

            // Timeout – hráč nic nenapsal
            await SafeWrite(player, "\n⏰ Time's up!");
            return null;
        }

        /// <summary> Vyčte zbývající vstup z již běžícího tasku (nebo vrátí null při timeoutu). </summary>
        private async Task<string> GetRemainingInputAsync(Task<string> existingReadTask, int timeoutMs)
        {
            if (existingReadTask.IsCompleted)
                return await existingReadTask;  // už mám výsledek

            var delayTask = Task.Delay(timeoutMs);
            if (await Task.WhenAny(existingReadTask, delayTask) == existingReadTask)
                return await existingReadTask;

            return null; // druhý hráč nic dalšího nenapsal
        }

        /// <summary>
        /// Aplikuje útok; vrací false pokud došlo k IO chybě při notifikaci (pak volat HandleDisconnectDuringFight).
        /// </summary>
        private async Task<bool> ApplyAttack(Player attacker, Player defender, string move)
        {
            AttackType type = ParseAttack(move);
            int cost = GetStaminaCost(type);
            if (attacker.Stamina < cost)
            {
                if (!await SafeWrite(attacker, $">> Not enough stamina for {type}!")) return false;
                if (!await SafeWrite(defender, $">> {attacker.Name} tried {type} but lacks stamina.")) return false;
                return true;
            }
            attacker.Stamina -= cost;
            int dmg = GetDamage(attacker, type);
            defender.Health = Math.Max(0, defender.Health - dmg);

            string msg = $"[{type.ToString().ToUpper()}] {attacker.Name} → {defender.Name} (-{dmg} HP)";
            if (!await SafeWrite(attacker, msg)) return false;
            if (!await SafeWrite(defender, msg)) return false;
            return true;
        }

        private enum AttackType { Fast, Heavy, Careful }
        private AttackType ParseAttack(string input) =>
            input?.Trim().ToLower() switch
            {
                "heavy" => AttackType.Heavy,
                "fast" => AttackType.Fast,
                _ => AttackType.Careful
            };

        private int GetStaminaCost(AttackType type) =>
            type switch
            {
                AttackType.Heavy => Math.Max(1, (int)(0.30 * _player.MaxStamina)),
                AttackType.Fast => Math.Max(1, (int)(0.10 * _player.MaxStamina)),
                _ => Math.Max(1, (int)(0.20 * _player.MaxStamina))
            };

        private int GetDamage(Player attacker, AttackType type) =>
            type switch
            {
                AttackType.Heavy => attacker.Strength,
                AttackType.Fast => (int)(attacker.Strength * 0.4),
                AttackType.Careful => (int)(attacker.Strength * 0.7),
                _ => (int)(attacker.Strength * 0.7)
            };

        private void RegenerateStamina(Player p) =>
            p.Stamina = Math.Min(p.Stamina + Math.Max(1, (int)(p.MaxStamina * 0.03)), p.MaxStamina);

        private void ApplyResults(Player winner, Player loser)
        {
            int coinLoss = Math.Min(loser.Coins, 50);
            winner.Coins += coinLoss;
            loser.Coins -= coinLoss;

            int xpLoss = Math.Min(loser.Experience, 100);
            winner.Experience += xpLoss;
            loser.Experience -= xpLoss;

            if (loser.Level > 1) loser.Level--;
            loser.Health = 0;   // zajištěno, že GameLoop ho pošle do lobby
        }

        // ------------------------------------------------------------------------
        //  Bezpečný zápis a ošetření odpojení
        // ------------------------------------------------------------------------

        /// <summary>
        /// Bezpečně zapíše zprávu hráči. Vrací true pokud zápis proběhl, false pokud došlo k chybě (např. odpojení).
        /// Používá lokální lock na writeru, aby se zabránilo souběžným zápisům z tohoto kódu.
        /// </summary>
        private async Task<bool> SafeWrite(Player p, string msg)
        {
            if (p == null) return false;
            try
            {
                var w = p.Writer;
                if (w == null) return false;

                // lock na writer instanci (serializuje zápisy z tohoto místa)
                lock (w)
                {
                    try
                    {
                        w.WriteLine(msg);
                    }
                    catch
                    {
                        // pokud WriteLine vyhodí, necháme to padnout do catch níže
                        throw;
                    }
                }

                // flush asynchronně mimo lock
                await w.FlushAsync();
                return true;
            }
            catch
            {
                // IO chyba nebo writer uzavřený
                return false;
            }
        }

        /// <summary>
        /// Korektní ukončení boje pokud se hráč odpojil nebo došlo k IO chybě.
        /// Oznamí druhému hráči výhru, aktualizuje stavy a uloží.
        /// </summary>
        private async Task HandleDisconnectDuringFight(Player disconnected, Player other)
        {
            try
            {
                // Oznamit druhému hráči (pokud to jde)
                await SafeWrite(other, "\n[FIGHT] Opponent disconnected. You win!");
            }
            catch { }

            // Uvolnit příznaky
            try { disconnected.IsInCombat = false; } catch { }
            try { other.IsInCombat = false; } catch { }

            // Označit odpojeného jako mrtvého, aby GameLoop ho poslal do lobby
            try { disconnected.Health = 0; } catch { }

            // Aktualizovat a uložit stavy (neblokovat při chybě)
            try
            {
                await _gameWorld.UpadateVluesForPlayerTOList(other);
                await _gameWorld.UpadateVluesForPlayerTOList(disconnected);
                await _gameWorld.SavePlayersList();
            }
            catch { }
        }

        // ------------------------------------------------------------------------
        //  Výpisy (bezpečné proti výjimkám)
        // ------------------------------------------------------------------------
        private async Task<string> ReadLineSafe(Player p)
        {
            try { return await p.Reader.ReadLineAsync(); } catch { return null; }
        }

        private async Task ShowOpponents(List<Player> list)
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════╗");
            sb.AppendLine("║       AVAILABLE OPPONENTS        ║");
            sb.AppendLine("╠══════════════════════════════════╣");
            for (int i = 0; i < list.Count; i++)
                sb.AppendLine($"║ {i + 1}. {list[i].Name,-22} ║");
            sb.AppendLine("╚══════════════════════════════════╝");
            await SafeWrite(_player, sb.ToString());
        }

        private async Task ShowFightStart(Player a, Player b)
        {
            string msg = "\n⚔️ FIGHT! ⚔️\n" +
                         $"{a.Name}  vs  {b.Name}\n" +
                         "Commands: fast (40% dmg, low stam) | careful (70% dmg, med stam) | heavy (100% dmg, high stam)\n" +
                         "First to type attacks first!";
            await SafeWrite(a, msg);
            await SafeWrite(b, msg);
        }

        private async Task ShowFightStatus(Player a, Player b)
        {
            string Bar(int cur, int max) =>
                new string('█', (int)((double)cur / max * 15)).PadRight(15, '░');

            string hpA = Bar(a.Health, a.MaxHealth);
            string hpB = Bar(b.Health, b.MaxHealth);
            string stA = Bar(a.Stamina, a.MaxStamina);
            string stB = Bar(b.Stamina, b.MaxStamina);

            string viewA = $"You:      HP [{hpA}] Stam [{stA}]\nOpponent: HP [{hpB}] Stam [{stB}]";
            string viewB = $"You:      HP [{hpB}] Stam [{stB}]\nOpponent: HP [{hpA}] Stam [{stA}]";

            await SafeWrite(a, viewA);
            await SafeWrite(b, viewB);
        }

        private async Task ShowFightEnd(Player winner, Player loser)
        {
            string winMsg = $"\n🏆 YOU WIN! +{winner.Coins}c +{winner.Experience}xp";
            string loseMsg = $"\n💀 YOU LOSE! -{loser.Coins}c -{loser.Experience}xp lvl -1\nReturning to lobby...";

            await SafeWrite(winner, winMsg);
            await SafeWrite(loser, loseMsg);
        }

        private Room FindPlayerRoom(Player player)
        {
            if (_gameWorld.MapsInGameWorld == null) return null;
            foreach (var map in _gameWorld.MapsInGameWorld.ToList())
            {
                if (map.RoomsInMap == null) continue;
                foreach (var room in map.RoomsInMap.ToList())
                {
                    if (room.PlayersInRoom == null) continue;
                    var snapshot = room.PlayersInRoom.ToList();
                    if (snapshot.Any(p => p.Name == player.Name))
                        return room;
                }
            }
            return null;
        }
    }
}
