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
        /*
        private List<Player> _activePlayers; 

        public WriteToConsole(List<Player> activePlayers)
        {
            _activePlayers = activePlayers;
        }
        */

        // Zpráva všem hráčům
        public static  async Task BroadcastAll(string message, List<Player> _activePlayers)
        {
            foreach (Player player in _activePlayers)
            {
                await player.Writer.WriteLineAsync(message);
            }
        }

        // Zpráva jednomu hráči
        public static async Task TextToPlayer(Player player, string message)
        {
            await player.Writer.WriteLineAsync(message);
        }

        //vzatí vstupu od hráče
        public static async Task<string> TakeInput(Player player) { 
           

            string input = await player.Reader.ReadLineAsync();
            return input ?? "";

           
        }
    }
}
