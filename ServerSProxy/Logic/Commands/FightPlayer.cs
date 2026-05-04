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
        public FightPlayer(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            if (_player.IsInCombat)
            {
                await WriteToConsole.TextToPlayer(_player, ">> You are already in combat!");
                return "error";
            }
            if (!_player.IsAlive)
            {
                await WriteToConsole.TextToPlayer(_player, ">> You cannot fight while dead.");
                return "error";
            }

            Room room = FindPlayerRoom(_player);
            if (room == null)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Room not found!");
                return "error";
            }

            await WriteToConsole.TextToPlayer(_player, "Enter name of player to fight: ");
            string name = await WriteToConsole.TakeInput(_player);
            if (string.IsNullOrWhiteSpace(name))
            {
                await WriteToConsole.TextToPlayer(_player, ">> Invalid name.");
                return "error";
            }

            Player target = null;
            // find target 
            if (room.PlayersInRoom != null)
            {
                target = room.PlayersInRoom.FirstOrDefault(p =>
                    p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && p != _player);
            }

            if (target == null)
            {
                await WriteToConsole.TextToPlayer(_player, $">> Player '{name}' not found in this room.");
                return "error";
            }
            if (target.IsInCombat)
            {
                await WriteToConsole.TextToPlayer(_player, $">> {target.Name} is already in combat.");
                return "error";
            }
            if (!target.IsAlive)
            {
                await WriteToConsole.TextToPlayer(_player, $">> {target.Name} is dead and cannot fight.");
                return "error";
            }

            
            await WriteToConsole.TextToPlayer(target, $"{_player.Name} challenges you to a fight! Accept? (yes/no): ");
            string response = await WriteToConsole.TakeInput(target);
            if (response?.Trim().ToLower() != "yes")
            {
                await WriteToConsole.TextToPlayer(_player, $">> {target.Name} declined the fight.");
                return "declined";
            }

            
            _player.IsInCombat = true;
            target.IsInCombat = true;
            _player.Stamina = _player.MaxStamina;
            target.Stamina = target.MaxStamina;

            await WriteToConsole.TextToPlayer(_player, $"\n⚔️ FIGHT against {target.Name} ⚔️");
            await WriteToConsole.TextToPlayer(target, $"\n⚔️ FIGHT against {_player.Name} ⚔️");

            Player winner = null;
            try
            {
                while (_player.Health > 0 && target.Health > 0)
                {
                    //  status to both
                    string status1 = $"You: HP {_player.Health}/{_player.MaxHealth} | Shield {_player.Shield}/{_player.MaxShield} | Stam {_player.Stamina}/{_player.MaxStamina}\n" +
                                     $"{target.Name}: HP {target.Health}/{target.MaxHealth} | Shield {target.Shield}/{target.MaxShield}";
                    string status2 = $"You: HP {target.Health}/{target.MaxHealth} | Shield {target.Shield}/{target.MaxShield} | Stam {target.Stamina}/{target.MaxStamina}\n" +
                                     $"{_player.Name}: HP {_player.Health}/{_player.MaxHealth} | Shield {_player.Shield}/{_player.MaxShield}";
                    await WriteToConsole.TextToPlayer(_player, status1);
                    await WriteToConsole.TextToPlayer(target, status2);

                    //  moves
                    await WriteToConsole.TextToPlayer(_player, "\nYour attack (fast/heavy/careful): ");
                    string move1 = await WriteToConsole.TakeInput(_player);
                    await WriteToConsole.TextToPlayer(target, "\nYour attack (fast/heavy/careful): ");
                    string move2 = await WriteToConsole.TakeInput(target);

                    var type1 = ParseAttack(move1);
                    var type2 = ParseAttack(move2);

                    // resolve attacks 
                    bool p1CanAttack = true, p2CanAttack = true;
                    int cost1 = GetStaminaCost(type1, _player);
                    int cost2 = GetStaminaCost(type2, target);

                    if (_player.Stamina < cost1)
                    {
                        await WriteToConsole.TextToPlayer(_player, "Not enough stamina!");
                        p1CanAttack = false;
                    }
                    if (target.Stamina < cost2)
                    {
                        await WriteToConsole.TextToPlayer(target, "Not enough stamina!");
                        p2CanAttack = false;
                    }

                    if (p1CanAttack)
                    {
                        _player.Stamina -= cost1;
                        int dmg = GetDamage(_player, type1);
                        dmg = ApplyShield(ref target, dmg);
                        if (dmg > 0)
                        {
                            target.Health = Math.Max(0, target.Health - dmg);
                            await WriteToConsole.TextToPlayer(_player, $"You hit {target.Name} for {dmg} HP.");
                            await WriteToConsole.TextToPlayer(target, $"{_player.Name} hits you for {dmg} HP.");
                        }
                    }

                    if (target.Health <= 0) break;

                    if (p2CanAttack)
                    {
                        target.Stamina -= cost2;
                        int dmg = GetDamage(target, type2);
                        dmg = ApplyShield(ref _player, dmg);
                        if (dmg > 0)
                        {
                            _player.Health = Math.Max(0, _player.Health - dmg);
                            await WriteToConsole.TextToPlayer(target, $"You hit {_player.Name} for {dmg} HP.");
                            await WriteToConsole.TextToPlayer(_player, $"{target.Name} hits you for {dmg} HP.");
                        }
                    }

                    //  regen
                    _player.Stamina = Math.Min(_player.MaxStamina, _player.Stamina + Math.Max(1, (int)(_player.MaxStamina * 0.03)));
                    target.Stamina = Math.Min(target.MaxStamina, target.Stamina + Math.Max(1, (int)(target.MaxStamina * 0.03)));
                }

                //  winner
                if (_player.Health <= 0)
                {
                    winner = target;
                    await WriteToConsole.TextToPlayer(_player, "\n💀 You were defeated!");
                    await WriteToConsole.TextToPlayer(target, $"\n🏆 You defeated {_player.Name}!");
                }
                else
                {
                    winner = _player;
                    await WriteToConsole.TextToPlayer(_player, $"\n🏆 You defeated {target.Name}!");
                    await WriteToConsole.TextToPlayer(target, "\n💀 You were defeated!");
                }

                Player loser = (winner == _player) ? target : _player;

                //  XP and coins from loser to winner
                int xpTransfer = loser.Experience;
                int coinsTransfer = loser.Coins;
                winner.Experience += xpTransfer;
                winner.Coins += coinsTransfer;
                loser.Experience = 0;
                loser.Coins = 0;
                loser.IsAlive = false;
                loser.Health = 0;

                await WriteToConsole.TextToPlayer(winner, $">> You gained {xpTransfer} XP and {coinsTransfer} coins.");
                await WriteToConsole.TextToPlayer(loser, $">> You lost all XP and coins.");

                // save both
                await _gameWorld.UpadateVluesForPlayerTOList(winner);
                await _gameWorld.UpadateVluesForPlayerTOList(loser);
                await _gameWorld.SavePlayersList();
            }
            finally
            {
                _player.IsInCombat = false;
                target.IsInCombat = false;
            }

            return "fightplayer";
        }

        public override bool Exit() => false;

        private int ApplyShield(ref Player defender, int damage)
        {
            int absorbed = Math.Min(defender.Shield, damage);
            defender.Shield -= absorbed;
            return damage - absorbed;
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

        private int GetStaminaCost(AttackType type, Player player) =>
            type switch
            {
                AttackType.Heavy => Math.Max(1, (int)(0.30 * player.MaxStamina)),
                AttackType.Fast => Math.Max(1, (int)(0.10 * player.MaxStamina)),
                _ => Math.Max(1, (int)(0.20 * player.MaxStamina))
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