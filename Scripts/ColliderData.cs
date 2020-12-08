using System.Collections.Generic;
using UnityEngine;

public enum ColliderType
{
    Box,
    Capsule,
    Circle,
    Polygon
}

[System.Serializable]
public class ColliderData
{
    public ColliderType type;
    public Vector2 position;
    public float rotation;
    public Vector2 size;
    public CapsuleDirection2D direction;
    public float radius;
    public List<PolygonPath> paths;

    public ColliderData(ColliderType type = ColliderType.Box)
    {
        this.type = type;
        if(type == ColliderType.Capsule)
            size = new Vector2(0.5f, 1);
        else
            size = Vector2.one;
        radius = 0.5f;
        paths = new List<PolygonPath>(new PolygonPath[] { new PolygonPath(true) });
    }

    private ColliderData() { }

    public ColliderData Clone()
    {
        ColliderData clone = new ColliderData();
        clone.type = type;
        clone.position = position;
        clone.rotation = rotation;
        clone.size = size;
        clone.direction = direction;
        clone.radius = radius;
        clone.paths = new List<PolygonPath>();
        for(int i = 0; i < paths.Count; i++)
            clone.paths.Add(paths[i].Clone());
        return clone;
    }

    public Rect Bounds()
    {
        switch(type)
        {
            case ColliderType.Box :
                return new Rect(position - size * 0.5f, size);
            case ColliderType.Capsule :
                Vector2 realSize = size;
                switch(direction)
                {
                    case CapsuleDirection2D.Vertical :
                        realSize.y = Mathf.Max(realSize.x, realSize.y);
                        break;
                    case CapsuleDirection2D.Horizontal :
                        realSize.x = Mathf.Max(realSize.x, realSize.y);
                        break;
                }
                return new Rect(position - realSize * 0.5f, realSize);
            case ColliderType.Circle :
                Vector2 realRadius = new Vector2(radius, radius);
                return new Rect(position - realRadius, realRadius * 2);
            case ColliderType.Polygon :
                bool hasPoint = false;
                Vector2 minPoint = Vector2.positiveInfinity;
                Vector2 maxPoint = Vector2.negativeInfinity;
                for(int i = 0; i < paths.Count; i++)
                {
                    for(int j = 0; j < paths[i].Count; j++)
                    {
                        hasPoint = true;
                        minPoint = new Vector2(Mathf.Min(paths[i][j].x, minPoint.x), Mathf.Min(paths[i][j].y, minPoint.y));
                        maxPoint = new Vector2(Mathf.Max(paths[i][j].x, maxPoint.x), Mathf.Max(paths[i][j].y, maxPoint.y));
                    }
                }
                if(hasPoint)
                    return new Rect(position + minPoint, maxPoint - minPoint);
                break;
        }
        return new Rect(0, 0, 0, 0);
    }

