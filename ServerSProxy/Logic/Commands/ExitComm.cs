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
    internal class ExitComm : Command
    {
        public ExitComm(Player player, GameWorld gameWorld) : base(player, gameWorld)
        {
        }

        public override async Task<string> Execute()
        {
            _player.IsAlive = false;

            WriteToConsole.TextToPlayer(_player, "Goodbye! See you next time.");

            return "exit";
        }

        public override bool Exit() => true;
    }
}
