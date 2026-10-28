using System;
using System.Collections.Generic;
using System.Linq;

namespace DTools.Core
{
    /// <summary>
    /// Điểm tọa độ 2D thuần túy không phụ thuộc AutoCAD API
    /// </summary>
    public struct Point2D : IEquatable<Point2D>
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("Tọa độ X không hợp lệ.");
            if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentException("Tọa độ Y không hợp lệ.");
            X = x;
            Y = y;
        }

        public double DistanceTo(Point2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public Point2D Offset(double dx, double dy) => new Point2D(X + dx, Y + dy);

        public bool Equals(Point2D other) => Math.Abs(X - other.X) < 1e-6 && Math.Abs(Y - other.Y) < 1e-6;
        public override bool Equals(object obj) => obj is Point2D other && Equals(other);
        public override int GetHashCode() => (X, Y).GetHashCode();
        public override string ToString() => $"({X:F2}, {Y:F2})";

        public static Point2D operator +(Point2D a, Vector2D b) => new Point2D(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator -(Point2D a, Point2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
    }

    /// <summary>
    /// Vector 2D thuần túy
    /// </summary>
    public struct Vector2D
    {
        public double X { get; }
        public double Y { get; }

        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Length => Math.Sqrt(X * X + Y * Y);

        public Vector2D Normalize()
        {
            double len = Length;
            if (len < 1e-9) return new Vector2D(0, 0);
            return new Vector2D(X / len, Y / len);
        }

        public Vector2D Perpendicular() => new Vector2D(-Y, X);

        public double DotProduct(Vector2D other) => X * other.X + Y * other.Y;

        public static Vector2D operator *(Vector2D v, double scale) => new Vector2D(v.X * scale, v.Y * scale);
    }

    /// <summary>
    /// Đoạn thẳng 2D
    /// </summary>
    public class Line2D
    {
        public Point2D Start { get; }
        public Point2D End { get; }

        public Line2D(Point2D start, Point2D end)
        {
            Start = start;
            End = end;
        }

        public double Length => Start.DistanceTo(End);

        public Point2D Midpoint => new Point2D((Start.X + End.X) / 2.0, (Start.Y + End.Y) / 2.0);

        public Vector2D Direction => (End - Start).Normalize();

        public (Line2D Left, Line2D Right) Offset(double distance)
        {
            Vector2D normal = Direction.Perpendicular() * distance;
            Line2D left = new Line2D(Start + normal, End + normal);
            Line2D right = new Line2D(Start + (normal * -1.0), End + (normal * -1.0));
            return (left, right);
        }
    }

    /// <summary>
    /// Đa giác / Đường gấp khúc 2D
    /// </summary>
    public class Polyline2D
    {
        public List<Point2D> Vertices { get; }
        public bool IsClosed { get; }

        public Polyline2D(IEnumerable<Point2D> vertices, bool isClosed = true)
        {
            Vertices = vertices?.ToList() ?? new List<Point2D>();
            IsClosed = isClosed;
        }

        public double TotalLength()
        {
            if (Vertices.Count < 2) return 0.0;
            double total = 0.0;
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                total += Vertices[i].DistanceTo(Vertices[i + 1]);
            }
            if (IsClosed && Vertices.Count > 2)
            {
                total += Vertices[Vertices.Count - 1].DistanceTo(Vertices[0]);
            }
            return total;
        }

        public double CalculateArea()
        {
            if (Vertices.Count < 3) return 0.0;
            double area = 0.0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                Point2D cur = Vertices[i];
                Point2D next = Vertices[(i + 1) % Vertices.Count];
                area += (cur.X * next.Y) - (next.X * cur.Y);
            }
            return Math.Abs(area) / 2.0;
        }

        public (Point2D Min, Point2D Max) GetBoundingBox()
        {
            if (Vertices.Count == 0) return (new Point2D(0, 0), new Point2D(0, 0));
            double minX = Vertices.Min(v => v.X);
            double maxX = Vertices.Max(v => v.X);
            double minY = Vertices.Min(v => v.Y);
            double maxY = Vertices.Max(v => v.Y);
            return (new Point2D(minX, minY), new Point2D(maxX, maxY));
        }
    }
}