    public bool Contains(Vector2 point)
    {
        Vector2 rotatedPoint = point;
        if(rotation != 0)
        {
            float radians = rotation * Mathf.Deg2Rad;
            rotatedPoint = new Vector2(Mathf.Cos(radians) * (point.x - position.x) - Mathf.Sin(radians) * (point.y - position.y) + position.x,
                                        Mathf.Sin(radians) * (point.x - position.x) + Mathf.Cos(radians) * (point.y - position.y) + position.y);
        }
        
        Rect bounds = Bounds();
        if(bounds.width > 0 && bounds.height > 0 && bounds.Contains(rotatedPoint))
        {
            switch(type)
            {
                case ColliderType.Box :
                    return true;
                case ColliderType.Capsule :
                    float capsuleRadius = 0;
                    switch(direction)
                    {
                        case CapsuleDirection2D.Vertical :
                            capsuleRadius = size.x * 0.5f;
                            if(size.y <= size.x)
                                return (rotatedPoint - position).sqrMagnitude <= capsuleRadius * capsuleRadius;
                            if(rotatedPoint.y < bounds.yMin + capsuleRadius)
                                return (rotatedPoint - new Vector2(position.x, bounds.yMin + capsuleRadius)).sqrMagnitude <= capsuleRadius * capsuleRadius;
                            if(rotatedPoint.y > bounds.yMax - capsuleRadius)
                                return (rotatedPoint - new Vector2(position.x, bounds.yMax - capsuleRadius)).sqrMagnitude <= capsuleRadius * capsuleRadius;
                            return true;
                        case CapsuleDirection2D.Horizontal :
                            capsuleRadius = size.y * 0.5f;
                            if(size.x <= size.y)
                                return (rotatedPoint - position).sqrMagnitude <= capsuleRadius * capsuleRadius;
                            if(rotatedPoint.x < bounds.xMin + capsuleRadius)
                                return (rotatedPoint - new Vector2(bounds.xMin + capsuleRadius, position.y)).sqrMagnitude <= capsuleRadius * capsuleRadius;
                            if(rotatedPoint.x > bounds.xMax - capsuleRadius)
                                return (rotatedPoint - new Vector2(bounds.xMax - capsuleRadius, position.y)).sqrMagnitude <= capsuleRadius * capsuleRadius;
                            return true;
                    }
                    break;
                case ColliderType.Circle :
                    return (rotatedPoint - position).sqrMagnitude <= radius * radius;
                case ColliderType.Polygon :
                    Vector2 localPoint = rotatedPoint - position;
                    for(int i = 0; i < paths.Count; i++)
                        if(PolygonContains(paths[i].path, localPoint))
                            return true;
                    return false;
            }
        }
        return false;
    }

    bool PolygonContains(List<Vector2> polygon, Vector2 point)
    {
        bool oddNodes = false;
        bool current = polygon[polygon.Count - 1].y > point.y;
        bool previous;

        int j = polygon.Count - 1;

        for(int i = 0; i < polygon.Count; i++)
        {
            previous = current;
            current = polygon[i].y > point.y;
            if(current != previous)
            {
                float constant = polygon[i].x;
                float multiple = 0;
                if(polygon[j].y != polygon[i].y)
                {
                    constant = polygon[i].x - (polygon[i].y * polygon[j].x) / (polygon[j].y - polygon[i].y) + (polygon[i].y * polygon[i].x) / (polygon[j].y - polygon[i].y);
                    multiple = (polygon[j].x - polygon[i].x) / (polygon[j].y - polygon[i].y);
                }
                oddNodes ^= point.y * multiple + constant < point.x;
            }
            j = i;
        }
        return oddNodes;
    }

    public Mesh MakeMesh(bool flipY = false)
    {
        switch(type)
        {
            case ColliderType.Box :
                return MakeBoxMesh(size, Vector2.zero, flipY);
            case ColliderType.Capsule :
                return MakeCapsuleMesh(size, direction, Vector2.zero, flipY);
            case ColliderType.Circle :
                return MakeCircleMesh(radius, Vector2.zero, flipY);
            case ColliderType.Polygon :
                return MakePolygonMesh(paths, Vector2.zero, flipY);
        }
        return null;
    }

    public static Mesh MakeMesh(Collider2D collider2D, bool flipY = false)
    {
        if(collider2D.GetType() == typeof(BoxCollider2D))
        {
            BoxCollider2D boxCollider2D = (BoxCollider2D)collider2D;
            return MakeBoxMesh(boxCollider2D.size, boxCollider2D.offset, flipY);
        } else if(collider2D.GetType() == typeof(CapsuleCollider2D))
        {
            CapsuleCollider2D capsuleCollider2D = (CapsuleCollider2D)collider2D;
            return MakeCapsuleMesh(capsuleCollider2D.size, capsuleCollider2D.direction, capsuleCollider2D.offset, flipY);
        } else if(collider2D.GetType() == typeof(CircleCollider2D))
        {
            CircleCollider2D circleCollider2D = (CircleCollider2D)collider2D;
            return MakeCircleMesh(circleCollider2D.radius, circleCollider2D.offset, flipY);
        } else if(collider2D.GetType() == typeof(PolygonCollider2D))
        {
            PolygonCollider2D polygonCollider2D = (PolygonCollider2D)collider2D;
            Vector2[][] paths = new Vector2[polygonCollider2D.pathCount][];
            for(int i = 0; i < paths.Length; i++)
                paths[i] = polygonCollider2D.GetPath(i);
            return MakePolygonMesh(paths, polygonCollider2D.offset, flipY);
        }
        return null;
    }

