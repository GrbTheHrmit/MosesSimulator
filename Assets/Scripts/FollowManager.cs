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
            instance = CreateInstance<FollowManager>();
        }
        return instance;
    }

    private FollowManager()
    {}

    private List<FollowerScript> uncollectedFollowers = new List<FollowerScript>();
    private List<FollowerScript> collectedFollowers = new List<FollowerScript>();
    public bool HasCollectedFollower(FollowerScript follower) { return collectedFollowers.Contains(follower); }

    private Rigidbody followObject = null;
    public Rigidbody FollowObject { set { followObject = value; } }
    public Vector3 LeaderPosition { get { return followObject.position; } }

    private float followDist = 3f;
    private float followMass = 100f;
    private Vector3 lastCenterOfMass = Vector3.zero;
    private Vector3 lastAveVelocity = Vector3.zero;
    private float lastBlobRadius = 0;

    private float timeSinceLastCollected = 0;
    private float timeSinceLastSpawn = 0;

    // Update is called once per frame
    public void FixedUpdate()
    {
        if (followObject == null) return;

        float totalWeight = followMass;
        Vector3 nextCenterOfMass = (followObject.transform.position - followObject.transform.forward * (followDist + collectedFollowers.Count)) * followMass;
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


        timeSinceLastCollected += Time.fixedDeltaTime;
        timeSinceLastSpawn += Time.fixedDeltaTime;

        CheckSpawnFollowers();

    }

    public void AddFollower(FollowerScript follower)
    {
        uncollectedFollowers.Remove(follower);
        collectedFollowers.Add(follower);
        timeSinceLastCollected = 0;
    }

    // Returns the number of followers spawned
    private int SpawnCluster(Vector3 location)
    {
        int numToSpawn = Random.Range(1, GameManager.Instance.SpawnSettings.maxCluster + 1);

        if(uncollectedFollowers.Count + numToSpawn > GameManager.Instance.SpawnSettings.maxUncollected)
        {
            int toMove = uncollectedFollowers.Count + numToSpawn - GameManager.Instance.SpawnSettings.maxUncollected;
            float distCutoff = float.MaxValue;
            int numpicked = 0;
            FollowerScript[] furthest = new FollowerScript[toMove];
            foreach(FollowerScript follower in uncollectedFollowers)
            {
                if(numpicked < toMove)
                {
                    furthest[numpicked] = follower;
                    // Set this to shortest distance
                    distCutoff = Mathf.Min(distCutoff, Vector3.Distance(follower.gameObject.transform.position, LeaderPosition));
                    numpicked++;
                    continue;
                }
                
                // If this follower is further than the cutoff we should add it
                if(Vector3.Distance(follower.gameObject.transform.position, LeaderPosition) > distCutoff)
                {
                    float newClosest = float.MaxValue;
                    bool foundClosest = false;
                    for(int i = 0; i < numpicked; i++)
                    {
                        // Using foundClosest to avoid accidentally having 2 with the same distance both be replaced
                        if (!foundClosest && Vector3.Distance(furthest[i].gameObject.transform.position, LeaderPosition) == distCutoff)
                        {
                            foundClosest = true;
                            furthest[i] = follower;
                        }

                        newClosest = Mathf.Min(newClosest, Vector3.Distance(furthest[i].gameObject.transform.position, LeaderPosition));
                    }
                    // Set new cutoff
                    distCutoff = newClosest;
                }
            }

            // Move the selected followers
            for(int i = 0; i < numpicked; i++)
            {
                // Spawn in reverse circle from the instantiation one below
                Vector3 spawnPos = location + Quaternion.Euler(0, -(i + 1) * (360f / numToSpawn), 0) * (GameManager.Instance.SpawnSettings.clusterSize * Vector3.forward);
                furthest[i].gameObject.transform.position = spawnPos;
            }

            numToSpawn -= toMove;
        }

        for(int i = 0; i < numToSpawn; i++)
        {
            Vector3 spawnPos = location + Quaternion.Euler(0, i * (360f / numToSpawn), 0) * (GameManager.Instance.SpawnSettings.clusterSize * Vector3.forward);

            GameObject newFollower = Instantiate(GameManager.Instance.FollowerObject, spawnPos, Quaternion.FromToRotation(Vector3.forward, location - spawnPos));
            uncollectedFollowers.Add(newFollower.GetComponent<FollowerScript>());
        }

        return numToSpawn;
    }

    private void CheckSpawnFollowers()
    {
        // Time waited for the player to collect someone goes up with how many followers are in the world
        float waitTime = GameManager.Instance.SpawnSettings.spawnRate + uncollectedFollowers.Count;
        if (timeSinceLastCollected > waitTime && timeSinceLastSpawn > GameManager.Instance.SpawnSettings.spawnRate)
        {
            int tries = 0;
            float distance;
            float angle;
            Vector3 position;
            float distToOthers;
            // Pick a place to spawn
            do
            {
                distance = Random.Range(GameManager.Instance.SpawnSettings.minSpawnDist, GameManager.Instance.SpawnSettings.minSpawnDist);
                angle = Random.Range(GameManager.Instance.SpawnSettings.minAngleFromForward, GameManager.Instance.SpawnSettings.maxAngleFromForward);
                position = followObject.transform.position + Quaternion.Euler(0, angle, 0) * followObject.transform.forward * distance;
                tries++;

                distToOthers = Mathf.Min(distance, Vector3.Distance(position, lastCenterOfMass) - lastBlobRadius);
                foreach(FollowerScript uncollected in uncollectedFollowers)
                {
                    distToOthers = Mathf.Min(distToOthers, Vector3.Distance(uncollected.gameObject.transform.position, position));
                }

            }
            while (distToOthers < GameManager.Instance.SpawnSettings.minDistFromOthers && tries < GameManager.Instance.SpawnSettings.maxTries);

            if(distToOthers >= GameManager.Instance.SpawnSettings.minDistFromOthers)
            {
                SpawnCluster(position);
                timeSinceLastSpawn = 0;
            }

        }
    }
}
