using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class TestMath : MonoBehaviour
{

    

    public MeshFilter targetMesh;

    public Mesh resultMesh;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void TutleParamarizasion()
    {
        var mesh = targetMesh.mesh;

        var triangles = mesh.triangles;
        var vertices = mesh.vertices;


        Matrix<float> mat1;

    }
}
