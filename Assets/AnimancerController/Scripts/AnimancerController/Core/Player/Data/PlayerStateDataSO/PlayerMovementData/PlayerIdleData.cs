using Animancer;
using System;
using UnityEngine;

[Serializable]
public class PlayerIdleData
{
    [field: SerializeField] public TransitionAsset idle { get; private set; }
}
