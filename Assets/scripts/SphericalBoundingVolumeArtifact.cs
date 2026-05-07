using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;

public class SBVA : IBoundingVolume //Spherical Bounding Volume
{
    private int _id;
    public int Id { get => _id; }

    public SBVA (int id)
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

    public virtual void Update(Vector3 _pos, Vector3 _size, Quaternion _orientation)
    {
        Right = _orientation * Vector3.right;
        Up = _orientation * Vector3.up;
        Forward = _orientation * Vector3.forward;
        Size = _size;
        Position = _pos;
        UpdateSimpleSize();
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
    public virtual bool CheckSBCA (SBCA circle) => false;
    public virtual bool CheckOBRA (SOBRA obr) => false;

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
            SBCA circle => Timed(circle, CheckSBCA),
            SOBRA obr => Timed(obr, CheckOBRA),
            _ => false
        };
    }
}

public class SBCA : SBVA //Spherical Bounding Circle
{
    public SBCA(int id) : base(id) {}
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
            RadiusAngle = SphericalUtils.ChordToAngle(value.x) * 0.5f;
            SRadius = SphericalUtils.AngleToSphericalDistance(RadiusAngle);
            
            base.Size = new Vector3(value.x, value.x, sagitta);
        }
    }
    public override bool CheckSBCA(SBCA other)
    // The dot product of two positions on a sphere is not a linear representation of spherical distance between them, but does contain the information.
    // Instead of adding the dot products together, adding the angles produces the actual combined spherical distance.
    // Trigonometric functions are expensive, so to avoid having to use cosinus during runtime, the cosine addition formula can be used to add the angles.
    // cos(θ_1 + θ_2) = cos(θ_1)​ * cos(θ_2) - sin(θ_1) * ​sin(θ_2)
    // The cosine of the combined angles are then normalized into the range [0, 1] as follows: (1 - cos(θ_1 + θ_2)) / 2
    // The cosine and sine of the spherical distance representaion of the radii are precalculated for all circles and stored in the properties CosRadius and SinRadius.​
    // This approach is more accurate than calculating the Euclidean distance between sphere representations of the circles but slightly slower.
    {
        float sDist = SphericalUtils.SphericalDistance(SphereNormal, other.SphereNormal);
        float sRadii = (1 - (CosRadius * other.CosRadius - SinRadius * other.SinRadius)) * 0.5f;

        return sRadii > sDist;
    }
    public override bool CheckOBRA(SOBRA obr) => obr.CheckSBCA(this);
}

public class SOBRA : SBVA //Spherical Oriented Bounding Rectangle
{
    public SOBRA(int id) : base(id) {}
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

            RightAng = SphericalUtils.ChordToAngle(Size.x) * Mathf.Rad2Deg * 0.5f;
            UpAng = SphericalUtils.ChordToAngle(Size.y) * Mathf.Rad2Deg * 0.5f;

