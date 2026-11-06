using UnityEngine;

namespace Hypoxia
{
    /// <summary>
    /// C1: Dinding Glif Laboratorium Geologi (Teka-teki P1).
    /// Hanya dapat dibaca oleh Pemain A (Rian) dengan senter geolog.
    /// Glif mengalami distorsi visual saat paparan CO mencapai band Terdistorsi/Halusinasi.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArchiveGlyphWall : MonoBehaviour, IInteractable
    {
        private bool isInspecting;

        public string InteractionPrompt => "[E] Periksa Dinding Glif Laboratorium";

        public void Interact()
        {
            isInspecting = !isInspecting;
        }

        private void Update()
        {
            if (isInspecting && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isInspecting = false;
            }
        }

        private void OnGUI()
        {
            if (!isInspecting) return;

            float w = 480f;
            float h = 160f;
            float cx = (Screen.width - w) * 0.5f;
            float cy = Screen.height * 0.25f;

            Color oldColor = GUI.color;
            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            GUI.DrawTexture(new Rect(cx, cy, w, h), Texture2D.whiteTexture);
            GUI.color = oldColor;

            PlayerExposure player = PlayerExposure.LocalInstance;
            bool isRian = player != null && player.IsRian;

            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle glyphStyle = new(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.75f, 0.2f) }
            };

            GUIStyle noteStyle = new(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(cx + 10, cy + 10, w - 20, 25), "UKIRAN DINDING LABORATORIUM GEOLOGI", titleStyle);

            if (isRian)
            {
                float exp = player != null ? player.CurrentExposure : 0f;
                var band = ExposureCalculator.GetBand(exp);
                string[] glyphs = GetGlyphsForBand(band);

                string display = $"[ {glyphs[0]} ]     [ {glyphs[1]} ]     [ {glyphs[2]} ]";
                GUI.Label(new Rect(cx + 10, cy + 45, w - 20, 50), display, glyphStyle);

                string hint = band switch
                {
                    ExposureCalculator.ExposureBand.Distorted => "Glif kedua tampak kabur/ganda akibat paparan CO! Konfirmasikan ke rekan.",
                    ExposureCalculator.ExposureBand.Hallucinating => "Penglihatan sangat terdistorsi! Glif berbayang dan sulit dibaca.",
                    _ => "Glif terbaca jelas di bawah senter tambang Rian."
                };
                GUI.Label(new Rect(cx + 10, cy + 105, w - 20, 25), hint, noteStyle);
            }
            else
            {
                GUI.Label(new Rect(cx + 10, cy + 55, w - 20, 30), "[ Glif Tertutup Debu Vulkanik ]", glyphStyle);
                GUI.Label(new Rect(cx + 10, cy + 95, w - 20, 45),
                    "Ukirannya terlalu tipis. Hanya senter spektrum tinggi Rian yang bisa memunculkan kontrasnya.\nGunakan Bagan Unsur Kimia (C) untuk menerjemahkan.",
                    noteStyle);
            }

            GUI.Label(new Rect(cx + 10, cy + 130, w - 20, 20), "Tekan [E] atau [Esc] untuk menutup", noteStyle);
        }

        public static string[] GetGlyphsForBand(ExposureCalculator.ExposureBand band)
        {
            return band switch
            {
                ExposureCalculator.ExposureBand.Distorted => new[] { "α", "δ?", "λ" },
                ExposureCalculator.ExposureBand.Hallucinating => new[] { "?", "?", "?" },
                _ => new[] { "α", "σ", "λ" }
            };
        }
    }
}
