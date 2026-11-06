#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Hypoxia
{
    public static class PuzzleTests
    {
        [InitializeOnLoadMethod]
        public static void RunTests()
        {
            // C1: Safe code validation
            Debug.Assert(ArchiveSafe.ValidateCode("428", "428"), "C1: Code 428 must be valid");
            Debug.Assert(ArchiveSafe.ValidateCode(" 428 ", "428"), "C1: Code with whitespace must be trimmed and valid");
            Debug.Assert(!ArchiveSafe.ValidateCode("999", "428"), "C1: Incorrect code must be rejected");
            Debug.Assert(!ArchiveSafe.ValidateCode("", "428"), "C1: Empty code must be rejected");
            Debug.Assert(!ArchiveSafe.ValidateCode(null, "428"), "C1: Null code must be rejected");

            // C1: Glyph distortion mapping across CO bands
            string[] normalGlyphs = ArchiveGlyphWall.GetGlyphsForBand(ExposureCalculator.ExposureBand.Normal);
            Debug.Assert(normalGlyphs.Length == 3 && normalGlyphs[0] == "α" && normalGlyphs[1] == "σ" && normalGlyphs[2] == "λ",
                "C1: Normal band must show clear α, σ, λ");

            string[] distortedGlyphs = ArchiveGlyphWall.GetGlyphsForBand(ExposureCalculator.ExposureBand.Distorted);
            Debug.Assert(distortedGlyphs[1] == "δ?", "C1: Distorted band must show ambiguous δ? for middle glyph");

            string[] hallucinatingGlyphs = ArchiveGlyphWall.GetGlyphsForBand(ExposureCalculator.ExposureBand.Hallucinating);
            Debug.Assert(hallucinatingGlyphs[0] == "?" && hallucinatingGlyphs[1] == "?" && hallucinatingGlyphs[2] == "?",
                "C1: Hallucinating band must show undecipherable ?");
        }
    }
}
#endif
