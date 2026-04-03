using ServerSProxy.Logic.PlayerCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.NPCs
{
    internal class NPC
    {

        public NPC() { }

        private string _name;
        private string _TextToTell;
        private bool _isQuestGiver;
        private List<Quest> _questsToGive;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string TextToTell
        {
            get { return _TextToTell; }
            set { _TextToTell = value; }
        }

        public bool IsQuestGiver
        {
            get { return _isQuestGiver; }
            set { _isQuestGiver = value; }
        }

        public List<Quest> QuestsToGive
        {
            get { return _questsToGive; }
            set { _questsToGive = value; }
        }


    }
}
