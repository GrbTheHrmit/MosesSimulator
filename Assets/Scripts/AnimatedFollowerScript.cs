using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

//[RequireComponent(typeof(Rigidbody))]
public class AnimatedFollowerScript : MonoBehaviour
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

    private List<AnimatedFollowerScript> inRange = new List<AnimatedFollowerScript>();
    private bool isFollowing = false;
    private bool isClimbing = false;
    private bool isRiding = false;
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

    [SerializeField]
    private float climbSpeed = 1f;

    private Animator animator;

    Vector3 lastOffsetPosition;
    Quaternion localRotation;
    Vector3 localVehicleOffset;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        //wanderTimer = maxWanderSeconds;
        rb = GetComponent<Rigidbody>();

        /*renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.SetMaterials(wanderMaterial);
        }*/

        animator = GetComponent<Animator>();
        if(animator == null)
        {
            Debug.LogError("Could Not Find Follower Animator");
        }
    }

    void FixedUpdate()
    {
        // Avoid other non followers
        /*if (!isFollowing)
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
         }*/

        Vector3 lookDir = FollowManager.Instance().LeaderPosition - rb.position;
        lookDir.y = 0;
        rb.MoveRotation(Quaternion.FromToRotation(rb.transform.forward, lookDir.normalized) * rb.rotation);
        Debug.DrawRay(rb.position, lookDir);

        if(isClimbing)
        {

            Vector3 newPosition = FollowManager.Instance().FollowObject.position + FollowManager.Instance().FollowObject.transform.TransformDirection(localVehicleOffset);
            Vector3 diff = newPosition - lastOffsetPosition;
            rb.position += diff;
            rb.rotation = FollowManager.Instance().FollowObject.rotation * localRotation;
            lastOffsetPosition = newPosition;
        }
        else if(isRiding)
        {
            Vector3 newPosition = FollowManager.Instance().FollowObject.position + FollowManager.Instance().FollowObject.transform.TransformDirection(localVehicleOffset);
            rb.position = newPosition;
            rb.rotation = FollowManager.Instance().FollowObject.rotation * localRotation;
        }
    }

    public void UpdateMovement(Vector3 centerOfMass, Vector3 aveVelocity)
    {
        /*Vector3 flockForce = Flock(centerOfMass, aveVelocity);
        flockForce.y = 0;

        
        rb.AddForce(flockForce, ForceMode.VelocityChange);
        Vector3 xzVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.velocity = Vector3.ClampMagnitude(xzVelocity, followerMaxSpeed) + new Vector3(0, rb.velocity.y, 0);*/
    }

    public void StartClimbing()
    {
        

        isClimbing = true;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.excludeLayers = LayerMask.GetMask("Player");
        rb.constraints = RigidbodyConstraints.None;

        Vector3 offset = rb.position - FollowManager.Instance().FollowObject.position;
        localVehicleOffset = FollowManager.Instance().FollowObject.transform.InverseTransformDirection(offset);
        localRotation = Quaternion.Inverse(rb.transform.rotation) * rb.rotation;
        lastOffsetPosition = rb.position;

        animator.SetBool("Climbing", true);
    }

    public void StartRiding(Vector3 localPosition)
    {
        isRiding = true;
        isClimbing = false;
        animator.SetBool("Climbing", false);
        animator.SetBool("Riding", true);
        //transform.localPosition = localPosition;

        Vector3 offset = rb.position - FollowManager.Instance().FollowObject.position;
        localVehicleOffset = FollowManager.Instance().FollowObject.transform.InverseTransformDirection(offset);
    }

    public void SaveFollower()
    {
        isSaved = true;

    }

    private void StartFollowing()
    {
        if (isFollowing) return;

        animator.SetBool("Following", true);

        isFollowing = true;
        //rb.excludeLayers = LayerMask.GetMask("Player");
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

        if (other.gameObject.tag == "Roof" && isClimbing)
        {
            StartRiding(other.gameObject.transform.position);
        }

        if (other.gameObject.tag == "Ladder" && !isClimbing && isFollowing)
        {
            StartClimbing();
        }
        

        if (other.gameObject.tag == "Player" && !isFollowing)
        {
            
            StartFollowing();
        }

        if (other.gameObject.tag == "Follower")
        {
            AnimatedFollowerScript otherFollower = other.gameObject.GetComponent<AnimatedFollowerScript>();
            if (otherFollower != null)
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
            AnimatedFollowerScript otherFollower = other.gameObject.GetComponent<AnimatedFollowerScript>();
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
        foreach (AnimatedFollowerScript follower in inRange)
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
