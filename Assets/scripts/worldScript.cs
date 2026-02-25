using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class worldScript : MonoBehaviour
{
    public ControlScript control;
    public Material material;
    ComputeBuffer BodyBuffer;

    void Start()
    {
        BodyBuffer = new ComputeBuffer(256, sizeof(float) * 16);
    }

    void Update()
    {
        List<Matrix4x4> BSList = new();
        List<Matrix4x4> OBBList = new();
        for (int i = 0; i < control.body_n; i++)
        {
            Vector3 pos;
            Vector3 right;
            Vector3 up;
            float width;
            float height;
            float radius;
            Matrix4x4 m = new Matrix4x4();
            switch(control.bodies[i].sequence.BodyType)
            {
                case "sphere":
                    pos = control.bodies[i].transform.position;
                    radius = EllipticBBox.EuclideanToEllipticDistance(control.bodies[i].BBox.Width/2);
                    m.SetRow(0, new Vector4(pos.x, pos.y, pos.z, 0));
                    m.SetRow(1, new Vector4(radius, 0, 0, 0));
                    m.SetRow(2, control.bodies[i].drawColor);
                    m.SetRow(3, Vector4.zero);
                    BSList.Add(m);
                    break;
                case "obb":
                    pos = control.bodies[i].transform.position;
                    right = control.bodies[i].transform.right;
                    up = control.bodies[i].transform.up;
                    width = control.bodies[i].transform.localScale.x;
                    height = control.bodies[i].transform.localScale.x;
                    m.SetRow(0, new Vector4(pos.x, pos.y, pos.z, 0));
                    m.SetRow(1, new Vector4(right.x, right.y, right.z, width));
                    m.SetRow(2, new Vector4(up.x, up.y, up.z, height));
                    m.SetRow(3, control.bodies[i].drawColor);
                    OBBList.Add(m);
                    break;
                default:
                    break;
            }
        }
        
        Matrix4x4[] BodyData = new Matrix4x4[control.body_n+1];
        BodyData[0].SetRow(0, new Vector4(control.body_n, BSList.Count, OBBList.Count, 0));

        for (int i = 0; i < BSList.Count; i++)
        {
            BodyData[1 + i] = BSList[i];
        }

        // Then OBBs
        for (int i = 0; i < OBBList.Count; i++)
        {
            BodyData[1 + BSList.Count + i] = OBBList[i];
        }
        BodyBuffer.SetData(BodyData);
        material.SetBuffer("_Bodies", BodyBuffer);
    }

    void OnDestroy()
    {
        BodyBuffer.Release();
    }
}
