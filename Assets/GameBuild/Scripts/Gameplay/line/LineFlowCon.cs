using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineFlowCon : MonoBehaviour
{
    public LineRenderer line;
    Material myMaterial;//line

    // Start is called before the first frame update
    void Awake()
    {
        line = GetComponent<LineRenderer>();
        myMaterial = line.material;
    }

    // Update is called once per frame
    void Update()
    {
        var off = myMaterial.mainTextureOffset;
        off += new Vector2(Time.deltaTime, 0);
        line.material.mainTextureOffset = off;

    }
}
