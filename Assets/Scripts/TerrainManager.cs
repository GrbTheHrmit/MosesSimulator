using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public GameObject TerrainPrefab;
    public GameObject FinishPrefab;

    [SerializeField]
    private int MaxTerrainDim = 64;
    [SerializeField]
    private int TerrainTileDims = 6;
    [SerializeField]
    private int SwapEdgeDistance = 2;
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

    private int currentBottomLeftX;
    private int currentBottomLeftY;

    private int TerrainBottomLeftX = 0;
    private int TerrainBottomLeftY = 0;

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

        heightArray = new float[pointsPerTile * MaxTerrainDim, pointsPerTile * MaxTerrainDim];
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

            // Set CenterOrigin to bottom left of tile location 0 ( World origin is the middle)
            float centerOffset = 0.5f * TerrainInterval;
            currentOrigin = new Vector3(-((MaxTerrainDim - 1) * 0.5f) * TerrainInterval, baseHeight, -((MaxTerrainDim - 1) * 0.5f) * TerrainInterval);
            currentOrigin -= new Vector3(centerOffset, 0, centerOffset);

            //Debug.Log(currentOrigin);

            //SetTerrainNeighbors();
            GenerateNewHeightMap();

            PlaceFinish();
            //SpawnPlayer();
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
        CheckMoveTiles();
    }

    private void CheckMoveTiles()
    {
        int playerTile = GetPlayerTileIdx();
        int playerX = playerTile % MaxTerrainDim;
        int playerY = playerTile / MaxTerrainDim;

        int diffX = (playerX - currentBottomLeftX + MaxTerrainDim) % MaxTerrainDim;
        int diffY = (playerY - currentBottomLeftY + MaxTerrainDim) % MaxTerrainDim;

        if(diffX < SwapEdgeDistance || diffX >= TerrainTileDims - SwapEdgeDistance)
        {
            SwapCol(!(diffX < SwapEdgeDistance));
        }

        if (diffY < SwapEdgeDistance || diffY >= TerrainTileDims - SwapEdgeDistance)
        {
            SwapRow(!(diffY < SwapEdgeDistance));
        }


    }

    private void SwapRow(bool increase)
    {
        int maxTileNum = MaxTerrainDim * MaxTerrainDim;

        if(increase)
        {
            // Move all items in this row
            for(int tile = 0; tile < TerrainTileDims; tile++)
            {
                int tileIdx = TerrainBottomLeftY * TerrainTileDims + tile;
                terrainList[tileIdx].gameObject.transform.position += new Vector3(0, 0, TerrainTileDims * TerrainInterval);

                // Recalculate Verts
                Vector3[] newVerts = new Vector3[terrainList[tileIdx].MyMesh.vertexCount];
                for (int i = 0; i < terrainList[tileIdx].MyMesh.vertexCount; i++)
                {
                    Vector3 vert = terrainList[tileIdx].MyMesh.vertices[i];
                    Vector3 worldVert = vert;
                    worldVert.x *= (TerrainInterval / TerrainDefaultScale);
                    worldVert.z *= (TerrainInterval / TerrainDefaultScale);
                    worldVert += terrainList[tileIdx].gameObject.transform.position;

                    float interpolatedHeight = GetHeightAtLocation(worldVert);
                    //Debug.Log(interpolatedHeight);
                    newVerts[i] = new Vector3(vert.x, interpolatedHeight, vert.z);
                }
                terrainList[tileIdx].MyMesh.vertices = newVerts;
                terrainList[tileIdx].RecomputeMeshCollider();
            }
            
            // Set the next row to be the bottomleft
            TerrainBottomLeftY = (TerrainBottomLeftY + 1) % TerrainTileDims;
            currentBottomLeftY = (currentBottomLeftY + 1) % MaxTerrainDim;
        }
        else
        {
            // Set the row we are going to move as the bottom left
            TerrainBottomLeftY = (TerrainBottomLeftY + TerrainTileDims - 1) % TerrainTileDims;
            currentBottomLeftY = (currentBottomLeftY + MaxTerrainDim - 1) % MaxTerrainDim;

            // Move all items in this row
            for (int tile = 0; tile < TerrainTileDims; tile++)
            {
                int tileIdx = TerrainBottomLeftY * TerrainTileDims + tile;
                terrainList[tileIdx].transform.position -= new Vector3(0, 0, TerrainTileDims * TerrainInterval);

                // Recalculate Verts
                Vector3[] newVerts = new Vector3[terrainList[tileIdx].MyMesh.vertexCount];
                for (int i = 0; i < terrainList[tileIdx].MyMesh.vertexCount; i++)
                {
                    Vector3 vert = terrainList[tileIdx].MyMesh.vertices[i];
                    Vector3 worldVert = vert;
                    worldVert.x *= (TerrainInterval / TerrainDefaultScale);
                    worldVert.z *= (TerrainInterval / TerrainDefaultScale);
                    worldVert += terrainList[tileIdx].gameObject.transform.position;

                    float interpolatedHeight = GetHeightAtLocation(worldVert);

                    newVerts[i] = new Vector3(vert.x, interpolatedHeight, vert.z);
                }
                terrainList[tileIdx].MyMesh.vertices = newVerts;
                terrainList[tileIdx].RecomputeMeshCollider();
            }
        }

    }

    private void SwapCol(bool increase)
    {
        int maxTileNum = MaxTerrainDim * MaxTerrainDim;

        if (increase)
        {
            // Move all items in this row
            for (int tile = 0; tile < TerrainTileDims; tile++)
            {
                int tileIdx = tile * TerrainTileDims + TerrainBottomLeftX;
                terrainList[tileIdx].gameObject.transform.position += new Vector3(TerrainTileDims * TerrainInterval, 0, 0);

                // Recalculate Verts
                Vector3[] newVerts = new Vector3[terrainList[tileIdx].MyMesh.vertexCount];
                for (int i = 0; i < terrainList[tileIdx].MyMesh.vertexCount; i++)
                {
                    Vector3 vert = terrainList[tileIdx].MyMesh.vertices[i];
                    Vector3 worldVert = vert;
                    worldVert.x *= (TerrainInterval / TerrainDefaultScale);
                    worldVert.z *= (TerrainInterval / TerrainDefaultScale);
                    worldVert += terrainList[tileIdx].gameObject.transform.position;

                    float interpolatedHeight = GetHeightAtLocation(worldVert);
                    //Debug.Log(interpolatedHeight);
                    newVerts[i] = new Vector3(vert.x, interpolatedHeight, vert.z);
                }
                terrainList[tileIdx].MyMesh.vertices = newVerts;
                terrainList[tileIdx].RecomputeMeshCollider();
            }

            // Set the next row to be the bottomleft
            TerrainBottomLeftX = (TerrainBottomLeftX + 1) % TerrainTileDims;
            currentBottomLeftX = (currentBottomLeftX + 1) % MaxTerrainDim;
        }
        else
        {
            // Set the row we are going to move as the bottom left
            TerrainBottomLeftX = (TerrainBottomLeftX + TerrainTileDims - 1) % TerrainTileDims;
            currentBottomLeftX = (currentBottomLeftX + MaxTerrainDim - 1) % MaxTerrainDim;

            // Move all items in this row
            for (int tile = 0; tile < TerrainTileDims; tile++)
            {
                int tileIdx = tile * TerrainTileDims + TerrainBottomLeftX;
                terrainList[tileIdx].transform.position -= new Vector3(TerrainTileDims * TerrainInterval, 0, 0);

                // Recalculate Verts
                Vector3[] newVerts = new Vector3[terrainList[tileIdx].MyMesh.vertexCount];
                for (int i = 0; i < terrainList[tileIdx].MyMesh.vertexCount; i++)
                {
                    Vector3 vert = terrainList[tileIdx].MyMesh.vertices[i];
                    Vector3 worldVert = vert;
                    worldVert.x *= (TerrainInterval / TerrainDefaultScale);
                    worldVert.z *= (TerrainInterval / TerrainDefaultScale);
                    worldVert += terrainList[tileIdx].gameObject.transform.position;

                    float interpolatedHeight = GetHeightAtLocation(worldVert);

                    newVerts[i] = new Vector3(vert.x, interpolatedHeight, vert.z);
                }
                terrainList[tileIdx].MyMesh.vertices = newVerts;
                terrainList[tileIdx].RecomputeMeshCollider();
            }
        }
    }

    private float CustomRandom(float x)
    {
        return Mathf.Sin(((Mathf.Sin(x * 53885.7f) * 31841.93f) % 1f) * 19) * 0.5f + 0.5f;
    }

    private float GetMappedValue(int tileNum, int pointNum)
    {
        int tileCol = tileNum % MaxTerrainDim;
        int tileRow = tileNum / MaxTerrainDim;
        int pointCol = pointNum % pointsPerTile;
        int pointRow = pointNum / pointsPerTile;

        Vector3 position = currentOrigin + new Vector3(tileCol * TerrainInterval + pointCol * PointInterval, baseHeight, tileRow * TerrainInterval + pointRow * PointInterval);

        float x = CustomRandom(position.x);
        float y = CustomRandom(position.z);

        return x * y;
    }

    public int GetPlayerTileIdx()
    {
        Vector3 relativePos = FollowManager.Instance().LeaderPosition - currentOrigin;
        int tileCol = Mathf.FloorToInt(relativePos.x / TerrainInterval);
        int tileRow = Mathf.FloorToInt(relativePos.z / TerrainInterval);

        return tileRow * MaxTerrainDim + tileCol;
    }

    private int GetTileIdxAtLocation(Vector3 location)
    {
        Vector3 relativePos = location - currentOrigin;
        int tileCol = Mathf.FloorToInt(relativePos.x / TerrainInterval);
        int tileRow = Mathf.FloorToInt(relativePos.z / TerrainInterval);

        return tileRow * MaxTerrainDim + tileCol;
    }

    private float GetHeightAtLocation(Vector3 location)
    {
        Vector3 relativePos = location - currentOrigin;

        // Slightly jank way to make sure we get a point we've calculated. world will loop
        int pointCol = Mathf.FloorToInt(relativePos.x / PointInterval) % (MaxTerrainDim * pointsPerTile);
        while(pointCol < 0)
        {
            pointCol += MaxTerrainDim * pointsPerTile;
        }
        relativePos.x += (pointCol - Mathf.FloorToInt(relativePos.x / PointInterval)) * PointInterval;

        int pointRow = Mathf.FloorToInt(relativePos.z / PointInterval) % (MaxTerrainDim * pointsPerTile);
        while (pointRow < 0)
        {
            pointRow += MaxTerrainDim * pointsPerTile;
        }
        relativePos.z += (pointRow - Mathf.FloorToInt(relativePos.z / PointInterval)) * PointInterval;

        float interpolatedHeight = 0;
        int used = 0;
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if ((0 <= pointCol + x) && (MaxTerrainDim * pointsPerTile > pointCol + x) &&
                    (0 <= pointRow + y) && (MaxTerrainDim * pointsPerTile > pointRow + y))
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
        for (int tile = 0; tile < MaxTerrainDim * MaxTerrainDim; tile++)
        {
            int tileCol = tile % MaxTerrainDim;
            int tileRow = tile / MaxTerrainDim;


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

        TerrainBottomLeftX = 0;
        TerrainBottomLeftY = 0;
        int currentBottomLeftTile = GetTileIdxAtLocation(terrainList[0].gameObject.transform.position);
        currentBottomLeftX = currentBottomLeftTile % MaxTerrainDim;
        currentBottomLeftY = currentBottomLeftTile / MaxTerrainDim;
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

            //Debug.Log(x + " , " + y);

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
