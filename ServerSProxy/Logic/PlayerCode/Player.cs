using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ServerSProxy.Logic.PlayerCode
{
    internal class Player
    {
        public Player() { }

        private string _name;

        [JsonIgnore]
        private Room _currentRoom;

        private string _class;
        private bool _isAlive = true;
        private bool _isInCombat;
        private bool _isKillable = true;
        private bool _isTrading;

        [JsonIgnore]
        private StreamWriter _writer;

        [JsonIgnore]
        private StreamReader _reader;

        public DateTime LastActive { get; set; } = DateTime.Now;

        private int _experience;
        private int _level = 1;
        private Inventory _inventory;
        private List<Quest> _activeQuests = new List<Quest>();

        private int _coins;

        private int _maxHealth;
        private int _health;
        private int _maxShield;
        private int _shield;
        private int _stamina;
        private int _maxStamina;
        private int _attackSpeed;
        private int _strength;

        // quipment tracking key = TypeOfItem  value = equipped item
        [JsonIgnore]
        private Dictionary<string, Item> _equippedItems = new Dictionary<string, Item>();

        [JsonIgnore]
        public StreamReader Reader
        {
            get => _reader;
            set => _reader = value;
        }

        [JsonIgnore]
        public StreamWriter Writer
        {
            get => _writer;
            set => _writer = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public bool IsInCombat
        {
            get => _isInCombat;
            set => _isInCombat = value;
        }

        public bool IsKillable
        {
            get => _isKillable;
            set => _isKillable = value;
        }

        public bool IsAlive
        {
            get => _isAlive;
            set => _isAlive = value;
        }

        public bool IsTrading
        {
            get => _isTrading;
            set => _isTrading = value;
        }

        [JsonIgnore]
        public Room CurrentRoom
        {
            get => _currentRoom;
            set => _currentRoom = value;
        }

        public string Class
        {
            get => _class;
            set => _class = value;
        }

        public int Experience
        {
            get => _experience;
            set => _experience = value;
        }

        public int Level
        {
            get => _level;
            set => _level = value;
        }

        public Inventory Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }

        public List<Quest> ActiveQuests
        {
            get => _activeQuests;
            set => _activeQuests = value;
        }

        public int Coins
        {
            get => _coins;
            set => _coins = value;
        }

        public int MaxHealth
        {
            get => _maxHealth;
            set => _maxHealth = value;
        }

        public int Health
        {
            get => _health;
            set => _health = value;
        }

        public int MaxShield
        {
            get => _maxShield;
            set => _maxShield = value;
        }

        public int Shield
        {
            get => _shield;
            set => _shield = value;
        }

        public int Stamina
        {
            get => _stamina;
            set => _stamina = value;
        }

        public int MaxStamina
        {
            get => _maxStamina;
            set => _maxStamina = value;
        }

        public int AttackSpeed
        {
            get => _attackSpeed;
            set => _attackSpeed = value;
        }

        public int Strength
        {
            get => _strength;
            set => _strength = value;
        }

        [JsonIgnore]
        public Dictionary<string, Item> EquippedItems
        {
            get => _equippedItems;
            set => _equippedItems = value;
        }

        /// <summary>
        /// Equips an item, replacing any existing item of the same type.
        /// The old item is returned (to be dropped to the room).
        /// </summary>
        public Item EquipItem(Item newItem)
        {
            if (newItem == null || !newItem.IsEquippable)
                return null;

            // Remove old item if exists
            if (EquippedItems.TryGetValue(newItem.TypeOfItem, out Item oldItem))
            {
                oldItem.Unequip(this);
                EquippedItems.Remove(newItem.TypeOfItem);
            }

            // equip new
            newItem.Equip(this);
            EquippedItems[newItem.TypeOfItem] = newItem;
            return oldItem; // caller must drop old item to room
        }

        /// <summary>
        /// Unequips the item of given type and returns it.
        /// </summary>
        public Item UnequipItem(string type)
        {
            if (EquippedItems.TryGetValue(type, out Item item))
            {
                item.Unequip(this);
                EquippedItems.Remove(type);
                return item;
            }
            return null;
        }

        public override string ToString()
        {
            return $"[{_class}] {_name} | Level {_level} | XP: {_experience}\n" +
                   $"HP: {_health}/{_maxHealth} | Shield: {_shield}/{_maxShield} | Stamina: {_stamina}/{_maxStamina}\n" +
                   $"Strength: {_strength} | Attack Speed: {_attackSpeed}\n" +
                   $"Coins: {_coins} | Room: {_currentRoom?.Name ?? "none"}";
        }
    }
}