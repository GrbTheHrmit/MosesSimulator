using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public GameObject TerrainPrefab;

    [SerializeField]
    private int TerrainTileDims = 3;
    private Vector3 currentOrigin = Vector3.zero;
    private float baseHeight = -300;

    [SerializeField]
    private int pointsPerTile = 50;
    private float[,] heightArray = null;

    private List<ProceduralTerrainScript> terrainList = new List<ProceduralTerrainScript>();
    private float TerrainInterval = 1000f;
    private float PointInterval;

    private int voronoiPointsPerTile = 50;
    private List<Vector2> voronoiPoints = new List<Vector2>();


    // Start is called before the first frame update
    void Start()
    {
        heightArray = new float[pointsPerTile * TerrainTileDims, pointsPerTile * TerrainTileDims];
        PointInterval = TerrainInterval / pointsPerTile;

        if(TerrainPrefab != null)
        {
            for(int i = 0; i < TerrainTileDims * TerrainTileDims; i++)
            {
                float x = (i % TerrainTileDims) - ((TerrainTileDims - 1) * 0.5f);
                float y = ((int)(i / TerrainTileDims)) - ((TerrainTileDims - 1) * 0.5f);
                Vector3 position = new Vector3(x * TerrainInterval, 0, y * TerrainInterval);
                GameObject newTerrain = Instantiate(TerrainPrefab, position, Quaternion.identity);
                terrainList.Add(newTerrain.GetComponent<ProceduralTerrainScript>());
                terrainList[i].InitTerrain();
            }
            currentOrigin = new Vector3(-((TerrainTileDims - 1) * 0.5f), baseHeight, -((TerrainTileDims - 1) * 0.5f));

            SetTerrainNeighbors();
            GenerateNewHeightMap();
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GenerateSeedPoints()
    {
        
    }

    private float CustomRandom(float x)
    {
        return (Mathf.Sin(x * 53885.7f) * 31841.93f) % 1f;
    }

    private float GetMappedValue(int tileNum, int pointNum)
    {
        int tileCol = tileNum % TerrainTileDims;
        int tileRow = tileNum / TerrainTileDims;
        int pointCol = pointNum % pointsPerTile;
        int pointRow = pointNum / pointsPerTile;

        Vector3 position = currentOrigin + new Vector3(tileCol * TerrainInterval + pointCol * PointInterval, baseHeight, tileRow * TerrainInterval + pointRow * PointInterval);

        float x = Mathf.Sin(CustomRandom(position.x) * 0.5f + 0.5f);
        float y = Mathf.Cos(CustomRandom(position.z) * 0.5f + 0.5f);

        return x * y;
    }

    private void GenerateNewHeightMap()
    {
        for (int tile = 0; tile < TerrainTileDims * TerrainTileDims; tile++)
        {
            int tileCol = tile % TerrainTileDims;
            int tileRow = tile / TerrainTileDims;

            for(int point = 0; point < pointsPerTile * pointsPerTile; point++)
            {
                int pointCol = point % pointsPerTile;
                int pointRow = point / pointsPerTile;

                float height = GetMappedValue(tile, point);
                heightArray[tileRow * pointsPerTile + pointRow, tileCol * pointsPerTile + pointCol] = height;

            }
        }

    }

    private void SetTerrainNeighbors()
    {
        for (int i = 0; i < TerrainTileDims * TerrainTileDims; i++)
        {
            int col = i % TerrainTileDims;
            int row = i / TerrainTileDims;

            Terrain left = null;
            Terrain right = null;
            Terrain top = null;
            Terrain bottom = null;

            if (col != 0)
            {
                left = terrainList[i - 1].MyTerrain;
            }
            if (col != TerrainTileDims - 1)
            {
                right = terrainList[i + 1].MyTerrain;
            }

            if (row != 0)
            {
                bottom = terrainList[i - TerrainTileDims].MyTerrain;
            }
            if (row != TerrainTileDims - 1)
            {
                top = terrainList[i + TerrainTileDims].MyTerrain;
            }

            terrainList[i].MyTerrain.SetNeighbors(left, top, right, bottom);
        }
    }
}
