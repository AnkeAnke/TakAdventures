using System.Collections;
using System.Collections.Generic;
using TakLogic;
using UnityEngine;

public class Match : MonoBehaviour
{
    public PlayerReserve DarkPlayerReserve, LightPlayerReserve;
    public int BoardSize = 5;

    private BoardState GameState;
    // Start is called before the first frame update
    void Start()
    {
        SetupGame();
    }

    void SetupGame()
    {
        var numStones = BoardState.GetBoardSetup(BoardSize);
        if (numStones == null)
        {
            Debug.LogError($"Could not create board of size {BoardSize}");
            return;
        }

        GameState = new BoardState(BoardSize);

        // Setup player reserves.
        PlayerState darkPlayer = GameState.GetPlayerState(Player.First);
        PlayerState lightPlayer = GameState.GetPlayerState(Player.Second);
        DarkPlayerReserve.SetupGame(darkPlayer);
        LightPlayerReserve.SetupGame(lightPlayer);

    }

    // Update is called once per frame
    void Update()
    {
        //if (GameState == null && BoardState.GetBoardSetup(BoardSize) != null)
        //    SetupGame();
    }
}
