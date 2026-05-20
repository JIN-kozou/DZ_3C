using UnityEngine;

/// <summary>
/// 将枪模根（挂在手腕/手骨骼下的子物体）拖到 <see cref="weaponModelRoot"/>，
/// 由 <see cref="PlayerArmedPresentation"/> 在掏枪/收枪帧调用 <see cref="IArmedWeaponModelVisibility"/>。
/// </summary>
[DisallowMultipleComponent]
public class ArmedWeaponModelRootVisibility : MonoBehaviour, IArmedWeaponModelVisibility
{
    [SerializeField, Tooltip("枪模型根物体（通常为手骨骼子节点下的整枪或仅渲染用子物体）。")]
    private GameObject weaponModelRoot;

    public void SetWeaponModelVisible(bool visible)
    {
        if (weaponModelRoot != null)
        {
            weaponModelRoot.SetActive(visible);
        }
    }
}
