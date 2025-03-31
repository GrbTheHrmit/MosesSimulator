using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public struct FollowerSpawnSettings
    {
        public float minSpawnDist;
        public float maxSpawnDist;

        public float minAngleFromForward;
        public float maxAngleFromForward;

        public int maxUncollected;
        public float minDistFromOthers;

        public int maxCluster;
        public float clusterSize;

        public float spawnRate;

        public int maxTries;
    }

    [SerializeField]
    private GameObject FollowerPrefab;
    public GameObject FollowerObject { get {  return FollowerPrefab; } }

    [SerializeField]
    private FollowerSpawnSettings spawnSettings = new FollowerSpawnSettings();
    public FollowerSpawnSettings SpawnSettings { get { return spawnSettings; } }

    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;   
    }

    void FixedUpdate()
    {
        FollowManager.Instance().FixedUpdate();
    }
}
