using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class FollowerScript : MonoBehaviour
{
    [SerializeField]
    private List<Material> wanderMaterial = new List<Material>();
    [SerializeField]
    private List<Material> followMaterial = new List<Material>();
    [SerializeField]
    private List<Material> savedMaterial = new List<Material>();

    private Rigidbody rb = null;
    private MeshRenderer renderer = null;
    public Vector3 GetVelocity { get {return Vector3.zero; } }

    private List<FollowerScript> inRange = new List<FollowerScript>();
    private bool isFollowing = false;
    private bool isSaved = false;

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
    private float leaderAvoidStrength = 10f;

    [SerializeField]
    private float wanderTargetDistance = 30.0f;
    [SerializeField]
    private float wanderMaxSpeed = 2.0f;
    [SerializeField]
    private float maxWanderSeconds = 8.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        wanderTimer = maxWanderSeconds;
        rb = GetComponent<Rigidbody>();

        renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.SetMaterials(wanderMaterial);
        }
    }

    void FixedUpdate()
    {
        // Avoid other non followers
        if (!isFollowing)
        {
            Vector3 wanderForce = Wander();
            wanderForce.y = 0;
            rb.AddForce(wanderForce, ForceMode.VelocityChange);
            Vector3 xzVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.velocity = Vector3.ClampMagnitude(xzVelocity, wanderMaxSpeed) + new Vector3(0, rb.velocity.y, 0);
        }
        else if (isSaved)
        {
            rb.velocity = Vector3.MoveTowards(rb.velocity, Vector3.zero, 1f * Time.fixedDeltaTime);
        }

        
        
    }

    public void UpdateMovement(Vector3 centerOfMass, Vector3 aveVelocity)
    {
        Vector3 flockForce = Flock(centerOfMass, aveVelocity);
        flockForce.y = 0;
        rb.AddForce(flockForce, ForceMode.VelocityChange);
        Vector3 xzVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.velocity = Vector3.ClampMagnitude(xzVelocity, followerMaxSpeed) + new Vector3(0, rb.velocity.y, 0);
    }

    public void SaveFollower()
    {
        isSaved = true;
        if (renderer != null)
        {
            renderer.SetMaterials(savedMaterial);
        }
    }

    private void StartFollowing()
    {
        if (isFollowing) return;

        if (renderer != null)
        {
            renderer.SetMaterials(followMaterial);
        }

        isFollowing = true;
        rb.excludeLayers = LayerMask.GetMask("Player");
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

    private Vector3 AvoidLeader()
    {
        Vector3 forceSum = Vector3.zero;
        if (isFollowing)
        {
            float dist = Mathf.Clamp((transform.position - FollowManager.Instance().LeaderPosition).magnitude, minAvoidDist, maxAvoidDist);
            forceSum -= SeekTarget(FollowManager.Instance().LeaderPosition) * (1.0f - ((dist - minAvoidDist) / (maxAvoidDist - minAvoidDist)));
        }

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
        forceSum += AvoidLeader() * leaderAvoidStrength;

        forceSum /= leaderAvoidStrength + maxAvoidStrength + 2;

        return forceSum;
    }
}
