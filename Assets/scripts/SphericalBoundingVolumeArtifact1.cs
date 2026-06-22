using UnityEngine;
using System;
using System.Diagnostics;

public class SBVA1 : IBoundingVolume //Spherical Bounding Volume
{
    private int _id;
    public int Id { get => _id; }

    public SBVA1 (int id)
    {
        _id = id;
        Simple = new AABB();
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
    public float? chordHeight = null;

    public virtual float ChordHeight {
        get
        {
            if (chordHeight == null)
            {
                float cordSqr = Size.x * Size.x + Size.y * Size.y;
                chordHeight = SphericalUtils.CalculateChordHeight(cordSqr, true);
            }
            return chordHeight.Value;
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
    public virtual Vector3 Size {
        get => size;
        set
        {
            if (size != value)
            {
                shapeUpdated = true;
                Simple.Size = value;
                if (size.x != value.x || size.y != value.y) { chordHeight = null; }
                size = value;    
            }
        }
    }

    public virtual void Reorient(Quaternion _orientation)
    {
        Right = _orientation * Vector3.right;
        Up = _orientation * Vector3.up;
        Forward = _orientation * Vector3.forward;
    }
    public void Resize(Vector3 _size) { Size = _size; }
    public void Reposition(Vector3 _pos) { Position = _pos; }
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
    public void Update(Vector3 _size, Quaternion _orientation, bool timed)
    {
        if (timed)
        {
            Timed(new Action<Vector3>(Resize), _size);
            Timed(new Action<Quaternion>(Reorient), _orientation);
            Timed(new Action<Vector3>(Reposition), -Forward);
            Timed(new Action(UpdateSimpleSize));    
        } else
        {
            Resize(_size);
            Reorient(_orientation);
            Reposition(-Forward);
            UpdateSimpleSize();   
        }
    }
    public bool CheckFastOverlaps(IBoundingVolume other) => Simple.SimpleIntersects(other.Simple);
    public virtual bool CheckSBCA1 (SBCA1 circle) => false;
    public virtual bool CheckSOBRA1 (SOBRA1 obr) => false;

    object Timed(Delegate func, params object[] args)
    {
        long start = Stopwatch.GetTimestamp();
        object result = func.DynamicInvoke(args);
        long end = Stopwatch.GetTimestamp();

        Record.Write(start, end, (this.GetType(), func.Method));

        return result;
    }

    public bool CheckCollision(IBoundingVolume other)
    {
        return other switch
        {
            SBCA1 circle => (bool)Timed(new Func<SBCA1, bool>(CheckSBCA1),circle),
            SOBRA1 obr => (bool)Timed(new Func<SOBRA1, bool>(CheckSOBRA1),obr),
            _ => false
        };
    }
}

public class SBCA1 : SBVA1 //Spherical Bounding Circle
{
    public SBCA1(int id) : base(id) {}
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

    public override float ChordHeight
    {
        get
        {
            chordHeight ??= SphericalUtils.CalculateChordHeight(Size.x);
            return chordHeight.Value;
        }
    }
    public override Vector3 Size { 
        get => base.Size;
        set
        {
            if (base.Size.x == value.x) { return; }
            Radius = value.x/2f;
            RadiusAngle = SphericalUtils.ChordToAngle(value.x) * 0.5f;
            base.Size = new Vector3(value.x, value.x, value.x);
        }
    }
    public override Vector3 Position {
        get => base.Position;
        set
        {
            base.Position = value;
            Simple.Position = value * ChordHeight;   
        }
    }
    public override void UpdateSimpleSize () {}
    public override bool CheckSBCA1(SBCA1 other)
    // The dot product of two positions on a sphere is not a linear representation of spherical distance between them, but does contain the information.
    // Instead of adding the dot products together, adding the angles produces the actual combined spherical distance.
    // Trigonometric functions are expensive, so to avoid having to use cosinus during runtime, the cosine addition formula can be used to add the angles.
    // cos(θ_1 + θ_2) = cos(θ_1)​ * cos(θ_2) - sin(θ_1) * ​sin(θ_2)
    // The cosine and sine of the spherical distance representaion of the radii are precalculated for all circles and stored in the properties CosRadius and SinRadius.​
    // This approach is more accurate than calculating the Euclidean distance between sphere representations of the circles but slightly slower.
    // The cosine comparison only holds if the cosines represent angles smaller than 180 degrees.
    // An additional check is performed to see if the combined radii are larger than 180 degrees, in which case they are allways intersecting.
    {
        if (radiusAngle + other.radiusAngle >= Mathf.PI) { return true; }
        float cosine = Vector3.Dot(Position, other.Position);
        float radCosine = CosRadius * other.CosRadius - SinRadius * other.SinRadius;
        return radCosine < cosine;
    }
    public override bool CheckSOBRA1(SOBRA1 obr) => obr.CheckSBCA1(this);
}

public class SOBRA1 : SBVA1 //Spherical Oriented Bounding Rectangle
{
    public SOBRA1(int id) : base(id) {}

    public override Vector3 Size
    {
        get => base.Size;
        set
        {
            if (base.Size.x == value.x && base.Size.y == value.y) { return; }
            base.Size = value;
            base.Size = new(value.x, value.y, 1 - ChordHeight);

            RightAng = SphericalUtils.ChordToAngle(base.Size.x) * Mathf.Rad2Deg * 0.5f;
            UpAng = SphericalUtils.ChordToAngle(base.Size.y) * Mathf.Rad2Deg * 0.5f;

            _GCBaseNormals = null;
            _BaseCorners = null;
        }
    }
    public override Vector3 Position {
        get => base.Position;
        set
        {
            base.Position = value;
            Simple.Position = value * (0.5f + 0.5f * ChordHeight);
        }
    }

    public float RightAng { get; set; }
    public float UpAng { get; set; }

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
    public override void Reorient(Quaternion _orientation)
    {
        base.Reorient(_orientation);
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
    }

    public bool ContainsPoint(Vector3 point, bool checkInv = false)
    {
        const float EPS = 1e-7f;

        bool contains = false;
        bool containsInv = false;

        for (int i = 0; i < 4; i ++)
        {
            float d = Vector3.Dot(point, GCNormals[i]);
            bool withinGC = d >= -EPS;

            contains = (i == 0 || contains) && withinGC;
            containsInv = checkInv && (i == 0 || containsInv) && d <= EPS;
        }

        return contains || containsInv;
    }

    public bool GCIntersect(SOBRA1 sobr)
    {
        Vector3[] normals = GCNormals;
        Vector3[] obrNormals = sobr.GCNormals;

        if (ContainsPoint(sobr.Position) || sobr.ContainsPoint(Position)) { return true; }

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Vector3 i1 = Vector3.Cross(normals[i], obrNormals[j]);
                if (ContainsPoint(i1, true) && sobr.ContainsPoint(i1, true)) { return true; }
            }
        }

        return false;
    }

    public override bool CheckSOBRA1 (SOBRA1 sobr) {
        return GCIntersect(sobr);
    }

    public override bool CheckSBCA1(SBCA1 circle)
    {
        Vector3[] gcNormals = GCNormals;
        if (ContainsPoint(circle.Position)) { return true; }

        for (int i = 0; i < 4; i++)
        {
            // Find the direction of the closest point on the given great circle to the center of the spherical circle.
            float d = Vector3.Dot(gcNormals[i], circle.Position);
            Vector3 gcClosest = circle.Position - d * gcNormals[i];

            // Find the ends of the great circle arc.
            Vector3 a = Corners[i];
            Vector3 b = Corners[(i + 1) % 4];

            float sign = Vector3.Dot(Vector3.Cross(a, gcClosest), Vector3.Cross(b, gcClosest));

            // Clamp the closest point on the great circle within the bounds of the great circle arc.
            if (sign < 0) // Closest point on the GC is already inside the arc
            {
                // Check if SBC intersects with the great circle
                // Taking the absolute value of the dot ensures that only intersection with the great circle returns true.
                // Otherwise it would be an intersection check with the hemisphere, which would result in false positives.
                if (Mathf.Abs(d) <= circle.SinRadius) { return true; }
            }
            else // Closest point on the GC is outside the arc. Therefore the closest point on the arc is one of its ends.
            {
                float da = Vector3.Dot(a, circle.Position);
                float db = Vector3.Dot(b, circle.Position);

                if (Mathf.Max(da, db) >= circle.CosRadius) { return true; }
            }
        }
        return false;
    }
}