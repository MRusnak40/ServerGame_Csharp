using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.PlayerCode
{
    internal enum  ClassEnumPlayer
    {
        WARRIOR,BERSERKER,MAGE,ARMORER



        // WARRIOR - vyvážená třída, dobrý základ ve všem
        // ↑ zdraví (střední-vyšší), ↑ štít (střední), ↑ stamina (střední)
        // ↑ síla (střední), ↑ rychlost útoku (střední)

        // BERSERKER - skleněné dělo, útočí rychle a silně ale umírá rychle
        // ↑↑ síla (nejvyšší), ↑↑ rychlost útoku (nejvyšší), ↑↑ stamina (vysoká)
        // ↓↓ štít (téměř žádný), ↓ zdraví (nízké)

        // MAGE - největší damage ale extrémně křehký
        // ↑↑↑ síla (absolutně nejvyšší)
        // ↓↓↓ zdraví (nejnižší), ↓↓ štít (nízký), ↓ stamina (střední)
        // ↓ rychlost útoku (pomalý, ale každý úder bolí)

        // ARMORER - živá zeď, téměř nezničitelný ale slabý útočník
        // ↑↑↑ zdraví (nejvyšší), ↑↑↑ štít (absolutně nejvyšší)
        // ↓↓ síla (nejnižší), ↓↓↓ rychlost útoku (nejpomalejší)
        // ↓ stamina (nízká)
    }
}
