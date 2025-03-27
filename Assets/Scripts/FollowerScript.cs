using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FollowerScript : MonoBehaviour
{
    
    private Rigidbody rb = null;
    public Vector3 GetVelocity { get {return Vector3.zero; } }

    private List<FollowerScript> inRange = new List<FollowerScript>();
    private bool isFollowing = false;

    [SerializeField]
    private float followerMaxSpeed = 5;
    [SerializeField]
    private float followerAccel = 3;
    [SerializeField]
    private float arrivalRadius = 2.0f;

    [SerializeField]
    private float minAvoidDist = 1.0f;
    [SerializeField]
    private float maxAvoidDist = 4.0f;
    [SerializeField]
    private float maxAvoidStrength = 5.0f;

    [SerializeField]
    private float wanderTargetDistance = 30.0f;
    [SerializeField]
    private float wanderMaxSpeed = 2.0f;
    [SerializeField]
    private float maxWanderSeconds = 8.0f;

    // Start is called before the first frame update
    void Start()
    {
        wanderTimer = maxWanderSeconds;
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Avoid other non followers
        if (!isFollowing)
        {
            rb.AddForce(Wander(), ForceMode.VelocityChange);
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, wanderMaxSpeed);
        }
        else
        {
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, followerMaxSpeed);
        }
        
    }

    public void UpdateMovement(Vector3 centerOfMass, Vector3 aveVelocity)
    {
        rb.AddForce(Flock(centerOfMass, aveVelocity), ForceMode.VelocityChange);
    }

    private void StartFollowing()
    {
        if (isFollowing) return;

        isFollowing = true;
        rb.excludeLayers = rb.excludeLayers | ~LayerMask.NameToLayer("Player");
        FollowManager.Instance().AddFollower(this);
        foreach (SphereCollider sph in GetComponents<SphereCollider>())
        {
            if (sph.isTrigger)
            {
                sph.radius = maxAvoidDist;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player" && !isFollowing)
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


    /// <summary>
    ///  Behavior Functions
    /// </summary>

    private float minVelocityDiff = 0.25f;
    private Vector3 MatchVelocity(Vector3 target)
    {
        if((target - rb.velocity).magnitude > minVelocityDiff)
        {
            return Vector3.ClampMagnitude(target - rb.velocity, followerAccel);
        }

        return Vector3.zero;
    }

    private Vector3 SeekTarget(Vector3 target)
    {
        if ((target - transform.position).magnitude > arrivalRadius)
        {
            return (target - transform.position).normalized * followerAccel;
        }

        return MatchVelocity(Vector3.zero);
    }

    private Vector3 AvoidOthers()
    {
        if (inRange.Count <= 0)
        {
            return Vector3.zero;
        }

        Vector3 forceSum = Vector3.zero;
        foreach (FollowerScript follower in inRange)
        {
            float dist = Mathf.Clamp((transform.position - follower.transform.position).magnitude, minAvoidDist, maxAvoidDist);
            forceSum -= SeekTarget(follower.transform.position) * (1.0f - ((dist - minAvoidDist) / (maxAvoidDist - minAvoidDist)));
        }
        forceSum /= inRange.Count;

        return forceSum;
    }

    Vector3 wanderTarget = Vector3.zero;
    private float wanderTimer = 0;

    private Vector3 Wander()
    {
        Vector3 forceSum = Vector3.zero;

        if (wanderTimer >= maxWanderSeconds || (gameObject.transform.position - wanderTarget).magnitude < arrivalRadius)
        {
            wanderTimer = 0;
            wanderTarget = gameObject.transform.position + (Quaternion.Euler(0, Random.Range(0, 361), 0) * (Random.Range(5, wanderTargetDistance) * Vector3.forward));
            Debug.Log(wanderTarget);
        }

        wanderTimer += Time.fixedDeltaTime;

        forceSum += SeekTarget(wanderTarget) * 1.0f;
        forceSum += AvoidOthers() * maxAvoidStrength;

        forceSum /= maxAvoidStrength + 1;

        return forceSum;

    }

    private Vector3 Flock(Vector3 CoM, Vector3 aveVelocity)
    {
        Vector3 forceSum = Vector3.zero;

        forceSum += SeekTarget(CoM) * 1.0f;
        forceSum += MatchVelocity(aveVelocity) * 1.0f;

        forceSum += AvoidOthers() * maxAvoidStrength;

        forceSum /= maxAvoidStrength + 2;

        return forceSum;
    }
}
