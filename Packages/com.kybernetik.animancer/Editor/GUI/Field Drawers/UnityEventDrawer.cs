// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2024 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] <see cref="PropertyDrawer"/> for <see cref="UnityEvent"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/UnityEventDrawer
    [CustomPropertyDrawer(typeof(UnityEvent), true)]
    public class UnityEventDrawer : UnityEditorInternal.UnityEventDrawer { }
}

#endif

