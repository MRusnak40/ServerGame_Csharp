using ServerSProxy.Logic.PlayerCode.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.NPCs
{
    internal class Enemy
    {


        public Enemy() { }

        private string _name;
        private int _level;


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

        private List<Item>? _droppingItems;










        // properties
        public List<Item>? DroppingItems
        {
            get { return _droppingItems; }
            set { _droppingItems = value; }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int Level
        {
            get { return _level; }
            set { _level = value; }
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


    }
}
