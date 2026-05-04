using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.NPCs;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.PlayerCode.Items;
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
            player.IsInCombat = true;
            player.Stamina = player.MaxStamina;

            await WriteToConsole.TextToPlayer(player, $"\n⚔️ FIGHT vs {enemy.Name} ⚔️");

            while (player.Health > 0 && enemy.Health > 0)
            {
                //  status
                string status = $"You: HP {player.Health}/{player.MaxHealth} | Shield {player.Shield}/{player.MaxShield} | Stam {player.Stamina}/{player.MaxStamina}\n" +
                                $"{enemy.Name}: HP {enemy.Health}/{enemy.MaxHealth} | Shield {enemy.Shield}/{enemy.MaxShield}";
                await WriteToConsole.TextToPlayer(player, status);

                //  move
                await WriteToConsole.TextToPlayer(player, "\nYour attack (fast/heavy/careful): ");
                string move = await WriteToConsole.TakeInput(player);
                var type = ParseAttack(move);

                int cost = GetStaminaCost(type);
                if (player.Stamina < cost)
                {
                    await WriteToConsole.TextToPlayer(player, $">> Not enough stamina for {type}!");
                    continue;
                }

                player.Stamina -= cost;
                int dmg = GetDamage(player, type);
                //  damage to enemy shield first
                if (enemy.Shield > 0)
                {
                    int absorbed = Math.Min(enemy.Shield, dmg);
                    enemy.Shield -= absorbed;
                    dmg -= absorbed;
                    await WriteToConsole.TextToPlayer(player, $"Enemy shield absorbs {absorbed} damage.");
                }
                if (dmg > 0)
                {
                    enemy.Health = Math.Max(0, enemy.Health - dmg);
                    await WriteToConsole.TextToPlayer(player, $"You hit {enemy.Name} for {dmg} HP.");
                }
                else if (enemy.Shield == 0 && dmg == 0)
                    await WriteToConsole.TextToPlayer(player, $"Your attack was completely blocked!");

                if (enemy.Health <= 0) break;

                // enmy turn after 5 seconds
                await WriteToConsole.TextToPlayer(player, $"{enemy.Name} prepares to attack...");
                await Task.Delay(5000);

                int enemyDmg = Math.Max(1, (int)(enemy.Strength * 0.6));
                //   player shield
                if (player.Shield > 0)
                {
                    int absorbed = Math.Min(player.Shield, enemyDmg);
                    player.Shield -= absorbed;
                    enemyDmg -= absorbed;
                    await WriteToConsole.TextToPlayer(player, $"Your shield absorbs {absorbed} damage.");
                }
                if (enemyDmg > 0)
                {
                    player.Health = Math.Max(0, player.Health - enemyDmg);
                    await WriteToConsole.TextToPlayer(player, $"{enemy.Name} hits you for {enemyDmg} HP.");
                }

                //  regen
                player.Stamina = Math.Min(player.MaxStamina, player.Stamina + Math.Max(1, (int)(player.MaxStamina * 0.03)));
            }

            // result
            if (player.Health > 0)
            {
                int xpGain = 10 * enemy.Level;
                int coinGain = 5 * enemy.Level;
                player.Experience += xpGain;
                player.Coins += coinGain;
                await WriteToConsole.TextToPlayer(player, $"\n🏆 You defeated {enemy.Name}!");
                await WriteToConsole.TextToPlayer(player, $">> +{xpGain} XP, +{coinGain} coins.");

                // handle loot
                if (enemy.DroppingItems != null && enemy.DroppingItems.Count > 0)
                {
                    await WriteToConsole.TextToPlayer(player, $">> {enemy.Name} dropped {enemy.DroppingItems.Count} item(s).");
                    foreach (var item in enemy.DroppingItems.ToList())
                    {
                        await WriteToConsole.TextToPlayer(player, $"\nItem: {item.Name} - {item.Description}");
                        await WriteToConsole.TextToPlayer(player, $"Value: {item.Value} coins. Take it? (yes/no): ");
                        string answer = await WriteToConsole.TakeInput(player);
                        if (answer?.Trim().ToLower() == "yes")
                        {
                            bool taken = await TryGiveItemToPlayer(player, item, room);
                            if (taken)
                            {
                                enemy.DroppingItems.Remove(item);
                                await WriteToConsole.TextToPlayer(player, "Item added to your inventory.");
                            }
                            else
                            {
                                await WriteToConsole.TextToPlayer(player, "Inventory full. Item remains on the ground.");
                            }
                        }
                        else
                        {
                            // item stays in room 
                        }
                    }

                    // remaining dropping items go to room
                    if (enemy.DroppingItems.Count > 0)
                    {
                        foreach (var item in enemy.DroppingItems)
                            room.DropedItems.Add(item);
                        await WriteToConsole.TextToPlayer(player, $"{enemy.DroppingItems.Count} item(s) left on the ground.");
                    }
                }

                // remove enemy from room
                try { room.EnemiesInRoom?.Remove(enemy); } catch { }

                // auto-complete quests based on this kill
                await _gameWorld.CheckQuestCompletion(player, enemy);
            }
            else
            {
                await WriteToConsole.TextToPlayer(player, "\n💀 You were defeated. Returning to lobby...");
                player.IsAlive = false;
                player.Health = 0;
            }

            player.IsInCombat = false;

            try
            {
                await _gameWorld.UpadateVluesForPlayerTOList(player);
                await _gameWorld.SavePlayersList();
            }
            catch { }
        }

        private async Task<bool> TryGiveItemToPlayer(Player player, Item item, Room room)
        {
            // try to add to inventory
            if (player.Inventory.AddItem(item))
            {
                if (item.IsEquippable)
                {
                    // Auto-equip and drop old item to room
                    Item old = player.EquipItem(item);
                    if (old != null)
                    {
                        room.DropedItems.Add(old);
                        await WriteToConsole.TextToPlayer(player, $"Unequipped {old.Name} to make room.");
                    }
                }
                return true;
            }
            return false;
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
                    if (room.PlayersInRoom.Any(p => p.Name == player.Name))
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