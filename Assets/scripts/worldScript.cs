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
        Vector3[] points = new Vector3[control.sphere_n];
        float[] rads = new float[control.sphere_n];
        Color[] colors = new Color[control.sphere_n];
        for (int i = 0; i < control.sphere_n; i++)
        {
            points[i] = control.spheres[i].transform.position;
            rads[i] = EllipticBBox.EuclideanToEllipticDistance(control.spheres[i].BBox.Width/2);
            colors[i] = control.spheres[i].drawColor;
        }
        posBuffer.SetData(points);
        radBuffer.SetData(rads);
        colorBuffer.SetData(colors);

        material.SetBuffer("_Points", posBuffer);
        material.SetBuffer("_ERadius", radBuffer);
        material.SetBuffer("_Colors", colorBuffer); 
        material.SetInt("_PointCount", control.sphere_n);
    }

    void OnDestroy()
    {
        posBuffer.Release();
        radBuffer.Release();
        colorBuffer.Release();
    }
}
