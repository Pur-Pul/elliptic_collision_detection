using System.Collections.Generic;
using UnityEngine;

public class worldScript : MonoBehaviour
{
    public ControlScript control;
    public Material material;
    ComputeBuffer posBuffer;
    ComputeBuffer radBuffer;
    ComputeBuffer colorBuffer;

    void Start()
    {
        posBuffer = new ComputeBuffer(256, sizeof(float) * 3);
        radBuffer = new ComputeBuffer(256, sizeof(float));
        colorBuffer = new ComputeBuffer(256, sizeof(float) * 4);
    }

    void Update()
    {
        Vector3[] points = new Vector3[control.body_n];
        float[] rads = new float[control.body_n];
        Color[] colors = new Color[control.body_n];
        for (int i = 0; i < control.body_n; i++)
        {
            points[i] = control.bodies[i].transform.position;
            rads[i] = EllipticBBox.EuclideanToEllipticDistance(control.bodies[i].BBox.Width/2);
            colors[i] = control.bodies[i].drawColor;
        }
        posBuffer.SetData(points);
        radBuffer.SetData(rads);
        colorBuffer.SetData(colors);

        material.SetBuffer("_Points", posBuffer);
        material.SetBuffer("_ERadius", radBuffer);
        material.SetBuffer("_Colors", colorBuffer); 
        material.SetInt("_PointCount", control.body_n);
    }

    void OnDestroy()
    {
        posBuffer.Release();
        radBuffer.Release();
        colorBuffer.Release();
    }
}
