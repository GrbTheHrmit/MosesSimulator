using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

public class ShrubManager : MonoBehaviour
{
    [SerializeField]
    private int numShrubs = 512;

    [SerializeField]
    private int maxActiveShrubs = 512;

    [SerializeField]
    private int maxPerTile = 8;
    [SerializeField]
    [Range(0f, 1f)]
    private float minShrubHeight = 0f;
    [SerializeField]
    [Range(0f, 1f)]
    private float maxShrubHeight = 1f;
    [SerializeField]
    private int maxTries = 3;

    [SerializeField]
    private List<GameObject> possibleShrubs = new List<GameObject>();

    [SerializeField]
    private Vector3 hideLocation = new Vector3(0, -1000, 0);

    private static ShrubManager _instance = null;


    private struct ShrubLocation
    {
        public int tile;
        public Vector3 pos; // Values stored from [0,1]
        public GameObject currentObj;
    }

    private Dictionary<int, List<ShrubLocation>> _shrubLocations = new Dictionary<int, List<ShrubLocation>>();
    private List<GameObject> activeShrubs = new List<GameObject>();
    private GameObject[] inactiveShrubs;
    private int inactiveCount = 0;

    public static ShrubManager Instance
    {
        get
        {
            return _instance;
        }
    }


    // Dont create more of these
    void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
        }
        _instance = this;

        inactiveShrubs = new GameObject[maxActiveShrubs];

        for (int i = 0; i < maxActiveShrubs; i++)
        {
            inactiveShrubs[i] = Instantiate(possibleShrubs[Random.Range(0, possibleShrubs.Count)], hideLocation, Quaternion.identity);
            inactiveCount++;
        }
    }

    public void PlaceShrubs(int tileDims)
    {
        for(int i = 0; i < tileDims * tileDims; i++)
        {
            _shrubLocations.Add(i, new List<ShrubLocation>());
        }

        GenerateShrubLocations(tileDims);
        PlaceShrubs();
    }

    private void GenerateShrubLocations(int tileDims)
    {
        int[] tileCount = new int[tileDims * tileDims];

        for(int i = 0; i < numShrubs; i++)
        {
            ShrubLocation shrub = new ShrubLocation();

            // Pick a tile
            int tile = 0;
            int tries = 0;
            while(tries < maxTries)
            {
                tries++;
                tile = Random.Range(0, tileDims * tileDims);
                if (tileCount[tile] <= maxPerTile)
                {
                    shrub.tile = tile;
                    break;
                }
            }

            // Get a location on this tile
            tries = 0;
            while(tries < maxTries)
            {
                tries++;

                Vector3 attempt = new Vector3(Random.Range(0f, 1f), 0, Random.Range(0f, 1f));
                attempt.y = TerrainManager.GetWorldHeightAtPoint(tile, attempt.x, attempt.z);
                shrub.pos = attempt;
                if(attempt.y >= minShrubHeight && attempt.y <= maxShrubHeight)
                {
                    break;
                }
            }

            _shrubLocations[tile].Add(shrub);

        }
    }

    private void PlaceShrubs()
    {
        foreach(List<ShrubLocation> shrubList in _shrubLocations.Values)
        {
            for(int i = 0; i < shrubList.Count; i++)
            {
                ShrubLocation shrub = shrubList[i];
                Vector3 location = TerrainManager.GetWorldLocationAtPoint(shrub.tile, shrub.pos.x, shrub.pos.z);
                // raycast or something idk

                shrub.currentObj = GetAvailableShrub();
                if(shrub.currentObj != null)
                {
                    Debug.Log("Tile: " + shrub.tile + " Pos: " + shrub.pos);
                    shrub.currentObj.transform.position = location;
                }
                
            }
        }
    }

    private GameObject GetAvailableShrub()
    {
        GameObject shrub = null;
        if (inactiveCount > 0)
        {
            shrub = inactiveShrubs[inactiveCount - 1];
            inactiveShrubs[inactiveCount - 1] = null;
            inactiveCount--;
        }
        return shrub;
    }

    private void ReturnShrub(GameObject shrubObj)
    {
        if(activeShrubs.Remove(shrubObj) && inactiveCount < maxActiveShrubs)
        {
            inactiveShrubs[inactiveCount] = shrubObj;
            inactiveCount++;
        }
    }
}
