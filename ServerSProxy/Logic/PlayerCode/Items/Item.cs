using ServerSProxy.Logic.PlayerCode;
using System;

namespace ServerSProxy.Logic.PlayerCode.Items
{
    internal class Item
    {
        public Item() { Quantity = 1; }

        private string _name;
        private string _description;
        private int _value;
        private bool _isStackable;
        private bool _isEquippable;

        private int _attackBonus;
        private int _healthBonus;
        private int _staminaBonus;
        private int _armorBonus;
        private int _attackSpeedBonus;
        private int _healUPBonus;

        private string _typeOfItem;
        private int _quantity;

        public bool IsEquippable
        {
            get => _isEquippable;
            set => _isEquippable = value;
        }

        public int AttackBonus
        {
            get => _attackBonus;
            set => _attackBonus = value;
        }

        public int HealthBonus
        {
            get => _healthBonus;
            set => _healthBonus = value;
        }

        public int StaminaBonus
        {
            get => _staminaBonus;
            set => _staminaBonus = value;
        }

        public int ArmorBonus
        {
            get => _armorBonus;
            set => _armorBonus = value;
        }

        public int AttackSpeedBonus
        {
            get => _attackSpeedBonus;
            set => _attackSpeedBonus = value;
        }

        public int HealUPBonus
        {
            get => _healUPBonus;
            set => _healUPBonus = value;
        }

        public string TypeOfItem
        {
            get => _typeOfItem;
            set => _typeOfItem = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public int Value
        {
            get => _value;
            set => _value = value;
        }

        public bool IsStackable
        {
            get => _isStackable;
            set => _isStackable = value;
        }

        public int Quantity
        {
            get => _quantity;
            set => _quantity = Math.Max(1, value);
        }

        // applies item bonuses to player stats 
        public void Equip(Player player)
        {
            if (player == null) return;
            player.MaxHealth += HealthBonus;
            player.Health += HealthBonus;
            player.MaxShield += ArmorBonus;
            player.Shield += ArmorBonus;
            player.MaxStamina += StaminaBonus;
            player.Stamina += StaminaBonus;
            player.Strength += AttackBonus;
            player.AttackSpeed += AttackSpeedBonus;
        }

        //removes item bonuses from player stats 
        public void Unequip(Player player)
        {
            if (player == null) return;
            player.MaxHealth -= HealthBonus;
            player.Health = Math.Min(player.Health, player.MaxHealth);
            player.MaxShield -= ArmorBonus;
            player.Shield = Math.Min(player.Shield, player.MaxShield);
            player.MaxStamina -= StaminaBonus;
            player.Stamina = Math.Min(player.Stamina, player.MaxStamina);
            player.Strength -= AttackBonus;
            player.AttackSpeed -= AttackSpeedBonus;
        }
    }
}