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

        // Completion conditions
        private string _targetEnemyName;
        private int _requiredKillCount;
        private int _currentKillCount;

        public Quest() { RequiredKillCount = 1; }

        public Quest(string name, string description, int experienceReward, int coinsReward,
                     string targetEnemyName = "", int requiredKillCount = 1)
        {
            Name = name;
            Description = description;
            ExperienceReward = experienceReward;
            CoinsReward = coinsReward;
            TargetEnemyName = targetEnemyName;
            RequiredKillCount = requiredKillCount;
            CurrentKillCount = 0;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public int ExperienceReward
        {
            get => _experienceReward;
            set => _experienceReward = value;
        }

        public int CoinsReward
        {
            get => _coinsReward;
            set => _coinsReward = value;
        }

        public string TargetEnemyName
        {
            get => _targetEnemyName;
            set => _targetEnemyName = value;
        }

        public int RequiredKillCount
        {
            get => _requiredKillCount;
            set => _requiredKillCount = Math.Max(1, value);
        }

        public int CurrentKillCount
        {
            get => _currentKillCount;
            set => _currentKillCount = value;
        }

        public bool IsCompleted => CurrentKillCount >= RequiredKillCount;

        public void RecordKill()
        {
            if (!IsCompleted)
                CurrentKillCount++;
        }
    }
}