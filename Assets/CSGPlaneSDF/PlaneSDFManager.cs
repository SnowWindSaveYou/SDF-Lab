using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;


#if UNITY_EDITOR

using UnityEditor;


#endif

public class PlaneSDFManager : MonoBehaviour
{
    public static PlaneSDFManager Instance;


    public MeshRenderer planeViewer;

    //public List<NodeControler> nodeList;


    public ComputeShader PlaneSDFComputeShader;

    RenderTexture PlaneSDFTex;
    RenderTexture TempPlaneSDFTex;

    const int RESOLUTION = 512;

    public float Smooth = 30;


    public int fixTimes = 30;
    public int fixRange = 256;

    public float testVal = 1;

    public bool autoFix = false;
    public bool useTruncat = false;
    public bool useFlood = false;
    

    [HideInInspector]
    public bool notifyUpdate = false;

    int kernelInitTex;
    int kernelDrawCircle;
    int kernelMinErosion;
    int kernelMinErosionPrepocess;
    int kernelMinErosionPostpocess;
    int kernelAddValue;
    int kernelTempToSDFTex;
    int kernelLipschitzProcess;
    int kernelMixToOffset;
    int kernelZeroPlaneVisit;
    int kernelRubCircle;
    int kernelSmooth;

    void InitComputeShader()
    {
        kernelInitTex = PlaneSDFComputeShader.FindKernel("InitPlaneSDFTex");
        kernelDrawCircle = PlaneSDFComputeShader.FindKernel("DrawCircle");
        kernelRubCircle = PlaneSDFComputeShader.FindKernel("RubCircle");
        kernelMinErosion = PlaneSDFComputeShader.FindKernel("MinErosion");
        kernelMinErosionPrepocess = PlaneSDFComputeShader.FindKernel("MinErosionPrePocess");
        kernelMinErosionPostpocess = PlaneSDFComputeShader.FindKernel("MinErosionPostPocess");
        kernelAddValue = PlaneSDFComputeShader.FindKernel("AddValue");

        kernelTempToSDFTex = PlaneSDFComputeShader.FindKernel("TempToSDFTex");
        kernelMixToOffset = PlaneSDFComputeShader.FindKernel("MixToOffset");
        kernelLipschitzProcess = PlaneSDFComputeShader.FindKernel("LipschitzProcess");
        kernelZeroPlaneVisit = PlaneSDFComputeShader.FindKernel("InitZeroPlaneVisitTex");
        kernelSmooth = PlaneSDFComputeShader.FindKernel("SmoothCircle");

        PlaneSDFComputeShader.SetFloat("_Resolution", RESOLUTION);

        planeViewer.material.SetFloat("_Resolution", RESOLUTION);
    }

