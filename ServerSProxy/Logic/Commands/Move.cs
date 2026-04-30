using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{
    internal class Move : Command
    {
        public Move(Player player, GameWorld gameWorld) : base(player, gameWorld)
        {
        }

        public override async Task<string> Execute()
        {
           
            Room currentRoom = GetPlayerRoom(_player);

            if (currentRoom == null)
            {
                await WriteToConsole.TextToPlayer(_player, "error: room not found!");
                return "error";
            }

           
            DisplayRoomInfo(currentRoom);

           
            List<Room> connectedRooms = GetConnectedRooms(currentRoom);

            if (connectedRooms.Count == 0)
            {
                await WriteToConsole.TextToPlayer(_player, "error: no connected rooms!");
                return "error";
            }

           
            await DisplayConnectedRooms(connectedRooms);

            await WriteToConsole.TextToPlayer(_player, "\n> select room (1-" + connectedRooms.Count + "): ");
            string? choice = await WriteToConsole.TakeInput(_player);

            if (!int.TryParse(choice, out int roomChoice) || roomChoice < 1 || roomChoice > connectedRooms.Count)
            {
                await WriteToConsole.TextToPlayer(_player, "error: invalid choice!");
                return "error";
            }

            // move player
            Room newRoom = connectedRooms[roomChoice - 1];
            await MovePlayerToRoom(_player, currentRoom, newRoom);

            await WriteToConsole.TextToPlayer(_player, $"\n> you entered {newRoom.Name}!");
            return "move";
        }

        public override bool Exit() => false;

        private Room GetPlayerRoom(Player player)
        {
            // search all maps and rooms to find player
            foreach (var map in _gameWorld.MapsInGameWorld)
            {
                if (map.RoomsInMap != null)
                {
                    foreach (var room in map.RoomsInMap)
                    {
                        if (room.PlayersInRoom != null && room.PlayersInRoom.Contains(player))
                            return room;
                    }
                }
            }
            return null;
        }

        private List<Room> GetConnectedRooms(Room currentRoom)
        {
            
            List<Room> connected = new List<Room>();

            if (currentRoom.ConnectedRooms == null || currentRoom.ConnectedRooms.Count == 0)
                return connected;

            // find rooms by name in all maps
            foreach (var map in _gameWorld.MapsInGameWorld)
            {
                if (map.RoomsInMap != null)
                {
                    foreach (var room in map.RoomsInMap)
                    {
                        if (currentRoom.ConnectedRooms.Contains(room.Name))
                            connected.Add(room);
                    }
                }
            }

            return connected;
        }

        private void DisplayRoomInfo(Room room)
        {
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n========================================");
            sb.AppendLine($"room: {room.Name}");
            sb.AppendLine($"level: {room.LevelOfRoom}");
            sb.AppendLine("========================================");

            // display players in room
            sb.Append("players (");
            sb.Append(room.PlayersInRoom?.Count ?? 0);
            sb.AppendLine("):");

            if (room.PlayersInRoom != null && room.PlayersInRoom.Count > 0)
            {
                foreach (var p in room.PlayersInRoom)
                {
                    sb.AppendLine($"  - {p.Name}");
                }
            }
            else
                sb.AppendLine("  - none");

            // display enemies in room
            sb.Append("enemies (");
            sb.Append(room.EnemiesInRoom?.Count ?? 0);
            sb.AppendLine("):");

            if (room.EnemiesInRoom != null && room.EnemiesInRoom.Count > 0)
            {
                foreach (var enemy in room.EnemiesInRoom)
                {
                    sb.AppendLine($"  - {enemy.Name} lvl{enemy.Level}");
                }
            }
            else
                sb.AppendLine("  - none");

            // display dropped items
            sb.Append("items (");
            sb.Append(room.DropedItems?.Count ?? 0);
            sb.AppendLine("):");

            if (room.DropedItems != null && room.DropedItems.Count > 0)
            {
                foreach (var item in room.DropedItems)
                {
                    sb.AppendLine($"  - {item.Name}");
                }
            }
            else
                sb.AppendLine("  - none");

            sb.AppendLine("========================================");

            WriteToConsole.TextToPlayer(_player, sb.ToString()).GetAwaiter().GetResult();
        }

        private async Task DisplayConnectedRooms(List<Room> rooms)
        {
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\nconnected rooms:");
            sb.AppendLine("----------------------------");

            for (int i = 0; i < rooms.Count; i++)
            {
                sb.AppendLine($"  {i + 1}. {rooms[i].Name} (lvl {rooms[i].LevelOfRoom})");
            }

            sb.AppendLine("----------------------------");

            await WriteToConsole.TextToPlayer(_player, sb.ToString());
        }

        private async Task MovePlayerToRoom(Player player, Room oldRoom, Room newRoom)
        {
           
            oldRoom.PlayersInRoom?.Remove(player);

           
            newRoom.PlayersInRoom?.Add(player);

         
            player.CurrentRoom = newRoom;

            // notify other players in new room
            List<Player> otherPlayers = newRoom.PlayersInRoom
                ?.Where(p => p.Name != player.Name)
                .ToList() ?? new List<Player>();

            if (otherPlayers.Count > 0)
            {
                await WriteToConsole.BroadcastAll($"\n[{newRoom.Name}] player {player.Name} entered!", otherPlayers);
            }
        }
    }
}
