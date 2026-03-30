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



        private TypeOfItemEnum _typeOfItem;








        // properties


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
