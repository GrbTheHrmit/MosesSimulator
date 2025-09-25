using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShrubManager : MonoBehaviour
{
    [SerializeField]
    private int numShrubs = 512;

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

    private static ShrubManager _instance = null;


    private struct ShrubLocation
    {
        public GameObject _obj;
        public int _tile;
        public Vector3 _pos; // Values stored from [0,1]
    }

    private ShrubLocation[] _shrubList;

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

        _shrubList = new ShrubLocation[numShrubs];
    }

    public void PlaceShrubs(int tileDims)
    {
        GenerateShrubLocations(tileDims);
        PlaceShrubs();
    }

    private void GenerateShrubLocations(int tileDims)
    {
        int[] tileCount = new int[tileDims * tileDims];

        for(int i = 0; i < numShrubs; i++)
        {
            _shrubList[i] = new ShrubLocation();

            // Pick a tile
            int tries = 0;
            while(tries < maxTries)
            {
                tries++;
                _shrubList[i]._tile = Random.Range(0, tileDims * tileDims);
                if (tileCount[_shrubList[i]._tile] <= maxPerTile)
                {
                    break;
                }
            }

            // Get a location on this tile
            tries = 0;
            while(tries < maxTries)
            {
                tries++;

                Vector3 attempt = new Vector3(Random.Range(0f, 1f), 0, Random.Range(0f, 1f));
                attempt.y = TerrainManager.GetWorldHeightAtPoint(_shrubList[i]._tile, attempt.x, attempt.z);
                _shrubList[i]._pos = attempt;
                if(attempt.y >= minShrubHeight && attempt.y <= maxShrubHeight)
                {
                    break;
                }
            }

        }
    }

    private void PlaceShrubs()
    {
        for (int i = 0; i < numShrubs; i++)
        {
            Vector3 location = TerrainManager.GetWorldLocationAtPoint(_shrubList[i]._tile, _shrubList[i]._pos.x, _shrubList[i]._pos.z);
            // raycast or something idk

            _shrubList[i]._obj = Instantiate(possibleShrubs[Random.Range(0, possibleShrubs.Count)], location, Quaternion.identity);
        }
    }
}
