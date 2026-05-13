using UnityEngine;

/// <summary>
/// 持枪（<see cref="PlayerReusableData.armedModeActive"/>）时显示一组 HUD，非持枪时隐藏。
/// 用于「GunHUD」根或武器 UI 父物体；<see cref="gunHudRoots"/> 留空时对父物体下所有第一层子物体统一显隐。
/// </summary>
[DisallowMultipleComponent]
public class GunHudVisibility : MonoBehaviour
{
    [SerializeField, Tooltip("留空则在运行时查找场景中的 Player。")]
    private Player player;

    [SerializeField, Tooltip("持枪时要显示的目标；留空则对本物体 Transform 下每一个第一层子物体 SetActive。")]
    private GameObject[] gunHudRoots;

    private Transform _parent;

    private void Awake()
    {
        _parent = transform;
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        player = FindObjectOfType<Player>();
    }

    private void LateUpdate()
    {
        ResolvePlayer();

        bool show = player != null &&
                    player.ReusableData != null &&
                    player.ReusableData.armedModeActive;

        if (gunHudRoots != null && gunHudRoots.Length > 0)
        {
            for (var i = 0; i < gunHudRoots.Length; i++)
            {
                var go = gunHudRoots[i];
                if (go != null && go.activeSelf != show)
                {
                    go.SetActive(show);
                }
            }

            return;
        }

        for (var c = 0; c < _parent.childCount; c++)
        {
            var child = _parent.GetChild(c).gameObject;
            if (child.activeSelf != show)
            {
                child.SetActive(show);
            }
        }
    }
}
