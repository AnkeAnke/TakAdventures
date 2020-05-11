using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = System.Diagnostics.Debug;

namespace TakLogic
{
    public class BoardState
    {
        static readonly Dictionary<int, (int NumStones, int NumCapstones)> NumStonesAndCapstonesPerBoardSize = new Dictionary<int, (int Stones, int Capstones)>
        {
            { 3, (10, 0) },
            { 4, (15, 0) },
            { 5, (21, 1) },
            { 6, (30, 1) },
            { 7, (40, 1) }, // Not fully specified in cheapass instructions.
            { 8, (50, 2) }
        };
        internal Stack<TakMove> HistoryMoveStack = new Stack<TakMove>();
        internal PlayerState[] Players;
        internal StoneStack[,] Fields;

        /// <summary>
        /// Board size, board has BoardSize x BoardSize many fields.
        /// </summary>
        public int BoardSize { get { return Fields.GetLength(0); } }


        /// <summary>
        /// Create new, empty board for a given field size.
        /// </summary>
        /// <param name="boardSize">Number of fields in each dimension, within [3,8].</param>
        public BoardState(int boardSize)
        {
            var numStonesTest = GetBoardSetup(boardSize);
            if (numStonesTest == null)
                throw new ArgumentException($"Invalid board size {boardSize}: No game configuration is known for this board size.", "boardSize");
            var numStones = numStonesTest.Value;

            Players = new PlayerState[2];
            Players[0] = new PlayerState { NumStones = numStones.NumStones, NumCapstones = numStones.NumCapstones };
            Players[1] = new PlayerState { NumStones = numStones.NumStones, NumCapstones = numStones.NumCapstones };
            Fields = new StoneStack[boardSize, boardSize];

            for (int x = 0; x < boardSize; ++x)
                for (int y = 0; y < boardSize; ++y)
                    Fields[x, y] = new StoneStack();
        }

        /// <summary>
        /// Provides a deep copy of a board state.
        /// </summary>
        /// <param name="other">Board to copy from.</param>
        public BoardState(BoardState other)
        {
            Fields = new StoneStack[other.BoardSize, other.BoardSize];
            for (int y = 0; y < BoardSize; y++)
                for (int x = 0; x < BoardSize; x++)
                {
                    Fields[x, y] = new StoneStack(Fields[x, y]);
                }
            HistoryMoveStack = new Stack<TakMove>(other.HistoryMoveStack.Reverse());
            Players[0] = Players[0];
            Players[1] = Players[1];
        }

        /// <summary>
        /// Get a copy of the stone stack at vector position.
        /// </summary>
        /// <param name="idx">2D int index in [0, BoardSize).</param>
        /// <returns>Copy of stone stack.</returns>
        public StoneStack this[Vector2Int idx]
        {
            get { return new StoneStack(Fields[idx.x, idx.y]); }
            private set { Fields[idx.x, idx.y] = value; }
        }

        /// <summary>
        /// Get a copy of the stone stack at position (x,y).
        /// </summary>
        /// <param name="x">x index in [0, BoardSize).</param>
        /// <param name="y">y index in [0, BoardSize).</param>
        /// <returns>Copy of stone stack.</returns>
        public StoneStack this[int x, int y]
        {
            get { return new StoneStack(Fields[x, y]); }
            private set { Fields[x, y] = value; }
        }

        /// <summary>
        /// Query the number of stones needed for a game of the given size.
        /// </summary>
        /// <param name="boardSize">Board size to query for.</param>
        /// <returns></returns>
        public static (int NumStones, int NumCapstones)? GetBoardSetup(int boardSize)
        {
            if (!NumStonesAndCapstonesPerBoardSize.ContainsKey(boardSize))
                return null;
            return NumStonesAndCapstonesPerBoardSize[boardSize];
        }

        /// <summary>
        /// Get the current state of the respective player.
        /// </summary>
        /// <param name="player">Player ID (must be first or second).</param>
        /// <returns></returns>
        public PlayerState GetPlayerState(Player player)
        {
            return Players[(int)player];
        }

        /// <summary>
        /// Get the player to make the next turn.
        /// </summary>
        /// <returns>Currently acting player.</returns>
        public Player GetActivePlayer()
        {
            return (Player)(HistoryMoveStack.Count % 2);
        }

