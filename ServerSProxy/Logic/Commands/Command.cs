using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{


    internal abstract class Command
    {

        protected Player _player;
        protected GameWorld _gameWorld;

        public Command(Player player, GameWorld gameWorld)
        {
            _player = player;
            _gameWorld = gameWorld;
        }

        public abstract Task<string> Execute(string input);
        public abstract bool Exit();

    }
}
