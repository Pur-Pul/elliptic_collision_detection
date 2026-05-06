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
        BodyBuffer = new ComputeBuffer(1024, sizeof(float) * 16);
    }

    void Update()
    {
        List<Matrix4x4> BSList = new();
        List<Matrix4x4> OBBList = new();

        void HandleSphere (Vector3 pos, float sRadius, Color color, Matrix4x4 m)
        {
            m.SetRow(0, new Vector4(pos.x, pos.y, pos.z, 0));
            m.SetRow(1, new Vector4(sRadius, 0, 0, 0));
            m.SetRow(2, color);
            m.SetRow(3, Vector4.zero);
            BSList.Add(m);
        }

        void HandleRectangle (Vector3 pos, Vector3 right, Vector3 up, float width, float height, Color color, Matrix4x4 m)
        {
            m.SetRow(0, new Vector4(pos.x, pos.y, pos.z, 0));
            m.SetRow(1, new Vector4(right.x, right.y, right.z, width));
            m.SetRow(2, new Vector4(up.x, up.y, up.z, height));
            m.SetRow(3, color);
            OBBList.Add(m);
        }

        for (int i = 0; i < control.body_n; i++)
        {
            Matrix4x4 m = new Matrix4x4();
            Vector3 pos = control.bodies[i].Position;
            switch (control.bodies[i].BBox)
            {
                case SBC circle:
                    HandleSphere(
                        pos,
                        circle.SRadius,
                        control.bodies[i].drawColor, 
                        m
                    );
                    break;
                case SBCA circle:
                    HandleSphere(
                        pos,
                        circle.SRadius,
                        control.bodies[i].drawColor, 
                        m
                    );
                    break;
                case BBoxSphere sphere:
                    HandleSphere(
                        pos,
                        SphericalUtils.AngleToSphericalDistance(SphericalUtils.ChordToAngle(sphere.Radius * 2f) * 0.5f),
                        control.bodies[i].drawColor,
                        m
                    );
                    break;
                case SOBR sobr:
                    HandleRectangle(
                        pos,
                        sobr.Right,
                        sobr.Up,
                        sobr.Size.x,
                        sobr.Size.y,
                        control.bodies[i].drawColor,
                        m
                    );
                    break;
                case SOBRA sobr:
                    HandleRectangle(
                        pos,
                        sobr.Right,
                        sobr.Up,
                        sobr.Size.x,
                        sobr.Size.y,
                        control.bodies[i].drawColor,
                        m
                    );
                    break;
                case OBBox obb:
                    HandleRectangle(
                        pos,
                        obb.Right,
                        obb.Up,
                        obb.Size.x,
                        obb.Size.y,
                        control.bodies[i].drawColor,
                        m
                    );
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
