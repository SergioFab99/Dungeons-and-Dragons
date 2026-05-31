using System.Collections.Generic;
using DungeonRpg;
using NUnit.Framework;

namespace DungeonRpg.Tests
{
    public class TurnManagerTests
    {
        [Test]
        public void BuildTurnQueue_OnlyIncludesActorsThatCanAct()
        {
            FakeTurnActor readyActor = new FakeTurnActor(true);
            FakeTurnActor defeatedActor = new FakeTurnActor(false);

            Queue<ITurnActor> queue = TurnManager.BuildTurnQueue(new ITurnActor[] { readyActor, defeatedActor, null });

            Assert.AreEqual(1, queue.Count);
            Assert.AreSame(readyActor, queue.Dequeue());
        }

        private class FakeTurnActor : ITurnActor
        {
            public bool CanAct { get; }

            public FakeTurnActor(bool canAct)
            {
                CanAct = canAct;
            }

            public void BeginTurn(TurnManager turnManager)
            {
            }
        }
    }
}