    void InitPlaneSDFTex()
    {
        if (PlaneSDFTex == null)
        {
            PlaneSDFTex = new RenderTexture(RESOLUTION, RESOLUTION, 0, RenderTextureFormat.RFloat);
            PlaneSDFTex.enableRandomWrite = true;
        }
        
        PlaneSDFComputeShader.SetTexture(kernelInitTex,"_PlaneSDFTex", PlaneSDFTex);
        PlaneSDFComputeShader.Dispatch(kernelInitTex,Mathf.CeilToInt( RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);

        planeViewer.material.SetTexture("_MainTex", PlaneSDFTex);
    }



    RenderTexture GetTempPlaneSDFTex(bool init= false)
    {
        if (TempPlaneSDFTex == null)
        {
            TempPlaneSDFTex = new RenderTexture(RESOLUTION, RESOLUTION, 0, RenderTextureFormat.RFloat);
            TempPlaneSDFTex.enableRandomWrite = true;
        }
        if (init)
        {
            PlaneSDFComputeShader.SetTexture(kernelInitTex, "_PlaneSDFTex", TempPlaneSDFTex);
            PlaneSDFComputeShader.Dispatch(kernelInitTex, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);
        }
        return TempPlaneSDFTex;
    }
    void GetTempToSDF()
    {
        PlaneSDFComputeShader.SetTexture(kernelTempToSDFTex, "_PlaneSDFTex", PlaneSDFTex);
        PlaneSDFComputeShader.SetTexture(kernelTempToSDFTex, "_TempPlaneSDFTex", TempPlaneSDFTex);
        PlaneSDFComputeShader.Dispatch(kernelTempToSDFTex, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);
    }
    void InitZeroPlaneVisitTex()
    {
        GetTempPlaneSDFTex();

        PlaneSDFComputeShader.SetTexture(kernelZeroPlaneVisit, "_PlaneSDFTex", PlaneSDFTex);
        PlaneSDFComputeShader.SetTexture(kernelZeroPlaneVisit, "_TempPlaneSDFTex", TempPlaneSDFTex);

        PlaneSDFComputeShader.Dispatch(kernelZeroPlaneVisit, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);

        planeViewer.material.SetTexture("_MainTex", PlaneSDFTex);
    }

    public void FixContinuityMinErosion()
    {
        
        PlaneSDFComputeShader.SetTexture(kernelMinErosion, "_PlaneSDFTex", PlaneSDFTex);
        PlaneSDFComputeShader.SetTexture(kernelMinErosion, "_TempPlaneSDFTex", TempPlaneSDFTex);

        PlaneSDFComputeShader.SetTexture(kernelMinErosionPrepocess, "_PlaneSDFTex", PlaneSDFTex);
        PlaneSDFComputeShader.SetTexture(kernelMinErosionPostpocess, "_PlaneSDFTex", PlaneSDFTex);

        if (useTruncat)
        {
            PlaneSDFComputeShader.Dispatch(kernelMinErosionPrepocess, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);
        }

        if (useFlood)
        {
            PlaneSDFComputeShader.Dispatch(kernelMinErosionPostpocess, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);
        }

        for (int l = 0; l < fixTimes; l++)
        {

            if(l%5 == 0)
            {
                if (useFlood)
                {
                    PlaneSDFComputeShader.Dispatch(kernelMinErosionPostpocess, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);
                }
            }

            PlaneSDFComputeShader.SetFloat("_Step", l);
            PlaneSDFComputeShader.Dispatch(kernelMinErosion, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);
            GetTempToSDF();
            //InitZeroPlaneVisitTex();
            //PlaneSDFComputeShader.SetVector("_Direction", Vector2.up);
            //for (int i = 1; i < fixRange; i +=2)
            //{
            //    //float st = Mathf.CeilToInt(Mathf.Pow(2, i));//Mathf.Pow(i,2)
            //    PlaneSDFComputeShader.SetFloat("_Step",i);
            //    PlaneSDFComputeShader.Dispatch(kernelMinErosion, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);

            //}
            //InitZeroPlaneVisitTex();
            //PlaneSDFComputeShader.SetVector("_Direction", Vector2.right);
            //for (int i = 1; i < fixRange; i +=2)
            //{

            //    PlaneSDFComputeShader.SetFloat("_Step", i);
            //    PlaneSDFComputeShader.Dispatch(kernelMinErosion, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);

            //}
        }
        Debug.Log("FixContinuityMinErosion");
        //PlaneSDFComputeShader.Dispatch(kernelMinErosionPostpocess, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);
    }

    public void FixContinuityLipschitz()
    {
        var tempTex = GetTempPlaneSDFTex();
        PlaneSDFComputeShader.SetTexture(kernelLipschitzProcess, "_PlaneSDFTex", PlaneSDFTex);
        PlaneSDFComputeShader.SetTexture(kernelLipschitzProcess, "_TempPlaneSDFTex", tempTex);

        PlaneSDFComputeShader.Dispatch(kernelLipschitzProcess, Mathf.CeilToInt(RESOLUTION / 8.0f), Mathf.CeilToInt(RESOLUTION / 8.0f), 1);

        GetTempToSDF();
    }

    public void DrawCircle(Vector2 uv, float r)
    {
        PlaneSDFComputeShader.SetTexture(kernelDrawCircle, "_PlaneSDFTex", PlaneSDFTex);
        PlaneSDFComputeShader.SetFloat("_testVal", testVal);
        PlaneSDFComputeShader.SetVector("_InputUV", uv);
        PlaneSDFComputeShader.SetFloat("_InputRadius", r);
        PlaneSDFComputeShader.SetFloat("_Smooth", Smooth);
        PlaneSDFComputeShader.Dispatch(kernelDrawCircle, RESOLUTION / 8, RESOLUTION / 8, 1);
    }

    public void RemoveCircle(Vector2 uv, float r)
    {
        PlaneSDFComputeShader.SetTexture(kernelRubCircle, "_PlaneSDFTex", PlaneSDFTex);

        PlaneSDFComputeShader.SetVector("_InputUV", uv);
        PlaneSDFComputeShader.SetFloat("_InputRadius", r);
        PlaneSDFComputeShader.SetFloat("_Smooth", Smooth);
        PlaneSDFComputeShader.Dispatch(kernelRubCircle, RESOLUTION / 8, RESOLUTION / 8, 1);
    }

    public void AddValue(Vector2 uv, float r, float value)
    {
        PlaneSDFComputeShader.SetTexture(kernelAddValue, "_PlaneSDFTex", PlaneSDFTex);


        PlaneSDFComputeShader.SetVector("_InputUV", uv);
        PlaneSDFComputeShader.SetFloat("_InputRadius", r);
        PlaneSDFComputeShader.SetFloat("_InputFloat", value);
        PlaneSDFComputeShader.Dispatch(kernelAddValue, RESOLUTION / 8, RESOLUTION / 8, 1);
    }

    public void MixToOffset(Vector2 uv, float r, Vector3 dir)
    {
        var tempTex = GetTempPlaneSDFTex();
        PlaneSDFComputeShader.SetTexture(kernelMixToOffset, "_PlaneSDFTex", PlaneSDFTex);
        PlaneSDFComputeShader.SetTexture(kernelMixToOffset, "_TempPlaneSDFTex", tempTex);
        PlaneSDFComputeShader.SetVector("_InputUV", uv);
        PlaneSDFComputeShader.SetFloat("_InputRadius", r);
        PlaneSDFComputeShader.SetVector("_Direction", dir);
        PlaneSDFComputeShader.Dispatch(kernelMixToOffset, RESOLUTION / 8, RESOLUTION / 8, 1);
        GetTempToSDF();
    }
    public void SmoothCircle(Vector2 uv, float r,float smooth)
    {
        PlaneSDFComputeShader.SetTexture(kernelSmooth, "_PlaneSDFTex", PlaneSDFTex);

        PlaneSDFComputeShader.SetVector("_InputUV", uv);
        PlaneSDFComputeShader.SetFloat("_InputRadius", r);
        PlaneSDFComputeShader.SetFloat("_Smooth", smooth);
        PlaneSDFComputeShader.Dispatch(kernelSmooth, RESOLUTION / 8, RESOLUTION / 8, 1);
    }

    public void Bake()
    {
        InitPlaneSDFTex();
        var w2l = planeViewer.transform.worldToLocalMatrix;
        var nodeList = GameObject.FindObjectsOfType<NodeControler>();
        nodeList = nodeList.OrderBy(n => n.name).ToArray();
        foreach (var node in nodeList)
        {
            
            var uv = (w2l.MultiplyPoint( node.transform.position)+Vector3.one*0.5f)*RESOLUTION;
            var scale = w2l.MultiplyVector( node.transform.localScale).x/2.0f * RESOLUTION;

            if(node.nodeType == NodeControler.NodeType.Circle)
            {
                DrawCircle(uv, scale);
            }
            else if (node.nodeType == NodeControler.NodeType.CircleRemove)
            {

                RemoveCircle(uv, scale);
            }
            else if(node.nodeType == NodeControler.NodeType.Add)
            {

                AddValue(uv, scale, node.nodeValue);
            }
            else if (node.nodeType == NodeControler.NodeType.Forward)
            {
                Vector3 dir = (w2l.MultiplyVector(node.transform.forward)).normalized;
                MixToOffset(uv, scale, dir);
            }
            else if (node.nodeType == NodeControler.NodeType.Smooth)
            {
                
                SmoothCircle(uv, scale, node.nodeValue);
            }

        }

        if (autoFix)
        {
            FixContinuityMinErosion();
        }
    }

    private void Awake()
    {
        Instance = this;
        InitComputeShader();
    }


    void LateUpdate()
    {
        if (notifyUpdate==true)
        {
            Debug.Log("Update SDF");
            Bake();
            notifyUpdate = false;
        }
    }


}

#if UNITY_EDITOR

[CustomEditor(typeof(PlaneSDFManager))]
public class PlaneSDFManagerEditor : Editor
{


    private PlaneSDFManager m_target;
    private void OnEnable()
    {
        m_target = (PlaneSDFManager)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();


        if (GUILayout.Button("Bake!"))
        {

            m_target.Bake();
        }

        if (GUILayout.Button("MinEro"))
        {

            m_target.FixContinuityMinErosion();
        }
        if (GUILayout.Button("Lipschitz"))
        {

            m_target.FixContinuityLipschitz();
        }
    }

}

#endif