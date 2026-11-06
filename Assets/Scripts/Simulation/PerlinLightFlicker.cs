using UnityEngine;

namespace Hypoxia
{
    /// <summary>
    /// E6: Simulasi kedipan lampu darurat tambang berbasis Mathf.PerlinNoise (Modul 5).
    /// Tanpa keyframe animasi manual. Intensitas berfluktuasi secara dinamis dan organik.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class PerlinLightFlicker : MonoBehaviour
    {
        [SerializeField] private float baseIntensity = 3.0f;
        [SerializeField] private float flickerAmount = 2.0f;
        [SerializeField] private float flickerSpeed = 12.0f;
        [SerializeField] private float noiseOffset = 0f;

        private Light targetLight;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            if (noiseOffset == 0f)
            {
                noiseOffset = Random.Range(0f, 100f);
            }
        }

        private void Update()
        {
            if (targetLight == null) return;

            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, 0f);
            targetLight.intensity = Mathf.Max(0f, baseIntensity + (noise - 0.5f) * flickerAmount);
        }

        public static float SampleIntensity(float baseInt, float flickerAmt, float t, float speed, float offset)
        {
            float noise = Mathf.PerlinNoise(t * speed + offset, 0f);
            return Mathf.Max(0f, baseInt + (noise - 0.5f) * flickerAmt);
        }
    }
}
