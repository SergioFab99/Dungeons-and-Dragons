using UnityEngine;

namespace DungeonRpg
{
    public class CombatResolver
    {
        public CombatResult Resolve(CharacterStats attackerStats, CharacterStats defenderStats)
        {
            return Resolve(attackerStats, defenderStats, Random.Range(1, 21));
        }

        public CombatResult Resolve(CharacterStats attackerStats, CharacterStats defenderStats, int diceRoll)
        {
            int safeRoll = Mathf.Clamp(diceRoll, 1, 20);
            int totalAttack = safeRoll + attackerStats.AttackBonus;
            bool hit = totalAttack >= defenderStats.Defense;
            int damage = hit ? attackerStats.Damage : 0;
            return new CombatResult(safeRoll, totalAttack, hit, damage);
        }
    }
}
