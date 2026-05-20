using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class GetSplineToLineRender : MonoBehaviour
{
    public SplineContainer splineContainer;
    LineRenderer line;
    public int Segment = 64;
    // Start is called before the first frame update
    void Awake()
    {
     line = this.GetComponent<LineRenderer>();   
    }

    // Update is called once per frame
    void Update()
    {
        var splineIget = splineContainer.Spline;//获取的spline
        line.positionCount = Segment;//把设定好的段数给自己的lineRenderer；

        for (int i = 0; i < Segment; i++) 
        {
            float t = i / (float)(Segment - 1);

            SplineUtility.Evaluate(splineIget,t, out float3 pos, out float3 tan, out float3 up);//采样曲线（点+三方向向量）
            //但是实际上只会用上pos

            Vector3 world = splineContainer.transform.TransformPoint(pos);
            line.SetPosition(i, world);//画点

        }
    }
}
