#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Hypoxia
{
    public static class IllusionFormTests
    {
        [InitializeOnLoadMethod]
        public static void RunTests()
        {
            // B1: band -> form mapping must stay in range and follow band order.
            Debug.Assert(IllusionEntity.FormIndexForBand(ExposureCalculator.ExposureBand.Normal, 3) == 0, "B1: Normal must use form 0");
            Debug.Assert(IllusionEntity.FormIndexForBand(ExposureCalculator.ExposureBand.Distorted, 3) == 1, "B1: Distorted must use form 1");
            Debug.Assert(IllusionEntity.FormIndexForBand(ExposureCalculator.ExposureBand.Hallucinating, 3) == 2, "B1: Hallucinating must use form 2");
            Debug.Assert(IllusionEntity.FormIndexForBand(ExposureCalculator.ExposureBand.Hallucinating, 2) == 1, "B1: form index must clamp to available forms");
            Debug.Assert(IllusionEntity.FormIndexForBand(ExposureCalculator.ExposureBand.Normal, 0) == -1, "B1: no forms means no index");
        }
    }
}
#endif