        /// <summary>
        /// Check whether the game would be won with the given move.
        /// The normal condition is that a player has connected two opposing sides of the board through a 'road'.
        /// Also, iff a player has placed their last stone or run out of stones, the game ends and is decided based on number of flat-topped stacks owned.
        /// If a player creates a road for both players, they win.
        /// </summary>
        /// <param name="move">Move that might lead to the end of game.</param>
        /// <returns>Winning player identifier or null, and their score.</returns>
        public (Player Winner, int Score) CheckWin(TakMove move)
        {
            BoardState moveResult = new BoardState(this);
            move.ApplyMove(moveResult);

            bool[,] alreadyVisited = new bool[BoardSize, BoardSize];
            bool[] playerHasWinningRoad = new bool[2];
            Queue<Vector2Int> searchQueue = new Queue<Vector2Int>(BoardSize * 2);

            for (int y = 0; y < BoardSize; y++)
                for (int x = 0; x < BoardSize; x++)
                {
                    // Possibly a part of a road between two sides?
                    if (Math.Min(x, y) != 0 && Math.Max(x, y) != BoardSize - 1) continue; // Not a border piece.
                    if (alreadyVisited[x, y]) continue;
                    Player currentOwner = Fields[x, y].CountsTowardsWin();
                    if (currentOwner == Player.None) continue;
                    if (playerHasWinningRoad[(byte)currentOwner]) continue; // Still evaluate rest for double road.

                    bool[] isSideConnected = new bool[4];

                    // Breadth-first search.
                    searchQueue.Enqueue(new Vector2Int(x, y));
                    while (searchQueue.Count > 0)
                    {
                        Vector2Int current = searchQueue.Dequeue();
                        alreadyVisited[current.x, current.y] = true;
                        
                        foreach (Direction dir in (Direction[])Enum.GetValues(typeof(Direction)))
                        {
                            Vector2Int neighbor = current + dir.ToVector();
                            if (IsInside(neighbor) && !alreadyVisited[neighbor.x, neighbor.y] && this[neighbor].CountsTowardsWin() == currentOwner)
                                {
                                    searchQueue.Enqueue(neighbor);
                                }
                        }

                        // Actually evaluate.
                        if (x == 0) isSideConnected[(byte)Direction.Left] = true;
                        if (y == 0) isSideConnected[(byte)Direction.Down] = true;
                        if (x == BoardSize - 1) isSideConnected[(byte)Direction.Right] = true;
                        if (y == BoardSize - 1) isSideConnected[(byte)Direction.Up] = true;
                    }

                    // Connecting two sides?
                    if ((isSideConnected[(byte)Direction.Left] && isSideConnected[(byte)Direction.Right]) ||
                        (isSideConnected[(byte)Direction.Down] && isSideConnected[(byte)Direction.Up]))
                    {
                        playerHasWinningRoad[(byte)currentOwner] = true;
                        // We can only announce the winner prematurely if the last actor won.
                        if (playerHasWinningRoad[(byte)move.Actor])
                        {
                            goto ReturnWinner;
                        }
                    }
                }

        ReturnWinner:
            if (playerHasWinningRoad[(byte)move.Actor])
            {
                return (move.Actor, GetPlayerWinningScore(move.Actor));
            }

            Player passivePlayer = move.Actor.GetOther();
            if (playerHasWinningRoad[(byte)passivePlayer])
            {
                return (passivePlayer, GetPlayerWinningScore(passivePlayer));
            }

            // If neither player has a road, check if there is a flat win.
            Player flatWinner = CheckFlatWin();
            if (flatWinner == Player.First || flatWinner == Player.Second)
            {
                return (flatWinner, GetPlayerWinningScore(flatWinner));
            }
            return (flatWinner, -1);
        }

        /// <summary>
        /// Verifies the current state.
        /// * Standing stones and capstones only at top of stacks?
        /// * Correct total number of stones?
        /// </summary>
        /// <returns>Is the board state sane?</returns>
        public bool VerifyState()
        {
            PlayerState[] stonesOnBoard = new PlayerState[2];
            StoneStack stack;
            Stone top;

            for (int y = 0; y < BoardSize; ++y)
                for (int x = 0; x < BoardSize; ++x)
                {
                    stack = this[x, y];
                    int initialCount = stack.Count;
                    while (stack.Count > 0)
                    {
                        top = stack.Pop();
                        if (top.Type == StoneType.Capstone)
                            stonesOnBoard[(int)top.Owner].NumCapstones++;
                        else
                            stonesOnBoard[(int)top.Owner].NumStones++;

                        // Invalid: Stone within the stack (i.e., not at top) is a wall or capstone.
                        if (stack.Count != initialCount - 1 && top.Type != StoneType.FlatStone)
                            return false;
                    }
                }

            for (int player = 0; player < 2; ++player)
            {
                // Invalid: Number of stones on the board and in players reserve does not add up to inital number.
                if (Players[player].NumStones + stonesOnBoard[player].NumStones != NumStonesAndCapstonesPerBoardSize[BoardSize].NumStones ||
                    Players[player].NumCapstones + stonesOnBoard[player].NumCapstones != NumStonesAndCapstonesPerBoardSize[BoardSize].NumCapstones)
                    return false;
            }
            return true;
            
        }

        /// <summary>
        /// Checks whether a position is inside the board range.
        /// Required for operator [].
        /// </summary>
        /// <param name="pos">Field position to check.</param>
        /// <returns>Is the position inside?</returns>
        public bool IsInside(Vector2Int pos)
        {
            return pos.x >= 0 && pos.y >= 0 && pos.x < BoardSize && pos.y < BoardSize;
        }

