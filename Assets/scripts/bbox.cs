using UnityEngine;
using System;
using System.Diagnostics;

public class BBox : IBoundingVolume
{
    private int _id;
    public int Id { get => _id; }
    private string typeName = "";
    private int?[] methodIds;
    public BBox (int id)
    {
        _id = id;
        Simple = new AABB();
        typeName = GetType().Name;
        methodIds = new int?[6];
    }

    private Vector3 position;
    private Vector3 size;
    private Vector3 right;
    private Vector3 up;
    private Vector3 forward;

    private bool shapeUpdated = false;
    public float? chordHeight = null;


    public virtual float ChordHeight {
        get
        {
            if (chordHeight == null)
            {
                float chordSqr = Size.x * Size.x + Size.y * Size.y;
                chordHeight = SphericalUtils.CalculateChordHeight(chordSqr, true);
            }
            return chordHeight.Value;
        }
    }

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

    public virtual Vector3 Position 
    {
        get => position;
        set
        {
            position = value;
            Simple.Position = position;
        }
    }

    public void Reorient(Quaternion _orientation)
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
            Timed(() => Reorient(_orientation), methodIds[2] ??= Record.GetId(typeName, nameof(Reorient)));
            Timed(() => Resize(_size), methodIds[3] ??= Record.GetId(typeName, nameof(Resize)));
            Timed(() => Reposition(-Forward), methodIds[4] ??= Record.GetId(typeName, nameof(Reposition)));
            Timed(() => UpdateSimpleSize(), methodIds[5] ??= Record.GetId(typeName, nameof(UpdateSimpleSize)));
        } else
        {
            Reorient(_orientation);
            Resize(_size);
            Reposition(-Forward);
            UpdateSimpleSize();   
        }
    }

    public bool CheckFastOverlaps(IBoundingVolume other) => Simple.SimpleIntersects(other.Simple);
	public virtual bool CheckSphere (BBoxSphere sphere) => false;
    public virtual bool CheckOBB (OBBox obb) => false;

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
            BBoxSphere sphere => Timed(() => CheckSphere(sphere), methodIds[0] ??= Record.GetId(typeName, nameof(CheckSphere))),
            OBBox obb => Timed(() => CheckOBB(obb), methodIds[1] ??= Record.GetId(typeName, nameof(CheckOBB))),
            _ => false
        };
    }
}

public class BBoxSphere : BBox
{
    public BBoxSphere(int id) : base(id) {}
    public float Radius { get; set; }
    public float CosRadius { get; set; }

    public override float ChordHeight
    {
        get
        {
            chordHeight ??= SphericalUtils.CalculateChordHeight(Size.x);
            return chordHeight.Value;
        }
    }

    public override Vector3 Position { 
        get => base.Position; 
        set => base.Position = value * ChordHeight; 
    }
    public override Vector3 Size
    {
        get => base.Size;
        set
        {
            if (base.Size.x == value.x) { return; }
            base.Size = new Vector3(value.x, value.x, value.x);
            Radius = value.x * 0.5f;
            CosRadius = Mathf.Cos(SphericalUtils.ChordToAngle(value.x) * 0.5f);
        }
    }
    public override float Chord { get => Size.x; }
    
    public override void UpdateSimpleSize () {}
    public override bool CheckSphere(BBoxSphere sphere)
    {
        float sqrDist = (sphere.Position - Position).sqrMagnitude;
        float combinedRadius = Radius + sphere.Radius;
        return  combinedRadius * combinedRadius > sqrDist;
    }
    public override bool CheckOBB(OBBox obb) => obb.CheckSphere(this);
}

public class OBBox : BBox
{
    public OBBox(int id) : base(id) {}

    public override Vector3 Size
    {
        get => base.Size;
        set
        {
            if (base.Size.x == value.x && base.Size.y == value.y) { return; }

            base.Size = value;
            base.Size = new(value.x, value.y, 1f - ChordHeight);
            
            halfSize = base.Size * 0.5f;
        }
    }
    public override Vector3 Position { 
        get => base.Position; 
        set => base.Position = value * (0.5f + 0.5f*ChordHeight);
    }

    private Vector3 halfSize;
    public Vector3 HalfSize { get => halfSize; }

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

    public override bool CheckOBB (OBBox obb) => SAT(obb);

    public override bool CheckSphere(BBoxSphere sphere) //https://gamedev.stackexchange.com/questions/163873/separating-axis-theorem-obb-vs-sphere
    {
        Vector3 obbToSphere = sphere.Position - Position;

        Vector3 sphereLocalPos = new (
            Vector3.Dot(obbToSphere, Right),
            Vector3.Dot(obbToSphere, Up),
            Vector3.Dot(obbToSphere, Forward)
        );

        Vector3 closestPointLocal = new(
            Mathf.Clamp(sphereLocalPos.x, -HalfSize.x, HalfSize.x),
            Mathf.Clamp(sphereLocalPos.y, -HalfSize.y, HalfSize.y),
            Mathf.Clamp(sphereLocalPos.z, -HalfSize.z, HalfSize.z)
        );

        float sqrDist = (closestPointLocal - sphereLocalPos).sqrMagnitude;
        return sphere.Radius * sphere.Radius > sqrDist;
    }
}