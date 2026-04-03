using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.PlayerCode
{
    internal class Quest
    {
        private string _name;
        private string _description;
        private int _experienceReward;
        private int _coinsReward;


        public Quest(string name, string description, int experienceReward, int coinsReward)
        {
            Name = name;
            Description = description;
            ExperienceReward = experienceReward;
            CoinsReward = coinsReward;
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public int ExperienceReward
        {
            get { return _experienceReward; }
            set { _experienceReward = value; }
        }

        public int CoinsReward
        {
            get { return _coinsReward; }
            set { _coinsReward = value; }
        }
    }
}
