using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pushes sphere centers/radii into every Renderer for shaders that read <c>_Position0..3</c> / <c>_Radius0..3</c>.
/// Runs before <see cref="AppearRevealGate"/> so the gate can append <c>_Reveal</c> on the same <see cref="MaterialPropertyBlock"/>.
/// </summary>
[DefaultExecutionOrder(-100)]
[ExecuteInEditMode]
public class ShaderPosition : MonoBehaviour
{
    public float radius = 1f;
    public Vector3 offset;
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

    /// <summary>
    /// 世界坐标是否落在任意已注册的 reveal 球内（与 reverse vision / Shader 揭示一致）。
    /// 用于怪物出生点等：在「视野外」即不在任一球内。
    /// </summary>
    /// <param name="worldPosition">检测点。</param>
    /// <param name="useXZPlaneDistance">true 时只用 XZ 平面距离（适合俯视角角色）；false 为三维距离。</param>
    public static bool IsWorldPositionInsideAnyRevealSphere(Vector3 worldPosition, bool useXZPlaneDistance = true)
    {
        for (int i = 0; i < instances.Count; i++)
        {
            ShaderPosition inst = instances[i];
            if (inst == null || !inst.isActiveAndEnabled) continue;

            Vector3 c = inst.transform.position + inst.offset;
            float r = Mathf.Max(0f, inst.radius);
            float distSq;
            if (useXZPlaneDistance)
            {
                float dx = worldPosition.x - c.x;
                float dz = worldPosition.z - c.z;
                distSq = dx * dx + dz * dz;
            }
            else
            {
                distSq = (worldPosition - c).sqrMagnitude;
            }

            if (distSq <= r * r) return true;
        }

        return false;
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
                    Vector3 pos = instances[i].transform.position + instances[i].offset;
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