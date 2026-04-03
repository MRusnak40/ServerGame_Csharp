using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.PlayerCode.Items
{
    internal class Item
    {
        public Item() { }


        //basic info
        private string _name;
        private string _description;
        private int _value; //value in coins
        private bool _isStackable;
        private bool _isEquippable;

        //stats
        private int _attackBonus;

        private int _healthBonus;

        private int _staminaBonus;

        private int _armorBonus;

        private int _attackSpeedBonus;

        private int _healUPBonus;



        private string _typeOfItem;








        // properties


        public bool IsEquippable
        {
            get { return _isEquippable; }
            set { _isEquippable = value; }
        }

        public int AttackBonus
        {
            get { return _attackBonus; }
            set { _attackBonus = value; }
        }

        public int HealthBonus
        {
            get { return _healthBonus; }
            set { _healthBonus = value; }
        }

        public int StaminaBonus
        {
            get { return _staminaBonus; }
            set { _staminaBonus = value; }
        }

        public int ArmorBonus
        {
            get { return _armorBonus; }
            set { _armorBonus = value; }
        }

        public int AttackSpeedBonus
        {
            get { return _attackSpeedBonus; }
            set { _attackSpeedBonus = value; }
        }

        public int HealUPBonus
        {
            get { return _healUPBonus; }
            set { _healUPBonus = value; }
        }

        public string TypeOfItem
        {
            get { return _typeOfItem; }
            set { _typeOfItem = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }



        public bool IsStackable
        {
            get { return _isStackable; }
            set { _isStackable = value; }
        }
    }
}
