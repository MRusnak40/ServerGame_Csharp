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
    internal class Chat: Command
    {
        public Chat(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            // chat start
            _player.IsKillable = false;
            await WriteToConsole.TextToPlayer(_player, "You entered chat mode.");

            bool chatting = true;
            while (chatting)
            {
                // what to text
                await WriteToConsole.TextToPlayer(_player, "Message: ");
                string message = await WriteToConsole.TakeInput(_player);
                if (string.IsNullOrWhiteSpace(message))
                {
                    await WriteToConsole.TextToPlayer(_player, "Empty message ignored.");
                    continue;
                }
                message = message.Trim();

               
                await WriteToConsole.TextToPlayer(_player, "Send to (all in ROOM / player name): ");

                WriteToConsole.TextToPlayer(_player, "▬▬▬▬▬▬▬▬▬▬ONLINE▬▬▬▬▬▬▬▬▬▬▬▬");
             

                foreach (Player p in _gameWorld.OnlinePlayers) {

                    if (p == _player) continue;
                    WriteToConsole.TextToPlayer(_player, p.Name);
                    WriteToConsole.TextToPlayer(_player, "------------------");


                }
                WriteToConsole.TextToPlayer(_player, "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                string target = await WriteToConsole.TakeInput(_player);
                target = target?.Trim().ToLower() ?? "";

                if (target == "all")
                {
               
                    if (_player.CurrentRoom == null || _player.CurrentRoom.PlayersInRoom == null)
                    {
                        await WriteToConsole.TextToPlayer(_player, "You are not in any room.");
                    }
                    else
                    {
                        string formatted = $"[Room] {_player.Name}: {message}";
                        foreach (var p in _player.CurrentRoom.PlayersInRoom)
                        {
                            await WriteToConsole.TextToPlayer(p, formatted);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(target))
                {
                    // finding player
                    Player targetPlayer = _gameWorld.OnlinePlayers.Find(p =>
                        p.Name.Equals(target, StringComparison.OrdinalIgnoreCase));

                    if (targetPlayer == null)
                    {
                        await WriteToConsole.TextToPlayer(_player, $"Player '{target}' not found.");
                    }
                    else
                    {
                        // send
                        await WriteToConsole.TextToPlayer(_player, $"[To {targetPlayer.Name}]: {message}");
                        
                        await WriteToConsole.TextToPlayer(targetPlayer, $"[From {_player.Name}]: {message}");
                    }
                }
                else
                {
                    await WriteToConsole.TextToPlayer(_player, "Invalid option. Use 'all' or exact player name.");
                }

                // continue
                await WriteToConsole.TextToPlayer(_player, "Continue chatting? (yes/no): ");
                string answer = await WriteToConsole.TakeInput(_player);
                if (answer?.Trim().ToLower() != "yes")
                    chatting = false;
            }

            //end
            _player.IsKillable = true;
            await WriteToConsole.TextToPlayer(_player, "You left chat mode.");
            return string.Empty;
        }

        public override bool Exit() => false;
    }
}

