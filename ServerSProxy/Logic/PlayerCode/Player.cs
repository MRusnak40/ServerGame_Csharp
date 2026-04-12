using ServerSProxy.Logic.GameWorldCode;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.PlayerCode
{
    internal class Player
    {
        public Player() { }

        //basic info
        private string _name;
        private Room _currentRoom;
        private string _class;
        private bool _isAlive; //is alive or not, if not, cant do anything, but can be revived
        private bool _isInCombat;//cant be fight with two people at the same time
        private bool _isKillable;//is in chat
        private StreamWriter _writer;
        private StreamReader _reader;
        public DateTime LastActive { get; set; } = DateTime.Now;

        //stats
        private int _experience;
        private int _level;
        private Inventory _inventory; 
        private List<Quest> _activeQuests;


        //money
        private int _coins;


        //health
        private int _maxHealth;
        private int _health;

        //shield
        private int _maxShield;
        private int _shield;

        //stamina
        private int _stamina;
        private int _maxStamina;

        //damage
        private int _attackSpeed;
        private int _strength;


        // properties
        public StreamReader Reader
        {
            get { return _reader; }
            set { _reader = value; }
        }

        public StreamWriter Writer
        {
            get { return _writer; }
            set { _writer = value; }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool IsInCombat
        {
            get { return _isInCombat; }
            set { _isInCombat = value; }
        }

        public bool IsKillable
        {
            get { return _isKillable; }
            set { _isKillable = value; }
        }

        public Room CurrentRoom
        {
            get { return _currentRoom; }
            set { _currentRoom = value; }
        }

        public string Class
        {
            get { return _class; }
            set { _class = value; }
        }

        public int Experience
        {
            get { return _experience; }
            set { _experience = value; }
        }

        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public Inventory Inventory
        {
            get { return _inventory; }
            set { _inventory = value; }
        }

         public List<Quest> ActiveQuests
        {
            get { return _activeQuests; }
            set { _activeQuests = value; }
        }

        public int Coins
        {
            get { return _coins; }
            set { _coins = value; }
        }

        public int MaxHealth
        {
            get { return _maxHealth; }
            set { _maxHealth = value; }
        }

        public int Health
        {
            get { return _health; }
            set { _health = value; }
        }

        public int MaxShield
        {
            get { return _maxShield; }
            set { _maxShield = value; }
        }

        public int Shield
        {
            get { return _shield; }
            set { _shield = value; }
        }

        public int Stamina
        {
            get { return _stamina; }
            set { _stamina = value; }
        }

        public int MaxStamina
        {
            get { return _maxStamina; }
            set { _maxStamina = value; }
        }

        public int AttackSpeed
        {
            get { return _attackSpeed; }
            set { _attackSpeed = value; }
        }

        public int Strength
        {
            get { return _strength; }
            set { _strength = value; }
        }

        public bool IsAlive
        {
            get { return _isAlive; }
            set { _isAlive = value; }
        }

        // toString
        public override string ToString()
        {
            return $"[{_class}] {_name} | Level {_level} | XP: {_experience}\n" +
                   $"HP: {_health}/{_maxHealth} | Štít: {_shield}/{_maxShield} | Stamina: {_stamina}/{_maxStamina}\n" +
                   $"Síla: {_strength} | Rychlost útoku: {_attackSpeed}\n" +
                   $"Coins: {_coins} | Místnost: {_currentRoom?.Name ?? "žádná"}";
        }


    }
}
