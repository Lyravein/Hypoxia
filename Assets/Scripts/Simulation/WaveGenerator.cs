using UnityEngine;

namespace Hypoxia
{
    /// <summary>
    /// E6: Simulasi permukaan air/genangan air (Modul 5).
    /// Mengubah posisi vertex mesh tiap frame menggunakan superposisi gelombang sinus + noise,
    /// lalu memanggil RecalculateNormals() agar pantulan dan pencahayaan dinamis. Tanpa keyframe.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WaveGenerator : MonoBehaviour
    {
        [Header("Wave Parameters")]
        [SerializeField] private float waveHeight = 0.08f;
        [SerializeField] private float waveSpeed = 2.0f;
        [SerializeField] private float waveFrequency = 1.2f;

        [Header("Grid Size")]
        [SerializeField] private int xSegments = 16;
        [SerializeField] private int zSegments = 16;
        [SerializeField] private float width = 4f;
        [SerializeField] private float length = 4f;

        private Mesh mesh;
        private Vector3[] baseVertices;
        private Vector3[] dynamicVertices;

        private void Awake()
        {
            BuildBasePlane();
        }

        private void BuildBasePlane()
        {
            mesh = new Mesh { name = "Dynamic_Puddle_Mesh" };
            GetComponent<MeshFilter>().sharedMesh = mesh;

            int vCount = (xSegments + 1) * (zSegments + 1);
            baseVertices = new Vector3[vCount];
            dynamicVertices = new Vector3[vCount];
            Vector2[] uvs = new Vector2[vCount];

            float dx = width / xSegments;
            float dz = length / zSegments;
            float startX = -width * 0.5f;
            float startZ = -length * 0.5f;

            for (int z = 0, i = 0; z <= zSegments; z++)
            {
                for (int x = 0; x <= xSegments; x++, i++)
                {
                    baseVertices[i] = new Vector3(startX + x * dx, 0f, startZ + z * dz);
                    dynamicVertices[i] = baseVertices[i];
                    uvs[i] = new Vector2((float)x / xSegments, (float)z / zSegments);
                }
            }

            int[] triangles = new int[xSegments * zSegments * 6];
            for (int ti = 0, vi = 0, z = 0; z < zSegments; z++, vi++)
            {
                for (int x = 0; x < xSegments; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + xSegments + 1;
                    triangles[ti + 2] = vi + 1;
                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + xSegments + 1;
                    triangles[ti + 5] = vi + xSegments + 2;
                }
            }

            mesh.vertices = dynamicVertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }

        private void Update()
        {
            if (baseVertices == null || mesh == null) return;

            float time = Time.time * waveSpeed;

            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector3 v = baseVertices[i];
                // Kombinasi 2 komponen arah gelombang (pola WaveGenerator GDD Bab 10)
                float wave = Mathf.Sin((v.x * waveFrequency) + time) *
                             Mathf.Cos((v.z * waveFrequency * 0.8f) + time * 0.7f);

                dynamicVertices[i] = new Vector3(v.x, v.y + wave * waveHeight, v.z);
            }

            mesh.vertices = dynamicVertices;
            mesh.RecalculateNormals();
        }

        public static float ComputeWaveHeight(float x, float z, float t, float freq, float height)
        {
            return Mathf.Sin((x * freq) + t) * Mathf.Cos((z * freq * 0.8f) + t * 0.7f) * height;
        }
    }
}
