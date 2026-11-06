using UnityEngine;

namespace Hypoxia
{
    /// <summary>
    /// E4: Procedural Terrain untuk area Dermaga berbasis Mathf.PerlinNoise pada vertex mesh.
    /// Memakai fixed seed agar deterministik di kedua klien tanpa replikasi network.
    /// Tinggi terrain dapat disetel via runtime slider (Modul 2).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralTerrain : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int widthSegments = 20;
        [SerializeField] private int lengthSegments = 20;
        [SerializeField] private float sizeX = 20f;
        [SerializeField] private float sizeZ = 20f;

        [Header("Perlin Parameters")]
        [SerializeField] private float noiseFrequency = 0.15f;
        [Range(0f, 10f)]
        [SerializeField] private float heightMultiplier = 2.5f;
        [SerializeField] private int seed = 42;

        private Mesh mesh;
        private Vector3[] baseVertices;
        private Vector3[] currentVertices;
        private MeshCollider meshCollider;

        public float HeightMultiplier
        {
            get => heightMultiplier;
            set
            {
                if (!Mathf.Approximately(heightMultiplier, value))
                {
                    heightMultiplier = Mathf.Clamp(value, 0f, 10f);
                    UpdateMeshHeights();
                }
            }
        }

        public int Seed => seed;

        private void Awake()
        {
            meshCollider = GetComponent<MeshCollider>();
            GenerateBaseGrid();
            UpdateMeshHeights();
        }

        public void GenerateBaseGrid()
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "Procedural_Dermaga_Terrain" };
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            int vCount = (widthSegments + 1) * (lengthSegments + 1);
            baseVertices = new Vector3[vCount];
            currentVertices = new Vector3[vCount];
            Vector2[] uvs = new Vector2[vCount];

            float dx = sizeX / widthSegments;
            float dz = sizeZ / lengthSegments;
            float startX = -sizeX * 0.5f;
            float startZ = -sizeZ * 0.5f;

            for (int z = 0, i = 0; z <= lengthSegments; z++)
            {
                for (int x = 0; x <= widthSegments; x++, i++)
                {
                    baseVertices[i] = new Vector3(startX + x * dx, 0f, startZ + z * dz);
                    currentVertices[i] = baseVertices[i];
                    uvs[i] = new Vector2((float)x / widthSegments, (float)z / lengthSegments);
                }
            }

            int[] triangles = new int[widthSegments * lengthSegments * 6];
            for (int ti = 0, vi = 0, z = 0; z < lengthSegments; z++, vi++)
            {
                for (int x = 0; x < widthSegments; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + widthSegments + 1;
                    triangles[ti + 2] = vi + 1;
                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + widthSegments + 1;
                    triangles[ti + 5] = vi + widthSegments + 2;
                }
            }

            mesh.Clear();
            mesh.vertices = currentVertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }

        public void UpdateMeshHeights()
        {
            if (mesh == null || baseVertices == null) return;

            // Offset berbasis fixed seed deterministik
            float seedOffsetX = (seed * 17) % 1000;
            float seedOffsetZ = (seed * 31) % 1000;

            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector3 v = baseVertices[i];
                float nx = (v.x + seedOffsetX) * noiseFrequency;
                float nz = (v.z + seedOffsetZ) * noiseFrequency;
                float y = Mathf.PerlinNoise(nx, nz) * heightMultiplier;
                currentVertices[i] = new Vector3(v.x, y, v.z);
            }

            mesh.vertices = currentVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;
            }
        }

        // Helper perhitungan tinggi untuk pengujian unit deterministik
        public static float SampleHeight(float x, float z, int fixedSeed, float freq, float mult)
        {
            float seedOffsetX = (fixedSeed * 17) % 1000;
            float seedOffsetZ = (fixedSeed * 31) % 1000;
            return Mathf.PerlinNoise((x + seedOffsetX) * freq, (z + seedOffsetZ) * freq) * mult;
        }
    }
}
