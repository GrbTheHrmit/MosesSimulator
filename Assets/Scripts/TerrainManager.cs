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
    // The world location of the map's bottom left corner
    private Vector3 currentOrigin = Vector3.zero;
    private float baseHeight = -5;

    [SerializeField]
    private int pointsPerTile = 50;
    private float[,] heightArray = null;
    private Vector3[] peakPositions = null;

    [SerializeField]
    private float MaxTerrainHeight = 30;

    private List<ProceduralTerrainScript> terrainList = new List<ProceduralTerrainScript>();
    private float TerrainInterval = 1000f;
    private float TerrainDefaultScale = 10f; // need for vert locations
    private float PointInterval;

    private int voronoiPointsPerTile = 50;
    private List<Vector2> voronoiPoints = new List<Vector2>();


    // For Tracking Looping World
    // these represent where the bottom left terrain object is in world X and Y coordinates
    private int currentBottomLeftX;
    private int currentBottomLeftY;
    
    // For Tracking Looping Terrain Array
    // These represent the actual array location of the current bottom left object
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
        peakPositions = new Vector3[MaxTerrainDim * MaxTerrainDim];
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

            currentBottomLeftX = 0;
            currentBottomLeftY = 0;

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
        int tileCol = Mathf.FloorToInt(relativePos.x / TerrainInterval) % MaxTerrainDim;
        int tileRow = Mathf.FloorToInt(relativePos.z / TerrainInterval) % MaxTerrainDim;
        int tileIdx = tileRow * MaxTerrainDim + tileCol;

        // Get the [0,1] value for relative x and z location on this tile
        float localX = (relativePos.x - (tileCol * TerrainInterval)) / TerrainInterval;
        float localZ = (relativePos.z - (tileRow * TerrainInterval)) / TerrainInterval;

        float interpolatedHeight = ComputeBarycentricHeight(tileIdx, localX, localZ);

        return interpolatedHeight * MaxTerrainHeight;

    }


    // This is the barycentric interpolation formula used for color interpolation of triangles, just adapted for height
    private float ComputeBarycentricHeight(int tile, float x, float z)
    {
        int tileCol = tile % MaxTerrainDim;
        int tileRow = tile / MaxTerrainDim;

        Vector3 v1 = peakPositions[tile];
        // NOTE: Doing it this way means that it will never consider a corner connection as potentially closer
        Vector3 v2;
        Vector3 v3;

        if (x > 0.5f)
        {
            int newCol = (tileCol + 1) % MaxTerrainDim;
            v2 = peakPositions[tileRow * MaxTerrainDim + newCol];
            v2.x += 1; // Adjusting because peaks are stored as 0-1 values for location
        }
        else
        {
            int newCol = (tileCol + (MaxTerrainDim - 1)) % MaxTerrainDim;
            v2 = peakPositions[tileRow * MaxTerrainDim + newCol];
            v2.x -= 1;
        }

        if (z > 0.5f)
        {
            int newRow = (tileRow + 1) % MaxTerrainDim;
            v3 = peakPositions[newRow * MaxTerrainDim + tileCol];
            v3.z += 1;
        }
        else
        {
            int newRow = (tileRow + (MaxTerrainDim - 1)) % MaxTerrainDim;
            v3 = peakPositions[newRow * MaxTerrainDim + tileCol];
            v3.z -= 1;
        }
        

        float w1 =  ( (v2.z - v3.z) * (x - v3.x) + (v3.x - v2.x) * (z - v3.z) ) / 
                    ( (v2.z - v3.z) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.z - v3.z) );

        float w2 =  ( (v3.z - v1.z) * (x - v3.x) + (v1.x - v3.x) * (z - v3.z) ) /
                    ( (v2.z - v3.z) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.z - v3.z) );

        float w3 = 1 - (w1 + w2);

        return v1.y * w1 + v2.y * w2 + v3.y * w3;

    }

    private void GenerateNewHeightMap()
    {
        // First we generate a set of peak locations for each tile
        for (int tile = 0; tile < MaxTerrainDim * MaxTerrainDim; tile++)
        {
            peakPositions[tile] = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
        }

        // Then we create a heightmap of interpolated values
        for (int tile = 0; tile < MaxTerrainDim * MaxTerrainDim; tile++)
        {

            int tileCol = tile % MaxTerrainDim;
            int tileRow = tile / MaxTerrainDim;


            for(int point = 0; point < pointsPerTile * pointsPerTile; point++)
            {
                int pointCol = point % pointsPerTile;
                int pointRow = point / pointsPerTile;

                float x = (float)pointCol / pointsPerTile;
                float z = (float)pointRow / pointsPerTile;

                float height = ComputeBarycentricHeight(tile, x, z);

                heightArray[tileRow * pointsPerTile + pointRow, tileCol * pointsPerTile + pointCol] = height;
            }

        }

        // Finally modify the terrain vertices to match the height map
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
