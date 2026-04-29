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
    internal class FightPlayer : Command
    {
        public FightPlayer(Player player, GameWorld gameWorld) : base(player, gameWorld)
        {
        }

        public override async Task<string> Execute()
        {
            // check if already in combat
            if (_player.IsInCombat)
            {
                await WriteToConsole.TextToPlayer(_player, "\nerror: you are already in combat!");
                return "error";
            }

            // find current room
            Room currentRoom = GetPlayerRoom(_player);
            if (currentRoom == null)
            {
                await WriteToConsole.TextToPlayer(_player, "\nerror: room not found!");
                return "error";
            }

            // get list of available opponents (players in room, excluding self)
            List<Player> availableOpponents = currentRoom.PlayersInRoom
                ?.Where(p => p.Name != _player.Name && !p.IsInCombat)
                .ToList() ?? new List<Player>();

            if (availableOpponents.Count == 0)
            {
                await WriteToConsole.TextToPlayer(_player, "\nerror: no opponents available in this room!");
                return "error";
            }

            // display opponents list
            await DisplayOpponents(availableOpponents);

            // get opponent selection
            await WriteToConsole.TextToPlayerOneLine(_player, "\n > select opponent (1-" + availableOpponents.Count + "): ");
            string? choice = await WriteToConsole.TakeInput(_player);

            if (!int.TryParse(choice, out int opponentChoice) || opponentChoice < 1 || opponentChoice > availableOpponents.Count)
            {
                await WriteToConsole.TextToPlayer(_player, "\nerror: invalid choice!");
                return "error";
            }

            // get selected opponent
            Player opponent = availableOpponents[opponentChoice - 1];

            // start fight
            await RunFight(_player, opponent);

            return "fight";
        }

        public override bool Exit() => false;

        private async Task DisplayOpponents(List<Player> opponents)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n");
            sb.AppendLine("  ╔═══════════════════════════════════════════════════════╗");
            sb.AppendLine("  ║           SELECT AN OPPONENT TO FIGHT                 ║");
            sb.AppendLine("  ╠═══════════════════════════════════════════════════════╣");

            for (int i = 0; i < opponents.Count; i++)
            {
                Player opp = opponents[i];
                string healthBar = GetHealthBar(opp.Health, opp.MaxHealth);
                sb.AppendLine($"  ║ {i + 1}. {opp.Name.PadRight(20)} │ lvl {opp.Level.ToString().PadRight(2)} │ {healthBar}║");
            }

            sb.AppendLine("  ╚═══════════════════════════════════════════════════════╝");

            await WriteToConsole.TextToPlayer(_player, sb.ToString());
        }

        private async Task RunFight(Player player1, Player player2)
        {
            // set both in combat
            player1.IsInCombat = true;
            player2.IsInCombat = true;

            // restore stamina to max
            player1.Stamina = player1.MaxStamina;
            player2.Stamina = player2.MaxStamina;

            // announce fight start
            await DisplayFightStart(player1, player2);

            // fight loop
            while (player1.Health > 0 && player2.Health > 0)
            {
                // display current state
                await DisplayFightState(player1, player2);

                // get moves from both players
                await WriteToConsole.TextToPlayerOneLine(player1, " > your move (fast/heavy/careful): ");
                string? move1 = await WriteToConsole.TakeInput(player1);

                await WriteToConsole.TextToPlayerOneLine(player2, " > your move (fast/heavy/careful): ");
                string? move2 = await WriteToConsole.TakeInput(player2);

                // process round
                await ProcessRound(player1, player2, move1, move2);
            }

            // determine winner and loser
            Player winner = player1.Health > 0 ? player1 : player2;
            Player loser = player1.Health > 0 ? player2 : player1;

            // display victory
            await DisplayFightEnd(winner, loser);

            // apply fight results
            ApplyFightResults(winner, loser);

            // end combat
            player1.IsInCombat = false;
            player2.IsInCombat = false;

            // loser goes to lobby
            loser.IsAlive = false;
        }

        private async Task DisplayFightStart(Player player1, Player player2)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n");
            sb.AppendLine("  ╔═══════════════════════════════════════════════════════╗");
            sb.AppendLine("  ║                      ⚔  BATTLE START  ⚔              ║");
            sb.AppendLine("  ╠═══════════════════════════════════════════════════════╣");
            sb.AppendLine($"  ║  * {player1.Name.PadRight(20)} VS {player2.Name.PadRight(19)} *  ║");
            sb.AppendLine("  ║                                                       ║");
            sb.AppendLine("  ║  attack types:                                        ║");
            sb.AppendLine("  ║    • fast    - 40% dmg, 10% stamina, can spam         ║");
            sb.AppendLine("  ║    • careful - 70% dmg, 20% stamina, balanced         ║");
            sb.AppendLine("  ║    • heavy   - 100% dmg, 30% stamina, slow cooldown   ║");
            sb.AppendLine("  ║                                                       ║");
            sb.AppendLine("  ╚═══════════════════════════════════════════════════════╝");
            sb.AppendLine();

            await WriteToConsole.TextToPlayer(player1, sb.ToString());
            await WriteToConsole.TextToPlayer(player2, sb.ToString());
        }

        private async Task DisplayFightState(Player player1, Player player2)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n  ╔═══════════════════════════════════════════════════════╗");
            sb.AppendLine("  ║                      ⚔  BATTLE ⚔                     ║");
            sb.AppendLine("  ╠═══════════════════════════════════════════════════════╣");

            // player 1 stats
            string hp1Bar = GetHealthBar(player1.Health, player1.MaxHealth);
            string stam1Bar = GetStaminaBar(player1.Stamina, player1.MaxStamina);
            sb.AppendLine($"  ║ {player1.Name.PadRight(28)}║");
            sb.AppendLine($"  ║ hp:  {hp1Bar}║");
            sb.AppendLine($"  ║ stm: {stam1Bar}║");

            sb.AppendLine("  ╠═══════════════════════════════════════════════════════╣");

            // player 2 stats
            string hp2Bar = GetHealthBar(player2.Health, player2.MaxHealth);
            string stam2Bar = GetStaminaBar(player2.Stamina, player2.MaxStamina);
            sb.AppendLine($"  ║ {player2.Name.PadRight(28)}║");
            sb.AppendLine($"  ║ hp:  {hp2Bar}║");
            sb.AppendLine($"  ║ stm: {stam2Bar}║");

            sb.AppendLine("  ╚═══════════════════════════════════════════════════════╝");

            await WriteToConsole.TextToPlayer(player1, sb.ToString());
            await WriteToConsole.TextToPlayer(player2, sb.ToString());
        }

        private async Task DisplayFightEnd(Player winner, Player loser)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n");
            sb.AppendLine("  ╔═══════════════════════════════════════════════════════╗");
            sb.AppendLine("  ║                     * BATTLE OVER *                   ║");
            sb.AppendLine("  ╠═══════════════════════════════════════════════════════╣");
            sb.AppendLine($"  ║  WINNER: {winner.Name.PadRight(45)}║");
            sb.AppendLine($"  ║  LOSER:  {loser.Name.PadRight(45)}║");
            sb.AppendLine("  ╚═══════════════════════════════════════════════════════╝");
            sb.AppendLine();

            await WriteToConsole.TextToPlayer(winner, sb.ToString());
            await WriteToConsole.TextToPlayer(loser, sb.ToString());
        }

        private async Task ProcessRound(Player p1, Player p2, string? move1, string? move2)
        {
            // parse attack types
            AttackType type1 = ParseAttackType(move1);
            AttackType type2 = ParseAttackType(move2);

            // calculate attack timing based on attack speed and type
            float timing1 = GetAttackTiming(p1, type1);
            float timing2 = GetAttackTiming(p2, type2);

            // determine attack order
            if (timing1 < timing2)
            {
                // player1 attacks first
                await AttackPlayer(p1, p2, type1);
                if (p2.Health > 0)
                    await AttackPlayer(p2, p1, type2);
            }
            else
            {
                // player2 attacks first
                await AttackPlayer(p2, p1, type2);
                if (p1.Health > 0)
                    await AttackPlayer(p1, p2, type1);
            }

            // regenerate stamina for both
            RegenerateStamina(p1);
            RegenerateStamina(p2);
        }

        private enum AttackType
        {
            Fast,
            Heavy,
            Careful
        }

        private AttackType ParseAttackType(string? input)
        {
            // parse attack type from input
            return input?.ToLower() switch
            {
                "fast" => AttackType.Fast,
                "heavy" => AttackType.Heavy,
                "careful" => AttackType.Careful,
                _ => AttackType.Careful
            };
        }

        private float GetAttackTiming(Player player, AttackType type)
        {
            // calculate timing in milliseconds
            // lower = faster attack
            // heavy = 3x longer cooldown (harder to spam)
            // fast = 1x
            // careful = 1x

            float baseTiming = 1000f / player.AttackSpeed;

            return type switch
            {
                AttackType.Heavy => baseTiming * 3,
                AttackType.Fast => baseTiming,
                AttackType.Careful => baseTiming,
                _ => baseTiming
            };
        }

        private async Task AttackPlayer(Player attacker, Player defender, AttackType type)
        {
            // check if enough stamina
            int staminaCost = GetStaminaCost(type);
            if (attacker.Stamina < staminaCost)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"\n [!] {attacker.Name} tried to use {type} but not enough stamina!");
                await WriteToConsole.TextToPlayer(attacker, sb.ToString());
                await WriteToConsole.TextToPlayer(defender, sb.ToString());
                return;
            }

            // deduct stamina
            attacker.Stamina -= staminaCost;

            // calculate damage
            int damage = GetDamage(attacker, type);

            // apply damage
            defender.Health -= damage;
            if (defender.Health < 0)
                defender.Health = 0;

            // notify both players with fancy output
            StringBuilder msg = new StringBuilder();
            msg.AppendLine();
            msg.AppendLine($"  ║ > {attacker.Name} uses [{type.ToString().ToUpper()}] attack!");
            msg.AppendLine($"  ║   {defender.Name} takes ~ {damage} damage!");

            await WriteToConsole.TextToPlayer(attacker, msg.ToString());
            await WriteToConsole.TextToPlayer(defender, msg.ToString());
        }

        private int GetStaminaCost(AttackType type)
        {
            // stamina cost as percentage of max stamina
            return type switch
            {
                AttackType.Heavy => 30,   // 30% cost - hard to spam
                AttackType.Fast => 10,    // 10% cost - easy to spam but low damage
                AttackType.Careful => 20, // 20% cost - balanced
                _ => 20
            };
        }

        private int GetDamage(Player player, AttackType type)
        {
            // damage as percentage of strength
            return type switch
            {
                AttackType.Heavy => (int)(player.Strength * 1.0f), // 100% damage - best but hard to use
                AttackType.Fast => (int)(player.Strength * 0.4f),  // 40% damage - worst damage
                AttackType.Careful => (int)(player.Strength * 0.7f), // 70% damage - balanced
                _ => (int)(player.Strength * 0.7f)
            };
        }

        private void RegenerateStamina(Player player)
        {
            // slow stamina regeneration per round (5%)
            int regen = (int)(player.MaxStamina * 0.05f);
            player.Stamina += regen;
            if (player.Stamina > player.MaxStamina)
                player.Stamina = player.MaxStamina;
        }

        private void ApplyFightResults(Player winner, Player loser)
        {
            // winner gets 50% of loser coins
            int coinReward = (int)(loser.Coins * 0.5f);
            winner.Coins += coinReward;

            // loser loses coins
            loser.Coins = Math.Max(0, loser.Coins - coinReward);

            // winner gets all loser xp
            int xpReward = loser.Experience;
            winner.Experience += xpReward;

            // loser loses all xp
            loser.Experience = 0;

            // loser loses 1 level if not already level 1
            if (loser.Level > 1)
                loser.Level--;

            // notify with fancy output
            Task.Run(async () =>
            {
                StringBuilder winMsg = new StringBuilder();
                winMsg.AppendLine("\n");
                winMsg.AppendLine("  ╔═══════════════════════════════════════════════════════╗");
                winMsg.AppendLine("  ║                  * YOU WIN! *                       ║");
                winMsg.AppendLine("  ╠═══════════════════════════════════════════════════════╣");
                winMsg.AppendLine($"  ║  +{coinReward.ToString().PadRight(3)} coins                                       ║");
                winMsg.AppendLine($"  ║  +{xpReward.ToString().PadRight(3)} experience                                   ║");
                winMsg.AppendLine("  ╚═══════════════════════════════════════════════════════╝");
                winMsg.AppendLine();
                await WriteToConsole.TextToPlayer(winner, winMsg.ToString());

                StringBuilder loseMsg = new StringBuilder();
                loseMsg.AppendLine("\n");
                loseMsg.AppendLine("  ╔═══════════════════════════════════════════════════════╗");
                loseMsg.AppendLine("  ║                  ~ YOU LOST! ~                      ║");
                loseMsg.AppendLine("  ╠═══════════════════════════════════════════════════════╣");
                loseMsg.AppendLine($"  ║  -{coinReward.ToString().PadRight(3)} coins                                       ║");
                loseMsg.AppendLine($"  ║  -{xpReward.ToString().PadRight(3)} experience                                   ║");
                if (loser.Level > 1)
                    loseMsg.AppendLine($"  ║  -1  level                                             ║");
                loseMsg.AppendLine("  ╚═══════════════════════════════════════════════════════╝");
                loseMsg.AppendLine();
                await WriteToConsole.TextToPlayer(loser, loseMsg.ToString());
            });
        }

        private Room GetPlayerRoom(Player player)
        {
            // search all maps to find which room contains player
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

        private string GetHealthBar(int current, int max)
        {
            // create visual health bar
            int barLength = 25;
            float percentage = (float)current / max;
            int filledLength = (int)(barLength * percentage);

            StringBuilder bar = new StringBuilder();
            bar.Append("[");
            for (int i = 0; i < barLength; i++)
            {
                if (i < filledLength)
                    bar.Append("█");
                else
                    bar.Append("░");
            }
            bar.Append("]");

            return bar.ToString();
        }

        private string GetStaminaBar(int current, int max)
        {
            // create visual stamina bar
            int barLength = 25;
            float percentage = (float)current / max;
            int filledLength = (int)(barLength * percentage);

            StringBuilder bar = new StringBuilder();
            bar.Append("[");
            for (int i = 0; i < barLength; i++)
            {
                if (i < filledLength)
                    bar.Append("▓");
                else
                    bar.Append("░");
            }
            bar.Append("]");

            return bar.ToString();
        }
    }
}