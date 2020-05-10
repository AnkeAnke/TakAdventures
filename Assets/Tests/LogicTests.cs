using System.Collections.Generic;
using NUnit.Framework;
using TakLogic;

namespace Tests
{
    public class LogicTests
    {
        public const int validBoardSizeMin = 3;
        public const int validBoardSizeMax = 8; // inclusive

        public static IEnumerable<TestCaseData> AllValidEmptyBoardStates
        {
            get
            {
                for (int size = validBoardSizeMin; size <= validBoardSizeMax; ++size)
                    yield return new TestCaseData(new TakLogic.BoardState(size)).SetName($"Empty board with {size}");
            }
        }

        [Test]
        public void BoardState_BoardSetup_IsNotNull_ForValidSizes([Range(validBoardSizeMin, validBoardSizeMax)] int size)
        {
            Assert.That(TakLogic.BoardState.GetBoardSetup(size), Is.Not.Null);
        }

        [Test]
        public void BoardState_BoardSetup_IsNull_ForInvalidSizes([Values(-1, 0, 1, 2, 9, 10, int.MaxValue, int.MinValue)] int size)
        {
            Assert.That(TakLogic.BoardState.GetBoardSetup(size), Is.Null);
        }

        [Test]
        public void BoardState_D0esNotThrow_OnCreation([Range(validBoardSizeMin, validBoardSizeMax)] int size)
        {
            Assert.DoesNotThrow(() => { new BoardState(size); });
        }

        [Test]
        public void BoardState_ThrowsOnInvaliSize_OnCreation([Values(-1, 0, 1, 2, 9, 10, int.MaxValue, int.MinValue)] int size)
        {
            Assert.Throws<System.ArgumentException>(() => { new BoardState(size); });
        }

        [Test, TestCaseSource(nameof(AllValidEmptyBoardStates))]
        public void BoardState_IsValid_OnCreation(TakLogic.BoardState board)
        {
            Assert.That(board.VerifyState(), Is.True);
        }
    }
}
