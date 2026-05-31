using System;

namespace DungeonRpg
{
    [Serializable]
    public class CharacterStats
    {
        public int MaxHealth;
        public int AttackBonus;
        public int Defense;
        public int Damage;

        public CharacterStats(int maxHealth, int attackBonus, int defense, int damage)
        {
            MaxHealth = maxHealth;
            AttackBonus = attackBonus;
            Defense = defense;
            Damage = damage;
        }
    }
}
