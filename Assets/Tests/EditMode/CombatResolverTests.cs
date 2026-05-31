using DungeonRpg;
using NUnit.Framework;

namespace DungeonRpg.Tests
{
    public class CombatResolverTests
    {
        [Test]
        public void Resolve_HitsWhenRollPlusBonusMeetsDefense()
        {
            CombatResolver resolver = new CombatResolver();
            CharacterStats attacker = new CharacterStats(20, 3, 12, 4);
            CharacterStats defender = new CharacterStats(8, 1, 10, 3);

            CombatResult result = resolver.Resolve(attacker, defender, 7);

            Assert.IsTrue(result.Hit);
            Assert.AreEqual(10, result.TotalAttack);
            Assert.AreEqual(4, result.Damage);
        }

        [Test]
        public void Resolve_MissesWhenRollPlusBonusIsBelowDefense()
        {
            CombatResolver resolver = new CombatResolver();
            CharacterStats attacker = new CharacterStats(20, 3, 12, 4);
            CharacterStats defender = new CharacterStats(8, 1, 15, 3);

            CombatResult result = resolver.Resolve(attacker, defender, 10);

            Assert.IsFalse(result.Hit);
            Assert.AreEqual(13, result.TotalAttack);
            Assert.AreEqual(0, result.Damage);
        }

        [Test]
        public void Resolve_ClampsDiceRollToD20Range()
        {
            CombatResolver resolver = new CombatResolver();
            CharacterStats attacker = new CharacterStats(20, 3, 12, 4);
            CharacterStats defender = new CharacterStats(8, 1, 10, 3);

            CombatResult lowRoll = resolver.Resolve(attacker, defender, -5);
            CombatResult highRoll = resolver.Resolve(attacker, defender, 99);

            Assert.AreEqual(1, lowRoll.DiceRoll);
            Assert.AreEqual(20, highRoll.DiceRoll);
        }
    }
}
