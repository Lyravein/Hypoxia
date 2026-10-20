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
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                    Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
                }
                else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
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
            GUI.Label(new Rect(20, 56, 350, 25), $"{tankStr}  |  [Q] O2  |  [F] Senter: {flashlightStr}");
            GUI.Label(new Rect(20, 78, 300, 20), "Debug Demo: [F1] 0%  [F2] 50%  [F3] 75%");
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
