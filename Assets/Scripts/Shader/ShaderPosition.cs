using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShaderPosition : MonoBehaviour
{
    public float radius = 1f;

    private static readonly List<ShaderPosition> instances = new List<ShaderPosition>();
    private static MaterialPropertyBlock block;

    private void OnEnable()
    {
        if (!instances.Contains(this))
            instances.Add(this);

        if (block == null) block = new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        instances.Remove(this);
    }

    private void LateUpdate()
    {
        UpdateAllRenderers();
    }

    private static void UpdateAllRenderers()
    {
        if (block == null) block = new MaterialPropertyBlock();

        Renderer[] renderers = FindObjectsOfType<Renderer>();

        foreach (Renderer rd in renderers)
        {
            if (rd == null) continue;

            rd.GetPropertyBlock(block);

            for (int i = 0; i < 4; i++)
            {
                if (i < instances.Count && instances[i] != null)
                {
                    Vector3 pos = instances[i].transform.position;
                    block.SetVector("_Position" + i, pos);
                    block.SetFloat("_Radius" + i, instances[i].radius);
                }
                else
                {
                    block.SetVector("_Position" + i, Vector3.zero);
                    block.SetFloat("_Radius" + i, 0f);
                }
            }

            rd.SetPropertyBlock(block);
        }
    }
}