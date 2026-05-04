using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.PlayerCode.Items;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{
    internal class Pickup : Command
    {
        public Pickup(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            Room room = FindPlayerRoom(_player);
            if (room == null)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Room not found!");
                return "error";
            }

            var items = room.DropedItems?.ToList() ?? new List<Item>();
            if (items.Count == 0)
            {
                await WriteToConsole.TextToPlayer(_player, ">> No items on the ground.");
                return "error";
            }

            // show items
            var sb = new StringBuilder();
            sb.AppendLine("\nItems on ground:");
            for (int i = 0; i < items.Count; i++)
                sb.AppendLine($" {i + 1}. {items[i].Name} (value: {items[i].Value})" +
                              (items[i].IsEquippable ? " [Equippable]" : ""));
            await WriteToConsole.TextToPlayer(_player, sb.ToString());

            await WriteToConsole.TextToPlayer(_player, $"\n>> Choose item to pick up (1-{items.Count}) or 0 to cancel:");
            string input = await WriteToConsole.TakeInput(_player);
            if (!int.TryParse(input, out int idx) || idx < 0 || idx > items.Count)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Invalid choice.");
                return "error";
            }
            if (idx == 0)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Pickup cancelled.");
                return "cancelled";
            }

            Item chosen = items[idx - 1];

            //  add to inventory
            bool added = TryPickupItem(_player, chosen, room);
            if (!added)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Your inventory is full. Would you like to drop an item to make room? (yes/no): ");
                string answer = await WriteToConsole.TakeInput(_player);
                if (answer?.Trim().ToLower() == "yes")
                {
                    // inventory and let player choose item to drop
                    await WriteToConsole.TextToPlayer(_player, "Your inventory:");
                    await ShowInventory();
                    await WriteToConsole.TextToPlayer(_player, "Enter the number of the item to drop (or 0 to cancel): ");
                    string dropInput = await WriteToConsole.TakeInput(_player);
                    if (int.TryParse(dropInput, out int dropIdx) && dropIdx > 0 && dropIdx <= _player.Inventory.Items.Count)
                    {
                        Item toDrop = _player.Inventory.Items[dropIdx - 1];
                        // dropped item is equipped unequip first
                        if (toDrop.IsEquippable && _player.EquippedItems.ContainsKey(toDrop.TypeOfItem))
                        {
                            _player.UnequipItem(toDrop.TypeOfItem);
                            await WriteToConsole.TextToPlayer(_player, $"Unequipped {toDrop.Name}.");
                        }
                        _player.Inventory.RemoveItem(toDrop);
                        room.DropedItems.Add(toDrop);
                        await WriteToConsole.TextToPlayer(_player, $"Dropped {toDrop.Name}.");

                        // Now try again to pick up chosen item
                        added = TryPickupItem(_player, chosen, room);
                        if (added)
                            await WriteToConsole.TextToPlayer(_player, $">> You picked up: {chosen.Name}");
                        else
                            await WriteToConsole.TextToPlayer(_player, ">> Failed to pick up item (still no space?).");
                    }
                    else
                    {
                        await WriteToConsole.TextToPlayer(_player, ">> Cancelled.");
                    }
                }
                else
                {
                    await WriteToConsole.TextToPlayer(_player, ">> Pickup cancelled.");
                }
            }
            else
            {
                await WriteToConsole.TextToPlayer(_player, $">> You picked up: {chosen.Name}");
            }

            //  others info
            var others = room.PlayersInRoom?.Where(p => p.Name != _player.Name).ToList() ?? new List<Player>();
            if (others.Count > 0)
            {
                await WriteToConsole.BroadcastAll($"\n[{room.Name}] {_player.Name} picked up {chosen.Name}.", others);
            }

            try
            {
                await _gameWorld.UpadateVluesForPlayerTOList(_player);
                await _gameWorld.SavePlayersList();
            }
            catch { }

            return "pickup";
        }

        public override bool Exit() => false;

        private bool TryPickupItem(Player player, Item item, Room room)
        {
            if (player.Inventory.AddItem(item))
            {
                // remove from ground
                room.DropedItems.Remove(item);
                // Auto-equip if equippable
                if (item.IsEquippable)
                {
                    Item old = player.EquipItem(item);
                    if (old != null)
                    {
                        room.DropedItems.Add(old);
                        WriteToConsole.TextToPlayer(player, $"Unequipped {old.Name} and put it on the ground.");
                    }
                }
                return true;
            }
            return false;
        }

        private async Task ShowInventory()
        {
            var sb = new StringBuilder();
            var items = _player.Inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                sb.AppendLine($" {i + 1}. {it.Name} (x{it.Quantity}) - Value {it.Value}" +
                              (it.IsEquippable ? " [E]" : ""));
            }
            if (items.Count == 0) sb.AppendLine("  (empty)");
            await WriteToConsole.TextToPlayer(_player, sb.ToString());
        }

        private Room FindPlayerRoom(Player player)
        {
            if (_gameWorld.MapsInGameWorld == null) return null;
            foreach (var map in _gameWorld.MapsInGameWorld.ToList())
            {
                if (map.RoomsInMap == null) continue;
                foreach (var room in map.RoomsInMap.ToList())
                {
                    if (room.PlayersInRoom == null) continue;
                    if (room.PlayersInRoom.Any(p => p.Name == player.Name))
                        return room;
                }
            }
            return null;
        }
    }
}