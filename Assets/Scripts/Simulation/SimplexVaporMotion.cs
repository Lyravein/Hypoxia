using Unity.Mathematics;
using UnityEngine;

namespace Hypoxia
{
    /// <summary>
    /// E6: Simulasi dinamika kabut uap gas racun CO (Modul 5).
    /// Menggunakan 3D Simplex Noise (Unity.Mathematics.noise.snoise) untuk menggeser posisi
    /// partikel atau transform uap di lorong poros secara dinamis tanpa keyframe manual.
    /// </summary>
    public sealed class SimplexVaporMotion : MonoBehaviour
    {
        [SerializeField] private float speed = 0.8f;
        [SerializeField] private float amplitude = 0.6f;
        [SerializeField] private float frequency = 0.5f;

        private Vector3 initialPosition;

        private void Awake()
        {
            initialPosition = transform.position;
        }

        private void Update()
        {
            float t = Time.time * speed;
            float3 p = new float3(initialPosition.x * frequency, initialPosition.y * frequency, t);

            float offsetX = noise.snoise(p);
            float offsetY = noise.snoise(p + new float3(31.4f, 17.2f, 0f));
            float offsetZ = noise.snoise(p + new float3(55.1f, 89.3f, 0f));

            transform.position = initialPosition + new Vector3(offsetX, offsetY, offsetZ) * amplitude;
        }

        public static Vector3 SampleOffset(Vector3 basePos, float t, float freq, float amp)
        {
            float3 p = new float3(basePos.x * freq, basePos.y * freq, t);
            float ox = noise.snoise(p);
            float oy = noise.snoise(p + new float3(31.4f, 17.2f, 0f));
            float oz = noise.snoise(p + new float3(55.1f, 89.3f, 0f));
            return new Vector3(ox, oy, oz) * amp;
        }
    }
}