    public static Mesh MakeBoxMesh(Vector2 size, Vector2 offset, bool flipY = false)
    {
        Vector2[] points = new Vector2[]
        {
            offset + new Vector2(-size.x, size.y) * 0.5f,
            offset + size * 0.5f,
            offset + new Vector2(size.x, -size.y) * 0.5f,
            offset - size * 0.5f
        };

        Mesh mesh = MeshPoints(points, flipY);
        mesh.name = "BoxCollider2D";
        return mesh;
    }

    public static Mesh MakeCapsuleMesh(Vector2 size, CapsuleDirection2D direction, Vector2 offset, bool flipY = false)
    {
        switch(direction)
        {
            case CapsuleDirection2D.Vertical :
                if(size.x >= size.y)
                {
                    Mesh mesh = MeshPoints(CirclePoints(size.x * 0.5f, offset), flipY);
                    mesh.name = "CapsuleCollider2D";
                    return mesh;
                } else
                {
                    float radius = size.x * 0.5f;
                    Vector2 circleCenter = new Vector2(0, size.y * 0.5f - radius);
                    Vector2[] points = new Vector2[]
                    {
                        offset + circleCenter + new Vector2(-radius, 0),
                        offset + circleCenter + new Vector2(-radius * cos15, radius * sin15),
                        offset + circleCenter + new Vector2(-radius * sqrt3div2, radius * 0.5f),
                        offset + circleCenter + new Vector2(-radius * sqrt2div2, radius * sqrt2div2),
                        offset + circleCenter + new Vector2(-radius * 0.5f, radius * sqrt3div2),
                        offset + circleCenter + new Vector2(-radius * sin15, radius * cos15),

                        offset + circleCenter + new Vector2(0, radius),
                        offset + circleCenter + new Vector2(radius * sin15, radius * cos15),
                        offset + circleCenter + new Vector2(radius * 0.5f, radius * sqrt3div2),
                        offset + circleCenter + new Vector2(radius * sqrt2div2, radius * sqrt2div2),
                        offset + circleCenter + new Vector2(radius * sqrt3div2, radius * 0.5f),
                        offset + circleCenter + new Vector2(radius * cos15, radius * sin15),
                        offset + circleCenter + new Vector2(radius, 0),

                        offset - circleCenter + new Vector2(radius, 0),
                        offset - circleCenter + new Vector2(radius * cos15, -radius * sin15),
                        offset - circleCenter + new Vector2(radius * sqrt3div2, -radius * 0.5f),
                        offset - circleCenter + new Vector2(radius * sqrt2div2, -radius * sqrt2div2),
                        offset - circleCenter + new Vector2(radius * 0.5f, -radius * sqrt3div2),
                        offset - circleCenter + new Vector2(radius * sin15, -radius * cos15),

                        offset - circleCenter + new Vector2(0, -radius),
                        offset - circleCenter + new Vector2(-radius * sin15, -radius * cos15),
                        offset - circleCenter + new Vector2(-radius * 0.5f, -radius * sqrt3div2),
                        offset - circleCenter + new Vector2(-radius * sqrt2div2, -radius * sqrt2div2),
                        offset - circleCenter + new Vector2(-radius * sqrt3div2, -radius * 0.5f),
                        offset - circleCenter + new Vector2(-radius * cos15, -radius * sin15),
                        offset - circleCenter + new Vector2(-radius, 0)
                    };

                    Mesh mesh = MeshPoints(points, flipY);
                    mesh.name = "CapsuleCollider2D";
                    return mesh;
                }
            case CapsuleDirection2D.Horizontal :
                if(size.y >= size.x)
                {
                    Mesh mesh = MeshPoints(CirclePoints(size.y * 0.5f, offset), flipY);
                    mesh.name = "CapsuleCollider2D";
                    return mesh;
                } else
                {
                    float radius = size.y * 0.5f;
                    Vector2 circleCenter = new Vector2(size.x * 0.5f - radius, 0);
                    Vector2[] points = new Vector2[]
                    {
                        offset + circleCenter + new Vector2(0, radius),
                        offset + circleCenter + new Vector2(radius * sin15, radius * cos15),
                        offset + circleCenter + new Vector2(radius * 0.5f, radius * sqrt3div2),
                        offset + circleCenter + new Vector2(radius * sqrt2div2, radius * sqrt2div2),
                        offset + circleCenter + new Vector2(radius * sqrt3div2, radius * 0.5f),
                        offset + circleCenter + new Vector2(radius * cos15, radius * sin15),

                        offset + circleCenter + new Vector2(radius, 0),
                        offset + circleCenter + new Vector2(radius * cos15, -radius * sin15),
                        offset + circleCenter + new Vector2(radius * sqrt3div2, -radius * 0.5f),
                        offset + circleCenter + new Vector2(radius * sqrt2div2, -radius * sqrt2div2),
                        offset + circleCenter + new Vector2(radius * 0.5f, -radius * sqrt3div2),
                        offset + circleCenter + new Vector2(radius * sin15, -radius * cos15),
                        offset + circleCenter + new Vector2(0, -radius),

                        offset - circleCenter + new Vector2(0, -radius),
                        offset - circleCenter + new Vector2(-radius * sin15, -radius * cos15),
                        offset - circleCenter + new Vector2(-radius * 0.5f, -radius * sqrt3div2),
                        offset - circleCenter + new Vector2(-radius * sqrt2div2, -radius * sqrt2div2),
                        offset - circleCenter + new Vector2(-radius * sqrt3div2, -radius * 0.5f),
                        offset - circleCenter + new Vector2(-radius * cos15, -radius * sin15),

                        offset - circleCenter + new Vector2(-radius, 0),
                        offset - circleCenter + new Vector2(-radius * cos15, radius * sin15),
                        offset - circleCenter + new Vector2(-radius * sqrt3div2, radius * 0.5f),
                        offset - circleCenter + new Vector2(-radius * sqrt2div2, radius * sqrt2div2),
                        offset - circleCenter + new Vector2(-radius * 0.5f, radius * sqrt3div2),
                        offset - circleCenter + new Vector2(-radius * sin15, radius * cos15),
                        offset - circleCenter + new Vector2(0, radius),
                    };

                    Mesh mesh = MeshPoints(points, flipY);
                    mesh.name = "CapsuleCollider2D";
                    return mesh;
                }
        }
        return null;
    }

