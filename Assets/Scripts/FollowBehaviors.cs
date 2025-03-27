using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBehaviors : ScriptableObject
{
    private static FollowBehaviors instance;
    public static FollowBehaviors Instance()
    {
        if (instance == null)
        {
            instance = new FollowBehaviors();
        }
        return instance;
    }

    private FollowBehaviors()
    { }

   
}
