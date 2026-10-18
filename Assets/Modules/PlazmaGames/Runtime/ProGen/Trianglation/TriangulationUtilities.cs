using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.ProGen.Trianglation
{
    /// <summary>
    /// Define a 2D edge
    /// </summary>
    internal struct Edge2D
    {
        public Vector2 A, B;
        public int IndexA, IndexB;

        /// <summary>
        /// Constructor take two ponts in space and there indeices in the overall vertex list
        /// </summary>
        public Edge2D(Vector2 A, Vector2 B, int indexA, int indexB)
        {
            this.A = A;
            this.B = B;
            this.IndexA = indexA;
            this.IndexB = indexB;
        }

        /// <summary>
        /// Check if two edges are equal
        /// </summary>
        public bool IsEqual(Edge2D other)
        {
            return (this.A == other.A && this.B == other.B) || (this.A == other.B && this.B == other.A);
        }

    }

    /// <summary>
    /// Define a 2D triangle
    /// </summary>
    internal struct Triangle2D
    {
        public Vector2 A, B, C;
        public int IndexA, IndexB, IndexC;

        /// <summary>
        /// Constructor take three ponts in space and there indeices in the overall vertex list
        /// </summary>
        public Triangle2D(Vector2 A, Vector2 B, Vector2 C, int indexA, int indexB, int indexC)
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.IndexA = indexA;
            this.IndexB = indexB;
            this.IndexC = indexC;
        }

        /// <summary>
        /// Draws a trangle for debugging
        /// </summary>
        public void DrawTriangle(Color? color = null, float duration = Mathf.Infinity)
        {
            Debug.DrawLine(new Vector3(this.A.x, 0.0f, this.A.y), new Vector3(this.B.x, 0.0f, this.B.y), color ?? Color.green, duration);
            Debug.DrawLine(new Vector3(this.A.x, 0.0f, this.A.y), new Vector3(this.C.x, 0.0f, this.C.y), color ?? Color.green, duration);
            Debug.DrawLine(new Vector3(this.B.x, 0.0f, this.B.y), new Vector3(this.C.x, 0.0f, this.C.y), color ?? Color.green, duration);
        }
    }

    /// <summary>
    /// Define a 3D edge
    /// </summary>
    internal struct Edge3D
    {
        public Vector3 A, B;
        public int IndexA, IndexB;

        /// <summary>
        /// Constructor take two ponts in space and there indeices in the overall vertex list
        /// </summary>
        public Edge3D(Vector3 A, Vector3 B, int indexA, int indexB)
        {
            this.A = A;
            this.B = B;
            this.IndexA = indexA;
            this.IndexB = indexB;
        }

        /// <summary>
        /// Check if two edges are equal
        /// </summary>
        public bool IsEqual(Edge3D other)
        {
            return (this.A == other.A && this.B == other.B) || (this.A == other.B && this.B == other.A);
        }

    }

    /// <summary>
    /// Define a 3D triangle
    /// </summary>
    internal struct Triangle3D
    {
        public Vector3 A, B, C;
        public int IndexA, IndexB, IndexC;

        /// <summary>
        /// Constructor take two ponts in space and there indeices in the overall vertex list
        /// </summary>
        public Triangle3D(Vector3 A, Vector3 B, Vector3 C, int indexA, int indexB, int indexC)
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.IndexA = indexA;
            this.IndexB = indexB;
            this.IndexC = indexC;
        }

        private bool CloseTo(Vector3 v, Vector3 other)
        {
            return (v - other).sqrMagnitude < 0.01f;
        }

        /// <summary>
        /// Check if two edges are equal
        /// </summary>
        public bool IsEqual(Triangle3D other)
        {
            return (CloseTo(this.A, other.A) || CloseTo(this.A, other.B) || CloseTo(this.A, other.C))
                && (CloseTo(this.B, other.A) || CloseTo(this.B, other.B) || CloseTo(this.B, other.C))
                && (CloseTo(this.C, other.A) || CloseTo(this.C, other.B) || CloseTo(this.C, other.C));
        }

    }

    /// <summary>
    /// Define a tetrahedran
    /// </summary>
    internal struct Tetrahedran
    {
        public Vector3 A, B, C, D;
        public int IndexA, IndexB, IndexC, IndexD;

        /// <summary>
        /// Constructor take three ponts in space and there indeices in the overall vertex list
        /// </summary>
        public Tetrahedran(Vector3 A, Vector3 B, Vector3 C, Vector3 D, int indexA, int indexB, int indexC, int indexD)
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.D = D;
            this.IndexA = indexA;
            this.IndexB = indexB;
            this.IndexC = indexC;
            this.IndexD = indexD;
        }

        /// <summary>
        /// Draws a trangle for debugging
        /// </summary>
        public void DrawTetrahedran(Color? color = null, float duration = Mathf.Infinity)
        {
            Debug.DrawLine(new Vector3(this.A.x, this.A.y, this.A.z), new Vector3(this.B.x, this.B.y, this.B.z), color ?? Color.green, duration);
            Debug.DrawLine(new Vector3(this.A.x, this.A.y, this.A.z), new Vector3(this.C.x, this.C.y, this.C.z), color ?? Color.green, duration);
            Debug.DrawLine(new Vector3(this.A.x, this.A.y, this.A.z), new Vector3(this.D.x, this.D.y, this.D.z), color ?? Color.green, duration);
            Debug.DrawLine(new Vector3(this.B.x, this.B.y, this.B.z), new Vector3(this.C.x, this.C.y, this.C.z), color ?? Color.green, duration);
            Debug.DrawLine(new Vector3(this.B.x, this.B.y, this.B.z), new Vector3(this.D.x, this.D.y, this.D.z), color ?? Color.green, duration);
            Debug.DrawLine(new Vector3(this.D.x, this.D.y, this.D.z), new Vector3(this.C.x, this.C.y, this.C.z), color ?? Color.green, duration);
        }
    }

    public sealed class DelaunayTriangulation
    {

        private readonly List<Vector2> _vertices;
        private readonly List<Triangle2D> _triangulation;

        /// <summary>
        /// Consturction take an a list of vertex positions
        /// </summary>
        public DelaunayTriangulation(List<Vector2> vertices)
        {
            this._vertices = vertices;
            _triangulation = new();
        }

        /// <summary>
        /// Calculates a "super" triangle enclosing all points in the vertex list
        /// </summary>
        private Triangle2D CalaulateSuperTriangle()
        {
            Vector2 min = new Vector2(Mathf.Infinity, Mathf.Infinity);
            Vector2 max = new Vector2(-Mathf.Infinity, -Mathf.Infinity);

            foreach (Vector2 vertex in _vertices)
            {
                min.x = Mathf.Min(min.x, vertex.x);
                min.y = Mathf.Min(min.y, vertex.y);
                max.x = Mathf.Max(max.x, vertex.x);
                max.y = Mathf.Max(max.y, vertex.y);
            }

            float dx = (max.x - min.x) * 10.0f;
            float dy = (max.y - min.y) * 10.0f;

            return new Triangle2D(new Vector2(min.x - dx, min.y - dy * 3), new Vector2(min.x - dx, max.y + dy), new Vector2(max.x + dx * 3, max.y + dy), -1, -1, -1);
        }

        /// <summary>
        /// Check if a point lies within in a circumcircle
        /// </summary>
        private bool IsPointInCircumcircle(Triangle2D triangle, Vector2 pt)
        {
            float dx = triangle.A.x - pt.x;
            float dy = triangle.A.y - pt.y;
            float ex = triangle.B.x - pt.x;
            float ey = triangle.B.y - pt.y;
            float fx = triangle.C.x - pt.x;
            float fy = triangle.C.y - pt.y;

            float ap = dx * dx + dy * dy;
            float bp = ex * ex + ey * ey;
            float cp = fx * fx + fy * fy;

            return dx * (ey * cp - bp * fy) - dy * (ex * cp - bp * fx) + ap * (ex * fy - ey * fx) < 0;
        }

        /// <summary>
        /// Find all unique edges in an edge list
        /// </summary>
        private List<Edge2D> FindUniqueEdges(List<Edge2D> edges)
        {
            List<Edge2D> uniqueEdges = new List<Edge2D>();

            for (int i = 0; i < edges.Count; i++)
            {
                bool isUnique = true;

                for (int j = 0; j < edges.Count; j++)
                {
                    if (i != j && edges[i].IsEqual(edges[j]))
                    {
                        isUnique = false;
                        break;
                    }
                }

                if (isUnique) uniqueEdges.Add(edges[i]);
            }

            return uniqueEdges;
        }

        /// <summary>
        /// Adds a vertex to the triangulation
        /// </summary>
        private void AddVertex(Vector2 vertex, int roomID)
        {
            List<Edge2D> edges = new();

            for (int i = _triangulation.Count - 1; i > -1; i--)
            {
                if (IsPointInCircumcircle(_triangulation[i], vertex))
                {
                    edges.Add(new Edge2D(_triangulation[i].A, _triangulation[i].B, _triangulation[i].IndexA, _triangulation[i].IndexB));
                    edges.Add(new Edge2D(_triangulation[i].B, _triangulation[i].C, _triangulation[i].IndexB, _triangulation[i].IndexC));
                    edges.Add(new Edge2D(_triangulation[i].C, _triangulation[i].A, _triangulation[i].IndexC, _triangulation[i].IndexA));
                    _triangulation.RemoveAt(i);
                }
            }

            List<Edge2D> uniqueEdges = FindUniqueEdges(edges);

            foreach (Edge2D edge in uniqueEdges)
            {
                _triangulation.Add(new Triangle2D(edge.A, edge.B, vertex, edge.IndexA, edge.IndexB, roomID));
            }
        }

        /// <summary>
        /// Checks if a two triangles have a shared edge
        /// </summary>
        private bool HasSharedEdages(Triangle2D triangle, Triangle2D otherTriangle)
        {
            return (triangle.A == otherTriangle.A || triangle.A == otherTriangle.B || triangle.A == otherTriangle.C || triangle.B == otherTriangle.A || triangle.B == otherTriangle.B || triangle.B == otherTriangle.C || triangle.C == otherTriangle.A || triangle.C == otherTriangle.B || triangle.C == otherTriangle.C);
        }

        /// <summary>
        /// Generate an Adjacency Matrix from a triangulation
        /// </summary>
        private float[,] GenerateAdjacencyMatrix()
        {
            float[,] adjacencyMatrix = new float[_vertices.Count, _vertices.Count];

            foreach (Triangle2D triangle in _triangulation)
            {
                adjacencyMatrix[triangle.IndexA, triangle.IndexB] = Vector2.Distance(triangle.A, triangle.B);
                adjacencyMatrix[triangle.IndexB, triangle.IndexA] = Vector2.Distance(triangle.B, triangle.A);
                adjacencyMatrix[triangle.IndexB, triangle.IndexC] = Vector2.Distance(triangle.B, triangle.C);
                adjacencyMatrix[triangle.IndexC, triangle.IndexB] = Vector2.Distance(triangle.C, triangle.B);
                adjacencyMatrix[triangle.IndexC, triangle.IndexA] = Vector2.Distance(triangle.C, triangle.A);
                adjacencyMatrix[triangle.IndexA, triangle.IndexC] = Vector2.Distance(triangle.A, triangle.C);
            }

            return adjacencyMatrix;
        }

        /// <summary>
        /// Computes the Delaunay Triangulation of a set of vertices and return the Adjacency Matrix 
        /// of the underlying graph.
        /// </summary>
        public float[,] CalculateDelaunayTriangulation()
        {
            Triangle2D superTriangle = CalaulateSuperTriangle();
            _triangulation.Add(superTriangle);

            for (int i = 0; i < _vertices.Count; i++)
            {
                AddVertex(_vertices[i], i);
            }

            _triangulation.RemoveAll(triangle => HasSharedEdages(triangle, superTriangle));

            return GenerateAdjacencyMatrix();
        }

        /// <summary>
        /// Draws the Delaunay Triangulation for debugging
        /// </summary>
        public void DrawDelaunayTriangulation(Color? color = null, float duration = Mathf.Infinity)
        {
            foreach (Triangle2D triangle in _triangulation)
            {
                triangle.DrawTriangle(color, duration);
            }
        }
    }

    /// <summary>
    /// Finds a tetrahedralization for a set of points in 3D space
    /// using the Watson's algorithm.
    /// Reference: https://dl.acm.org/doi/pdf/10.1145/10515.10542
    /// </summary>
    public sealed class DelaunayTetrahedralization
    {

        private readonly List<Vector3> _vertices;
        private readonly List<Tetrahedran> _triangulation;

        /// <summary>
        /// Consturction take an a list of vertex positions
        /// </summary>
        public DelaunayTetrahedralization(List<Vector3> vertices)
        {
            this._vertices = vertices;
            _triangulation = new();
        }

        /// <summary>
        /// Calculates a "super" tetrahedran enclosing all points in the vertex list
        /// </summary>
        private Tetrahedran CalaulateSuperTetrahedran()
        {
            Vector3 min = new(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 max = new(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);

            foreach (Vector3 vertex in _vertices)
            {
                min.x = Mathf.Min(min.x, vertex.x);
                min.y = Mathf.Min(min.y, vertex.y);
                min.z = Mathf.Min(min.z, vertex.z);
                max.x = Mathf.Max(max.x, vertex.x);
                max.y = Mathf.Max(max.y, vertex.y);
                max.z = Mathf.Max(max.z, vertex.z);
            }

            float dx = (max.x - min.x) * 2.0f;
            float dy = (max.y - min.y) * 2.0f;
            float dz = (max.z - min.z) * 2.0f;

            return new Tetrahedran(
                new Vector3(max.x + dx, min.y - dy, min.z - dz),
                new Vector3(min.x - dx, max.y + dy, min.z - dz),
                new Vector3(min.x - dx, min.y - dy, max.z + dz),
                new Vector3(max.x + dx, max.y + dy, max.z + dz),
                -1,
                -1,
                -1,
                -1
                );
        }

        /// <summary>
        /// This function checks if a point lies within in a circumball
        /// </summary>
        /// A Circumball is a sphere that passes through all points on the tetrahedran
        /// The standard form of equation of a circumball is (x - x0)^2 + (y - y0)^2 + (z - z0)^2 = r^2
        /// where the vector <x0, y0, z0> is the circumcenter and r is the circumradius. Inorder to find
        /// the circumcenter we can use the following equations we can be dervived from above with a bit of work
        /// (express the standform of the circumball equation in terms of Matrices):
        /// 
        ///     x0 = |dx| / 2|a|; y0 = |dy|/2|a|; z0 = |dz|/2|a| were
        ///             
        ///         | x_1^2 + y_1^2 + z_1^2, x1, y1, z1, 1 |           | x_1, y_1, z_1, 1 |         | x_1^2 + y_1^2 + z_1^2, x1, y1, z1 | 
        ///         | x_2^2 + y_2^2 + z_2^2, x2, y2, z2, 1 |           | x_2, y_2, z_2, 1 |         | x_2^2 + y_2^2 + z_2^2, x2, y2, z2 |
        ///     D = | x_3^2 + y_3^2 + z_3^2, x3, y3, z3, 1 | and   a = | x_3, y_3, z_3, 1 | and c = | x_3^2 + y_3^2 + z_3^2, x3, y3, z3 |
        ///         | x_4^2 + y_4^2 + z_4^2, x4, y4, z4, 1 |           | x_4, y_4, z_4, 1 |         | x_4^2 + y_4^2 + z_4^2, x4, y4, z4 |
        ///         
        ///     dx is the (+)determinat of D with column x deleted and vice versa for dy ((-)determinat) and dz((+)determinat).
        ///     
        ///     It also follows that the circumradius is as follows:
        ///   
        ///     r = Mathf.Sqrt(|dx|^2 + |dy|^2 + |dz|^2 - 4|a||c|) / 2 * |a|
        ///     
        /// Using the above equations for the circumcenter and circumradius we can determine if a point, pt with the following 
        /// condition:
        /// (x - x0)^2 + (y - y0)^2 + (z - z0)^2 - r^2 < 0
        private bool IsPointInCircumball(Tetrahedran tetrahedran, Vector3 pt)
        {
            float dx = new Matrix4x4(
                new Vector4(tetrahedran.A.sqrMagnitude, tetrahedran.B.sqrMagnitude, tetrahedran.C.sqrMagnitude, tetrahedran.D.sqrMagnitude),
                new Vector4(tetrahedran.A.y, tetrahedran.B.y, tetrahedran.C.y, tetrahedran.D.y),
                new Vector4(tetrahedran.A.z, tetrahedran.B.z, tetrahedran.C.z, tetrahedran.D.z),
                new Vector4(1, 1, 1, 1)
                ).determinant;

            float dy = new Matrix4x4(
                new Vector4(tetrahedran.A.sqrMagnitude, tetrahedran.B.sqrMagnitude, tetrahedran.C.sqrMagnitude, tetrahedran.D.sqrMagnitude),
                new Vector4(tetrahedran.A.x, tetrahedran.B.x, tetrahedran.C.x, tetrahedran.D.x),
                new Vector4(tetrahedran.A.z, tetrahedran.B.z, tetrahedran.C.z, tetrahedran.D.z),
                new Vector4(1, 1, 1, 1)
                ).determinant;

            dy *= -1;

            float dz = new Matrix4x4(
                new Vector4(tetrahedran.A.sqrMagnitude, tetrahedran.B.sqrMagnitude, tetrahedran.C.sqrMagnitude, tetrahedran.D.sqrMagnitude),
                new Vector4(tetrahedran.A.x, tetrahedran.B.x, tetrahedran.C.x, tetrahedran.D.x),
                new Vector4(tetrahedran.A.y, tetrahedran.B.y, tetrahedran.C.y, tetrahedran.D.y),
                new Vector4(1, 1, 1, 1)
                ).determinant;

            float a = new Matrix4x4(
                new Vector4(tetrahedran.A.x, tetrahedran.B.x, tetrahedran.C.x, tetrahedran.D.x),
                new Vector4(tetrahedran.A.y, tetrahedran.B.y, tetrahedran.C.y, tetrahedran.D.y),
                new Vector4(tetrahedran.A.z, tetrahedran.B.z, tetrahedran.C.z, tetrahedran.D.z),
                new Vector4(1, 1, 1, 1)
                ).determinant;

            float c = new Matrix4x4(
                new Vector4(tetrahedran.A.sqrMagnitude, tetrahedran.B.sqrMagnitude, tetrahedran.C.sqrMagnitude, tetrahedran.D.sqrMagnitude),
                new Vector4(tetrahedran.A.x, tetrahedran.B.x, tetrahedran.C.x, tetrahedran.D.x),
                new Vector4(tetrahedran.A.y, tetrahedran.B.y, tetrahedran.C.y, tetrahedran.D.y),
                new Vector4(tetrahedran.A.z, tetrahedran.B.z, tetrahedran.C.z, tetrahedran.D.z)
                ).determinant;

            Vector3 circumcenter = new(dx / (2.0f * a), dy / (2.0f * a), dz / (2.0f * a));
            float circumradiusSquared = (dx * dx + dy * dy + dz * dz - 4 * a * c) / (4 * a * a);

            return (pt - circumcenter).sqrMagnitude - circumradiusSquared < 0;
        }

        /// <summary>
        /// Find all unique edges in an edge list
        /// </summary>
        private List<Triangle3D> FindUniqueTriangles(List<Triangle3D> triangles)
        {
            List<Triangle3D> uniqueTriangles = new();

            for (int i = 0; i < triangles.Count; i++)
            {
                bool isUnique = true;

                for (int j = 0; j < triangles.Count; j++)
                {
                    if (i != j && triangles[i].IsEqual(triangles[j]))
                    {
                        isUnique = false;
                        break;
                    }
                }

                if (isUnique) uniqueTriangles.Add(triangles[i]);
            }

            return uniqueTriangles;
        }

        /// <summary>
        /// Adds a vertex to the triangulation
        /// </summary>
        private void AddVertex(Vector3 vertex, int roomID)
        {
            List<Triangle3D> triangles = new();

            for (int i = _triangulation.Count - 1; i > -1; i--)
            {
                if (IsPointInCircumball(_triangulation[i], vertex))
                {
                    triangles.Add(new Triangle3D(_triangulation[i].A, _triangulation[i].B, _triangulation[i].C, _triangulation[i].IndexA, _triangulation[i].IndexB, _triangulation[i].IndexC));
                    triangles.Add(new Triangle3D(_triangulation[i].A, _triangulation[i].B, _triangulation[i].D, _triangulation[i].IndexA, _triangulation[i].IndexB, _triangulation[i].IndexD));
                    triangles.Add(new Triangle3D(_triangulation[i].A, _triangulation[i].C, _triangulation[i].D, _triangulation[i].IndexA, _triangulation[i].IndexC, _triangulation[i].IndexD));
                    triangles.Add(new Triangle3D(_triangulation[i].B, _triangulation[i].C, _triangulation[i].D, _triangulation[i].IndexB, _triangulation[i].IndexC, _triangulation[i].IndexD));
                    _triangulation.RemoveAt(i);
                }
            }

            List<Triangle3D> uniqueTriangles = FindUniqueTriangles(triangles);

            Debug.Log("RoomID: " + roomID + " " + uniqueTriangles.Count);

            foreach (Triangle3D triangle in uniqueTriangles)
            {
                _triangulation.Add(new Tetrahedran(triangle.A, triangle.B, triangle.C, vertex, triangle.IndexA, triangle.IndexB, triangle.IndexC, roomID));
            }
        }

        /// <summary>
        /// Checks if a two triangles have a shared face
        /// </summary>
        private bool HasSharedTriangle(Tetrahedran triangle, Tetrahedran otherTriangle)
        {
            return (
                triangle.A == otherTriangle.A
                || triangle.A == otherTriangle.B
                || triangle.A == otherTriangle.C
                || triangle.B == otherTriangle.A
                || triangle.B == otherTriangle.B
                || triangle.B == otherTriangle.C
                || triangle.C == otherTriangle.A
                || triangle.C == otherTriangle.B
                || triangle.C == otherTriangle.C
                || triangle.D == otherTriangle.D
                || triangle.D == otherTriangle.A
                || triangle.D == otherTriangle.B
                || triangle.D == otherTriangle.C
                || triangle.A == otherTriangle.D
                || triangle.B == otherTriangle.D
                || triangle.C == otherTriangle.D);
        }

        /// <summary>
        /// Generate an Adjacency Matrix from a triangulation
        /// </summary>
        private float[,] GenerateAdjacencyMatrix()
        {
            float[,] adjacencyMatrix = new float[_vertices.Count, _vertices.Count];

            foreach (Tetrahedran triangle in _triangulation)
            {

                adjacencyMatrix[triangle.IndexA, triangle.IndexB] = Vector2.Distance(triangle.A, triangle.B);
                adjacencyMatrix[triangle.IndexB, triangle.IndexA] = Vector2.Distance(triangle.B, triangle.A);
                adjacencyMatrix[triangle.IndexB, triangle.IndexC] = Vector2.Distance(triangle.B, triangle.C);
                adjacencyMatrix[triangle.IndexC, triangle.IndexB] = Vector2.Distance(triangle.C, triangle.B);
                adjacencyMatrix[triangle.IndexC, triangle.IndexA] = Vector2.Distance(triangle.C, triangle.A);
                adjacencyMatrix[triangle.IndexA, triangle.IndexC] = Vector2.Distance(triangle.A, triangle.C);
                adjacencyMatrix[triangle.IndexD, triangle.IndexA] = Vector2.Distance(triangle.D, triangle.A);
                adjacencyMatrix[triangle.IndexA, triangle.IndexD] = Vector2.Distance(triangle.A, triangle.D);
                adjacencyMatrix[triangle.IndexB, triangle.IndexD] = Vector2.Distance(triangle.B, triangle.D);
                adjacencyMatrix[triangle.IndexD, triangle.IndexB] = Vector2.Distance(triangle.D, triangle.B);
                adjacencyMatrix[triangle.IndexD, triangle.IndexC] = Vector2.Distance(triangle.D, triangle.C);
                adjacencyMatrix[triangle.IndexC, triangle.IndexD] = Vector2.Distance(triangle.C, triangle.D);

            }

            return adjacencyMatrix;
        }

        /// <summary>
        /// Computes the Delaunay Triangulation of a set of vertices and return the Adjacency Matrix 
        /// of the underlying graph.
        /// </summary>
        public float[,] CalculateDelaunayTriangulation()
        {
            Tetrahedran superTriangle = CalaulateSuperTetrahedran();
            _triangulation.Add(superTriangle);

            for (int i = 0; i < _vertices.Count; i++)
            {
                AddVertex(_vertices[i], i);
            }

            _triangulation.RemoveAll(triangle => HasSharedTriangle(triangle, superTriangle));

            return GenerateAdjacencyMatrix();
        }

        /// <summary>
        /// Draws the Delaunay Triangulation for debugging
        /// </summary>
        public void DrawDelaunayTriangulation(Color? color = null, float duration = Mathf.Infinity)
        {
            foreach (Tetrahedran tetrahedran in _triangulation)
            {
                tetrahedran.DrawTetrahedran(color, duration);
            }
        }
    }
}
