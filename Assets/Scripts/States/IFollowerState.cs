using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IFollowerState : ScriptableObject
{
    public abstract IFollowerState Update(float deltaTime);

    private FollowerScript controlledFollower = null;

    IFollowerState(FollowerScript i_controlledFollower)
    {
        controlledFollower = i_controlledFollower;
    }
}
