namespace DungeonRpg
{
    public class CombatResult
    {
        public int DiceRoll { get; }
        public int TotalAttack { get; }
        public bool Hit { get; }
        public int Damage { get; }

        public CombatResult(int diceRoll, int totalAttack, bool hit, int damage)
        {
            DiceRoll = diceRoll;
            TotalAttack = totalAttack;
            Hit = hit;
            Damage = damage;
        }
    }
}
