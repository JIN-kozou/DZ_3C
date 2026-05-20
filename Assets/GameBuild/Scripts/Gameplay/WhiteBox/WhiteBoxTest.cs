using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteBoxTest : MonoBehaviour
{
    public float height;
    public bool isClimbable;

    private Camera cam;

    private void Start()
    {
        height = transform.localScale.y;
        cam = Camera.main;

        if (height <= 2)
            isClimbable = true;
        else 
            isClimbable = false;
    }

    private void OnGUI()
    {
        if (cam == null) return;

        Vector3 worldPos = transform.position;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0) return;

        float x = screenPos.x - 60f;
        float y = Screen.height - screenPos.y - 20f;

        GUI.Label(new Rect(x, y, 160f, 24f), "高度: " + height);
        GUI.Label(new Rect(x, y + 22f, 160f, 24f), "可直接攀爬: " + isClimbable);
    }
}