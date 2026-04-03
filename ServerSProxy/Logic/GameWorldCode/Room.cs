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
        private List<Room>? _connectedRooms;

        private List<Player>? _playersInRoom;

        private List<NPC>? _npcsInRoom;

        private List<Enemy>? _enemiesInRoom;



        private List<Item>? _dropedItems;


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

        public List<Room>? ConnectedRooms
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
    }
}
