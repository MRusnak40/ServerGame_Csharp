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
        public FightPlayer(Player player, GameWorld gameWorld) : base(player, gameWorld)
        {
        }

        public override async Task<string> Execute()
        {

            await WriteToConsole.TextToPlayer(_player, $"Set name to attack");

            string name = await WriteToConsole.TakeInput(_player);
            Player player=new();

            foreach (Player p in _gameWorld.OnlinePlayers)
            {
                if (p.Name == name)
                {
                    player = p;
                    WriteToConsole.TextToPlayer(p, "BYL JSI VYZVAN");
                    WriteToConsole.TextToPlayer(p, "BYL JSI VYZVAN");
                    WriteToConsole.TextToPlayer(p, "BYL JSI VYZVAN");
                    WriteToConsole.TextToPlayer(p, "BYL JSI VYZVAN");
                    WriteToConsole.TextToPlayer(p, "BYL JSI VYZVAN");

                }

            }






            // 3. Samotný závod - Task.WhenAny vrátí první dokončený úkol
            var winnerTask = await Task.WhenAny(WriteToConsole.TakeInput(_player), WriteToConsole.TakeInput(player));



            string result = await winnerTask;


            string winnerName = (winnerTask.Id == 1) ? "Hráč 1" : "Hráč 2";
            WriteToConsole.BroadcastAll($"{winnerName} byl rychlejší! Napsal: {result}",_gameWorld.OnlinePlayers);


            await WriteToConsole.TextToPlayer(_player, $"Konec! Vyhrál {winnerName} s textem: {result}");
            await WriteToConsole.TextToPlayer(_player, $"Konec! Vyhrál {winnerName} s textem: {result}");


            return "fight";
        }

        public override bool Exit() => false;
    }
}