    public static Mesh MakeCircleMesh(float radius, Vector2 offset, bool flipY = false)
    {
        Mesh mesh = MeshPoints(CirclePoints(radius, offset), flipY);
        mesh.name = "CircleCollider2D";
        return mesh;
    }

    public static Mesh MakePolygonMesh(Vector2[][] paths, Vector2 offset, bool flipY = false)
    {
        CombineInstance[] combine = new CombineInstance[paths.Length];
        for(int i = 0; i < paths.Length; i++)
        {
            Vector2[] points = new Vector2[paths[i].Length];
            for(int j = 0; j < points.Length; j++)
                points[j] = offset + paths[i][j];
            combine[i].mesh = MeshPoints(points, flipY);
            combine[i].transform = Matrix4x4.identity;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine);
        mesh.name = "PolygonCollider2D";
        return mesh;
    }

    public static Mesh MakePolygonMesh(List<PolygonPath> paths, Vector2 offset, bool flipY = false)
    {
        CombineInstance[] combine = new CombineInstance[paths.Count];
        for(int i = 0; i < paths.Count; i++)
        {
            Vector2[] points = new Vector2[paths[i].Count];
            for(int j = 0; j < points.Length; j++)
                points[j] = offset + paths[i][j];
            combine[i].mesh = MeshPoints(points, flipY);
            combine[i].transform = Matrix4x4.identity;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine);
        mesh.name = "PolygonCollider2D";
        return mesh;
    }

