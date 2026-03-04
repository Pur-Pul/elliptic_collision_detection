using System.Collections.Generic;

public class SpeedRecord
{
    Dictionary<(BoundingType, BoundingType), (long, int)> collisions;

    public void Reset()
    {
        collisions.Clear();
    }

    public void Collision (long start, long end, (BoundingType, BoundingType) key)
    {
        collisions.TryGetValue(key, out var record);
        collisions[key] = (record.Item1 + end - start, record.Item2 + 1);
    }
}