using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TakLogic;
using UnityEngine;

public class Match : MonoBehaviour
{
    public PlayerReserve DarkPlayerReserve, LightPlayerReserve;
    public int BoardSize = 5;
    public BoardHandle Board;

    private BoardState GameState;
    private PlayerReserve[] PlayerReserves;
    private Queue<TakMove> QueuedMoves;
    // Start is called before the first frame update
    void Start()
    {
        // TEST!
        QueuedMoves = new Queue<TakMove>();
        BoardState test = new BoardState(5);

        Stone lightFlat;
        lightFlat.Owner = Player.Second;
        lightFlat.Type = StoneType.FlatStone;

        Stone darkFlat;
        darkFlat.Owner = Player.First;
        darkFlat.Type = StoneType.FlatStone;

        Stone lightStanding;
        lightStanding.Owner = Player.Second;
        lightStanding.Type = StoneType.StandingStone;

        Stone darkCapstone;
        darkCapstone.Owner = Player.First;
        darkCapstone.Type = StoneType.Capstone;

        List<int> singleMove = new List<int> { 1 };

        QueuedMoves.Enqueue(new PlaceStone(Player.First, lightFlat, new Vector2Int(1, 1)));
        QueuedMoves.Enqueue(new PlaceStone(Player.Second, darkFlat, new Vector2Int(2, 2)));
        QueuedMoves.Enqueue(new MoveStack(Player.First, new Vector2Int(2, 2), Direction.Down, singleMove));
        QueuedMoves.Enqueue(new MoveStack(Player.Second, new Vector2Int(1, 1), Direction.Right, singleMove));
        QueuedMoves.Enqueue(new PlaceStone(Player.First, darkCapstone, new Vector2Int(0, 0)));
        QueuedMoves.Enqueue(new PlaceStone(Player.Second, lightStanding, new Vector2Int(2, 0)));

        foreach (TakMove move in QueuedMoves)
        {
            Debug.Log(move.ApplyMove(test));
        }

        SetupGameFromBoardState(test);
    }

    public void SetupGame()
    {
        PlayerReserves = new PlayerReserve[] { DarkPlayerReserve, LightPlayerReserve };

        var numStones = BoardState.GetBoardSetup(BoardSize);
        if (numStones == null)
        {
            Debug.LogError($"Could not create board of size {BoardSize}");
            return;
        }

        GameState = new BoardState(BoardSize);

        // Setup player reserves.
        for (int player = 0; player < 2; ++player)
        {
            PlayerState playerState = GameState.GetPlayerState((Player)player);
            PlayerReserves[player].SetupGame(playerState);
        }

    }

    public void SetupGameFromBoardState(BoardState state)
    {
        PlayerReserves = new PlayerReserve[] { DarkPlayerReserve, LightPlayerReserve };

        GameState = state;
        BoardSize = state.BoardSize;

        // Setup player reserves.
        for (int player = 0; player < 2; ++player)
        {
            PlayerState playerState = GameState.GetPlayerState((Player)player);
            PlayerReserves[player].RandomSeed = 42;
            PlayerReserves[player].SetupGame(playerState);
        }

        Board.Clear();
        Board.SetupBoard();

        // Add stones to board.

        Random.InitState(42);
        for (int y = 0; y < BoardSize; ++y)
            for (int x = 0; x < BoardSize; ++x)
            {
                StoneStack stones = GameState[x, y].GetReversed();
                while (stones.Count > 0)
                {
                    Stone stone = stones.Pop();
                    //StoneHandle newStone;
                    PlayerReserve owner = PlayerReserves[(int)stone.Owner];
                    bool isCapstone = (stone.Type == StoneType.Capstone);
                    StoneHandle newStone = Instantiate(isCapstone ? owner.Capstone : owner.Stone);
                    newStone.SetOwner(stone.Owner);

                    if (stone.Type == StoneType.StandingStone)
                        newStone.Flip();

                    Board[x, y].Push(newStone);
                }
            }
    }

    // Update is called once per frame
    void Update()
    {
        //if (GameState == null && BoardState.GetBoardSetup(BoardSize) != null)
        //    SetupGame();
    }
}
