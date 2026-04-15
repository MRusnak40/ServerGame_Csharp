using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.PlayerCode
{
    internal class PlayerClassTemplate
    {
        public string ClassId { get; set; }       
        public string DisplayName { get; set; }    
        public string Description { get; set; }    

        
        public int BaseHealth { get; set; }
        public int BaseShield { get; set; }
        public int BaseStamina { get; set; }
        public int BaseStrength { get; set; }
        public int BaseAttackSpeed { get; set; }
    }
}
