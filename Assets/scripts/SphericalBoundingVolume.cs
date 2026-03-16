using UnityEngine;
using System;
using System.Diagnostics;

public class SBV : IBoundingVolume //Spherical Bounding Volume
{
    private Vector3 position;
    private Vector3 size;

    public RuntimeRecord Record { get; set; }
    public ISimpleBoundingVolume Simple { get; set; }
    public virtual Vector3 Position 
    {
        get => position;
        set
        {
            Simple.Position = value;
            position = value;
        }
    }
    public virtual Vector3 Size {
        get => size;
        set
        {
            Simple.Size = value;
            size = value;
        }
    }

    public bool CheckFastOverlaps(IBoundingVolume other) => Simple.SimpleIntersects(other.Simple);
    public virtual bool CheckSBC (SBC circle) => false;
    public virtual bool CheckOBR (SOBR obr) => false;

    bool Timed<T>(
        T volume,
        Func<T, bool> check
    ) where T : IBoundingVolume
    {
        long start = Stopwatch.GetTimestamp();
        bool result = check(volume);
        long end = Stopwatch.GetTimestamp();

        Record.Write(start, end, (GetType(), check.Method));

        return result;
    }

    public bool CheckCollision(IBoundingVolume other)
    {
        return other switch
        {
            SBC circle => Timed(circle, CheckSBC),
            SOBR obr => Timed(obr, CheckOBR),
            _ => false
        };
    }
}

public class SBC : SBV //Spherical Bounding Circle
{
    private float eRadius;
    private float radius;
    public float Radius {
        get => radius;
        set
        {
            radius = value;
            eRadius = SphericalUtils.EuclideanToSphericalDistance(value);
        }
    }
    public float ERadius { get => eRadius; }
    public override Vector3 Size { 
        get => base.Size;
        set
        {
            base.Size = value;
            Radius = value.x/2f;
        }
    }

    public override bool CheckSBC(SBC other)
    {
        float eDist = SphericalUtils.SphericalDistance(Position, other.Position);
        float eRadii = ERadius + other.ERadius;
        return eRadii > eDist;
    }
    public override bool CheckOBR(SOBR obr) => obr.CheckSBC(this);
}


public class SOBR : SBV //Spherical Oriented Bounding Volume
{
    private Vector3 right;
    private Vector3 up;
    private Vector3 forward;

    public Vector3 Right {
        get => right;
        set
        {
            right = value;
            UpdateSimpleSize();
        } 
    }
    public Vector3 Up
    {
        get => up;
        set
        {
            up = value;
            UpdateSimpleSize();
        } 
    }
    public Vector3 Forward
    {
        get => forward;
        set
        {
            forward = value;
            UpdateSimpleSize();
        } 
    }

    public override Vector3 Size {
        get => base.Size;
        set
        {
            base.Size = value;
            halfSize = value/2f;
            UpdateSimpleSize();
        }
    }
    private Vector3 halfSize;
    public Vector3 HalfSize
    {
        get => halfSize;
    }

    void UpdateSimpleSize ()
    {
        Vector3 r = Right * Size.x;
        Vector3 u = Up * Size.y;
        Vector3 f = Forward * Size.z;
        Simple.Size = new Vector3(
            Mathf.Abs(r.x) + Mathf.Abs(u.x) + Mathf.Abs(f.x),
            Mathf.Abs(r.y) + Mathf.Abs(u.y) + Mathf.Abs(f.y),
            Mathf.Abs(r.z) + Mathf.Abs(u.z) + Mathf.Abs(f.z)
        );
    }

    public float Project(Vector3 axis)
    {
        return MathF.Abs(Vector3.Dot(Size.x/2 * Right, axis))
            + MathF.Abs(Vector3.Dot(Size.y/2 * Up, axis))
            + MathF.Abs(Vector3.Dot(Size.z/2 * Forward, axis));
    }

    //https://dev.to/pratyush_mohanty_6b8f2749/the-math-behind-bounding-box-collision-detection-aabb-vs-obbseparate-axis-theorem-1gdn
    public bool SAT(SOBR obr)
    { 
        Vector3 toVector = obr.Position - Position;
        Vector3[] axesA = { Right, Up, Forward };
        Vector3[] axesB = { obr.Right, obr.Up, obr.Forward };

        for (int i = 0; i < 3; i++)
        {
            Vector3 axis = axesA[i];
            float rA = halfSize[i]; 
            float rB = obr.Project(axis);
            float distance = MathF.Abs(Vector3.Dot(toVector, axis));
            if (distance > rA + rB) return false;
        }

        for (int i = 0; i < 3; i++)
        {
            Vector3 axis = axesB[i];
            float rA = obr.halfSize[i]; 
            float rB = Project(axis);
            float distance = MathF.Abs(Vector3.Dot(toVector, axis));
            if (distance > rA + rB) return false;
        }

        foreach (var a in axesA)
        {
            foreach (var b in axesB)
            {
                Vector3 cross = Vector3.Cross(a, b);
                if (cross.sqrMagnitude < 1e-6f) continue;
                Vector3 axis = cross.normalized;
                float rA = obr.Project(axis);
                float rB = Project(axis);
                float distance = MathF.Abs(Vector3.Dot(toVector, axis));
                if (distance > rA + rB) return false;
            }
        }
        return true;
    }

    public override bool CheckOBR (SOBR obr)
    {
        return SAT(obr);
    }

    public override bool CheckSBC(SBC circle) //https://gamedev.stackexchange.com/questions/163873/separating-axis-theorem-obr-vs-circle
    {
        Vector3 obrToCircle = circle.Position - Position;

        Vector3 local_pos = new (
            Vector3.Dot(obrToCircle, Right),
            Vector3.Dot(obrToCircle, Up),
            Vector3.Dot(obrToCircle, Forward)
        );

        Vector3 closestPointLocal = new(
            Mathf.Clamp(local_pos.x, -Size.x/2, Size.x/2),
            Mathf.Clamp(local_pos.y, -Size.y/2, Size.y/2),
            Mathf.Clamp(local_pos.z, -Size.z/2, Size.z/2)
        );
        
        float sDist = SphericalUtils.SphericalDistance(closestPointLocal, local_pos);

        return sDist < circle.Radius;
    }
}