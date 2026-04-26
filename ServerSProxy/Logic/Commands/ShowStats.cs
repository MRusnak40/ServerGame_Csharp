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
    internal class ShowStats : Command
    {
        public ShowStats(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
           
            string stats = _player.ToString();
            await WriteToConsole.TextToPlayer(_player, stats);
            return string.Empty;   
            
        }

        public override bool Exit() => false;
    }
}
