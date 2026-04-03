using ServerSProxy.Logic.PlayerCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.ServersLogic
{
    internal class WriteToConsole
    {

        private List<Player> _activePlayers; // reference na tvůj existující list

        public WriteToConsole(List<Player> activePlayers)
        {
            _activePlayers = activePlayers;
        }

        // Zpráva všem hráčům
        public async Task BroadcastAll(string message)
        {
            foreach (Player player in _activePlayers)
            {
                await player.Writer.WriteLineAsync(message);
            }
        }

        // Zpráva jednomu hráči
        public async Task TextToPlayer(Player player, string message)
        {
            await player.Writer.WriteLineAsync(message);
        }
    }
}
