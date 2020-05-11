using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardHandle : MonoBehaviour
{
    public MeshRenderer DarkSquare, LightSquare;
    public Match Parent;
    protected StoneStackHandle[,] Fields;
    void Start()
    {
        SetupBoard();
    }

    public void SetupBoard()
    {
        if (!Parent)
            Parent = transform.parent.GetComponent<Match>();
        SetupBoard(Parent.BoardSize);
    }

    public void SetupBoard(int boardSize)
    {
        Collider[] temporaryInstances = { Instantiate(DarkSquare.GetComponent<Collider>()), Instantiate(DarkSquare.GetComponent<Collider>()) };
        Bounds[] squareBounds = { temporaryInstances[0].bounds, temporaryInstances[1].bounds };
        Vector3 extentDiff = squareBounds[0].size - squareBounds[1].size;
        if (Mathf.Abs(extentDiff.x) > Mathf.Epsilon || Mathf.Abs(extentDiff.z) > Mathf.Epsilon)
            throw new System.Exception("Given board squares colliders differ too much in size.");
        Vector2 squareSize = new Vector3(Mathf.Min(squareBounds[0].size.x, squareBounds[1].size.x),
                                         Mathf.Min(squareBounds[0].size.z, squareBounds[1].size.z));
        float[] squareHeightOffset = { -squareBounds[0].min.y, -squareBounds[1].min.y };

        Debug.Log($"Square size: {squareSize}, from min({squareBounds[0].size.x}, {squareBounds[1].size.x})");
        Debug.Log($"Dark collider: {squareBounds[0]}");


        Fields = new StoneStackHandle[boardSize, boardSize];
        for (int y = 0; y < boardSize; ++y)
            for (int x = 0; x < boardSize; ++x)
            {
                int squareId = (x + y) % 2;
                MeshRenderer newSquare = Instantiate(squareId > 0 ? DarkSquare : LightSquare);
                newSquare.transform.parent = this.transform;
                newSquare.transform.localPosition = new Vector3(squareSize.x * (-0.5f * boardSize + x + 0.5f),
                                                                squareHeightOffset[squareId],
                                                                squareSize.y * (-0.5f * boardSize + y + 0.5f));
                Fields[x, y] = newSquare.GetComponentInChildren<StoneStackHandle>();
            }
    }

    public void Clear()
    {
        for (int child = 0; child < transform.childCount; ++child)
        {
            Destroy(transform.GetChild(child).gameObject);
        }
        Fields = null;
    }

    void Update()
    {

    }

    public StoneStackHandle this[Vector2Int idx]
    {
        get { return Fields[idx.x, idx.y]; }
    }

    public StoneStackHandle this[int x, int y]
    {
        get { return Fields[x, y]; }
    }

}
