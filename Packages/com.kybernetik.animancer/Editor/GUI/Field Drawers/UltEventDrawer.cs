// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2024 Kybernetik //

#if UNITY_EDITOR && ULT_EVENTS

using UnityEditor;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] <see cref="PropertyDrawer"/> for <see cref="UltEvent"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/UltEventDrawer
    [CustomPropertyDrawer(typeof(UltEvent), true)]
    public class UltEventDrawer : UltEvents.Editor.UltEventDrawer,
        PropertyDrawers.IDiscardOnSelectionChange
    { }
}

#endif

