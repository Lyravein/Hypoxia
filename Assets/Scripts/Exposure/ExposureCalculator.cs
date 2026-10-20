using System;

namespace Hypoxia
{
    public static class ExposureCalculator
    {
        public const float PassiveRate = 0.12f;       // % per second in enclosed space
        public const float SprintMultiplier = 2.0f;   // 2x walking rate
        public const float LeakingPipeBonus = 0.30f;   // +0.3% per second near pipe
        public const float OpenAirDecayRate = -0.10f;  // % per second decay in open air
        public const float OxygenRecovery = 20.0f;     // 20% reduction per tank
        public const float MaxExposure = 100.0f;

        public enum ExposureBand
        {
            Normal,       // 0 - 49%
            Distorted,    // 50 - 74%
            Hallucinating // 75 - 100%
        }

        public static float CalculateDelta(bool inOpenAir, bool nearPipe, bool isSprinting, float multiplier = 1.0f)
        {
            if (inOpenAir)
            {
                return OpenAirDecayRate;
            }

            float rate = PassiveRate;
            if (isSprinting)
            {
                rate *= SprintMultiplier;
            }

            if (nearPipe)
            {
                rate += LeakingPipeBonus;
            }

            // ponytail: simple scalar character multiplier (Rian 0.8, Maya 1.0). Upgrade to character attribute system if roles expand.
            return rate * multiplier;
        }

        public static float ApplyOxygen(float currentExposure, ref int tanks)
        {
            if (tanks <= 0) return currentExposure;
            tanks--;
            return Math.Max(0f, currentExposure - OxygenRecovery);
        }

        public static ExposureBand GetBand(float exposure)
        {
            if (exposure < 50f) return ExposureBand.Normal;
            if (exposure < 75f) return ExposureBand.Distorted;
            return ExposureBand.Hallucinating;
        }
    }
}
