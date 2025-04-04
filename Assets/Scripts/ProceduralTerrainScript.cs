using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.TerrainTools;

public class ProceduralTerrainScript : MonoBehaviour
{
    [SerializeField]
    private int pointsPerTile = 500;

    //private Terrain myTerrain = null;
    //public Terrain MyTerrain { get { return myTerrain; } }

    private Mesh myMesh = null;
    public Mesh MyMesh { get { return myMesh; } }
    
    private MeshCollider myCollider = null;

    // Start is called before the first frame update
    void Start()
    {
        

        //GenerateNewHeightmap();
    }

    public void InitTerrain()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        myMesh = filter.mesh;

        myCollider = GetComponent<MeshCollider>();
        if(myCollider != null )
        {
            myCollider.sharedMesh = myMesh;
        }

        /*myTerrain = GetComponent<Terrain>();
        TerrainData newTerrainData = new TerrainData();
        //newTerrainData.baseMapResolution = pointsPerTile;
        newTerrainData.heightmapResolution = pointsPerTile;
        newTerrainData.SetDetailResolution(1024, 32);
        newTerrainData.size = new Vector3(1000, 10, 1000);
        myTerrain.terrainData = newTerrainData;
        GetComponent<TerrainCollider>().terrainData = newTerrainData;*/


    }


    public void RecomputeMeshCollider()
    {
        // Turn it off and back on again lmao (tells it to refresh the collider)
        myCollider.sharedMesh = null;
        myCollider.sharedMesh = MyMesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /*void GenerateNewHeightmap()
    {
        float initX = transform.position.x;
        float initY = transform.position.z;
        
        float[,] newHeightArray = new float[pointsPerTile, pointsPerTile];
        float interval = 1000f / ((float)pointsPerTile - 1);

        for (int iX = 0; iX < pointsPerTile; iX++)
        {
            for(int iY = 0; iY < pointsPerTile; iY++)
            {
                float x = 0.001f * (iX * interval + initX);
                float y = 0.001f * (iY * interval + initY);
                newHeightArray[iX, iY] = 0.5f * Mathf.PerlinNoise(x, y);
                if((iX == 0 || iX == 512) && (iY == 0 || iY == 512))
                {
                    Debug.Log("pos: " + (x) + ", " + (y));
                    Debug.Log(0.5f * Mathf.PerlinNoise(x, y));
                }
            }
        }

        myTerrain.terrainData.SetHeights(0, 0, newHeightArray);
    }*/

}
