using UnityEngine;
using System;
using System.Diagnostics;

public class BBox1 : IBoundingVolume
{
    public RuntimeRecord Record { get; set; }
    private float width;
    public virtual float Width 
    { 
        get => width;
        set 
        {
            max_x = null;
            max_y = null;
            max_z = null;
            min_x = null;
            min_y = null;
            min_z = null;
            width = value;
        }
    }

    public virtual Vector3 Right { get => Vector3.right; }
    public virtual Vector3 Up { get => Vector3.up; }
    public virtual Vector3 Forward { get => Vector3.forward; }
    public virtual float RightWidth { get => Width; }
    public virtual float UpWidth { get => Width; }
    public virtual float ForwardWidth { get => Width; }

    private Vector3 position;
    public Vector3 Position 
    {
        get => position;
        set
        {
            max_x = null;
            max_y = null;
            max_z = null;
            min_x = null;
            min_y = null;
            min_z = null;
            position = value;
        }
    }
    private float? max_x;
    private float? max_y;
    private float? max_z;
    private float? min_x;
    private float? min_y;
    private float? min_z;

    public float MaxX
    {
        get => max_x ??= Position.x + Width/2f;
    }
    public float MaxY
    {
        get => max_y ??= Position.y + Width/2f;
    }
    public float MaxZ
    {
        get => max_z ??= Position.z + Width/2f;
    }
    public float MinX
    {
        get => min_x ??= Position.x - Width/2f;
    }
    public float MinY
    {
        get => min_y ??= Position.y - Width/2f;
    }
    public float MinZ
    {
        get => min_z ??= Position.z - Width/2f;
    }

    public Vector3[][] GetAABBEdges()
    {
        Vector3[] _vertices =
        {
            new (MinX, MinY, MinZ), //left    top     front
            new (MaxX, MinY, MinZ), //right   top     front
            new (MinX, MaxY, MinZ), //left    bottom  front
            new (MaxX, MaxY, MinZ), //right   bottom  front
            new (MinX, MinY, MaxZ), //left    top     back
            new (MaxX, MinY, MaxZ), //right   top     back
            new (MinX, MaxY, MaxZ), //left    bottom  back
            new (MaxX, MaxY, MaxZ)  //right   bottom  back
        };

        Face[] _faces =
        {
            new (_vertices[0], _vertices[1], _vertices[3], _vertices[2]),   //front
            new (_vertices[4], _vertices[0], _vertices[2], _vertices[6]),   //left
            new (_vertices[1], _vertices[5], _vertices[7], _vertices[3]),   //right
            new (_vertices[4], _vertices[5], _vertices[1], _vertices[0]),   //top
            new (_vertices[2], _vertices[3], _vertices[7], _vertices[6]),   //bottom
            new (_vertices[5], _vertices[4], _vertices[6], _vertices[7])    //back
        };
        Vector3[][] _edges = new Vector3[24][];
        for (int i = 0; i < _faces.Length; i++)
        {
            Vector3[][] _face_edges = _faces[i].Edges;
            _edges[i*4] = _face_edges[0];
            _edges[i*4+1] = _face_edges[1];
            _edges[i*4+2] = _face_edges[2];
            _edges[i*4+3] = _face_edges[3];
        }
        return _edges;
    }

    public bool CheckContains(IBoundingVolume other)
    {
        return other switch
        {
            BBox1 bbox => Timed(bbox, AABBContains),
            _ => false
        };
    }

    public bool AABBContains(BBox1 other)
    {
        return (
            MaxX >= other.MaxX &&
            MaxY >= other.MaxY &&
            MaxZ >= other.MaxZ &&
            MinX <= other.MinX &&
            MinY <= other.MinY &&
            MinZ <= other.MinZ
		);
    }

    public bool CheckFastOverlaps(IBoundingVolume other)
    {
        return other switch
        {
            BBox1 bbox => Timed(bbox, AABBIntersects),
            _ => false
        };
    }

