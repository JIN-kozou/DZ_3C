using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DZ_3C.MachineRepair
{
    /// <summary>
    /// 当 Inspector 中列出的所有接收器需求均满足时，加载指定场景。
    /// 挂在场景任意物体上，拖入全部接收器并填写目标场景名（须加入 Build Settings）。
    /// </summary>
    [DisallowMultipleComponent]
    public class MachinePartReceiversSceneGate : MonoBehaviour
    {
        private static readonly List<MachinePartReceiversSceneGate> ActiveGates = new();

        [SerializeField] private List<MachinePartReceiver> receivers = new();
        [SerializeField] private string sceneToLoad;
        [SerializeField] private bool loadOnlyOnce = true;

        private bool hasTriggered;

        public IReadOnlyList<MachinePartReceiver> Receivers => receivers;

        private void OnEnable()
        {
            if (!ActiveGates.Contains(this))
            {
                ActiveGates.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveGates.Remove(this);
        }

        /// <summary>零件提交后由 <see cref="MachinePartReceiver"/> 调用。</summary>
        public static void NotifyReceiverProgressChanged()
        {
            for (int i = ActiveGates.Count - 1; i >= 0; i--)
            {
                MachinePartReceiversSceneGate gate = ActiveGates[i];
                if (gate == null)
                {
                    ActiveGates.RemoveAt(i);
                    continue;
                }

                gate.TryLoadSceneIfComplete();
            }
        }

        public void TryLoadSceneIfComplete()
        {
            if (loadOnlyOnce && hasTriggered)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                return;
            }

            if (!AreAllReceiversComplete())
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
            {
                Debug.LogError(
                    $"[MachineRepair] Scene gate on '{name}': scene '{sceneToLoad}' is not in Build Settings or name is invalid.");
                return;
            }

            hasTriggered = true;
            Debug.Log($"[MachineRepair] All receivers complete. Loading scene '{sceneToLoad}'.");
            SceneManager.LoadScene(sceneToLoad);
        }

        public bool AreAllReceiversComplete()
        {
            if (receivers == null || receivers.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < receivers.Count; i++)
            {
                MachinePartReceiver receiver = receivers[i];
                if (receiver == null)
                {
                    return false;
                }

                if (!receiver.AreAllRequirementsSatisfied())
                {
                    return false;
                }
            }

            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("Collect All Receivers In Scene")]
        private void CollectAllReceiversInScene()
        {
            receivers.Clear();
            MachinePartReceiver[] found = FindObjectsOfType<MachinePartReceiver>(true);
            receivers.AddRange(found);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[MachineRepair] Scene gate: collected {receivers.Count} receiver(s) on '{name}'.");
        }
#endif
    }
}
