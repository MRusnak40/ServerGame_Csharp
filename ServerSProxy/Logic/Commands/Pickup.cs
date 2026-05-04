using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.PlayerCode.Items;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{
    internal class Pickup : Command
    {
        public Pickup(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            // Najdi místnost hráče
            Room room = FindPlayerRoom(_player);
            if (room == null)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Room not found!");
                return "error";
            }

            // Snapshot droped items
            var items = room.DropedItems?.ToList() ?? new List<Item>();
            if (items.Count == 0)
            {
                await WriteToConsole.TextToPlayer(_player, ">> No items on the ground.");
                return "error";
            }

            // Vypiš položky
            var sb = new StringBuilder();
            sb.AppendLine("\nItems on ground:");
            for (int i = 0; i < items.Count; i++)
                sb.AppendLine($" {i + 1}. {items[i].Name} (value: {items[i].Value})");
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
                return string.Empty;
            }

            Item chosen = items[idx - 1];

            // Pokusíme se přidat do inventáře (pokud Inventory podporuje AddItem nebo veřejný List<Item>)
            bool addedToInventory = TryAddToInventory(_player, chosen);

            // Odeber z místnosti (bezpečně)
            try
            {
                room.DropedItems?.Remove(chosen);
            }
            catch { /* ignore */ }

            if (addedToInventory)
            {
                await WriteToConsole.TextToPlayer(_player, $">> You picked up: {chosen.Name}");
            }
            else
            {
                // fallback: přidej hodnotu jako mince
                _player.Coins += chosen.Value;
                await WriteToConsole.TextToPlayer(_player, $">> You picked up {chosen.Name} and sold it for {chosen.Value} coins (added to your coins).");
            }

            // Notify other players in room
            var others = room.PlayersInRoom?.Where(p => p.Name != _player.Name).ToList() ?? new List<Player>();
            if (others.Count > 0)
            {
                await WriteToConsole.BroadcastAll($"\n[{room.Name}] {_player.Name} picked up {chosen.Name}.", others);
            }

            // Uložit změny do GameWorld (pokud chcete persistovat, volitelné)
            try
            {
                await _gameWorld.UpadateVluesForPlayerTOList(_player);
                await _gameWorld.SavePlayersList();
            }
            catch { }

            return "pickup";
        }

        public override bool Exit() => false;

        private bool TryAddToInventory(Player player, Item item)
        {
            if (player.Inventory == null) return false;

            try
            {
                var inv = player.Inventory;
                var invType = inv.GetType();

                // 1) metoda AddItem(Item)
                var addMethod = invType.GetMethod("AddItem", new Type[] { typeof(Item) });
                if (addMethod != null)
                {
                    addMethod.Invoke(inv, new object[] { item });
                    return true;
                }

                // 2) metoda Add(Item)
                addMethod = invType.GetMethod("Add", new Type[] { typeof(Item) });
                if (addMethod != null)
                {
                    addMethod.Invoke(inv, new object[] { item });
                    return true;
                }

                // 3) veřejné pole / vlastnost typu List<Item>
                var listProp = invType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => typeof(IList).IsAssignableFrom(p.PropertyType) &&
                                         p.PropertyType.IsGenericType &&
                                         p.PropertyType.GetGenericArguments()[0] == typeof(Item));
                if (listProp != null)
                {
                    var listObj = listProp.GetValue(inv) as IList;
                    listObj?.Add(item);
                    return true;
                }

                // 4) veřejné pole
                var listField = invType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(f => typeof(IList).IsAssignableFrom(f.FieldType) &&
                                         f.FieldType.IsGenericType &&
                                         f.FieldType.GetGenericArguments()[0] == typeof(Item));
                if (listField != null)
                {
                    var listObj = listField.GetValue(inv) as IList;
                    listObj?.Add(item);
                    return true;
                }
            }
            catch
            {
                // ignore reflection errors, fallback níže
            }

            return false;
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
                    var snapshot = room.PlayersInRoom.ToList();
                    if (snapshot.Any(p => p.Name == player.Name))
                        return room;
                }
            }
            return null;
        }
    }
}
