using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.NPCs;
using ServerSProxy.Logic.ServersLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.Commands
{
    internal class TakeQuest : Command
    {
        public TakeQuest(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            Room room = FindPlayerRoom(_player);
            if (room == null)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Room not found.");
                return "error";
            }

            var npcs = room.NpcsInRoom?.ToList() ?? new List<NPC>();
            // Filter only those with at least one quest
            var questNpcs = npcs.Where(n => n.QuestsToGive != null && n.QuestsToGive.Count > 0).ToList();
            if (questNpcs.Count == 0)
            {
                await WriteToConsole.TextToPlayer(_player, ">> No NPCs with quests here.");
                return "error";
            }

            // display NPCs 
            var sb = new StringBuilder();
            sb.AppendLine("\nNPCs with quests:");
            for (int i = 0; i < questNpcs.Count; i++)
            {
                var npc = questNpcs[i];
                var quest = npc.QuestsToGive[0];
                sb.AppendLine($" {i + 1}. {npc.Name} -> \"{quest.Name}\" - {quest.Description} (Reward: {quest.ExperienceReward} XP, {quest.CoinsReward} coins)");
            }
            await WriteToConsole.TextToPlayer(_player, sb.ToString());

            await WriteToConsole.TextToPlayer(_player, "\nChoose an NPC (number) or 0 to cancel: ");
            string input = await WriteToConsole.TakeInput(_player);
            if (!int.TryParse(input, out int idx) || idx < 0 || idx > questNpcs.Count)
            {
                await WriteToConsole.TextToPlayer(_player, ">> Invalid choice.");
                return "error";
            }
            if (idx == 0) return "cancelled";

            NPC chosenNpc = questNpcs[idx - 1];
            Quest offeredQuest = chosenNpc.QuestsToGive[0];

            await WriteToConsole.TextToPlayer(_player, $"\n{chosenNpc.Name} says: \"{chosenNpc.TextToTell}\"");
            await WriteToConsole.TextToPlayer(_player, $"Quest: {offeredQuest.Name} - {offeredQuest.Description}");
            await WriteToConsole.TextToPlayer(_player, $"Reward: {offeredQuest.ExperienceReward} XP, {offeredQuest.CoinsReward} coins");
            await WriteToConsole.TextToPlayer(_player, "Accept? (yes/no): ");
            string answer = await WriteToConsole.TakeInput(_player);

            if (answer?.Trim().ToLower() == "yes")
            {
                // sdd to playera ctive quests 
                _player.ActiveQuests.Add(offeredQuest);
                chosenNpc.QuestsToGive.RemoveAt(0);
                await WriteToConsole.TextToPlayer(_player, $">> Quest '{offeredQuest.Name}' accepted!");
            }
            else
            {
                await WriteToConsole.TextToPlayer(_player, ">> Quest declined.");
            }

            return "takequest";
        }

        public override bool Exit() => false;

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