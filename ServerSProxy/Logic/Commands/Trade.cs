using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.PlayerCode.Items;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{
    internal class Trade : Command
    {
        // track the last trade action for rollback on disconnect
        private (Player seller, Player buyer, Item item, int coins) lastTrade;

        public Trade(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            if (_player.IsInCombat || !_player.IsAlive)
            {
                await WriteToConsole.TextToPlayer(_player, ">> You cannot trade right now.");
                return "error";
            }

            await WriteToConsole.TextToPlayer(_player, "Enter the name of the player to trade with: ");
            string targetName = await WriteToConsole.TakeInput(_player);
            if (string.IsNullOrWhiteSpace(targetName)) return "error";

            Room room = FindPlayerRoom(_player);
            if (room == null)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Room not found.");
                return "error";
            }

            Player target = room.PlayersInRoom?.FirstOrDefault(p =>
                p.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase) && p != _player);

            if (target == null)
            {
                await WriteToConsole.TextToPlayer(_player, $">> Player '{targetName}' not found in this room.");
                return "error";
            }
            if (target.IsInCombat || !target.IsAlive)
            {
                await WriteToConsole.TextToPlayer(_player, $">> {target.Name} cannot trade now.");
                return "error";
            }
            if (target.IsTrading)
            {
                await WriteToConsole.TextToPlayer(_player, $">> {target.Name} is already trading.");
                return "error";
            }

            // Initiate trade
            await WriteToConsole.TextToPlayer(target, $"{_player.Name} wants to trade with you. Join? (yes/no): ");
            string answer = await WriteToConsole.TakeInput(target);
            if (answer?.Trim().ToLower() != "yes")
            {
                await WriteToConsole.TextToPlayer(_player, $">> {target.Name} declined the trade.");
                return "declined";
            }

            _player.IsTrading = true;
            target.IsTrading = true;
            lastTrade = (null, null, null, 0);

            try
            {
                await WriteToConsole.TextToPlayer(_player, "Trade started. You will take turns offering items.");
                await WriteToConsole.TextToPlayer(target, "Trade started. You will take turns offering items.");

                bool initiatorTurn = true; // _player starts
                while (true)
                {
                    Player offerer = initiatorTurn ? _player : target;
                    Player receiver = initiatorTurn ? target : _player;

                    // show offerer inventory
                    await WriteToConsole.TextToPlayer(offerer, "\nYour inventory:");
                    await ShowInventory(offerer, offerer);
                    await WriteToConsole.TextToPlayer(offerer, "Enter the number of the item you want to sell (or 'done' to finish): ");
                    string input = await WriteToConsole.TakeInput(offerer);
                    if (input?.Trim().ToLower() == "done")
                    {
                        await WriteToConsole.TextToPlayer(offerer, "Trade ended.");
                        await WriteToConsole.TextToPlayer(receiver, $"{offerer.Name} finished trading.");
                        break;
                    }

                    if (!int.TryParse(input, out int idx) || idx < 1 || idx > offerer.Inventory.Items.Count)
                    {
                        await WriteToConsole.TextToPlayer(offerer, "Invalid selection.");
                        continue;
                    }

                    Item item = offerer.Inventory.Items[idx - 1];
                    int price = item.Value;

                    // show receiver the offer
                    await WriteToConsole.TextToPlayer(receiver, $"\n{offerer.Name} offers: {item.Name} (price: {price} coins).");
                    await WriteToConsole.TextToPlayer(receiver, $"Your coins: {receiver.Coins}, Inventory space: {(receiver.Inventory.CanAddItem(item) ? "available" : "full")}");
                    await WriteToConsole.TextToPlayer(receiver, "Accept? (yes/no): ");
                    string accept = await WriteToConsole.TakeInput(receiver);

                    if (accept?.Trim().ToLower() == "yes")
                    {
                        //  receiver can pay and has space
                        if (receiver.Coins < price)
                        {
                            await WriteToConsole.TextToPlayer(receiver, "Not enough coins.");
                            await WriteToConsole.TextToPlayer(offerer, $"{receiver.Name} cannot afford the item.");
                            continue;
                        }
                        if (!receiver.Inventory.AddItem(item))
                        {
                            await WriteToConsole.TextToPlayer(receiver, "Inventory full. Cannot accept.");
                            await WriteToConsole.TextToPlayer(offerer, $"{receiver.Name} has no inventory space.");
                            continue;
                        }

                        //  exchange
                        offerer.Inventory.RemoveItem(item);
                        receiver.Coins -= price;
                        offerer.Coins += price;

                        lastTrade = (offerer, receiver, item, price);

                        await WriteToConsole.TextToPlayer(offerer, $"You sold {item.Name} for {price} coins.");
                        await WriteToConsole.TextToPlayer(receiver, $"You bought {item.Name} for {price} coins.");

                        // item is equippable auto-equip for receiver
                        if (item.IsEquippable)
                        {
                            Item old = receiver.EquipItem(item);
                            if (old != null)
                            {
                                // drop old item to room
                                Room receiverRoom = FindPlayerRoom(receiver);
                                if (receiverRoom != null)
                                    receiverRoom.DropedItems.Add(old);
                                await WriteToConsole.TextToPlayer(receiver, $"Unequipped {old.Name} to make room.");
                            }
                        }
                    }
                    else
                    {
                        await WriteToConsole.TextToPlayer(offerer, $"{receiver.Name} declined the offer.");
                    }

                    
                    initiatorTurn = !initiatorTurn;
                }
            }
            catch (Exception)
            {
                // rollback last trade if something goes wrong
                if (lastTrade.seller != null)
                {
                    lastTrade.seller.Inventory.AddItem(lastTrade.item);
                    lastTrade.buyer.Inventory.RemoveItem(lastTrade.item);
                    lastTrade.buyer.Coins += lastTrade.coins;
                    lastTrade.seller.Coins -= lastTrade.coins;
                    await WriteToConsole.TextToPlayer(lastTrade.seller, "Trade interrupted. Last transaction rolled back.");
                    await WriteToConsole.TextToPlayer(lastTrade.buyer, "Trade interrupted. Last transaction rolled back.");
                }
                throw; // rethrow to be handled by caller
            }
            finally
            {
                _player.IsTrading = false;
                target.IsTrading = false;
                await _gameWorld.UpadateVluesForPlayerTOList(_player);
                await _gameWorld.UpadateVluesForPlayerTOList(target);
                await _gameWorld.SavePlayersList();
            }

            return "trade";
        }

        public override bool Exit() => false;

        private async Task ShowInventory(Player viewer, Player owner)
        {
            var sb = new StringBuilder();
            var items = owner.Inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                sb.AppendLine($" {i + 1}. {it.Name} (x{it.Quantity}) - Value: {it.Value} coins");
            }
            if (items.Count == 0) sb.AppendLine("  (empty)");
            await WriteToConsole.TextToPlayer(viewer, sb.ToString());
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