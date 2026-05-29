using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[ExecuteInEditMode]
public class ShaderPosition : MonoBehaviour
{
    public float radius = 1f;
    public Vector3 offset;

    private static readonly List<ShaderPosition> instances = new List<ShaderPosition>();

    // AppearShader graph uses these names for sphere slots 10–13 (not _Position10..13).
    private static readonly string[] GraphExtraPositionNames =
    {
        "_Position9_1", "_Position9_2", "_Position9_3", "_Position9_4"
    };

    private static readonly string[] GraphExtraRadiusNames =
    {
        "_Radius9_1", "_Radius9_2", "_Radius9_3", "_Radius9_4"
    };

    private static bool updatedThisFrame;

    private void OnEnable()
    {
        if (!instances.Contains(this))
            instances.Add(this);
    }

    private void OnDisable()
    {
        instances.Remove(this);
    }

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

            if (distSq <= r * r)
                return true;
        }

        return false;
    }

    private void LateUpdate()
    {
        if (updatedThisFrame) return;
        updatedThisFrame = true;

        for (int i = 0; i < 15; i++)
        {
            if (i < instances.Count && instances[i] != null)
            {
                Vector3 pos = instances[i].transform.position + instances[i].offset;
                float rad = instances[i].radius;

                Shader.SetGlobalVector("_Position" + i, pos);
                Shader.SetGlobalFloat("_Radius" + i, rad);

                int graphExtra = i - 10;
                if (graphExtra >= 0 && graphExtra < GraphExtraPositionNames.Length)
                {
                    Shader.SetGlobalVector(GraphExtraPositionNames[graphExtra], pos);
                    Shader.SetGlobalFloat(GraphExtraRadiusNames[graphExtra], rad);
                }
            }
            else
            {
                Shader.SetGlobalVector("_Position" + i, Vector3.zero);
                Shader.SetGlobalFloat("_Radius" + i, 0f);

                int graphExtra = i - 10;
                if (graphExtra >= 0 && graphExtra < GraphExtraPositionNames.Length)
                {
                    Shader.SetGlobalVector(GraphExtraPositionNames[graphExtra], Vector3.zero);
                    Shader.SetGlobalFloat(GraphExtraRadiusNames[graphExtra], 0f);
                }
            }
        }
    }

    private void Update()
    {
        updatedThisFrame = false;
    }
}
