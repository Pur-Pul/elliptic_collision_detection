using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;

public class SBV : IBoundingVolume //Spherical Bounding Volume
{
    private int _id;
    public int Id { get => _id; }
    private string typeName = "";
    private int?[] methodIds;
    public SBV (int id)
    {
        _id = id;
        Simple = new AABB();
        typeName = GetType().Name;
        methodIds = new int?[6];
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

    private RuntimeRecord record; 
    public RuntimeRecord Record {
        get => record;
        set
        {
            record = value;
            Simple.Record = value;
        }
    }
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
                
                if (size.x != value.x || size.y != value.y) {
                    chordHeight = null;
                    chord = null;
                }
                size = value;
            }
        }
    }

    private float? chord;
    public virtual float Chord => chord ??= Mathf.Sqrt(Size.x * Size.x + Size.y * Size.y);

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
            Timed(() => Resize(_size), methodIds[3] ??= Record.GetId(typeName, nameof(Resize)));
            Timed(() => Reorient(_orientation), methodIds[2] ??= Record.GetId(typeName, nameof(Reorient)));
            Timed(() => Reposition(-Forward), methodIds[4] ??= Record.GetId(typeName, nameof(Reposition)));
            Timed(() => UpdateSimpleSize(), methodIds[5] ??= Record.GetId(typeName, nameof(UpdateSimpleSize)));
        } else
        {
            Resize(_size);
            Reorient(_orientation);
            Reposition(-Forward);
            UpdateSimpleSize();   
        }
    }
    public virtual bool CheckSBC (SBC circle) => false;
    public virtual bool CheckSOBR (SOBR sobr) => false;

    T Timed<T> (Func<T> func, int id)
    {
        long start = Stopwatch.GetTimestamp();
        T result = func();
        long end = Stopwatch.GetTimestamp();

        Record.Write(start, end, id);

        return result;
    }

    void Timed (Action action, int id)
    {
        long start = Stopwatch.GetTimestamp();
        action();
        long end = Stopwatch.GetTimestamp();

        Record.Write(start, end, id);
    }

    public bool CheckCollision(IBoundingVolume other)
    {
        return other switch
        {
            SBC circle => Simple.SimpleIntersects(circle.Simple) && Timed(() => CheckSBC(circle), methodIds[0] ??= Record.GetId(typeName, nameof(CheckSBC))),
            SOBR sobr => Simple.SimpleIntersects(sobr.Simple) && Timed(() => CheckSOBR(sobr), methodIds[1] ??= Record.GetId(typeName, nameof(CheckSOBR))),
            _ => false
        };
    }
}

public class SBC : SBV //Spherical Bounding Circle
{
    public SBC(int id) : base(id) {}
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

    public override float ChordHeight {
        get
        {
            chordHeight ??= SphericalUtils.CalculateChordHeight(Size.x);
            return chordHeight.Value;
        }
    }

    public override Vector3 Position { 
        get => base.Position; 
        set {
            base.Position = value; 
            Simple.Position = value * ChordHeight;
        }
    }

    public override Vector3 Size { 
        get => base.Size;
        set
        {
            if (base.Size.x == value.x) { return; }
            Radius = value.x * 0.5f;
            RadiusAngle = SphericalUtils.ChordToAngle(value.x) * 0.5f;
            base.Size = new Vector3(value.x, value.x, value.x);
        }
    }

    public override float Chord => Size.x;

    public override void UpdateSimpleSize () {}
    public override bool CheckSBC(SBC other)
    {
        return radiusAngle + other.radiusAngle > Vector3.Angle(Position, other.Position) * Mathf.Deg2Rad;
    }
    public override bool CheckSOBR(SOBR sobr) => sobr.CheckSBC(this);
}

