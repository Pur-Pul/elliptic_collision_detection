using UnityEngine;
using System;
using System.Diagnostics;

public class SBV : IBoundingVolume //Spherical Bounding Volume
{
    private int _id;
    public int Id { get => _id; }

    public SBV (int id)
    {
        _id = id;
    }

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
    public SBC(int id) : base(id) {}
    private float sagitta;
    public float Radius { get; set; }
    public float radiusAngle;
    public float RadiusAngle {
        get => radiusAngle;
        set
        {
            radiusAngle = value;
            SinRadius = Mathf.Sin(value);
            CosRadius = Mathf.Cos(value);
        }
    }
    public float SinRadius { get; set; }
    public float CosRadius { get; set; }

    public float SRadius { get; set; }
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
            RadiusAngle = SphericalUtils.ChordToAngle(value.x) / 2f;
            SRadius = SphericalUtils.AngleToSphericalDistance(RadiusAngle);
            
            base.Size = new Vector3(value.x, value.x, sagitta);
        }
    }
    public override bool CheckSBC(SBC other)
    // The dot product of two positions on a sphere is not a linear representation of spherical distance between them, but does contain the information.
    // Instead of adding the dot products together, adding the angles produces the actual combined spherical distance.
    // Trigonometric functions are expensive, so to avoid having to use cosinus during runtime, the cosine addition formula can be used to add the angles.
    // cos(θ_1 + θ_2) = cos(θ_1)​ * cos(θ_2) - sin(θ_1) * ​sin(θ_2)
    // The cosine of the combined angles are then normalized into the range [0, 1] as follows: (1 - cos(θ_1 + θ_2)) / 2
    // The cosine and sine of the spherical distance representaion of the radii are precalculated for all circles and stored in the properties CosRadius and SinRadius.​
    // This approach is more accurate than calculating the Euclidean distance between sphere representations of the circles but slightly slower.
    {
        float sDist = SphericalUtils.SphericalDistance(SphereNormal, other.SphereNormal);
        float sRadii = (1 - (CosRadius * other.CosRadius - SinRadius * other.SinRadius)) / 2f;

        return sRadii > sDist;
    }
    public override bool CheckOBR(SOBR obr) => obr.CheckSBC(this);
}


public class SOBR : SBV //Spherical Oriented Bounding Volume
{
    public SOBR(int id) : base(id) {}
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
            + MathF.Abs(Vector3.Dot(HalfSize.y * Up, axis))
            + MathF.Abs(Vector3.Dot(HalfSize.z * Forward, axis));
    }

    //https://dev.to/pratyush_mohanty_6b8f2749/the-math-behind-bounding-box-collision-detection-aabb-vs-obbseparate-axis-theorem-1gdn
    public bool SAT(SOBR obr)
    {
        // Not checking edge-edge separation reduces the number of axes to check by nine.
        // This does mean some false positives compared to the baseline, since there still are a small number of possible edge-edge and corner-edge collisions.
        Vector3 toVector = obr.Position - Position;
        Vector3[] axes = { Right, Up, Forward, obr.Right, obr.Up, obr.Forward };

        for (int i = 0; i < 6; i++)
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