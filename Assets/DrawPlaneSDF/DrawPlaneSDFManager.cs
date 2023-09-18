using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawPlaneSDFManager : MonoBehaviour
{


    public enum EditToolType
    {
        DrawSphere,
        RubSphere,
        DragSphere
    }

    public RectTransform ImageTransform;
    public RawImage Image;
    public ComputeShader DrawProcessComputeShader;

    public int Resolusion = 512;


    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        handleMouseProcess();
    }

    void handleMouseProcess()
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ImageTransform, Input.mousePosition, Camera.main,out localPos);
        Vector2 ImageUV = (localPos + Vector2.one * 0.5f) * Resolusion;


    }


    void DispatchDrawProcess(int kernel)
    {
        DrawProcessComputeShader.Dispatch(kernel, Mathf.CeilToInt(Resolusion / 8.0f), Mathf.CeilToInt(Resolusion / 8.0f), 1);
    }
}
