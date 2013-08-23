using System;
using System.Collections.Generic;
using System.Linq;

namespace Triangulation {

    /// <summary>
    /// Represents a 2D point that is linked to an index in a backing store.
    /// </summary>
    public class Point {
        public float X { get; set; }
        public float Y { get; set; }
        public uint Index { get; set; }

        public Point(uint index, float allVals) {
            X = Y = allVals;
            Index = index;
        }

        public Point(uint index, float x, float y) {
            X = x;
            Y = y;
            Index = index;
        }

        public float DistanceTo(Point b) {
            return (float) Math.Sqrt(DistanceSqTo(b));
        }

        public float DistanceSqTo(Point b) {
            Point temp = this - b;
            return temp.X * temp.X + temp.Y * temp.Y;
        }

        public float LengthAsVector {
            get {
                return (float) Math.Sqrt(Length2AsVector);
            }
        }

        public float Length2AsVector {
            get {
                return X * X + Y * Y;
            }
        }

        public static Point operator -(Point lhs, Point rhs) {
            return new Point(0, lhs.X - rhs.X, lhs.Y - rhs.Y);
        }

        public static Point operator +(Point lhs, Point rhs) {
            return new Point(0, lhs.X + rhs.X, lhs.Y + rhs.Y);
        }
    }

    /// <summary>
    /// Represents an edge between two points.
    /// </summary>
    public class Edge {
        public Point First { get; set; }
        public Point Second { get; set; }

        public Edge(Point first, Point second) {
            First = first;
            Second = second;
        }

        public bool IsColinearWith(Point p) {
            float dist = Math.Abs((p - First).LengthAsVector - (p - Second).LengthAsVector);
            return Math.Abs(dist - Length) < 0.00001;
        }

        public float Length {
            get {
                return (First - Second).LengthAsVector;
            }
        }

        public override bool Equals(object obj) {
            Edge e = (Edge) obj;
            return (First.Index == e.First.Index && Second.Index == e.Second.Index) ||
                   (First.Index == e.Second.Index && Second.Index == e.First.Index);
        }

        public override int GetHashCode() {
            // hash so that the smaller is always first
            if (First.Index < Second.Index) {
                return (int)((First.Index << 16) | Second.Index);
            } else {
                return (int)((Second.Index << 16) | First.Index);
            }
        }
    }

    /// <summary>
    /// Represents a circle with a center and radius.
    /// </summary>
    public class Circle {
        public Point Center { get; set; }
        public float Radius { get; set; }

        public Circle(Point center, float radius) {
            Center = center;
            Radius = radius;
        }

        public bool Contains(Point p) {
            return p.DistanceSqTo(Center) < (Radius * Radius);
        }
    }

    /// <summary>
    /// Represents a triangle formed by 3 points and 3 edges between them.
    /// </summary>
    public class Triangle {
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }
        public Point Point3 { get; set; }
        public Edge Edge1 { get; set; }
        public Edge Edge2 { get; set; }
        public Edge Edge3 { get; set; }

        public Circle Circumcircle { get; private set; }

        public Triangle(Point point1, Point point2, Point point3) {
            Point1 = point1;
            Point2 = point2;
            Point3 = point3;
            Edge1 = new Edge(point1, point2);
            Edge2 = new Edge(point2, point3);
            Edge3 = new Edge(point3, point1);

            RecalculateCircumcircle();
        }

        public IEnumerable<Edge> Edges() {
            yield return Edge1;
            yield return Edge2;
            yield return Edge3;
        }

        public IEnumerable<Point> Points() {
            yield return Point1;
            yield return Point2;
            yield return Point3;
        }

        public void RecalculateCircumcircle() {
            Point b = Point2 - Point1;
            Point c = Point3 - Point1;
            float d = 2 * (b.X * c.Y - b.Y * c.X);
            float bLen = b.Length2AsVector;
            float cLen = c.Length2AsVector;

            Point center = new Point(
                    0, 
                    (c.Y * bLen - b.Y * cLen) / d,
                    (b.X * cLen - c.X * bLen) / d) + Point1;

            Circumcircle = new Circle(center, center.DistanceTo(Point1));
        }

