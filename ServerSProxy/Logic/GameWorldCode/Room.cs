using ServerSProxy.Logic.NPCs;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.PlayerCode.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.GameWorldCode
{
    internal class Room
    {


        private string _name;
        private int _levelOfRoom;

        //lists
        private List<string>? _connectedRooms;


        private List<Player>? _playersInRoom;

        private List<NPC>? _npcsInRoom;

        private List<Enemy>? _enemiesInRoom;



        private List<Item>? _dropedItems;


        public Room()
        {
            _connectedRooms = new List<string>();
            _playersInRoom = new List<Player>();
            _npcsInRoom = new List<NPC>();
            _enemiesInRoom = new List<Enemy>();
            _dropedItems = new List<Item>();
        }


        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int LevelOfRoom
        {
            get { return _levelOfRoom; }
            set { _levelOfRoom = value; }
        }

        public List<string>? ConnectedRooms
        {
            get { return _connectedRooms; }
            set { _connectedRooms = value; }
        }

        public List<Player>? PlayersInRoom
        {
            get { return _playersInRoom; }
            set { _playersInRoom = value; }
        }

        public List<NPC>? NpcsInRoom
        {
            get { return _npcsInRoom; }
            set { _npcsInRoom = value; }
        }

        public List<Enemy>? EnemiesInRoom
        {
            get { return _enemiesInRoom; }
            set { _enemiesInRoom = value; }
        }

        public List<Item>? DropedItems
        {
            get { return _dropedItems; }
            set { _dropedItems = value; }
        }


        public override string ToString()
        {
            return $@"
              <<| {Name.ToUpper()} |>>
              --[ Level: {LevelOfRoom} ]--
  
              Entities: [ Plr:{PlayersInRoom?.Count ?? 0} / Npc:{NpcsInRoom?.Count ?? 0} ]
              Threats:  [ Hostile:{EnemiesInRoom?.Count ?? 0} ]
              Ground:   [ Items:{DropedItems?.Count ?? 0} ]
  
              ==============================";
        }

    }
}
