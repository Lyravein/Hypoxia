#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hypoxia
{
    public static class ProceduralSimulationTests
    {
        [InitializeOnLoadMethod]
        public static void RunTests()
        {
            // E4: Deterministic Terrain Perlin Height
            float h1 = ProceduralTerrain.SampleHeight(5f, 10f, 42, 0.15f, 2.5f);
            float h2 = ProceduralTerrain.SampleHeight(5f, 10f, 42, 0.15f, 2.5f);
            Debug.Assert(Mathf.Approximately(h1, h2), "E4: SampleHeight must be deterministic across identical inputs and seed");

            float h3 = ProceduralTerrain.SampleHeight(5f, 10f, 42, 0.15f, 5.0f);
            Debug.Assert(Mathf.Approximately(h3, h1 * 2f), "E4: Height multiplier must scale height linearly");

            // E5: L-System String Rewriting
            Dictionary<char, string> testRules = new Dictionary<char, string>
            {
                ['A'] = "AB",
                ['B'] = "A"
            };
            string iter0 = LSystemGenerator.Rewrite("A", testRules, 0);
            string iter1 = LSystemGenerator.Rewrite("A", testRules, 1);
            string iter2 = LSystemGenerator.Rewrite("A", testRules, 2);
            string iter3 = LSystemGenerator.Rewrite("A", testRules, 3);
            Debug.Assert(iter0 == "A", "E5: Iteration 0 must match axiom");
            Debug.Assert(iter1 == "AB", "E5: Iteration 1 must match rule");
            Debug.Assert(iter2 == "ABA", "E5: Iteration 2 must match rule");
            Debug.Assert(iter3 == "ABAAB", "E5: Iteration 3 must match rule");

            // E6: WaveGenerator height formula
            float w1 = WaveGenerator.ComputeWaveHeight(0f, 0f, 0f, 1.2f, 0.08f);
            Debug.Assert(Mathf.Approximately(w1, 0f), "E6: Wave height at origin t=0 must be 0 (sin(0) = 0)");

            // E6: PerlinLightFlicker intensity clamp
            float intensity = PerlinLightFlicker.SampleIntensity(3.0f, 2.0f, 1.5f, 12.0f, 0f);
            Debug.Assert(intensity >= 0f, "E6: Light intensity must never fall below 0");
        }
    }
}
#endif
