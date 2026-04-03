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



        public  Inventory(List<Item> items, List<Item> itemsPicked, int capacity)
        {
            Items = items;
            ItemsPicked = itemsPicked;
            Capacity = capacity;
        }
        public List<Item> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public List<Item> ItemsPicked
        {
            get { return _itemsPicked; }
            set { _itemsPicked = value; }
        }

        public int Capacity
        {
            get { return _capacity; }
            set { _capacity = value; }
        }
    }
}
