#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Hypoxia
{
    public static class ExposureCalculatorTests
    {
        [InitializeOnLoadMethod]
        public static void RunTests()
        {
            // A1: rates & multipliers
            float walkDelta = ExposureCalculator.CalculateDelta(false, false, false);
            float sprintDelta = ExposureCalculator.CalculateDelta(false, false, true);
            Debug.Assert(Mathf.Approximately(walkDelta, 0.12f), "A1: walk rate must be 0.12");
            Debug.Assert(Mathf.Approximately(sprintDelta, 0.24f), "A1: sprint rate must be 2x walk rate (0.24)");

            // GDD Bab 6: Rian 20% lebih lambat (0.8 multiplier)
            float rianDelta = ExposureCalculator.CalculateDelta(false, false, false, 0.8f);
            Debug.Assert(Mathf.Approximately(rianDelta, 0.12f * 0.8f), "Bab 6: Rian rate must be 20% slower (0.096)");

            float pipeDelta = ExposureCalculator.CalculateDelta(false, true, false);
            Debug.Assert(Mathf.Approximately(pipeDelta, 0.42f), "A1: pipe rate must add 0.30 (0.42)");

            float openAirDelta = ExposureCalculator.CalculateDelta(true, false, false);
            Debug.Assert(openAirDelta < 0f, "A1: open air must decay");

            // A2: band thresholds
            Debug.Assert(ExposureCalculator.GetBand(49.9f) == ExposureCalculator.ExposureBand.Normal, "A2: 49.9% must be Normal");
            Debug.Assert(ExposureCalculator.GetBand(50.0f) == ExposureCalculator.ExposureBand.Distorted, "A2: 50% must be Distorted");
            Debug.Assert(ExposureCalculator.GetBand(74.9f) == ExposureCalculator.ExposureBand.Distorted, "A2: 74.9% must be Distorted");
            Debug.Assert(ExposureCalculator.GetBand(75.0f) == ExposureCalculator.ExposureBand.Hallucinating, "A2: 75% must be Hallucinating");

            // A3: oxygen logic
            int tanks = 3;
            float exp = ExposureCalculator.ApplyOxygen(50f, ref tanks);
            Debug.Assert(Mathf.Approximately(exp, 30f), "A3: oxygen must reduce by 20%");
            Debug.Assert(tanks == 2, "A3: tanks must decrement to 2");

            int emptyTanks = 0;
            float sameExp = ExposureCalculator.ApplyOxygen(50f, ref emptyTanks);
            Debug.Assert(Mathf.Approximately(sameExp, 50f), "A3: empty tank must not change exposure");
            Debug.Assert(emptyTanks == 0, "A3: empty tanks must remain 0");

            // ponytail: test suite covers Sprint 1 acceptance criteria. Add integration test runner when CI pipeline is introduced.
        }
    }
}
#endif
