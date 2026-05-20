using System;
using UnityEngine;

namespace DZ_3C.AI.HTN
{
    [DisallowMultipleComponent]
    public class CheckpointCounter : MonoBehaviour
    {
        public event Action<CheckpointCounter> OnPassCountChanged;

        [SerializeField] private int passCount;
        public int PassCount => passCount;

        public void RegisterPass()
        {
            passCount++;
            OnPassCountChanged?.Invoke(this);
        }
    }
}
