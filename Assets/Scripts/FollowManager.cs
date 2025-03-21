using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowManager : ScriptableObject
{

    private static FollowManager instance;
    public static FollowManager Instance()
    {
        if(instance == null)
        {
            instance = new FollowManager();
        }
        return instance;
    }

    private FollowManager()
    {}

    private List<FollowerScript> collectedFollowers = new List<FollowerScript>();
    public bool HasCollectedFollower(FollowerScript follower) { return collectedFollowers.Contains(follower); }

    private Rigidbody followObject = null;
    public Rigidbody FollowObject { set { followObject = value; } }

    private float followMass = 100f;
    private Vector3 lastCenterOfMass = Vector3.zero;
    private Vector3 lastAveVelocity = Vector3.zero;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (followObject == null) return;

        float totalWeight = followMass;
        Vector3 nextCenterOfMass = followObject.transform.position * followMass;
        Vector3 nextAveVelocity = followObject.velocity;

        foreach (FollowerScript follower in collectedFollowers)
        {
            follower.UpdateMovement(lastCenterOfMass, lastAveVelocity);
            totalWeight++;
            nextCenterOfMass += follower.gameObject.transform.position;
            nextAveVelocity += follower.GetVelocity;
        }

        lastCenterOfMass = nextCenterOfMass / totalWeight;
        lastAveVelocity = nextAveVelocity / totalWeight;
    }

    public void AddFollower(FollowerScript follower)
    {
        collectedFollowers.Add(follower);
    }
}
