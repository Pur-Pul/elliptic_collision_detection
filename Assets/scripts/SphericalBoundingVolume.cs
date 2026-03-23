using UnityEngine;
using System;
using System.Diagnostics;

public class SBV : IBoundingVolume //Spherical Bounding Volume
{
    private Vector3 right;
    private Vector3 up;
    private Vector3 forward;

    public bool shapeUpdated = false;

    public virtual Vector3 Right
    { 
        get => right;
        set
        {
            if (right != value)
            {
                shapeUpdated = true;
            }
            right = value;
        }
    }
    public virtual Vector3 Up
    { 
        get => up;
        set
        {
            if (up != value)
            {
                shapeUpdated = true;
            }
            up = value;
        }
    }
    public virtual Vector3 Forward
    { 
        get => forward;
        set
        {
            if (forward != value)
            {
                shapeUpdated = true;
            }
            forward = value;
        }
    }

    private Vector3 position;
    private Vector3 size;

    public RuntimeRecord Record { get; set; }
    public ISimpleBoundingVolume Simple { get; set; }
    public Vector3 SphereNormal { get; set; }
    public virtual Vector3 Position 
    {
        get => position;
        set
        {
            Simple.Position = value;
            position = value;
            SphereNormal = value;
        }
    }
    public virtual Vector3 Size {
        get => size;
        set
        {
            if (size != value)
            {
                shapeUpdated = true;
                Simple.Size = value;
                size = value;    
            }
        }
    }

    public virtual void UpdateSimpleSize ()
    {
        if (shapeUpdated) {
            Vector3 r = Right * Size.x;
            Vector3 u = Up * Size.y;
            Vector3 f = Forward * Size.z;
            Simple.Size = new Vector3(
                Mathf.Abs(r.x) + Mathf.Abs(u.x) + Mathf.Abs(f.x),
                Mathf.Abs(r.y) + Mathf.Abs(u.y) + Mathf.Abs(f.y),
                Mathf.Abs(r.z) + Mathf.Abs(u.z) + Mathf.Abs(f.z)
            );
            shapeUpdated = false;
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
    private float sRadius;
    private float radius;
    private float sagitta;
    public float Radius {
        get => radius;
        set
        {
            radius = value;
            sRadius = SphericalUtils.EuclideanToSphericalDistance(value);
        }
    }
    public float SRadius { get => sRadius; }
    public override Vector3 Position {
        get => base.Position;
        set
        {
            base.Position = value * (1 - sagitta);
            SphereNormal = value;
        }
    }

    public override Vector3 Size { 
        get => base.Size;
        set
        {
            if (base.Size.x == value.x) { return; }
            sagitta = SphericalUtils.CalculateSagitta(value.x);
            Radius = value.x/2f;
            base.Size = new Vector3(value.x, value.x, sagitta);
        }
    }
    public override bool CheckSBC(SBC other)
    {
        float sDist = SphericalUtils.SphericalDistance(SphereNormal, other.SphereNormal);
        float sRadii = SphericalUtils.EuclideanToSphericalDistance(Radius + other.Radius);
        return sRadii > sDist;
    }
    public override bool CheckOBR(SOBR obr) => obr.CheckSBC(this);
}


public class SOBR : SBV //Spherical Oriented Bounding Volume
{
    private float sagitta;
    public override Vector3 Size
    {
        get => base.Size;
        set
        {
            if (base.Size.x == value.x && base.Size.y == value.y) { return; }
            float cordSqr = value.x * value.x + value.y * value.y;
            sagitta = SphericalUtils.CalculateSagitta(cordSqr, true);
            Vector3 newSize = new(value.x, value.y, sagitta);
            base.Size = newSize;
            halfSize = newSize/2f;
        }
    }
    public override Vector3 Position
    {
        get => base.Position;
        set
        {
            base.Position = value * (1 - sagitta);
            SphereNormal = value;
        }
    }

    private Vector3 halfSize;
    public Vector3 HalfSize
    {
        get => halfSize;
    }

    public float Project(Vector3 axis)
    {
        return MathF.Abs(Vector3.Dot(HalfSize.x * Right, axis))
            + MathF.Abs(Vector3.Dot(HalfSize.y * Up, axis));
    }

    //https://dev.to/pratyush_mohanty_6b8f2749/the-math-behind-bounding-box-collision-detection-aabb-vs-obbseparate-axis-theorem-1gdn
    public bool SAT(SOBR obr)
    {
        Vector3 toVector = obr.Position - Position;
        Vector3[] axes = { Right, Up, obr.Right, obr.Up};

        for (int i = 0; i < 4; i++)
        {
            Vector3 axis = axes[i];
            float rA = Project(axis);
            float rB = obr.Project(axis);
            float distance = MathF.Abs(Vector3.Dot(toVector, axis));
            if (distance > rA + rB)  { return false; }
        }
        return true;
    }

    public override bool CheckOBR (SOBR obr) { return SAT(obr); }

    public override bool CheckSBC(SBC circle) //https://gamedev.stackexchange.com/questions/163873/separating-axis-theorem-obr-vs-circle
    {
        Vector3 localCirclePos = new (
            Vector3.Dot(circle.Position, Right),
            Vector3.Dot(circle.Position, Up),
            0
        );

        Vector3 closestPointToCircle =
            Position
            + Right * Mathf.Clamp(localCirclePos.x, -HalfSize.x, HalfSize.x)
            + Up * Mathf.Clamp(localCirclePos.y, -HalfSize.y, HalfSize.y); 


        return (closestPointToCircle - circle.Position).magnitude < circle.Radius;
    }
}