        /// <summary>
        /// Get score a player would earn on win.
        /// </summary>
        /// <param name="player">Player one or two.</param>
        /// <returns>Points total.</returns>
        private int GetPlayerWinningScore(Player player)
        {
            if (player != Player.First && player != Player.Second)
                throw new ArgumentException($"Not a valid player: Player enum {player} is not assigned a winning score, only First (0) and Second (1) are valid inputs.", "player");
            return BoardSize * BoardSize + Players[(byte)player].NumStones + Players[(byte)player].NumCapstones;
        }

        /// <summary>
        /// Check if the game is finished by a 'flat win'.
        /// This happens if either all fields are filled, or a player ran out of pieces.
        /// </summary>
        /// <returns>Winning player, if any.</returns>
        private Player CheckFlatWin()
        {
            bool noPieceWin = false;
            if (Players[0].NumPiecesTotal == 0 || Players[1].NumPiecesTotal == 0)
                noPieceWin = true;

            if (!noPieceWin)
            {
                for (int y = 0; y < BoardSize; ++y)
                    for (int x = 0; x < BoardSize; ++x)
                    {
                        if (Fields[x, y].Count == 0)
                            return Player.None;
                    }
            }

            // Count flat topped stacks to get winner.
            int[] numStacks = new int[2];
            for (int y = 0; y < BoardSize; ++y)
                for (int x = 0; x < BoardSize; ++x)
                {
                    if (Fields[x, y].Count > 0)
                    {
                        Stone top = Fields[x, y].Peek();
                        if (top.Type == StoneType.FlatStone)
                            numStacks[(byte)top.Owner]++;
                    }
                }
            if (numStacks[0] == numStacks[1]) return Player.Both;
            return numStacks[0] > numStacks[1] ? Player.First : Player.Second;
        }
    }

    /// <summary>
    /// A tower of stones.
    /// </summary>
    public class StoneStack : Stack<Stone>
    {
        public StoneStack() : base() { }
        public StoneStack(StoneStack other) : base(other.Reverse()) { }

        public StoneStack(IEnumerable<Stone> collection) : base(collection) { }

        /// <summary>
        /// Returns the player in control of the stack.
        /// </summary>
        /// <returns>Controlling player identifier or null iff empty.</returns>
        public Player GetControllingPlayer()
        {
            if (Count == 0)
                return Player.None;
            return Peek().Owner;
        }

        /// <summary>
        /// Returns the player this stack counts for, if any.
        /// Needed to evaluate whether a player has won.
        /// </summary>
        /// <returns>Controlling player identifier or null iff empty.</returns>
        public Player CountsTowardsWin()
        {
            if (Count == 0)
                return Player.None;

            Stone top = Peek();
            if (top.Type == StoneType.StandingStone)
                return Player.None;

            return top.Owner;
        }

        public StoneStack GetReversed()
        {
            StoneStack result = new StoneStack((IEnumerable<Stone>)this);
            return result;
        }
    }

    /// <summary>
    /// Stone status of a player.
    /// </summary>
    public struct PlayerState
    {
        /// <summary>
        /// Number of unplayed stones.
        /// </summary>
        public int NumStones;

        /// <summary>
        /// Number of unplayed capstones.
        /// </summary>
        public int NumCapstones;

        /// <summary>
        /// Total number of pieces, i.e., regular stones and capstones.
        /// </summary>
        public int NumPiecesTotal { get { return NumStones + NumCapstones; } }
    }

    /// <summary>
    /// Directions to move in, relative to player 1.
    /// </summary>
    public enum Direction
    {
        Left,
        Up,
        Right,
        Down
    }

    /// <summary>
    /// Type / orientation of stone.
    /// </summary>
    public enum StoneType
    {
        FlatStone,
        StandingStone,
        Capstone
    }

    /// <summary>
    /// Player identifier, can be used as index.
    /// </summary>
    public enum Player : byte
    {
        First = 0,
        Second = 1,
        Both = 2,
        None = 42
    }

    static class EnumExtension
    {
        /// <summary>
        /// Converts a Direction enum into the corresponding int vector.
        /// Add to position to get neighboring field.
        /// </summary>
        /// <param name="dir">Neighbor/movement direction.</param>
        /// <returns>Index difference vector.</returns>
        public static Vector2Int ToVector(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Left:
                    return Vector2Int.left;
                case Direction.Right:
                    return Vector2Int.right;
                case Direction.Up:
                    return Vector2Int.up;
                case Direction.Down:
                    return Vector2Int.down;
                default:
                    throw new ArgumentException("Invalid direction input", "dir");
            }
        }

        /// <summary>
        /// returns the other player.
        /// </summary>
        /// <param name="player">Original player.</param>
        /// <returns>The valid player not being the input player.</returns>
        public static Player GetOther(this Player player)
        {
            if (player == Player.None) return Player.None;
            return (Player)(1 - (byte)player);
        }
    }

    /// <summary>
    /// Description of a stone as type and player color.
    /// </summary>
    public struct Stone
    {
        public Player Owner;
        public StoneType Type;
    }
}

