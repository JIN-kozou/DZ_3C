using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ObjectActiveGroupManager : MonoBehaviour
{
    [System.Serializable]
    public class ActiveItem
    {
        [Header("显示用名字")]
        public string itemName;

        [Header("要控制的物体")]
        public GameObject target;

        [Header("这个物体自己的开关")]
        public bool active = true;
    }

    [System.Serializable]
    public class ActiveGroup
    {
        [Header("组名")]
        public string groupName;

        [Header("整组开关")]
        public bool groupActive = true;

        [Header("组内物体")]
        public List<ActiveItem> items = new List<ActiveItem>();
    }

    [Header("所有分组")]
    public List<ActiveGroup> groups = new List<ActiveGroup>();

    private void OnValidate()
    {
        ApplyAll();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyAll();
        }
#endif
    }

    public void ApplyAll()
    {
        foreach (ActiveGroup group in groups)
        {
            if (group == null) continue;

            foreach (ActiveItem item in group.items)
            {
                if (item == null || item.target == null) continue;

                bool finalActive = group.groupActive && item.active;

                if (item.target.activeSelf != finalActive)
                {
                    item.target.SetActive(finalActive);
                }
            }
        }
    }
}