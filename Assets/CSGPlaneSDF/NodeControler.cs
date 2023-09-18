using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeControler : MonoBehaviour
{


    public enum NodeType
    {
        Circle,
        CircleRemove,
        Quad,
        Smooth,
        Add,
        Forward
    }


    public NodeType nodeType = NodeType.Circle;

    public float nodeValue = 30;


    // Start is called before the first frame update
    void Start()
    {
        transform.hasChanged = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged == true)
        {
            PlaneSDFManager.Instance.notifyUpdate = true;
            transform.hasChanged = false;
        }
    }
}