    public virtual bool AABBIntersects(BBox1 other)
    {
        return !(
            MaxX <= other.MinX || MinX >= other.MaxX ||
            MaxY <= other.MinY || MinY >= other.MaxY ||
            MaxZ <= other.MinZ || MinZ >= other.MaxZ
		);
    }

    public virtual Vector3 CheckPoint (Vector3 point)
    {
        float overlapx = Math.Min(MaxX, point.x) - Math.Max(MinX, point.x);
        float overlapy = Math.Min(MaxY, point.y) - Math.Max(MinY, point.y);
        float overlapz = Math.Min(MaxZ, point.z) - Math.Max(MinZ, point.z);

        if (overlapx <= overlapy && overlapx <= overlapz)
        {
            return new Vector3(Position.x < point.x ? -overlapx : overlapx, 0, 0);
        }
        else if (overlapy <= overlapx && overlapy <= overlapz)
        {
            return new Vector3(0, Position.y < point.y ? -overlapy : overlapy, 0);   
        }
        else
        {
            return new Vector3(0, 0, Position.z < point.z ? -overlapz : overlapz);   
        }
    }
	
	public virtual bool CheckCircle (BBoxCircle circle)
    {
        return false;
    }
    
    public virtual bool CheckOBR (OBRectangle obr) => obr.SAT(this);

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
            BBoxCircle circle => Timed(circle, CheckCircle),
            OBRectangle obr => Timed(obr, CheckOBR),
            BBox1 aabb => Timed(aabb, AABBIntersects),
            _ => false
        };
    }
}

public class BBoxCircle : BBox1
{
    private float radius;
    private float radius2;
    public float Radius { get => radius; set { radius = value; } }
    public override float Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            Radius = value/2f;
            radius2 = Radius*Radius;
        }
    }
    public override bool CheckCircle(BBoxCircle circle)
    {
        float eDist = EllipticBBox.EllipticDistance(Position, circle.Position);
        float combinedRadii = Radius + circle.Radius;
        return combinedRadii < eDist;
    }
    public override bool CheckOBR(OBRectangle obr) => obr.CheckCircle(this);
}

public class OBRectangle : BBox1
{
    //https://dev.to/pratyush_mohanty_6b8f2749/the-math-behind-bounding-box-collision-detection-aabb-vs-obrseparate-axis-theorem-1gdn
    bool SATAxis(BBox1 bbox, Vector3 axis, float scalar)
    {
        float left = MathF.Abs(Vector3.Dot(bbox.Position - Position, axis));
        float right = scalar + 
            MathF.Abs(Vector3.Dot(bbox.RightWidth/2 * bbox.Right, axis)) +
            MathF.Abs(Vector3.Dot(bbox.UpWidth/2 * bbox.Up, axis)) + 
            MathF.Abs(Vector3.Dot(bbox.ForwardWidth/2 * bbox.Forward, axis));

        return left <= right;
    }
    
    public bool SAT(BBox1 bbox)
    {
        return 
            SATAxis(bbox, Right, RightWidth/2) || 
            SATAxis(bbox, Up, UpWidth/2) || 
            SATAxis(bbox, Forward, ForwardWidth/2);
    }

    public override bool CheckOBR (OBRectangle obr)
    {
        return SAT(obr);
    }

    public override bool CheckCircle(BBoxCircle circle) //https://gamedev.stackexchange.com/questions/163873/separating-axis-theorem-obr-vs-circle
    {
        Vector3 obrToCircle = circle.Position - Position;

        Vector3 local_pos = new (
            Vector3.Dot(obrToCircle, Right),
            Vector3.Dot(obrToCircle, Up),
            Vector3.Dot(obrToCircle, Forward)
        );

        Vector3 closestPointLocal = new(
            Mathf.Clamp(local_pos.x, -RightWidth/2, RightWidth/2),
            Mathf.Clamp(local_pos.y, -UpWidth/2, UpWidth/2),
            Mathf.Clamp(local_pos.z, -ForwardWidth/2, ForwardWidth/2)
        );

        float eDist = EllipticBBox.EllipticDistance(closestPointLocal, local_pos);

        return eDist < circle.Radius;
    }
}