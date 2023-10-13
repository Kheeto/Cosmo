using UnityEngine;

public class Planet : MonoBehaviour {

    [Range(2, 256)] public int resolution;

    // If set to true, the planet will be generated automatically every time settings are updated
    public bool autoUpdate = true;

    public enum FaceRenderMask { All, Top, Bottom, Left, Right, Front, Back }
    public FaceRenderMask faceRenderMask;

    public ShapeSettings shapeSettings;
    public ColourSettings colourSettings;

    // Variables for the Editor script
    [HideInInspector] public bool shapeSettingsFoldout;
    [HideInInspector] public bool colourSettingsFoldout;
    ShapeGenerator shapeGenerator = new ShapeGenerator();
    ColourGenerator colourGenerator = new ColourGenerator();

    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;

    /// <summary>
    /// Initialize the planet, generate the mesh and colours
    /// </summary>
    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
        GenerateColours();
    }

    /// <summary>
    /// Initialize the shape and colour generators, generate the meshFilters and the terrainFaces
    /// </summary>
    private void Initialize()
    {
        shapeGenerator.UpdateSettings(shapeSettings);
        colourGenerator.UpdateSettings(colourSettings);

        if (meshFilters == null || meshFilters.Length == 0)
            meshFilters = new MeshFilter[6];
        terrainFaces = new TerrainFace[6];

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < 6; i++)
        {
            // If the mesh filter is null, create a new one
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("mesh");
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>();
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }
            meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colourSettings.planetMaterial;

            // Create new terrain faces and render the meshes
            terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i]);
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            meshFilters[i].gameObject.SetActive(renderFace);
        }
    }

    /// <summary>
    /// Construct the terrainFaces for each meshFilter and update the elevation values
    /// </summary>
    private void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
                terrainFaces[i].ConstructMesh();
        }
        colourGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }

    /// <summary>
    /// Update the colours and UVs from the colourGenerator
    /// </summary>
    private void GenerateColours()
    {
        colourGenerator.UpdateColours();
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
                terrainFaces[i].UpdateUVs(colourGenerator);
        }
    }

    public void OnShapeSettingsUpdated()
    {
        if (!autoUpdate) return;
        Initialize();
        GenerateMesh();
    }

    public void OnColourSettingsUpdated()
    {
        if (!autoUpdate) return;
        Initialize();
        GenerateColours();
    }
}
