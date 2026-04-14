using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{
    internal class Fight : Command
    {
        public Fight(Player player, GameWorld gameWorld) : base(player, gameWorld)
        {
        }

        public override Task<string> Execute()
        {
            throw new NotImplementedException();
        }

        public override bool Exit()
        {
            throw new NotImplementedException();
        }
    }
}