    public static Mesh MeshPoints(Vector2[] points, bool flipY = false)
    {
        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[points.Length];
        if(flipY)
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                points[i].y = -points[i].y;
                vertices[i] = new Vector3(points[i].x, points[i].y, 0);
            }
        } else
        {
            for(int i = 0; i < vertices.Length; i++)
                vertices[i] = new Vector3(points[i].x, points[i].y, 0);
        }
        
        // Use the triangulator to get indices for creating triangles
        Triangulator triangulator = new Triangulator(points);
        int[] indices = triangulator.Triangulate();

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    const float sqrt2div2 = 0.70710678118f;
    const float sqrt3div2 = 0.86602540378f;
    const float cos15 = 0.96592582628f;
    const float sin15 = 0.2588190451f;

    public static Vector2[] CirclePoints(float radius, Vector2 offset)
    {
        return new Vector2[]
        {
            offset + new Vector2(0, radius),
            offset + new Vector2(radius * sin15, radius * cos15),
            offset + new Vector2(radius * 0.5f, radius * sqrt3div2),
            offset + new Vector2(radius * sqrt2div2, radius * sqrt2div2),
            offset + new Vector2(radius * sqrt3div2, radius * 0.5f),
            offset + new Vector2(radius * cos15, radius * sin15),

            offset + new Vector2(radius, 0),
            offset + new Vector2(radius * cos15, -radius * sin15),
            offset + new Vector2(radius * sqrt3div2, -radius * 0.5f),
            offset + new Vector2(radius * sqrt2div2, -radius * sqrt2div2),
            offset + new Vector2(radius * 0.5f, -radius * sqrt3div2),
            offset + new Vector2(radius * sin15, -radius * cos15),

            offset + new Vector2(0, -radius),
            offset + new Vector2(-radius * sin15, -radius * cos15),
            offset + new Vector2(-radius * 0.5f, -radius * sqrt3div2),
            offset + new Vector2(-radius * sqrt2div2, -radius * sqrt2div2),
            offset + new Vector2(-radius * sqrt3div2, -radius * 0.5f),
            offset + new Vector2(-radius * cos15, -radius * sin15),

            offset + new Vector2(-radius, 0),
            offset + new Vector2(-radius * cos15, radius * sin15),
            offset + new Vector2(-radius * sqrt3div2, radius * 0.5f),
            offset + new Vector2(-radius * sqrt2div2, radius * sqrt2div2),
            offset + new Vector2(-radius * 0.5f, radius * sqrt3div2),
            offset + new Vector2(-radius * sin15, radius * cos15)
        };
    }

    public static MeshRenderer MakeMeshRenderer(Collider2D collider2D, Material material = null)
    {
        Mesh mesh = MakeMesh(collider2D);
        if(mesh != null)
        {
            collider2D.gameObject.AddComponent<MeshFilter>().mesh = mesh;
            MeshRenderer renderer = collider2D.gameObject.AddComponent<MeshRenderer>();
            renderer.material = material;
            return renderer;
        }
        return null;
    }
}

[System.Serializable]
public class PolygonPath
{
    public List<Vector2> path = new List<Vector2>();
    public Vector2 this[int i] { get { return path[i]; } set { path[i] = value; } }
    public int Count => path.Count;
    
    private static Vector2[] pentagonPoints = new Vector2[]
    {
        new Vector2(0, 1),
        new Vector2(-0.9510565f, 0.309017f),
        new Vector2(-0.5877852f, -0.8090171f),
        new Vector2(0.5877854f, -0.8090169f),
        new Vector2(0.9510565f, 0.3090171f)
    };

    public PolygonPath() : this(true) { }

    public PolygonPath(bool makePentagon = true)
    {
        if(makePentagon)
            path = new List<Vector2>(pentagonPoints);
    }

    public void Add(Vector2 point)
    {
        path.Add(point);
    }

    public void Remove(Vector2 point)
    {
        path.Remove(point);
    }

    public void RemoveAt(int i)
    {
        path.RemoveAt(i);
    }

    public PolygonPath Clone()
    {
        PolygonPath clone = new PolygonPath();
        for(int i = 0; i < Count; i++)
            clone.Add(path[i]);
        return clone;
    }
}