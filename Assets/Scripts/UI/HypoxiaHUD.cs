using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Hypoxia
{
    [DisallowMultipleComponent]
    public sealed class HypoxiaHUD : MonoBehaviour
    {
        private string serverIp = "127.0.0.1";
        private ushort port = 7777;
        private Color currentBarColor = new(0.2f, 0.85f, 0.4f);
        private Texture2D whiteTexture;
        private bool showDebugPanel = false;
        private bool showChemicalChart = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureHUD()
        {
            if (FindAnyObjectByType<HypoxiaHUD>() == null && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.gameObject.AddComponent<HypoxiaHUD>();
            }
        }

        private void Awake()
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        private void OnDestroy()
        {
            if (whiteTexture != null)
            {
                Destroy(whiteTexture);
            }
        }

        private void Update()
        {
            NetworkManager nm = NetworkManager.Singleton;
            bool isInGame = nm != null && (nm.IsClient || nm.IsServer) && !PlayerExposure.IsAnyPlayerPassedOut();

            if (isInGame)
            {
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
                    {
                        showChemicalChart = !showChemicalChart;
                    }

                    if (UnityEngine.InputSystem.Keyboard.current.f4Key.wasPressedThisFrame)
                    {
                        showDebugPanel = !showDebugPanel;
                        if (showDebugPanel)
                        {
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                        }
                    }

                    if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                        Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
                    }
                }

                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked && !showDebugPanel)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnGUI()
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null) return;

            if (!nm.IsClient && !nm.IsServer)
            {
                DrawConnectionMenu(nm);
                return;
            }

            DrawNetworkStatus(nm);

            PlayerExposure player = PlayerExposure.LocalInstance;
            if (player != null)
            {
                DrawExposureBar(player);
                DrawOxygenInventory(player);
                DrawInteractionPrompt(player);
                if (showChemicalChart && !player.IsRian)
                {
                    DrawChemicalChart(player);
                }
            }

            if (PlayerExposure.IsAnyPlayerPassedOut())
            {
                DrawGameOverScreen(player);
            }
        }

        private void DrawConnectionMenu(NetworkManager nm)
        {
            GUI.Box(new Rect(20, 20, 300, 160), "HypoXia: Pulau Halusinasi - LAN");

            GUI.Label(new Rect(35, 55, 60, 25), "Host IP:");
            serverIp = GUI.TextField(new Rect(95, 55, 205, 25), serverIp);

            if (GUI.Button(new Rect(35, 90, 265, 30), "Host Sesi (LAN)"))
            {
                ConfigureTransport(true, serverIp);
                nm.StartHost();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (GUI.Button(new Rect(35, 125, 265, 30), "Gabung Sesi (LAN)"))
            {
                ConfigureTransport(false, serverIp);
                nm.StartClient();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void ConfigureTransport(bool isHost, string ip)
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                if (isHost)
                {
                    // Listen on 0.0.0.0 so all LAN adapters accept connections
                    transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
                }
                else
                {
                    transport.SetConnectionData(ip, port);
                }
            }
        }

        private void DrawNetworkStatus(NetworkManager nm)
        {
            string status = nm.IsHost ? "Host (LAN 7777)" : $"Klien ({serverIp})";
            GUI.Label(new Rect(Screen.width - 160, 10, 150, 25), $"Status: {status}");
        }

        private void DrawExposureBar(PlayerExposure player)
        {
            float exposure = player.CurrentExposure;
            ExposureCalculator.ExposureBand band = ExposureCalculator.GetBand(exposure);

            // A2: Color shift at 50% and 75% thresholds
            Color targetColor = band switch
            {
                ExposureCalculator.ExposureBand.Distorted => new Color(1.0f, 0.78f, 0.1f),     // Amber
                ExposureCalculator.ExposureBand.Hallucinating => new Color(0.95f, 0.18f, 0.18f), // Red
                _ => new Color(0.2f, 0.85f, 0.4f)                                                // Green/Normal
            };

            // Responsive within 1s lerp
            currentBarColor = Color.Lerp(currentBarColor, targetColor, Time.deltaTime * 6f);

            float barWidth = 260f;
            float barHeight = 16f;
            float x = 20f;
            float y = 35f;

            GUI.Label(new Rect(x, 12f, 300f, 20f), $"{player.CharacterName} - Paparan CO");

            // Background
            Color oldColor = GUI.color;
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), whiteTexture);

            // Filled bar (diegetic minimal: no raw numbers)
            GUI.color = currentBarColor;
            float fill = Mathf.Clamp01(exposure / ExposureCalculator.MaxExposure);
            GUI.DrawTexture(new Rect(x + 2, y + 2, (barWidth - 4) * fill, barHeight - 4), whiteTexture);

            GUI.color = oldColor;
        }

        private void DrawOxygenInventory(PlayerExposure player)
        {
            int tanks = player.RemainingTanks;
            string tankStr = "";
            for (int i = 0; i < 3; i++)
            {
                tankStr += i < tanks ? "[O2] " : "[--] ";
            }

            string flashlightStr = player.IsFlashlightOn.Value ? "ON" : "OFF";
            string chartHint = !player.IsRian ? "  |  [C] Bagan Kimia" : "";
            GUI.Label(new Rect(20, 56, 450, 25), $"{tankStr}  |  [Q] O2  |  [F] Senter: {flashlightStr}{chartHint}");
            GUI.Label(new Rect(20, 78, 400, 20), "Debug: [F1] 0%  [F2] 50%  [F3] 75%  |  [F4] Panel Modul 2");

            if (showDebugPanel)
            {
                DrawProceduralDebugPanel();
            }
        }

        private void DrawProceduralDebugPanel()
        {
            float panelWidth = 320f;
            float panelHeight = 220f;
            float x = Screen.width - panelWidth - 20f;
            float y = 45f;

            Color oldColor = GUI.color;
            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), whiteTexture);
            GUI.color = oldColor;

            GUI.Label(new Rect(x + 15, y + 10, panelWidth - 30, 25), "<b>Panel Debug Modul 2 & 5</b>");

            // Slider Terrain (E4)
            ProceduralTerrain terrain = FindAnyObjectByType<ProceduralTerrain>();
            if (terrain != null)
            {
                GUI.Label(new Rect(x + 15, y + 40, panelWidth - 30, 20), $"Tinggi Terrain Dermaga: {terrain.HeightMultiplier:F2}");
                float newH = GUI.HorizontalSlider(new Rect(x + 15, y + 62, panelWidth - 30, 20), terrain.HeightMultiplier, 0f, 6f);
                if (!Mathf.Approximately(newH, terrain.HeightMultiplier)) terrain.HeightMultiplier = newH;
            }

            // Slider L-System (E5)
            LSystemGenerator lsystem = FindAnyObjectByType<LSystemGenerator>();
            if (lsystem != null)
            {
                GUI.Label(new Rect(x + 15, y + 90, panelWidth - 30, 20), $"Sudut Cabang L-System: {lsystem.BranchAngle:F0}°");
                float newAngle = GUI.HorizontalSlider(new Rect(x + 15, y + 112, panelWidth - 30, 20), lsystem.BranchAngle, 10f, 50f);
                if (!Mathf.Approximately(newAngle, lsystem.BranchAngle)) lsystem.BranchAngle = newAngle;

                GUI.Label(new Rect(x + 15, y + 138, panelWidth - 30, 20), $"Panjang Cabang L-System: {lsystem.BranchLength:F2}m");
                float newLen = GUI.HorizontalSlider(new Rect(x + 15, y + 160, panelWidth - 30, 20), lsystem.BranchLength, 0.3f, 1.8f);
                if (!Mathf.Approximately(newLen, lsystem.BranchLength)) lsystem.BranchLength = newLen;

                string nextPresetLabel = lsystem.Preset == LSystemRulePreset.MossTendril ? "Switch: Wall Crack" : "Switch: Moss Tendril";
                if (GUI.Button(new Rect(x + 15, y + 185, panelWidth - 30, 25), nextPresetLabel))
                {
                    lsystem.Preset = lsystem.Preset == LSystemRulePreset.MossTendril ? LSystemRulePreset.WallCrack : LSystemRulePreset.MossTendril;
                }
            }
        }

        private void DrawInteractionPrompt(PlayerExposure player)
        {
            if (!player.TryGetComponent(out PlayerInteractor interactor) || interactor.Hovered == null) return;

            GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.55f, 300f, 25f), interactor.Hovered.InteractionPrompt);
        }

        private void DrawChemicalChart(PlayerExposure player)
        {
            float w = 340f;
            float h = 185f;
            float x = 20f;
            float y = 110f;

            Color oldColor = GUI.color;
            GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, w, h), whiteTexture);
            GUI.color = oldColor;

            GUI.Label(new Rect(x + 15, y + 10, w - 30, 25), "<b>BAGAN UNSUR KIMIA (MAYA)</b>");
            GUI.Label(new Rect(x + 15, y + 38, w - 30, 20), "•  Glif α (Karbon Aktif)       = <b>4</b>");
            GUI.Label(new Rect(x + 15, y + 62, w - 30, 20), "•  Glif σ (Oksigen Terikat)  = <b>2</b>");
            GUI.Label(new Rect(x + 15, y + 86, w - 30, 20), "•  Glif λ (Gas Belerang)     = <b>8</b>");
            GUI.Label(new Rect(x + 15, y + 112, w - 30, 40), "<color=#e0a020>Catatan: δ = 9 (memiliki garis tegak, jangan tertukar dengan σ yang melengkung).</color>");
            GUI.Label(new Rect(x + 15, y + 155, w - 30, 20), "Tekan [C] untuk menutup");
        }

        private void DrawGameOverScreen(PlayerExposure player)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.12f, 0.02f, 0.02f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
            GUI.color = oldColor;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle subtitleStyle = new(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(cx - 200, cy - 80, 400, 40), "SESI GAGAL", titleStyle);
            GUI.Label(new Rect(cx - 250, cy - 35, 500, 30), "Karakter pingsan akibat paparan karbon monoksida (100%).", subtitleStyle);

            if (GUI.Button(new Rect(cx - 120, cy + 20, 240, 40), "Mulai Ulang dari Checkpoint"))
            {
                if (player != null)
                {
                    player.RestartSessionServerRpc();
                }
            }
        }
    }
}