public class SOBR : SBV //Spherical Oriented Bounding Rectangle
{
    public SOBR(int id) : base(id) {}
    public override Vector3 Size
    {
        get => base.Size;
        set
        {
            if (base.Size.x == value.x && base.Size.y == value.y) { return; }
            base.Size = value;
            base.Size = new(value.x, value.y, 1 - ChordHeight);

            RightAng = SphericalUtils.ChordToAngle(Size.x) * Mathf.Rad2Deg * 0.5f;
            UpAng = SphericalUtils.ChordToAngle(Size.y) * Mathf.Rad2Deg * 0.5f;

            _GCBaseNormals = null;
        }
    }

    public override Vector3 Position { 
        get => base.Position; 
        set {
            base.Position = value; 
            Simple.Position = value * (0.5f + 0.5f * ChordHeight);
        }
    }

    public float RightAng { get; set; }
    public float UpAng { get; set; }

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

    public Vector3[] GCNormals { get; set; }

    public override void Reorient(Quaternion _orientation)
    {
        base.Reorient(_orientation);
        GCNormals = new [] {
            _orientation * GCBaseNormals[0],
            _orientation * GCBaseNormals[1],
            _orientation * GCBaseNormals[2],
            _orientation * GCBaseNormals[3]
        };
    }

    public bool ContainsPoint(Vector3 point)
    {
        // epsilon = e-5 caused false positives and e-8 caused floating point flicker.
        const float EPS = 1e-7f;
        
        return Vector3.Dot(point, GCNormals[0]) >= -EPS &&
            Vector3.Dot(point, GCNormals[1]) >= -EPS &&
            Vector3.Dot(point, GCNormals[2]) >= -EPS &&
            Vector3.Dot(point, GCNormals[3]) >= -EPS;
    }

    public bool GCIntersect(SOBR obr)
    {
        Vector3[] normals = GCNormals;
        Vector3[] obrNormals = obr.GCNormals;

        if (ContainsPoint(obr.Position) || obr.ContainsPoint(Position)) { return true; }

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Vector3 i1 = Vector3.Cross(normals[i], obrNormals[j]).normalized;
                Vector3 i2 = -i1;
                if (
                    (ContainsPoint(i1) && obr.ContainsPoint(i1)) 
                    || (ContainsPoint(i2) && obr.ContainsPoint(i2))
                ) { return true; }
            }
        }

        return false;
    }

    public override bool CheckSOBR (SOBR sobr) {
        return GCIntersect(sobr);
    }

    public override bool CheckSBC(SBC circle)
    {
        // The arcAngle can be cached.
        // The edge points themselves could also be cached and rotated during an update, but there could be a better solution.
        // Instead of checking if gcClosest is within arc bounds, it can be checked to be within the SOBR, which would allow skipping normalizations.
        // The sign calculation also does not require normalized points.
        Vector3[] gcNormals = GCNormals;
        if (ContainsPoint(circle.Position)) { return true; }

        List<Vector3> corners = new();
        for (int i = 0; i < 4; i++)
        {
            corners.Add(Vector3.Cross(gcNormals[i], gcNormals[(i + 3) % 4]).normalized); 
        }

        for (int i = 0; i < 4; i++)
        {
            // Find the closest point on the given great circle to the center of the spherical circle.
            Vector3 gcClosest = (circle.Position - Vector3.Dot(gcNormals[i], circle.Position) * gcNormals[i]).normalized;

            // Find the ends of the great circle arc.
            Vector3 a = corners[i];
            Vector3 b = corners[(i + 1) % 4];

            float arcAngle = Vector3.Angle(a, b);
            float aAng = Vector3.Angle(a, gcClosest);
            float bAng = Vector3.Angle(b, gcClosest);

            // Clamp the closest point on the great circle within the bounds of the great circle arc.
            Vector3 pointOnArc;
            if (aAng < arcAngle && bAng < arcAngle) { pointOnArc = gcClosest; } // Inside the arc
            else // Outside the arc
            {
                pointOnArc = aAng < bAng 
                    ? a
                    : b;
            }

            // Check if the closest point on the arc is within the bounds of the spherical circle.
            if (Vector3.Angle(pointOnArc, circle.Position) <= circle.RadiusAngle * Mathf.Rad2Deg) { return true; }
        }
        return false;
    }
}