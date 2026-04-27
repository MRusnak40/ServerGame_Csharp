using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.GameWorldCode
{
    internal class Map
    {

     


        private string _nameOfMap;

        List<Room>? _roomsInMap;


        public Map()
        {
            _roomsInMap = new List<Room>();
        }
        public string NameOfMap
        {
            get { return _nameOfMap; }
            set { _nameOfMap = value; }
        }

        public List<Room>? RoomsInMap
        {
            get { return _roomsInMap; }
            set { _roomsInMap = value; }
        }
    }
}
