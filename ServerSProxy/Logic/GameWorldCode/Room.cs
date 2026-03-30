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

    }
}
