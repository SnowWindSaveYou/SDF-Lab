using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SnowWind
{

    public class TriMeshOctTree : MonoBehaviour
    {
        Vector3[] vertices;
        int[] triangles;
        Bounds rootBounds;

        public float minNodeSize = 0.01f;
        public TriMeshOctNode rootNode;

        public MeshFilter targetMesh;
        // Start is called before the first frame update
        void Start()
        {
            CreateOctTree(targetMesh);
        }


        private void OnDrawGizmos()
        {
            rootNode?.Draw();
        }

        void CreateOctTree(MeshFilter meshFilter)
        {
            //rootBounds = new Bounds(
            //    this.transform.position + this.transform.lossyScale * 0.5f,
            //    this.transform.position - this.transform.lossyScale * 0.5f
            //    );
            //Debug
            rootBounds = this.GetComponent<Collider>().bounds;
            rootNode = new TriMeshOctNode(rootBounds, minNodeSize);

            vertices = meshFilter.mesh.vertices;
            triangles = meshFilter.mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var l2w = meshFilter.transform.localToWorldMatrix;
                AddTriangleToOctTree(l2w.MultiplyPoint(a), l2w.MultiplyPoint(b), l2w.MultiplyPoint(c));
            }


        }

        void AddTriangleToOctTree(Vector3 a, Vector3 b, Vector3 c)
        {


            if (rootBounds.Contains(a)
                && rootBounds.Contains(b)
                && rootBounds.Contains(c))
            {
                
                Bounds triBounds = new Bounds();
                triBounds.max = new Vector3(
                    Mathf.Max(new float[3] { a.x, b.x, c.x }),
                    Mathf.Max(new float[3] { a.y, b.y, c.y }),
                    Mathf.Max(new float[3] { a.z, b.z, c.z })
                    );
                triBounds.min = new Vector3(
                    Mathf.Min(new float[3] { a.x, b.x, c.x }),
                    Mathf.Min(new float[3] { a.y, b.y, c.y }),
                    Mathf.Min(new float[3] { a.z, b.z, c.z })
                    );
                Debug.Log(triBounds);
                rootNode.AddTriangle(a, b, c, triBounds);
            }
        }

    }



    public class TriMeshOctNode
    {

        Bounds bounds;
        float minSize;
        Bounds[] childBounds;
        TriMeshOctNode[] children;


        public TriMeshOctNode(Bounds nodeBounds, float minNodeSize)
        {
            this.bounds = nodeBounds;
            this.minSize = minNodeSize;

            float quarter = nodeBounds.size.x / 4.0f;
            float childLength = nodeBounds.size.x / 2;

            Vector3 childSize = new Vector3(childLength, childLength, childLength);
            childBounds = new Bounds[8];

            //定义每个子树的边界
            childBounds[0] = new Bounds(nodeBounds.center + new Vector3(-quarter, quarter, -quarter), childSize);
            childBounds[1] = new Bounds(nodeBounds.center + new Vector3(quarter, quarter, -quarter), childSize);
            childBounds[2] = new Bounds(nodeBounds.center + new Vector3(-quarter, quarter, quarter), childSize);
            childBounds[3] = new Bounds(nodeBounds.center + new Vector3(quarter, quarter, quarter), childSize);
            childBounds[4] = new Bounds(nodeBounds.center + new Vector3(-quarter, -quarter, -quarter), childSize);
            childBounds[5] = new Bounds(nodeBounds.center + new Vector3(quarter, -quarter, -quarter), childSize);
            childBounds[6] = new Bounds(nodeBounds.center + new Vector3(-quarter, -quarter, quarter), childSize);
            childBounds[7] = new Bounds(nodeBounds.center + new Vector3(quarter, -quarter, quarter), childSize);
        }


        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Bounds triBounds)
        {
            DivideAndAdd(a, b, c, triBounds);
        }

        /// 使用递归完成八叉树的创建
        public void DivideAndAdd(Vector3 a, Vector3 b, Vector3 c, Bounds triBounds)
        {
            //如果包围盒已经小于等于最小尺寸就停止，直接返回树
            if (bounds.size.x <= minSize)
            {
                return;
            }

            //如果children列表为空，就创建一个八个元素的树（children列表默认值为空，所以第一次必然创建八叉树）
            if (children == null)
            {
                children = new TriMeshOctNode[8];//这里虽然创建了8个OctreeNode，但是其值仍为空，上面八个创建的是childBounds，不一样
            }

            bool dividing = false;


            //对于新创建的八个子树，遍历
            for (int i = 0; i < 8; i++)
            {
                //创建的树默认是空值，所以遍历所有并为其赋值，使用了OctreeNode的构造函数中创建的childBounds
                if (children[i] == null)
                {
                    children[i] = new TriMeshOctNode(childBounds[i], minSize);
                }
                //通过判断碰撞体包围盒是否相交来判断区域内是否有物体
                if (childBounds[i].Intersects(triBounds))
                {
                    //如果判断相交就改变dividing的布尔值为真
                    dividing = true;
                    //【递归部分】如果有相交就需要继续向下进行分割
                    children[i].DivideAndAdd(a, b, c, triBounds);
                }
            }
            

            //如果场景内没有GameObject，dividing的值就不会发生改变，就是一直为false，那么就不会创建children
            if (dividing == false)
            {
                children = null;
            }
            else
            {
                Debug.Log("Split");
            }
        }
        //绘制方法
        public void Draw()
        {
            Gizmos.color = new Color(0, 1, 0);
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            //【递归】这里的绘制也有递归
            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i] != null)
                    {
                        children[i].Draw();
                    }
                }
            }

            //Gizmos.DrawSphere(nodeBounds.center, 0.1f);
            //Debug.Log("包围盒中心为：" + nodeBounds.center + "包围盒size为：" + nodeBounds.size);
        }
    }


}