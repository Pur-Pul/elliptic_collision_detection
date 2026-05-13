using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;

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
    public void Update(Vector3 _pos, Vector3 _size, Quaternion _orientation, bool timed)
    {
        if (timed)
        {
            Timed(new Action<Vector3>(Resize), _size);
            Timed(new Action<Quaternion>(Reorient), _orientation);
            Timed(new Action<Vector3>(Reposition), _pos);
            Timed(new Action(UpdateSimpleSize));    
        } else
        {
            Resize(_size);
            Reorient(_orientation);
            Reposition(_pos);
            UpdateSimpleSize();   
        }
    }

    public bool CheckFastOverlaps(IBoundingVolume other) => Simple.SimpleIntersects(other.Simple);
    public virtual bool CheckSBC (SBC circle) => false;
    public virtual bool CheckOBR (SOBR obr) => false;

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
            SBC circle => (bool)Timed(new Func<SBC, bool>(CheckSBC),circle),
            SOBR obr => (bool)Timed(new Func<SOBR, bool>(CheckOBR),obr),
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
            base.Position = value * (1 - sagitta * 0.5f);
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
            
            base.Size = new Vector3(value.x, value.x, value.x);
        }
    }
    public override void UpdateSimpleSize () {}
    public override bool CheckSBC(SBC other)
    {
        return radiusAngle + other.radiusAngle > Vector3.Angle(SphereNormal, other.SphereNormal) * Mathf.Deg2Rad;
    }
    public override bool CheckOBR(SOBR obr) => obr.CheckSBC(this);
}

public class SOBR : SBV //Spherical Oriented Bounding Rectangle
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

            RightAng = SphericalUtils.ChordToAngle(Size.x) * Mathf.Rad2Deg * 0.5f;
            UpAng = SphericalUtils.ChordToAngle(Size.y) * Mathf.Rad2Deg * 0.5f;

            _GCBaseNormals = null;
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
        const float EPS = 1e-5f;

        return Vector3.Dot(point, GCNormals[0]) >= -EPS &&
            Vector3.Dot(point, GCNormals[1]) >= -EPS &&
            Vector3.Dot(point, GCNormals[2]) >= -EPS &&
            Vector3.Dot(point, GCNormals[3]) >= -EPS;
    }

    public bool GCIntersect(SOBR obr)
    {
        Vector3[] normals = GCNormals;
        Vector3[] obrNormals = obr.GCNormals;

        if (ContainsPoint(obr.SphereNormal) || obr.ContainsPoint(SphereNormal)) { return true; }

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

    public override bool CheckOBR (SOBR obr) {
        return GCIntersect(obr);
    }

    public override bool CheckSBC(SBC circle)
    {
        // The arcAngle can be cached.
        // The edge points themselves could also be cached and rotated during an update, but there could be a better solution.
        // Instead of checking if gcClosest is within arc bounds, it can be checked to be within the SOBR, which would allow skipping normalizations.
        // The sign calculation also does not require normalized points.
        Vector3[] gcNormals = GCNormals;
        if (ContainsPoint(circle.SphereNormal)) { return true; }

        List<Vector3> corners = new();
        for (int i = 0; i < 4; i++)
        {
            corners.Add(Vector3.Cross(gcNormals[i], gcNormals[(i + 3) % 4]).normalized); 
        }

        for (int i = 0; i < 4; i++)
        {
            // Find the closest point on the given great circle to the center of the spherical circle.
            Vector3 gcClosest = (circle.SphereNormal - Vector3.Dot(gcNormals[i], circle.SphereNormal) * gcNormals[i]).normalized;

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
                float sign = Mathf.Sign(Vector3.Dot(Vector3.Cross(a, gcClosest), Vector3.Cross(a, b)));
                if (sign > 0) { pointOnArc = b; }
                else { pointOnArc = a; }
            }

            // Check if the closest point on the arc is within the bounds of the spherical circle.
            if (Vector3.Angle(pointOnArc, circle.SphereNormal) < circle.RadiusAngle * Mathf.Rad2Deg) { return true; }
        }
        return false;
    }
}