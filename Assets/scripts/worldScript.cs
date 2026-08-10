using System.Collections.Generic;
using UnityEngine;

public class worldScript : MonoBehaviour
{
    public ControlScript control;
    public Material material;
    ComputeBuffer BodyBuffer;
    public Renderer rend;

    void Start()
    {
        BodyBuffer = new ComputeBuffer(1024, sizeof(float) * 16);
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        bool shouldRender = control.RenderInput.isOn;
        
        if (rend.enabled != shouldRender)
        {
            rend.enabled = shouldRender;
        }

        if (rend.enabled == false) { return; }
        List<Matrix4x4> BSList = new();
        List<Matrix4x4> OBBList = new();
        List<Matrix4x4> SAABBList = new();

        void HandleCircle (Vector3 pos, float sRadius, Color color, Matrix4x4 m)
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

        void HandleSAABB (Vector3 position, Vector3 sphericalPos, Vector3 fastSphericalPos, Vector2 extents, Vector2 fastExtents, Color color, Matrix4x4 m)
        {
            m.SetRow(0, new Vector4(sphericalPos.x, sphericalPos.y, fastSphericalPos.x, fastSphericalPos.y));
            m.SetRow(1, new Vector4(extents.x, extents.y, fastExtents.x, fastExtents.y));
            m.SetRow(2, new Vector4(position.x, position.y, position.z, 0));
            m.SetRow(3, color);
            SAABBList.Add(m);
        }

        for (int i = 0; i < control.body_n; i++)
        {
            Matrix4x4 m = new();
            switch (control.bodies[i].BBox)
            {
                case SBC circle:
                    HandleCircle(
                        circle.Position,
                        circle.CosRadius,
                        control.bodies[i].drawColor, 
                        m
                    );
                    break;
                case SBCA1 circle:
                    HandleCircle(
                        circle.Position,
                        circle.CosRadius,
                        control.bodies[i].drawColor, 
                        m
                    );
                    break;
                case SBCA2 circle:
                    HandleCircle(
                        circle.Position,
                        circle.CosRadius,
                        control.bodies[i].drawColor, 
                        m
                    );
                    break;
                case BBoxSphere sphere:
                    HandleCircle(
                        -sphere.Forward,
                        sphere.CosRadius,
                        control.bodies[i].drawColor,
                        m
                    );
                    break;
                case SOBR sobr:
                    HandleRectangle(
                        sobr.Position,
                        sobr.Right,
                        sobr.Up,
                        sobr.Size.x,
                        sobr.Size.y,
                        control.bodies[i].drawColor,
                        m
                    );
                    break;
                case SOBRA1 sobr:
                    HandleRectangle(
                        sobr.Position,
                        sobr.Right,
                        sobr.Up,
                        sobr.Size.x,
                        sobr.Size.y,
                        control.bodies[i].drawColor,
                        m
                    );
                    break;
                case SOBRA2 sobr:
                    HandleRectangle(
                        sobr.Position,
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
                        -obb.Forward,
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
        
        for (int i = 0; i < control.saabb_n; i++)
        {
            
            Matrix4x4 m = new();
            SAABB saabb = control.SAABBs[i];
            
            HandleSAABB(
                saabb.Position,
                saabb.SphericalPos,
                saabb.FastSphericalPos,
                new Vector2(saabb.AzimuthalExtent, saabb.PolarExtent),
                new Vector2(saabb.TanAzimuthalExtent, saabb.CosPolarExtent),
                saabb.Size.z > 0 ? Color.limeGreen : Color.hotPink,
                m
            );
        }

        Matrix4x4[] BodyData = new Matrix4x4[control.body_n + control.saabb_n + 1];
        BodyData[0].SetRow(0, new Vector4(control.body_n, BSList.Count, OBBList.Count, SAABBList.Count));

        for (int i = 0; i < BSList.Count; i++)
        {
            BodyData[1 + i] = BSList[i];
        }

        for (int i = 0; i < OBBList.Count; i++)
        {
            BodyData[1 + BSList.Count + i] = OBBList[i];
        }

        for (int i = 0; i < SAABBList.Count; i++)
        {
            BodyData[1 + BSList.Count + OBBList.Count + i] = SAABBList[i];
        }

        BodyBuffer.SetData(BodyData);
        material.SetBuffer("_Bodies", BodyBuffer);
    }

    void OnDestroy()
    {
        BodyBuffer.Release();
    }
}
