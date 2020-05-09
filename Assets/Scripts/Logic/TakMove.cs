using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TakLogic
{
    /// <summary>
    /// Description of a possible move (place stones, move stones, special).
    /// </summary>
    public abstract class TakMove
    {
        /// <summary>
        /// Player executing the move.
        /// </summary>
        public Player Actor;

        /// <summary>
        /// Create generic tak move of an actor.
        /// </summary>
        /// <param name="actor">Player executing the move.</param>
        public TakMove(Player actor)
        {
            Actor = actor;
        }

        /// <summary>
        /// Applies the move to the given board.
        /// Will check its own validity first.
        /// </summary>
        /// <param name="board">Input state, will be updated.</param>
        /// <returns></returns>
        public abstract bool ApplyMove(BoardState board);

        /// <summary>
        /// Assuming a valid board state, check whether the move is allowed.
        /// Does leave board untouched.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public abstract bool IsValidMove(BoardState board);
    }

    /// <summary>
    /// Placing a stone of any kind on a free space.
    /// </summary>
    public class PlaceStone : TakMove
    {
        /// <summary>
        /// Stone to be placed.
        /// </summary>
        public Stone Stone;

        /// <summary>
        /// Position for stone placement.
        /// </summary>
        public Vector2Int Position;

        /// <summary>
        /// Create plecement move.
        /// </summary>
        /// <param name="player">Player placing the stone.</param>
        /// <param name="stone">Stone to be placed.</param>
        /// <param name="pos">Position to place on.</param>
        public PlaceStone(Player player, Stone stone, Vector2Int pos) : base(player)
        {
            Stone = stone;
            Position = pos;
        }

        /// <summary>
        /// Create placement move.
        /// </summary>
        /// <param name="player">Acting player.</param>
        /// <param name="type">Type of stone to be placed.</param>
        /// <param name="pos">Position to place stone at.</param>
        public PlaceStone(Player player, StoneType type, Vector2Int pos)
            : this(player, new Stone { Owner = player, Type = type }, pos) { }

        override public bool ApplyMove(BoardState board)
        {
            if (!IsValidMove(board))
                return false;

            board[Position].Push(Stone);
            if (Stone.Type == StoneType.Capstone)
                board.Players[(int)Stone.Owner].NumCapstones--;
            else
                board.Players[(int)Stone.Owner].NumStones--;

            board.HistoryMoveStack.Push(this);
            return true;
            
        }

        override public bool IsValidMove(BoardState board)
        {
            bool emptyField = (board[Position].Count == 0);
            bool actorIsOwner = (board.HistoryMoveStack.Count % 2 == (int)Actor);

            // Only on the first move, the opposing players stone may be placed.
            if (board.HistoryMoveStack.Count < 2)
            {
                bool flatStone = (Stone.Type != StoneType.FlatStone);
                return !actorIsOwner && flatStone && emptyField;
            }

            // Make sure the owner has enough stones to place.
            PlayerState ownerState = board.Players[(int)Stone.Owner];
            bool enoughStones = Stone.Type == StoneType.Capstone
                                   ? (ownerState.NumCapstones > 0)
                                   : (ownerState.NumStones > 0);
            return actorIsOwner && emptyField && enoughStones;
        }
    }


    /// <summary>
    /// Moving a stack of stones.
    /// Picking up to BoardSize many stones of a stack controlled by the actor,
    /// moving in one direction,
    /// dropping at least one stone from the bottom at each field passed.
    /// Stones can only be dropped on stacks with a flat stone on top.
    /// Exception: Iff the stack is one capstone, a standing stone can be flattened.
    /// </summary>
    public class MoveStack : TakMove
    {
        /// <summary>
        /// Board position the stones are taken from.
        /// </summary>
        public Vector2Int StartPosition;

        /// <summary>
        /// Number of stones moved from the top.
        /// </summary>
        public int NumStonesTaken { get { return NumStonesDroppedPerField.Sum(); } }

        /// <summary>
        /// Direction the stack is moved in.
        /// </summary>
        public Direction MoveDirection;

        /// <summary>
        /// Number of stones dropped per field moved over.
        /// </summary>
        public List<int> NumStonesDroppedPerField;

        /// <summary>
        /// Create a stack moving action.
        /// </summary>
        /// <param name="player">Acting player.</param>
        /// <param name="startPosition">Position the stack is picked from.</param>
        /// <param name="direction">Direction the stack moves in.</param>
        /// <param name="numStonesDropped">Number of stones dropped along the way.</param>
        public MoveStack(Player player, Vector2Int startPosition,
                         Direction direction, List<int> numStonesDropped)
            : base(player)
        {
            StartPosition = startPosition;
            MoveDirection = direction;
            NumStonesDroppedPerField = numStonesDropped;
        }

        override public bool ApplyMove(BoardState board)
        {
            if (!IsValidMove(board)) return false;

            // Stack moved, reversed order (lowest one on top).
            StoneStack reverseStack = new StoneStack();
            StoneStack startingStack = board[StartPosition];
            for (int stone = 0; stone < NumStonesTaken; ++stone)
                reverseStack.Push(startingStack.Pop());

            for (int step = 0; step < NumStonesDroppedPerField.Count; ++step)
            {
                Vector2Int pos = StartPosition + (step + 1) * MoveDirection.ToVector();

                // A wall might be flattened in the end.
                // Since we checked that the move is valid, we need no further checks.
                if (step == NumStonesDroppedPerField.Count-1 && board[pos].Peek().Type == StoneType.StandingStone)
                {
                    Stone flattenedWall = board[pos].Pop();
                    flattenedWall.Type = StoneType.FlatStone;
                    board[pos].Push(flattenedWall);
                }
                for (int stone = 0; stone < NumStonesDroppedPerField[step]; ++stone)
                    board[pos].Push(reverseStack.Pop());
            }
            return true;
        }

        override public bool IsValidMove(BoardState board)
        {
            // First two moves need to be placements.
            bool normalTurn = (board.HistoryMoveStack.Count >= 2);
            bool validStartingPosition = (board.IsInside(StartPosition));
            if (!normalTurn || !validStartingPosition) return false;

            bool actorOwnsStack = (board[StartPosition].Peek().Owner == Actor);
            bool stoneLimitObeyed = (NumStonesTaken <= board.BoardSize);
            bool enoughStones = (NumStonesTaken <= board[StartPosition].Count);
            bool alwaysDroppedStone = (NumStonesDroppedPerField.Min() > 0);
            if (!actorOwnsStack || !stoneLimitObeyed || !enoughStones || !alwaysDroppedStone)
                return false;

            // Moving a single capstone in the end can flatten a wall.
            bool canFlattenWall = (NumStonesDroppedPerField[NumStonesDroppedPerField.Count - 1] == 1
                                    &&  board[StartPosition].Peek().Type == StoneType.Capstone);
            for (int step = 0; step < NumStonesDroppedPerField.Count; ++step)
            {
                // Moving outside the field?
                Vector2Int pos = StartPosition + (step + 1) * MoveDirection.ToVector();
                if (!board.IsInside(pos)) return false;

                // Can place on top?
                StoneType top = board[pos].Peek().Type;
                bool placeStone = (top == StoneType.FlatStone);
                bool flattenStone = (top == StoneType.StandingStone
                                        && canFlattenWall
                                        && step == NumStonesDroppedPerField.Count - 1);
                if (!placeStone && !flattenStone) return false;
            }
            return true;
        }
    }
}
