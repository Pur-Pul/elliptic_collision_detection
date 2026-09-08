using UnityEngine;
using System;
using System.Diagnostics;

/*
This is an unused class to experiment with bounding volume heirarchies. It is very unfinished.
*/

public class BVH : IBoundingVolume
{
    private int _id;
    public int Id { get => _id; }
    private string typeName = "";
    private int?[] methodIds;
    public BVH (int id)
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

    public bool shapeUpdated = false;
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

    public bool PruneSBC { get; set; }
    public bool PruneSOBR { get; set; }
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
    public virtual void UpdateSimpleSize () {}
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
            BVH_BC bc => Simple.SimpleIntersects(bc.Simple),
            OBBox obb => Simple.SimpleIntersects(obb.Simple),
            _ => false
        };
    }
}

public class BVH_BC : BVH
{
    public BVH_BC(int id) : base(id) {}
    public float Radius { get; set; }
    public float CosRadius { get; set; }
    public float SinRadius { get; set; }

    public override Vector3 Size
    {
        get => base.Size;
        set
        {
            if (base.Size.x == value.x) { return; }
            base.Size = new Vector3(value.x, value.x, value.x);
            Radius = value.x * 0.5f;
            float radiusAngle = SphericalUtils.ChordToAngle(value.x) * 0.5f;
            CosRadius = Mathf.Cos(radiusAngle);
            SinRadius = Mathf.Sin(radiusAngle);
        }
    }

    public override float Chord => Size.x;
    
    public override void UpdateSimpleSize ()
    {
        if (shapeUpdated) {
            //Position.y * z - Position.z * y
            //Position.x * z - Position.z * x
            //Position.x * y - Position.y * x

            //right (1,0,0)
            float sinRight = Mathf.Sqrt(1 - Position.x*Position.x);
            float maxRight = Position.x >= CosRadius
                ? 1f
                : Position.x*CosRadius + sinRight*SinRadius;
            float minRight = -Position.x >= CosRadius
                ? -1f
                : Position.x*CosRadius - sinRight*SinRadius;

            //up (0,1,0)
            float sinUp = Mathf.Sqrt(1 - Position.y*Position.y);
            float maxUp = Position.y >= CosRadius
                ? 1f
                : Position.y*CosRadius + sinUp*SinRadius;
            float minUp = -Position.y >= CosRadius
                ? -1f
                : Position.y*CosRadius - sinUp*SinRadius;
            
            //forward (0,0,1)
            float sinForward = Mathf.Sqrt(1 - Position.z*Position.z);
            float maxForward = Position.z >= CosRadius
                ? 1f
                : Position.z*CosRadius + sinForward*SinRadius;
            float minForward = -Position.z >= CosRadius
                ? -1f
                : Position.z*CosRadius - sinForward*SinRadius;

            Simple.Size = new Vector3(
                maxRight - minRight,
                maxUp - minUp,
                maxForward - minForward
            );
            Simple.Position = new Vector3(
                (minRight + maxRight) * 0.5f,
                (minUp + maxUp) * 0.5f,
                (minForward + maxForward) * 0.5f
            );
            shapeUpdated = false;
        }
    }
}

public class BVH_OBR : BVH
{
    public BVH_OBR(int id) : base(id) {}

    private Vector3 halfSize;
    public Vector3 HalfSize { get => halfSize; }
}