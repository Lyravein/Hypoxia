using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypoxia
{
    /// <summary>
    /// Raycast interaksi milik pemain lokal. Hanya mengirim permintaan; host yang memutuskan (B2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float reach = 3.5f;

        private NetworkObject networkObject;

        public IllusionEntity Hovered { get; private set; }

        private void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
        }

        private void Update()
        {
            if (networkObject != null && (!networkObject.IsSpawned || !networkObject.IsOwner)) return;

            PlayerExposure local = PlayerExposure.LocalInstance;
            if (local != null && local.PassedOut) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Hovered = null;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, reach))
            {
                Hovered = hit.collider.GetComponentInParent<IllusionEntity>();
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && Hovered != null)
            {
                Hovered.RequestInteract();
            }
        }
    }
}
