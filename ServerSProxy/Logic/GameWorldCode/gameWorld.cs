using ServerSProxy.Logic.PlayerCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.GameWorldCode
{
    internal class gameWorld
    {

        List<Map>? _mapsInGameWorld;

        private int _numberOfMapsInGameWorld;

        private List<Player> _onlinePlayers;


        public List<Map>? MapsInGameWorld
        {
            get { return _mapsInGameWorld; }
            set { _mapsInGameWorld = value; }
        }

        public int NumberOfMapsInGameWorld
        {
            get { return _numberOfMapsInGameWorld; }
            set { _numberOfMapsInGameWorld = value; }
        }

        public List<Player> OnlinePlayers
        {
            get { return _onlinePlayers; }
            set { _onlinePlayers = value; }
        }
    }
}
