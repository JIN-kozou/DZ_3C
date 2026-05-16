using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Player builds default to the Performant URP tier unless Quality per-platform is set.
/// Forces High Fidelity (HDR + full renderer features) and enables post-processing on all cameras.
/// </summary>
public static class UrpRenderingBuildBootstrap
{
    const string HighFidelityQualityName = "High Fidelity";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureHighFidelityQuality()
    {
        int targetIndex = QualitySettings.GetQualityLevel();
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] != HighFidelityQualityName)
                continue;
            targetIndex = i;
            break;
        }

        if (QualitySettings.GetQualityLevel() == targetIndex)
            return;

        QualitySettings.SetQualityLevel(targetIndex, applyExpensiveChanges: true);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ConfigureSceneCameras()
    {
        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null)
                continue;

            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            if (data == null)
                continue;

            data.renderPostProcessing = true;
            if (data.volumeLayerMask.value == 0)
                data.volumeLayerMask = 1;
        }
    }
}
