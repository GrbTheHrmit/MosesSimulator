using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowerScript : MonoBehaviour
{
    public Vector3 GetVelocity { get {return Vector3.zero; } }

    private List<FollowerScript> inRange = new List<FollowerScript>();
    private bool isFollowing = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        // Avoid other non followers
        if (!isFollowing && inRange.Count > 0)
        {
            // TODO Avoid others
        }
    }

    public void UpdateMovement(Vector3 centerOfMass, Vector3 aveVelocity)
    {
        // TODO Flock
    }

    private void StartFollowing()
    {
        if (isFollowing) return;

        isFollowing = true;
        FollowManager.Instance().AddFollower(this);
        foreach (SphereCollider sph in GetComponents<SphereCollider>())
        {
            if (sph.isTrigger)
            {
                sph.radius = 2;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            StartFollowing();
        }

        if(other.gameObject.tag == "Follower")
        {
            FollowerScript otherFollower = other.gameObject.GetComponent<FollowerScript>();
            if(otherFollower != null)
            {
                if (!isFollowing && FollowManager.Instance().HasCollectedFollower(otherFollower))
                {
                    StartFollowing();
                }
                else
                {
                    inRange.Add(otherFollower);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Follower")
        {
            FollowerScript otherFollower = other.gameObject.GetComponent<FollowerScript>();
            if (otherFollower != null && inRange.Contains(otherFollower))
            {
                inRange.Remove(otherFollower);
            }
        }
    }
}
