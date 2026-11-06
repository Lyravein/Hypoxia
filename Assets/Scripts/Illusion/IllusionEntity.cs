using Unity.Netcode;
using UnityEngine;

namespace Hypoxia
{
    /// <summary>
    /// Satu entitas fisik dengan beberapa wujud visual. Wujud yang tampil mengikuti band paparan
    /// pemain lokal (B1); collider dan titik interaksi tinggal di root dan tidak pernah berubah.
    /// Interaksi dikirim ke host dan diverifikasi pada objek asli (B2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IllusionEntity : NetworkBehaviour, IInteractable
    {
        [Tooltip("Wujud per band: 0 Normal, 1 Terdistorsi, 2 Halusinasi berat.")]
        [SerializeField] private GameObject[] formsByBand = new GameObject[3];

        [SerializeField] private bool isCorrectChoice;

        [SerializeField] private GateController gateToOpen;

        [SerializeField] private Transform handleTransform;

        public NetworkVariable<bool> IsSolved = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private int appliedForm = -1;

        public bool IsCorrectChoice => isCorrectChoice;

        public string InteractionPrompt
        {
            get
            {
                if (IsSolved.Value) return "Tuas [Sudah Ditarik]";
                PlayerExposure local = PlayerExposure.LocalInstance;
                float exposure = local != null ? local.CurrentExposure : 0f;
                var band = ExposureCalculator.GetBand(exposure);
                if (band == ExposureCalculator.ExposureBand.Hallucinating)
                {
                    return "[E] Tarik Patung Obor";
                }
                return isCorrectChoice ? "[E] Tarik Tuas Listrik" : "[E] Periksa Manekin Rusak";
            }
        }

        public void Interact() => RequestInteract();

        public static int FormIndexForBand(ExposureCalculator.ExposureBand band, int formCount)
        {
            if (formCount <= 0) return -1;
            return Mathf.Clamp((int)band, 0, formCount - 1);
        }

        public override void OnNetworkSpawn()
        {
            appliedForm = -1;
            ApplyLocalForm();
            IsSolved.OnValueChanged += OnSolvedChanged;
            ApplyHandle(IsSolved.Value);
        }

        public override void OnNetworkDespawn()
        {
            IsSolved.OnValueChanged -= OnSolvedChanged;
        }

        private void OnSolvedChanged(bool previous, bool current) => ApplyHandle(current);

        private void ApplyHandle(bool solved)
        {
            // ponytail: rotasi tuas instan 45 derajat. Upgrade ke interpolasi halus jika diperlukan.
            if (handleTransform != null)
            {
                handleTransform.localRotation = solved ? Quaternion.Euler(45f, 0f, 0f) : Quaternion.identity;
            }
        }

        private void Update()
        {
            ApplyLocalForm();
        }

        private void ApplyLocalForm()
        {
            PlayerExposure local = PlayerExposure.LocalInstance;
            float exposure = local != null ? local.CurrentExposure : 0f;
            int form = FormIndexForBand(ExposureCalculator.GetBand(exposure), formsByBand.Length);
            if (form == appliedForm) return;

            appliedForm = form;
            for (int i = 0; i < formsByBand.Length; i++)
            {
                if (formsByBand[i] != null) formsByBand[i].SetActive(i == form);
            }
        }

        public void RequestInteract()
        {
            if (!IsSpawned) return;
            RequestInteractRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestInteractRpc()
        {
            if (IsSolved.Value) return;

            // Host memverifikasi pada objek asli, bukan pada wujud yang dilihat klien.
            if (!isCorrectChoice) return;

            IsSolved.Value = true;
            if (gateToOpen != null) gateToOpen.OpenServer();
        }
    }
}
