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

    // Terrain Height Vars //

    private Vector3 peakPosition = Vector3.zero;
    public Vector3 Peak { get { return peakPosition; } }

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


    public void GenerateNewPeak()
    {
        peakPosition = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
    }

}
