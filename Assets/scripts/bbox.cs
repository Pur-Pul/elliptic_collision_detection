using UnityEngine;
using System;
using System.Diagnostics;

public class BBox : IBoundingVolume
{
    private int _id;
    public int Id { get => _id; }

    public BBox (int id)
    {
        _id = id;
    }

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

    public void Reorient(Quaternion _orientation)
    {
        Right = _orientation * Vector3.right;
        Up = _orientation * Vector3.up;
        Forward = _orientation * Vector3.forward;
    }

    public void Resize(Vector3 _size)
    {
        Size = _size;
    }

    public void Reposition(Vector3 _pos)
    {
        Position = _pos;
    }

    public void Update(Vector3 _pos, Vector3 _size, Quaternion _orientation)
    {
        Timed(new Action<Quaternion>(Reorient), _orientation);
        Timed(new Action<Vector3>(Resize), _size);
        Timed(new Action<Vector3>(Reposition), _pos);
        Timed(new Action(UpdateSimpleSize));
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
            BBoxSphere sphere => (bool)Timed(new Func<BBoxSphere, bool>(CheckSphere),sphere),
            OBBox obb => (bool)Timed(new Func<OBBox, bool>(CheckOBB),obb),
            _ => false
        };
    }
}

public class BBoxSphere : BBox
{
    public BBoxSphere(int id) : base(id) {}
    private float sagitta;
    public float Radius { get; set; }
    public override Vector3 Size
    {
        get => base.Size;
        set
        {
            float chord = value.x;
            base.Size = new Vector3(chord, chord, chord);
            sagitta = SphericalUtils.CalculateSagitta(value.x);
            Radius = value.x/2f;
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
    
    public override void UpdateSimpleSize () {}
    public override bool CheckSphere(BBoxSphere sphere)
    {
        float sqrDist = (sphere.Position - Position).sqrMagnitude;
        float combinedRadius = Radius + sphere.Radius;
        return sqrDist < combinedRadius * combinedRadius;
    }
    public override bool CheckOBB(OBBox obb) => obb.CheckSphere(this);
}

public class OBBox : BBox
{
    public OBBox(int id) : base(id) {}
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
            halfSize = newSize * 0.5f;
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
        // This allows for false positive collisions, since the bottom edges of the OBB may collide before the spherical rectangles do.
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
        Vector3 obbToPoint = sphere.Position - Position;

        Vector3 spherePosLocal = new (
            Vector3.Dot(obbToPoint, Right),
            Vector3.Dot(obbToPoint, Up),
            Vector3.Dot(obbToPoint, Forward)
        );

        Vector3 closestPointLocal = new(
            Mathf.Clamp(spherePosLocal.x, -HalfSize.x, HalfSize.x),
            Mathf.Clamp(spherePosLocal.y, -HalfSize.y, HalfSize.y),
            Mathf.Clamp(spherePosLocal.z, -HalfSize.z, HalfSize.z)
        );

        float sqrDist = (closestPointLocal - spherePosLocal).sqrMagnitude;
        return sqrDist < sphere.Radius * sphere.Radius;
    }
}