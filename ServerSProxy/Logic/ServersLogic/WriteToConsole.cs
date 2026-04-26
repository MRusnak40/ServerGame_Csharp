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

        // vsem hracum zprava
        public static async Task BroadcastAll(string message, List<Player> _activePlayers)
        {
            
            var players = _activePlayers.ToArray();
            foreach (Player player in players)
            {
                await player.Writer.WriteLineAsync(message);
            }
        }

        // dany hrac zprava
        public static async Task TextToPlayer(Player player, string message)
        {
            await player.Writer.WriteLineAsync(message);
        }

        public static async Task TextToPlayerOneLine(Player player, string message)
        {
            await player.Writer.WriteAsync(message);
        }

        //vrati od hrace
        public static async Task<string> TakeInput(Player player) {

            // await player.Writer.WriteAsync("Command: ");
            /*
            await player.Writer.WriteAsync("\n ❯ ");
            await player.Writer.FlushAsync(); 
            */

            string input = await player.Reader.ReadLineAsync();
            return input ?? "";

           
        }
    }
}
