using ServerSProxy.Logic.PlayerCode.Items;
using System.Collections.Generic;
using System.Linq;

namespace ServerSProxy.Logic.PlayerCode
{
    internal class Inventory
    {
        public Inventory() { }

        private List<Item> _items;
        private List<Item> _itemsPicked;
        private int _capacity;

        public Inventory(List<Item> items, List<Item> itemsPicked, int capacity)
        {
            Items = items ?? new List<Item>();
            ItemsPicked = itemsPicked ?? new List<Item>();
            Capacity = capacity;
        }

        public List<Item> Items
        {
            get => _items;
            set => _items = value;
        }

        public List<Item> ItemsPicked
        {
            get => _itemsPicked;
            set => _itemsPicked = value;
        }

        public int Capacity
        {
            get => _capacity;
            set => _capacity = value;
        }

        /// <summary>
        /// Tries to add an item, respecting capacity and stacking rules.
        /// Returns true if the item was fully added.
        /// </summary>
        public bool AddItem(Item item)
        {
            if (item == null) return false;

            // Stackable items may be merged
            if (item.IsStackable)
            {
                var existing = Items.FirstOrDefault(i => i.Name == item.Name && i.IsStackable);
                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                    return true;
                }
            }

            // Check capacity (each distinct item takes one slot)
            if (Items.Count >= Capacity)
                return false;

            Items.Add(item);
            return true;
        }

        /// <summary>
        /// Removes an item from inventory (by reference).
        /// </summary>
        public bool RemoveItem(Item item)
        {
            return Items.Remove(item);
        }

        /// <summary>
        /// Checks if there is free capacity for a new distinct item (or stacking possibility).
        /// </summary>
        public bool CanAddItem(Item item)
        {
            if (item == null) return false;
            if (item.IsStackable && Items.Any(i => i.Name == item.Name && i.IsStackable))
                return true;
            return Items.Count < Capacity;
        }
    }
}