using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class StoneHandle : MonoBehaviour
{
    public Material DarkColor, LightColor;
    public TakLogic.StoneType StoneType;
    protected TakLogic.Player Owner;

    void Start()
    {

    }

    void Update()
    {

    }

    public void SetOwner(TakLogic.Player owner)
    {
        Owner = owner;
        GetComponent<MeshRenderer>().material = (owner == TakLogic.Player.First ? DarkColor : LightColor);
    }

    public void Flip()
    {
        switch (StoneType)
        {
            case TakLogic.StoneType.FlatStone:
                StoneType = TakLogic.StoneType.StandingStone;
                transform.rotation = Quaternion.Euler(90, 0, 0);
                break;

            case TakLogic.StoneType.StandingStone:
                StoneType = TakLogic.StoneType.FlatStone;
                transform.rotation = Quaternion.identity;
                break;
            default:
                break;
        }
    }
}