            _GCBaseNormals = null;
            _BaseCorners = null;
        }
    }


    public float RightAng { get; set; }
    public float UpAng { get; set; }

    public override Vector3 Position
    {
        get => base.Position;
        set
        {
            base.Position = value * (1 - sagitta*0.5f);
            SphereNormal = value;
        }
    }

    Vector3[] _BaseCorners = null;
    Vector3[] _GCBaseNormals = null;
    Vector3[] GCBaseNormals { 
        get
        {
            if (_GCBaseNormals == null) {
                Quaternion q1 = Quaternion.AngleAxis(RightAng, Vector3.up);
                Quaternion q1inv = Quaternion.Inverse(q1);
                Quaternion q2 = Quaternion.AngleAxis(UpAng, Vector3.right);
                Quaternion q2inv = Quaternion.Inverse(q2);

                _GCBaseNormals = new[] {
                    q1 * Vector3.right,
                    q2inv * Vector3.up,
                    q1inv * -Vector3.right,
                    q2 * -Vector3.up,
                };
            }
            return _GCBaseNormals;
        } 
    }
    Vector3[] BaseCorners
    {
        get
        {
            if (_BaseCorners == null)
            {
                _BaseCorners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    _BaseCorners[i] = Vector3.Cross(GCBaseNormals[i], GCBaseNormals[(i + 3) % 4]).normalized;
                }
            }
            return _BaseCorners;
        }
    }

    public float[] ArcAngles { get; set; }
    public Vector3[] GCNormals { get; set; }
    public Vector3[] Corners { get; set; }
    public override void Update(Vector3 _pos, Vector3 _size, Quaternion _orientation)
    {
        Size = _size;
        Right = _orientation * Vector3.right;
        Up = _orientation * Vector3.up;
        Forward = _orientation * Vector3.forward;
        GCNormals = new [] {
            _orientation * GCBaseNormals[0],
            _orientation * GCBaseNormals[1],
            _orientation * GCBaseNormals[2],
            _orientation * GCBaseNormals[3]
        };
        Corners = new []
        {
            _orientation * BaseCorners[0],
            _orientation * BaseCorners[1],
            _orientation * BaseCorners[2],
            _orientation * BaseCorners[3]
        };
        Position = _pos;
        UpdateSimpleSize();
    }

    public bool ContainsPoint(Vector3 point, bool checkInv = false)
    {
        const float EPS = 1e-5f;

        bool contains = false;
        bool containsInv = false;

        for (int i = 0; i < 4; i ++)
        {
            bool withinGC = Vector3.Dot(point, GCNormals[i]) >= -EPS;
            contains = (i == 0 || contains) && withinGC;
            containsInv = checkInv && (i == 0 || containsInv) && !withinGC;
        }

        return contains || containsInv;
    }

    public bool GCIntersect(SOBRA obr)
    {
        Vector3[] normals = GCNormals;
        Vector3[] obrNormals = obr.GCNormals;

        if (ContainsPoint(obr.SphereNormal) || obr.ContainsPoint(SphereNormal)) { return true; }

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Vector3 i1 = Vector3.Cross(normals[i], obrNormals[j]);
                if (ContainsPoint(i1, true) && obr.ContainsPoint(i1, true)) { return true; }
            }
        }

        return false;
    }

    public override bool CheckOBRA (SOBRA obr) {
        return GCIntersect(obr);
    }

    public override bool CheckSBCA(SBCA circle)
    {
        Vector3[] gcNormals = GCNormals;
        if (ContainsPoint(circle.SphereNormal)) { return true; }

        for (int i = 0; i < 4; i++)
        {
            // Find the direction of the closest point on the given great circle to the center of the spherical circle.
            float d = Vector3.Dot(gcNormals[i], circle.SphereNormal);
            Vector3 gcClosest = circle.SphereNormal - d * gcNormals[i];

            // Find the ends of the great circle arc.
            Vector3 a = Corners[i];
            Vector3 b = Corners[(i + 1) % 4];

            // Clamp the closest point on the great circle within the bounds of the great circle arc.
            if (
                Vector3.Dot(Vector3.Cross(a, gcClosest), Vector3.Cross(a, b)) >= 0 &&
                Vector3.Dot(Vector3.Cross(b, gcClosest), Vector3.Cross(b, a)) >= 0
            ) // Inside the arc
            {
                // The dot product between gcNormal and circle.SphereNormal is equal to the sine of the angle between circle.SphereNormal and gcClosest.
                // dot(p, n) = cos(theta + pi/2) = sin(theta) = |cross(p, q)|, where p = circle.SphereNormal, n = GCNormal and q = gcClosest.
                // This holds true since p is not between q and n.
                float sinRadiusSquared = 1f - circle.CosRadius * circle.CosRadius;
                if (d * d <= sinRadiusSquared) { return true; }
                else { continue; }
            }
            else // Outside the arc
            {
                // The closest point on the arc is one of the ends.
                float sign = Mathf.Sign(Vector3.Dot(Vector3.Cross(a, gcClosest), Vector3.Cross(a, b)));
                if (
                    (sign > 0 && Vector3.Dot(b, circle.SphereNormal) >= circle.CosRadius) 
                    || (sign < 0 && Vector3.Dot(a, circle.SphereNormal) >= circle.CosRadius)
                ) { return true; }
            }
        }
        return false;
    }
}