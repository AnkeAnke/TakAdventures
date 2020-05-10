using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TakLogic;
using UnityEngine;

public class StoneStackHandle : MonoBehaviour
{
    public List<StoneHandle> Stones;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Push(StoneHandle stone)
    {
        float stackHeight = 0;
        foreach (StoneHandle child in Stones)
        {
            Bounds extent = child.GetComponent<Collider>().bounds;
            stackHeight = Mathf.Max(stackHeight, extent.extents.y + child.transform.position.y);
        }

        Stones.Add(stone);
        stone.transform.parent = this.transform;

        float stoneBottom = stone.GetComponent<Collider>().bounds.extents.y;
        stone.transform.localPosition = new Vector3(0, stackHeight + stoneBottom, 0);
    }

    public StoneHandle Peek()
    {
        return Stones.Last();
    }

    public StoneHandle Pop()
    {
        StoneHandle stone = Stones.Last();
        Stones.RemoveAt(Stones.Count - 1);
        return stone;
    }
}
