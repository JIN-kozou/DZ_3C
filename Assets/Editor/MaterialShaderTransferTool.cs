using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialShaderTransferTool : EditorWindow
{
    private Material targetMaterial;
    private Shader newShader;
    private string folderPath = "Assets";

    [MenuItem("Tools/Material Shader Transfer Tool")]
    public static void ShowWindow()
    {
        GetWindow<MaterialShaderTransferTool>("Shader Transfer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Shader Transfer Tool", EditorStyles.boldLabel);

        newShader = (Shader)EditorGUILayout.ObjectField("New Shader", newShader, typeof(Shader), false);

        GUILayout.Space(10);

        targetMaterial = (Material)EditorGUILayout.ObjectField("Single Material", targetMaterial, typeof(Material), false);

        if (GUILayout.Button("单个材质测试"))
        {
            ChangeShader(targetMaterial);
            AssetDatabase.SaveAssets();
        }

        GUILayout.Space(10);

        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);

        if (GUILayout.Button("批量处理文件夹中的材质"))
        {
            BatchProcessFolder();
        }

        if (GUILayout.Button("处理当前选中的材质/物体"))
        {
            BatchProcessSelection();
        }
    }

    private void ChangeShader(Material mat)
    {
        if (mat == null || newShader == null)
        {
            Debug.LogWarning("请先指定 Material 和 Shader");
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("Unity 正在编译或更新，请稍后再试！");
            return;
        }

        Undo.RecordObject(mat, "Change Shader And Keep Material Data");

        MaterialData data = SaveMaterialData(mat);

        mat.shader = newShader;

        ApplyMaterialData(mat, data);

        EditorUtility.SetDirty(mat);
    }

    private void BatchProcessFolder()
    {
        if (newShader == null)
        {
            Debug.LogWarning("请先选择 Shader");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            EditorUtility.DisplayProgressBar(
                "Batch Processing Materials",
                mat.name,
                guids.Length == 0 ? 1 : (float)i / guids.Length
            );

            ChangeShader(mat);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        Debug.Log("文件夹批量处理完成");
    }

    private void BatchProcessSelection()
    {
        if (newShader == null)
        {
            Debug.LogWarning("请先选择 Shader");
            return;
        }

        HashSet<Material> mats = new HashSet<Material>();

        foreach (Object obj in Selection.objects)
        {
            if (obj is Material mat)
            {
                mats.Add(mat);
            }
            else if (obj is GameObject go)
            {
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer r in renderers)
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m != null) mats.Add(m);
                    }
                }
            }
        }

        int index = 0;
        foreach (Material mat in mats)
        {
            EditorUtility.DisplayProgressBar(
                "Processing Selected Materials",
                mat.name,
                mats.Count == 0 ? 1 : (float)index / mats.Count
            );

            ChangeShader(mat);
            index++;
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        Debug.Log("选中对象处理完成");
    }

    private class MaterialData
    {
        public Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        public Dictionary<string, Color> colors = new Dictionary<string, Color>();
        public Dictionary<string, float> floats = new Dictionary<string, float>();
    }

    private MaterialData SaveMaterialData(Material mat)
    {
        MaterialData data = new MaterialData();

        string[] textureSlots =
        {
            "_MainTex", "_BaseMap", "_BaseColorMap", "_AlbedoMap",
            "_BumpMap", "_NormalMap",
            "_MetallicGlossMap", "_MetallicMap",
            "_SpecGlossMap", "_SpecularMap",
            "_OcclusionMap",
            "_EmissionMap",
            "_ParallaxMap",
            "_MaskMap"
        };

        string[] colorSlots =
        {
            "_Color",
            "_BaseColor",
            "_EmissionColor"
        };

        string[] floatSlots =
        {
            "_Metallic",
            "_Smoothness",
            "_Glossiness",
            "_BumpScale",
            "_NormalStrength",
            "_OcclusionStrength",
            "_Parallax",
            "_Cutoff",
            "_Surface",
            "_Blend",
            "_AlphaClip",
            "_SmoothnessTextureChannel"
        };

        foreach (string slot in textureSlots)
        {
            if (mat.HasProperty(slot))
            {
                Texture tex = mat.GetTexture(slot);
                if (tex != null) data.textures[slot] = tex;
            }
        }

        foreach (string slot in colorSlots)
        {
            if (mat.HasProperty(slot))
            {
                data.colors[slot] = mat.GetColor(slot);
            }
        }

        foreach (string slot in floatSlots)
        {
            if (mat.HasProperty(slot))
            {
                data.floats[slot] = mat.GetFloat(slot);
            }
        }

        // 从 HDR Emission Color 里估算 Emission Intensity
        if (mat.HasProperty("_EmissionColor"))
        {
            Color emissionColor = mat.GetColor("_EmissionColor");
            float intensity = Mathf.Max(emissionColor.r, emissionColor.g, emissionColor.b);
            data.floats["_EmissionIntensity"] = intensity;
        }

        return data;
    }

    private void ApplyMaterialData(Material mat, MaterialData data)
    {
        TrySetTexture(mat, data.textures,
            new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_AlbedoMap" },
            new[] { "_BaseMap", "_MainTex", "_BaseColorMap", "_AlbedoMap" });

        TrySetTexture(mat, data.textures,
            new[] { "_BumpMap", "_NormalMap" },
            new[] { "_BumpMap", "_NormalMap" });

        TrySetTexture(mat, data.textures,
            new[] { "_MetallicGlossMap", "_MetallicMap" },
            new[] { "_MetallicGlossMap", "_MetallicMap", "_MaskMap" });

        TrySetTexture(mat, data.textures,
            new[] { "_SpecGlossMap", "_SpecularMap" },
            new[] { "_SpecGlossMap", "_SpecularMap" });

        TrySetTexture(mat, data.textures,
            new[] { "_OcclusionMap" },
            new[] { "_OcclusionMap" });

        TrySetTexture(mat, data.textures,
            new[] { "_EmissionMap" },
            new[] { "_EmissionMap" });

        TrySetTexture(mat, data.textures,
            new[] { "_ParallaxMap" },
            new[] { "_ParallaxMap" });

        TrySetColor(mat, data.colors,
            new[] { "_BaseColor", "_Color" },
            new[] { "_BaseColor", "_Color" });

        TrySetColor(mat, data.colors,
            new[] { "_EmissionColor" },
            new[] { "_EmissionColor" });

        TrySetFloat(mat, data.floats,
            new[] { "_Metallic" },
            new[] { "_Metallic", "_MetallicValue" });

        TrySetFloat(mat, data.floats,
            new[] { "_Smoothness", "_Glossiness" },
            new[] { "_Smoothness", "_Glossiness" });

        TrySetFloat(mat, data.floats,
            new[] { "_BumpScale", "_NormalStrength" },
            new[] { "_BumpScale", "_NormalStrength" });

        TrySetFloat(mat, data.floats,
            new[] { "_OcclusionStrength" },
            new[] { "_OcclusionStrength" });

        TrySetFloat(mat, data.floats,
            new[] { "_SmoothnessTextureChannel" },
            new[] { "_SmoothnessTextureChannel" });

        TrySetFloat(mat, data.floats,
            new[] { "_EmissionIntensity" },
            new[] { "_EmissionIntensity" });

        // 确保 Emission keyword 打开
        if (mat.HasProperty("_EmissionMap") || mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
        }
    }

    private void TrySetTexture(Material mat, Dictionary<string, Texture> saved, string[] oldSlots, string[] newSlots)
    {
        Texture tex = null;

        foreach (string oldSlot in oldSlots)
        {
            if (saved.TryGetValue(oldSlot, out tex))
            {
                break;
            }
        }

        if (tex == null) return;

        foreach (string newSlot in newSlots)
        {
            if (mat.HasProperty(newSlot))
            {
                mat.SetTexture(newSlot, tex);
                return;
            }
        }
    }

    private void TrySetColor(Material mat, Dictionary<string, Color> saved, string[] oldSlots, string[] newSlots)
    {
        bool found = false;
        Color color = Color.white;

        foreach (string oldSlot in oldSlots)
        {
            if (saved.TryGetValue(oldSlot, out color))
            {
                found = true;
                break;
            }
        }

        if (!found) return;

        foreach (string newSlot in newSlots)
        {
            if (mat.HasProperty(newSlot))
            {
                mat.SetColor(newSlot, color);
                return;
            }
        }
    }

    private void TrySetFloat(Material mat, Dictionary<string, float> saved, string[] oldSlots, string[] newSlots)
    {
        bool found = false;
        float value = 0f;

        foreach (string oldSlot in oldSlots)
        {
            if (saved.TryGetValue(oldSlot, out value))
            {
                found = true;
                break;
            }
        }

        if (!found) return;

        foreach (string newSlot in newSlots)
        {
            if (mat.HasProperty(newSlot))
            {
                mat.SetFloat(newSlot, value);
                return;
            }
        }
    }
}