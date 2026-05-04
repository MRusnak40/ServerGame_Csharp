using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.NPCs;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{
    internal class FightEnemy : Command
    {
        private const int ROUND_TIMEOUT_MS = 15000;

        public FightEnemy(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            if (_player.IsInCombat)
            {
                await WriteToConsole.TextToPlayer(_player, ">> You are already in combat!");
                return "error";
            }

            Room room = FindPlayerRoom(_player);
            if (room == null)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Room not found!");
                return "error";
            }

            var enemies = room.EnemiesInRoom?.ToList() ?? new List<Enemy>();
            if (enemies.Count == 0)
            {
                await WriteToConsole.TextToPlayer(_player, ">> No enemies here.");
                return "error";
            }

            // show enemies
            var sb = new StringBuilder();
            sb.AppendLine("\nEnemies in room:");
            for (int i = 0; i < enemies.Count; i++)
                sb.AppendLine($" {i + 1}. {enemies[i].Name} (lvl {enemies[i].Level}) HP:{enemies[i].Health}/{enemies[i].MaxHealth}");
            await WriteToConsole.TextToPlayer(_player, sb.ToString());

            await WriteToConsole.TextToPlayer(_player, $"\n>> Choose enemy to fight (1-{enemies.Count}): ");
            string input = await WriteToConsole.TakeInput(_player);
            if (!int.TryParse(input, out int idx) || idx < 1 || idx > enemies.Count)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Invalid choice.");
                return "error";
            }

            Enemy enemy = enemies[idx - 1];

            await RunFight(_player, enemy, room);
            return "fightenemy";
        }

        public override bool Exit() => false;

        private async Task RunFight(Player player, Enemy enemy, Room room)
        {
            // init
            player.IsInCombat = true;
            player.Stamina = player.MaxStamina;

            await WriteToConsole.TextToPlayer(player, $"\n⚔️ FIGHT vs {enemy.Name} ⚔️");
            while (player.Health > 0 && enemy.Health > 0)
            {
                // status
                string status = $"You: HP {player.Health}/{player.MaxHealth} | Stam {player.Stamina}/{player.MaxStamina}\n" +
                                $"{enemy.Name}: HP {enemy.Health}/{enemy.MaxHealth}";
                await WriteToConsole.TextToPlayer(player, status);

                // player move
                await WriteToConsole.TextToPlayer(player, "\nYour attack (fast/heavy/careful): ");
                string move = await WriteToConsole.TakeInput(player);
                var type = ParseAttack(move);

                int cost = GetStaminaCost(type);
                if (player.Stamina < cost)
                {
                    await WriteToConsole.TextToPlayer(player, $">> Not enough stamina for {type}!");
                }
                else
                {
                    player.Stamina -= cost;
                    int dmg = GetDamage(player, type);
                    enemy.Health = Math.Max(0, enemy.Health - dmg);
                    await WriteToConsole.TextToPlayer(player, $"You hit {enemy.Name} for {dmg} HP.");
                }

                if (enemy.Health <= 0) break;

                // enemy turn (simple AI)
                await Task.Delay(400); // small pause
                int enemyDmg = Math.Max(1, (int)(enemy.Strength * 0.6));
                player.Health = Math.Max(0, player.Health - enemyDmg);
                await WriteToConsole.TextToPlayer(player, $"{enemy.Name} hits you for {enemyDmg} HP.");

                // regen stamina slowly
                player.Stamina = Math.Min(player.MaxStamina, player.Stamina + Math.Max(1, (int)(player.MaxStamina * 0.03)));
            }

            // result
            if (player.Health > 0)
            {
                // win: transfer coins and xp from enemy if any (enemy class has no coins/xp fields)
                await WriteToConsole.TextToPlayer(player, $"\n🏆 You defeated {enemy.Name}!");
                // remove enemy from room
                try { room.EnemiesInRoom?.Remove(enemy); } catch { }
                // optional: drop items from enemy
                if (enemy.DroppingItems != null && enemy.DroppingItems.Count > 0)
                {
                    foreach (var it in enemy.DroppingItems)
                        room.DropedItems?.Add(it);
                    await WriteToConsole.TextToPlayer(player, $">> Enemy dropped {enemy.DroppingItems.Count} items.");
                }
            }
            else
            {
                await WriteToConsole.TextToPlayer(player, "\n💀 You were defeated. Returning to lobby...");
                player.IsAlive = false;
                player.Health = 0;
            }

            player.IsInCombat = false;

            // persist player state
            try
            {
                await _gameWorld.UpadateVluesForPlayerTOList(player);
                await _gameWorld.SavePlayersList();
            }
            catch { }
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
    }
}
