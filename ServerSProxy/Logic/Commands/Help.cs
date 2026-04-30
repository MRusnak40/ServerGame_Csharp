using ServerSProxy.Logic.GameWorldCode;
using ServerSProxy.Logic.PlayerCode;
using ServerSProxy.Logic.ServersLogic;
using System.Text;

namespace ServerSProxy.Logic.Commands
{
    internal class Help : Command
    {
        public Help(Player player, GameWorld gameWorld) : base(player, gameWorld) { }

        public override async Task<string> Execute()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("           AVAILABLE COMMANDS");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine(" chat     – Enter chat mode");
            sb.AppendLine(" exit     – Leave the game");
            sb.AppendLine(" help     – Show this help");
            sb.AppendLine(" stats    – Show your stats");
            sb.AppendLine(" move     – Move to another room");
            // sb.AppendLine(" pickup   – Pick up an item");
            // sb.AppendLine(" fight    – FightPlayer an NPC");
            sb.AppendLine(" fight    – FightPlayer a player");
            // sb.AppendLine(" trade    – Trade with another player");
            sb.AppendLine("═══════════════════════════════════════");

            await WriteToConsole.TextToPlayer(_player, sb.ToString());
            return string.Empty;
        }

        public override bool Exit() => false;
    }
}