        public override bool Equals(object obj) {
            Triangle tri = (Triangle) obj;
            return !Edges().Except(tri.Edges()).Any();
        }

        public override int GetHashCode() {
            return (int)((Point1.Index << 20) | (Point2.Index & 0x3FF << 10) | (Point3.Index & 0x3FF));
        }
    }

    /// <summary>
    /// Type of modification performed to the buffer.
    /// </summary>
    public enum ModificationType {
        /// <summary>
        /// Objects were removed from the buffer.
        /// </summary>
        Removed,
        /// <summary>
        /// Objects were added to the buffer.
        /// </summary>
        Added
    }

    public class TrianglesModifiedArgs : EventArgs {
        public IEnumerable<Triangle> NewTriangles { get; private set; }
        public ModificationType EventType { get; private set; }

        public TrianglesModifiedArgs(IEnumerable<Triangle> newTris, ModificationType type) {
            NewTriangles = newTris;
            EventType = type;
        }
    }

    public class PointEventArgs : EventArgs {
        public Point SelectedPoint { get; private set; }
        public int PointNum { get; private set; }

        public PointEventArgs(Point point, int num) {
            SelectedPoint = point;
            PointNum = num;
        }
    }

    public class EdgeArgs : EventArgs {
        public Edge Edge { get; private set; }
        public ModificationType EventType { get; private set; }

        public EdgeArgs(Edge edge, ModificationType type) {
            Edge = edge;
            EventType = type;
        }
    }

    /// <summary>
    /// Calculates a delaunay triangulation on a point cloud in two dimensions.
    /// </summary>
    public class Delaunay {
        /// <summary>
        /// Fired when triangles are created.
        /// </summary>
        public event EventHandler<TrianglesModifiedArgs> CreatedTriangles;

        /// <summary>
        /// Fired when triangles are removed.
        /// </summary>
        public event EventHandler<TrianglesModifiedArgs> RemovedTriangles;

        /// <summary>
        /// Fired when a new point is selected for addition to the triangle buffer.
        /// </summary>
        public event EventHandler<PointEventArgs> PointSelected;

        /// <summary>
        /// Fired when a duplicate edge is removed from the edge buffer.
        /// </summary>
        public event EventHandler<EdgeArgs> EdgeRemoved;

        /// <summary>
        /// Fired when an edge is added to the edge buffer.
        /// </summary>
        public event EventHandler<EdgeArgs> EdgeAdded;

        private List<Point>    activePoints   = null;
        private List<Triangle> tris           = null;
        private HashSet<Triangle> workingTris = null;
        private Triangle       superTri       = null;

        public Delaunay(IEnumerable<Point> points) {
            tris = new List<Triangle>(points.Count() * 2);
            workingTris = new HashSet<Triangle>();
            activePoints = new List<Point>(points);
            activePoints.Sort((p1, p2) => {
                return p1.X < p2.X ? -1 : 1;
            });

            // initialize the triangulation with the super triangle
            superTri = FindSuperTriangle();
            workingTris.Add(superTri);
        }

        public IList<Triangle> CalculateTriangles() {
            // there will be six points minimum; three for the super tri and 
            // at least 3 for the triangulated points
            if (activePoints.Count < 6) {
                return new List<Triangle>();
            }

            if (activePoints.Count == 6) {
                return new List<Triangle> {
                    new Triangle(activePoints[0], activePoints[1], activePoints[2])
                };
            }

            HashSet<Triangle> toRemove = new HashSet<Triangle>();
            HashSet<Edge> edges = new HashSet<Edge>();

            float curX = 0;
            int num = 0;
            foreach (Point point in activePoints) {
                if (superTri.Points().Contains(point)) {
                    continue;
                }

                num++;
                curX = point.X;
                FirePointEvent(point, num);

                var intersected = workingTris.Where(tri => {
                    return tri.Circumcircle.Contains(point) ||
                           tri.Circumcircle.Center.X + tri.Circumcircle.Radius < curX;
                });
                
                toRemove.Clear();
                edges.Clear();
                foreach (var replaced in intersected) {
                    toRemove.Add(replaced);

                    if (replaced.Circumcircle.Center.X + replaced.Circumcircle.Radius < curX) {
                        tris.Add(replaced);
                        continue;
                    }

                    foreach (var edge in replaced.Edges()) {
                        if (edges.Contains(edge)) {
                            edges.Remove(edge);
                            FireEdgeEvent(edge, ModificationType.Removed);
                        } else {
                            edges.Add(edge);
                            FireEdgeEvent(edge, ModificationType.Added);
                        }
                    }
                }

                workingTris.ExceptWith(toRemove);
                FireTriangleEvent(ModificationType.Removed);

                var newTris = 
                    from edge in edges
                    select new Triangle(point, edge.First, edge.Second);

                foreach (Triangle tri in newTris) {
                    workingTris.Add(tri);
                }

                FireTriangleEvent(ModificationType.Added);
            }

            tris.AddRange(workingTris);

            // remove super triangle
            tris.RemoveAll(t => {
                return t.Edges().Any(e => {
                    return superTri.Points()
                        .Any(p => p.Index == e.First.Index || p.Index == e.Second.Index);
                });
            });

            return tris;
        }

        private void FireTriangleEvent(ModificationType type) {
            switch (type) {
                case ModificationType.Added:
                    var created = CreatedTriangles;
                    if (created != null) {
                        created(this, new TrianglesModifiedArgs(tris, type));
                    }
                    break;
                case ModificationType.Removed:
                    var removed = RemovedTriangles;
                    if (removed != null) {
                        removed(this, new TrianglesModifiedArgs(tris, type));
                    }
                    break;
            }
        }

        private void FireEdgeEvent(Edge edge, ModificationType type) {
            switch (type) {
                case ModificationType.Added:
                    var created = EdgeAdded;
                    if (created != null) {
                        created(this, new EdgeArgs(edge, type));
                    }
                    break;
                case ModificationType.Removed:
                    var removed = EdgeRemoved;
                    if (removed != null) {
                        removed(this, new EdgeArgs(edge, type));
                    }
                    break;
            }
        }

        private void FirePointEvent(Point point, int num) {
            var selected = PointSelected;
            if (selected != null) {
                selected(this, new PointEventArgs(point, num));
            }
        }

        private Triangle FindSuperTriangle() {
            Point min, max;
            GetMinMax(out min, out max);

            float width = max.X - min.X;
            float height = max.Y - min.Y;

            Point first = new Point(0, min.X - width, min.Y - 2);
            Point second = new Point(0, max.X + width, min.Y - 2);
            Point third = new Point(0, min.X + width / 2, max.Y + height);

            AddPoint(first);
            AddPoint(second);
            AddPoint(third);

            return new Triangle(first, second, third);
        }

        private void AddPoint(Point p) {
            p.Index = (uint) activePoints.Count;
            activePoints.Add(p);
        }

        private void GetMinMax(out Point min, out Point max) {
            Point minPoint = new Point(0, Single.MaxValue);
            Point maxPoint = new Point(0, Single.MinValue);

            foreach (Point p in activePoints) {
                if (p.X < minPoint.X) {
                    minPoint.X = p.X;
                }
                if (p.X > maxPoint.X) {
                    maxPoint.X = p.X;
                }
                if (p.Y < minPoint.Y) {
                    minPoint.Y = p.Y;
                }
                if (p.Y > maxPoint.Y) {
                    maxPoint.Y = p.Y;
                }
            }

            min = minPoint;
            max = maxPoint;
        }

    }

    public static class Triangulate {
        public static uint[] Delaunay(IEnumerable<Point> points) {
            IList<Triangle> tris = new Delaunay(points).CalculateTriangles();
            return TriangleListToIndexArray(tris);
        }

        public static uint[] TriangleListToIndexArray(IList<Triangle> triangles) {
            uint[] outArray = new uint[(uint) triangles.Count * 3];
            
            for (int curTri = 0; curTri < triangles.Count; curTri++) {
                int outIndex = curTri * 3;
                outArray[outIndex] = triangles[curTri].Point1.Index;
                outArray[outIndex + 1] = triangles[curTri].Point2.Index;
                outArray[outIndex + 2] = triangles[curTri].Point3.Index;
            }

            return outArray;
        }

    }
}
