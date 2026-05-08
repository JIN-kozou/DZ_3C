namespace SnapshotShaders.URP
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class WorldScanExample : MonoBehaviour
    {
        [SerializeField] private float scanSpeed;
        [SerializeField] private float scanDuration;

        [SerializeField] private Volume worldScanVolume;
        private WorldScanSettings worldScanSettings;

        private void Start()
        {
            if(worldScanVolume == null || worldScanVolume.profile == null)
            {
                return;
            }
            worldScanVolume.profile.TryGet(out worldScanSettings);
        }

        private void Update()
        {
            if(worldScanSettings != null)
            {
                var t = Time.time % scanDuration;
                var distance = t * scanSpeed;

                worldScanSettings.scanDistance.value = distance;
            }
        }
    }
}
