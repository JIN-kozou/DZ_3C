#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Appear + sphere logic lives in <c>Assets/Shader/AppearShader.shadergraph</c>.
/// Reveal along UV: add Blackboard floats <c>_Reveal</c>, <c>_RevealSoft</c> and a Custom Function calling <c>AppearRevealLib.hlsl</c>, then multiply into Surface Alpha after existing Appear/clip path.
/// </summary>
public static class AppearRevealShaderOrderMenu
{
    const string LibPath = "Assets/Shader/AppearRevealLib.hlsl";
    const string GraphPath = "Assets/Shader/AppearShader.shadergraph";
    const string GdPath = "Assets/Magic_Spline/Shaders/GD_UniversalPremultiplited.shader";

    [MenuItem("Tools/Magic Spline/Verify AppearShader Reveal Setup")]
    public static void VerifyAppearShaderPipeline()
    {
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), LibPath)))
            Debug.LogError($"Missing {LibPath} (Reveal mask for Shader Graph Custom Function).");
        else
            Debug.Log($"{LibPath}: OK.");

        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), GraphPath)))
            Debug.LogError($"Missing {GraphPath}.");
        else
            Debug.Log($"{GraphPath}: open in Shader Graph; wire Appear first, then multiply Reveal mask into Alpha (see AppearRevealLib.hlsl).");

        string gd = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), GdPath));
        if (gd.Contains("clip(-1)"))
            Debug.LogWarning($"{GdPath} still contains clip(-1); particle shaders should stay free of Appear — use AppearShader materials for reveal spheres.");
        else
            Debug.Log($"{GdPath}: no appear clip (OK for standalone flipbook).");

        Debug.Log("AppearRevealShaderOrderMenu: done. Use Shader Graphs_AppearShader material + ShaderPosition + AppearRevealGate for gameplay.");
    }
}
#endif
