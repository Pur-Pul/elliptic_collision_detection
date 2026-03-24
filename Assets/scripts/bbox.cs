using UnityEngine;
using System;
using System.Diagnostics;
using Unity.Mathematics;
using NUnit.Framework.Internal.Execution;

public class BBox : IBoundingVolume
{
    private Vector3 position;
    private Vector3 size;
    private Vector3 right;
    private Vector3 up;
    private Vector3 forward;

    private bool shapeUpdated = false;

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

    public RuntimeRecord Record { get; set; }
    public ISimpleBoundingVolume Simple { get; set; }
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
    public virtual Vector3 Position 
    {
        get => position;
        set
        {
            Simple.Position = value;
            position = value;
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
	public virtual bool CheckSphere (BBoxSphere sphere) => false;
    public virtual bool CheckOBB (OBBox obb) => false;

    bool Timed<T>(
        T volume,
        Func<T, bool> check
    ) where T : IBoundingVolume
    {
        long start = Stopwatch.GetTimestamp();
        bool result = check(volume);
        long end = Stopwatch.GetTimestamp();

        Record.Write(start, end, (this.GetType(), check.Method));

        return result;
    }

    public bool CheckCollision(IBoundingVolume other)
    {
        return other switch
        {
            BBoxSphere sphere => Timed(sphere, CheckSphere),
            OBBox obb => Timed(obb, CheckOBB),
            _ => false
        };
    }
}

public class BBoxSphere : BBox
{
    private float radius;
    public float Radius2 { get; set; }
    public float Radius {
        get => radius;
        set {
            radius = value;
            Radius2 = Radius*Radius;
        } 
    }
    public override Vector3 Size
    {
        get => base.Size;
        set
        {
            base.Size = new Vector3(value.x, value.x, value.x);
            Radius = value.x/2f;
        }
    }
    public override void UpdateSimpleSize () {}
    public override bool CheckSphere(BBoxSphere sphere)
    // This needs to be changed as Euclidean distance between spheres is not a good representation of distance between circles on a sphere.
    // The angle between the circle centers and their radii can be calculated instead. This is fine for the baseline and should be improved in the artifact.
    {
        float centerDist = Radius + sphere.Radius;
        return (Position - sphere.Position).sqrMagnitude < centerDist * centerDist;
    }
    public override bool CheckOBB(OBBox obb) => obb.CheckSphere(this);
}

public class OBBox : BBox
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
            base.Position = value * (1 - sagitta*0.5f);
        }
    }

    private Vector3 halfSize;
    public Vector3 HalfSize
    {
        get => halfSize;
    }

    public float Project(Vector3 axis)
    {
        return MathF.Abs(Vector3.Dot(halfSize.x * Right, axis))
            + MathF.Abs(Vector3.Dot(halfSize.y * Up, axis))
            + MathF.Abs(Vector3.Dot(halfSize.z * Forward, axis));
    }

    //https://dev.to/pratyush_mohanty_6b8f2749/the-math-behind-bounding-box-collision-detection-aabb-vs-obbseparate-axis-theorem-1gdn
    public bool SAT(OBBox bbox)
    {
        Vector3 toVector = bbox.Position - Position;
        Vector3[] axesA = { Right, Up, Forward };
        Vector3[] axesB = { bbox.Right, bbox.Up, bbox.Forward };

        for (int i = 0; i < 3; i++)
        {
            Vector3 axis = axesA[i];
            float rA = halfSize[i];
            float rB = bbox.Project(axis);
            float distance = MathF.Abs(Vector3.Dot(toVector, axis));
            if (distance > rA + rB) return false;
        }

        for (int i = 0; i < 3; i++)
        {
            Vector3 axis = axesB[i];
            float rA = Project(axis);
            float rB = bbox.halfSize[i];
            float distance = MathF.Abs(Vector3.Dot(toVector, axis));
            if (distance > rA + rB) return false;
        }

        foreach (var a in axesA)
        {
            foreach (var b in axesB)
            {
                Vector3 cross = Vector3.Cross(a, b);
                if (cross.sqrMagnitude < float.Epsilon) continue;
                Vector3 axis = cross.normalized;
                float rA = Project(axis);
                float rB = bbox.Project(axis);
                float distance = MathF.Abs(Vector3.Dot(toVector, axis));
                if (distance > rA + rB) return false;
            }
        }
        return true;
    }

    public override bool CheckOBB (OBBox obb)
    {
        return SAT(obb);
    }

    public override bool CheckSphere(BBoxSphere sphere) //https://gamedev.stackexchange.com/questions/163873/separating-axis-theorem-obb-vs-sphere
    {
        Vector3 obbToSphere = sphere.Position - Position;

        Vector3 local_pos = new (
            Vector3.Dot(obbToSphere, Right),
            Vector3.Dot(obbToSphere, Up),
            Vector3.Dot(obbToSphere, Forward)
        );

        Vector3 closestPointLocal = new(
            Mathf.Clamp(local_pos.x, -Size.x/2, Size.x/2),
            Mathf.Clamp(local_pos.y, -Size.y/2, Size.y/2),
            Mathf.Clamp(local_pos.z, -Size.z/2, Size.z/2)
        );

        return (closestPointLocal - local_pos).sqrMagnitude < sphere.Radius * sphere.Radius;
    }
}