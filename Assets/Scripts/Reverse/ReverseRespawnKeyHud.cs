using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 控制 HUD 上 T 键提示：仅在有复活点（阵列 / Battery 存档）时显示。
    /// 挂在 FQRXsign（或任意常驻激活物体）上，tKeyRoot 指向子物体 T。
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseRespawnKeyHud : MonoBehaviour
    {
        [SerializeField] private GameObject tKeyRoot;
        [SerializeField] private ReverseCoreStack coreStack;

        private bool lastShown;

        private void Awake()
        {
            if (tKeyRoot == null)
            {
                Transform t = transform.Find("T");
                if (t != null)
                {
                    tKeyRoot = t.gameObject;
                }
            }

            if (coreStack == null)
            {
                coreStack = FindObjectOfType<ReverseCoreStack>();
            }
        }

        private void OnEnable()
        {
            ReverseRespawnAvailability.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            ReverseRespawnAvailability.Changed -= Refresh;
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            bool show = ReverseRespawnAvailability.HasRespawnPoint(coreStack);
            if (show == lastShown)
            {
                return;
            }

            lastShown = show;
            if (tKeyRoot != null)
            {
                tKeyRoot.SetActive(show);
            }
        }
    }
}
