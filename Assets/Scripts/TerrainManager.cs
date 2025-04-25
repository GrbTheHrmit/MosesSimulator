using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public GameObject TerrainPrefab;
    public GameObject FinishPrefab;

    [SerializeField]
    private int TerrainTileDims = 3;
    private Vector3 currentOrigin = Vector3.zero;
    private float baseHeight = -5;

    [SerializeField]
    private int pointsPerTile = 50;
    private float[,] heightArray = null;

    private List<ProceduralTerrainScript> terrainList = new List<ProceduralTerrainScript>();
    private float TerrainInterval = 1000f;
    private float TerrainDefaultScale = 10f; // need for vert locations
    private float PointInterval;

    private int voronoiPointsPerTile = 50;
    private List<Vector2> voronoiPoints = new List<Vector2>();

    private static TerrainManager instance = null;
    public static float GetWorldHeightAtLocation(Vector3 location)
    {
        if(instance != null)
        {
            return instance.GetHeightAtLocation(location) + instance.baseHeight;
        }

        return 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        heightArray = new float[pointsPerTile * TerrainTileDims, pointsPerTile * TerrainTileDims];
        PointInterval = TerrainInterval / pointsPerTile;

        if(TerrainPrefab != null)
        {
            for(int i = 0; i < TerrainTileDims * TerrainTileDims; i++)
            {
                float x = (i % TerrainTileDims) - ((TerrainTileDims - 1) * 0.5f);
                float y = ((int)(i / TerrainTileDims)) - ((TerrainTileDims - 1) * 0.5f);
                Vector3 position = new Vector3(x * TerrainInterval, baseHeight, y * TerrainInterval);
                GameObject newTerrain = Instantiate(TerrainPrefab, position, Quaternion.identity);
                terrainList.Add(newTerrain.GetComponent<ProceduralTerrainScript>());
                terrainList[i].InitTerrain();
            }

            float centerOffset = 0.5f * TerrainInterval;
            currentOrigin = new Vector3(-((TerrainTileDims - 1) * 0.5f) * TerrainInterval, baseHeight, -((TerrainTileDims - 1) * 0.5f) * TerrainInterval);
            currentOrigin -= new Vector3(centerOffset, 0, centerOffset);

            Debug.Log(currentOrigin);

            //SetTerrainNeighbors();
            GenerateNewHeightMap();

            PlaceFinish();
            SpawnPlayer();
        }
        
    }

    void SpawnPlayer()
    {
        Vector3 location = Vector3.zero;
        location.y = GetHeightAtLocation(location) + 2;

        GameManager.Instance.SpawnPlayer(location);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Todo something about moving terrain around
    }

    private float CustomRandom(float x)
    {
        return Mathf.Sin(((Mathf.Sin(x * 53885.7f) * 31841.93f) % 1f) * 19) * 0.5f + 0.5f;
    }

    private float GetMappedValue(int tileNum, int pointNum)
    {
        int tileCol = tileNum % TerrainTileDims;
        int tileRow = tileNum / TerrainTileDims;
        int pointCol = pointNum % pointsPerTile;
        int pointRow = pointNum / pointsPerTile;

        Vector3 position = currentOrigin + new Vector3(tileCol * TerrainInterval + pointCol * PointInterval, baseHeight, tileRow * TerrainInterval + pointRow * PointInterval);

        float x = CustomRandom(position.x);
        float y = CustomRandom(position.z);

        return x * y;
    }

    private float GetHeightAtLocation(Vector3 location)
    {
        Vector3 relativePos = location - currentOrigin;
        int pointCol = Mathf.FloorToInt(relativePos.x / PointInterval);
        int pointRow = Mathf.FloorToInt(relativePos.z / PointInterval);

        float interpolatedHeight = 0;
        int used = 0;
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if ((0 <= pointCol + x) && (TerrainTileDims * pointsPerTile > pointCol + x) &&
                    (0 <= pointRow + y) && (TerrainTileDims * pointsPerTile > pointRow + y))
                {
                    float distRatio = Vector3.Distance(relativePos, new Vector3(pointCol * PointInterval, relativePos.y, pointRow * PointInterval)) / PointInterval;
                    interpolatedHeight += (1f - distRatio) * heightArray[pointRow + y, pointCol + x];
                    used++;
                }
            }
        }

        interpolatedHeight *= (5 - used);

        return interpolatedHeight * 40;

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

        for(int tile = 0; tile < TerrainTileDims * TerrainTileDims; tile++)
        {
            Vector3[] newVerts = new Vector3[terrainList[tile].MyMesh.vertexCount];
            for (int i = 0; i < terrainList[tile].MyMesh.vertexCount; i++)
            {
                Vector3 vert = terrainList[tile].MyMesh.vertices[i];
                Vector3 worldVert = vert;
                worldVert.x *= (TerrainInterval / TerrainDefaultScale);
                worldVert.z *= (TerrainInterval / TerrainDefaultScale);
                worldVert += terrainList[tile].gameObject.transform.position;

                float interpolatedHeight = GetHeightAtLocation(worldVert);

                newVerts[i] = new Vector3(vert.x, interpolatedHeight, vert.z);
            }
            terrainList[tile].MyMesh.vertices = newVerts;
            terrainList[tile].RecomputeMeshCollider();
        }

    }

    private void PlaceFinish()
    {
        if(FinishPrefab != null)
        {
            float tx = Random.Range(0.0f, 1.0f);
            float ty = Random.Range(0.0f, 1.0f);

            float x = Mathf.Sin((tx * 2 * 3.141f) + 0.5f);
            float y = Mathf.Cos((ty * 2 * 3.141f) + 0.3f);

            // dont spawn too close to the center
            if(Mathf.Abs(x) < 0.2f)
            {
                x = Mathf.Sign(x) * 0.2f;
            }

            if (Mathf.Abs(y) < 0.2f)
            {
                y = Mathf.Sign(y) * 0.2f;
            }

            // Remap from [-1,1] to [0,1]
            x = x * 0.1f + 0.5f;
            y = y * 0.1f + 0.5f;

            Debug.Log(x + " , " + y);

            Vector3 position = currentOrigin + new Vector3(x * TerrainTileDims * TerrainInterval, 0, y * TerrainTileDims * TerrainInterval);
            position.y += GetHeightAtLocation(position) - 15;
            Instantiate(FinishPrefab, position, Quaternion.identity);
        }
    }

    /*private void SetTerrainNeighbors()
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
    }*/
}
