using ServerSProxy.Logic.PlayerCode.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.PlayerCode
{
    internal class Inventory
    {
        public Inventory() { }

        private List<Item> _items;
        private List<Item> _itemsPicked;
        private int _capacity;

    }
}
