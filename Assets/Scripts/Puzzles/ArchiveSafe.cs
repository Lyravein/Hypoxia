using Unity.Netcode;
using UnityEngine;

namespace Hypoxia
{
    /// <summary>
    /// C1: Brankas Arsip Laboratorium (Teka-teki P1).
    /// Membutuhkan kode 3 angka yang diterjemahkan dari glif dinding via bagan unsur kimia.
    /// Verifikasi kode server-authoritative via RPC ke Host.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArchiveSafe : NetworkBehaviour, IInteractable
    {
        public const string DefaultTargetCode = "428";

        [SerializeField] private string targetCode = DefaultTargetCode;
        [SerializeField] private Transform doorTransform;
        [SerializeField] private Collider blockingCollider;

        public NetworkVariable<bool> IsUnlocked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool isKeypadOpen;
        private string enteredCode = "";
        private string feedbackMessage = "";
        private float feedbackTimer;

        public bool IsKeypadOpen => isKeypadOpen;

        public string InteractionPrompt => IsUnlocked.Value
            ? "Brankas Arsip [Terbuka - Dokumen Riset Ditemukan]"
            : "[E] Buka Keypad Brankas Arsip";

        public void Interact()
        {
            if (IsUnlocked.Value) return;

            isKeypadOpen = !isKeypadOpen;
            if (isKeypadOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                feedbackMessage = "";
            }
        }

        public override void OnNetworkSpawn()
        {
            IsUnlocked.OnValueChanged += OnUnlockedChanged;
            ApplyDoorState(IsUnlocked.Value);
        }

        public override void OnNetworkDespawn()
        {
            IsUnlocked.OnValueChanged -= OnUnlockedChanged;
        }

        private void OnUnlockedChanged(bool prev, bool current)
        {
            ApplyDoorState(current);
            if (current)
            {
                isKeypadOpen = false;
            }
        }

        private void ApplyDoorState(bool unlocked)
        {
            // ponytail: rotasi pintu brankas instan 100 derajat. Upgrade ke animasi fisika/lerp jika diperlukan.
            if (doorTransform != null)
            {
                doorTransform.localRotation = unlocked ? Quaternion.Euler(0f, -100f, 0f) : Quaternion.identity;
            }

            if (blockingCollider != null)
            {
                blockingCollider.enabled = !unlocked;
            }
        }

        private void Update()
        {
            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f) feedbackMessage = "";
            }

            if (isKeypadOpen && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isKeypadOpen = false;
            }
        }

        private void OnGUI()
        {
            if (!isKeypadOpen || IsUnlocked.Value) return;

            float w = 260f;
            float h = 320f;
            float cx = (Screen.width - w) * 0.5f;
            float cy = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(cx, cy, w, h), "<b>KEYPAD BRANKAS ARSIP</b>");

            string display = string.IsNullOrEmpty(enteredCode) ? "- - -" : enteredCode.PadRight(3, '-');
            GUIStyle codeStyle = new(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(cx + 20, cy + 30, w - 40, 40), display, codeStyle);

            if (!string.IsNullOrEmpty(feedbackMessage))
            {
                GUIStyle fbStyle = new(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = feedbackMessage == "KODE BENAR" ? Color.green : Color.red }
                };
                GUI.Label(new Rect(cx + 20, cy + 70, w - 40, 20), feedbackMessage, fbStyle);
            }

            // Grid tombol 1-9
            float btnW = 60f;
            float btnH = 35f;
            float startX = cx + 35f;
            float startY = cy + 95f;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int digit = r * 3 + c + 1;
                    if (GUI.Button(new Rect(startX + c * 65f, startY + r * 40f, btnW, btnH), digit.ToString()))
                    {
                        if (enteredCode.Length < 3) enteredCode += digit;
                    }
                }
            }

            // Baris bawah: Hapus, 0, Buka
            if (GUI.Button(new Rect(startX, startY + 120f, btnW, btnH), "C"))
            {
                enteredCode = "";
                feedbackMessage = "";
            }

            if (GUI.Button(new Rect(startX + 65f, startY + 120f, btnW, btnH), "0"))
            {
                if (enteredCode.Length < 3) enteredCode += "0";
            }

            if (GUI.Button(new Rect(startX + 130f, startY + 120f, btnW, btnH), "ENTER"))
            {
                SubmitCode(enteredCode);
            }

            if (GUI.Button(new Rect(cx + 35f, cy + 270f, w - 70f, 30f), "Tutup (Esc)"))
            {
                isKeypadOpen = false;
            }
        }

        public void SubmitCode(string code)
        {
            if (string.IsNullOrEmpty(code) || !IsSpawned) return;
            SubmitCodeRpc(code);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitCodeRpc(string code)
        {
            if (IsUnlocked.Value) return;

            bool success = ValidateCode(code, targetCode);
            if (success)
            {
                IsUnlocked.Value = true;
            }
            else
            {
                ReportFailedAttemptRpc();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReportFailedAttemptRpc()
        {
            feedbackMessage = "KODE SALAH";
            feedbackTimer = 2.0f;
            enteredCode = "";
        }

        public static bool ValidateCode(string input, string target)
        {
            return !string.IsNullOrEmpty(input) && input.Trim() == target.Trim();
        }
    }
}
