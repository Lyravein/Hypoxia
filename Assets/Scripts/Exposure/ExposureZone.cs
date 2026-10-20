using System.Collections.Generic;
using UnityEngine;

namespace Hypoxia
{
    public enum ExposureZoneType
    {
        LeakingPipe,
        OpenAir
    }

    [RequireComponent(typeof(Collider))]
    public sealed class ExposureZone : MonoBehaviour
    {
        public static readonly List<ExposureZone> AllZones = new();

        [SerializeField] private ExposureZoneType zoneType = ExposureZoneType.LeakingPipe;

        private Collider zoneCollider;

        public ExposureZoneType ZoneType
        {
            get => zoneType;
            set => zoneType = value;
        }

        private void OnEnable()
        {
            zoneCollider = GetComponent<Collider>();
            if (!AllZones.Contains(this))
            {
                AllZones.Add(this);
            }
        }

        private void OnDisable()
        {
            AllZones.Remove(this);
        }

        public bool Contains(Vector3 position)
        {
            if (zoneCollider == null) zoneCollider = GetComponent<Collider>();
            return zoneCollider != null && zoneCollider.bounds.Contains(position);
        }

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }
    }
}
