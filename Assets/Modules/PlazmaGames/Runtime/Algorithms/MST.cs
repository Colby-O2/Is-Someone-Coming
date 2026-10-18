using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Algorithms
{
    public sealed class MST
    {
        private readonly int _numberOfVertices;
        private readonly float[,] _adjacencyMatrix;
        private Dictionary<int, List<int>> _parent;
        private readonly List<Vector2> _vertexPositions;

        /// <summary>
        /// Constructor given an adjacency matrix and list of vertex position in world space
        /// </summary>
        public MST(float[,] adjacencyMatrix, List<Vector2> vertexPositions)
        {
            this._numberOfVertices = adjacencyMatrix.GetLength(0);
            this._adjacencyMatrix = adjacencyMatrix;
            this._vertexPositions = vertexPositions;
        }

        /// <summary>
        /// Finds the minimum cost node
        /// </summary>
        private int GetMinimumKey(float[] key, bool[] inSet)
        {
            float min = Mathf.Infinity;
            int minIndex = -1;

            for (int v = 0; v < key.Length; v++)
            {
                if (!inSet[v] && key[v] < min)
                {
                    min = key[v];
                    minIndex = v;
                }
            }

            return minIndex;
        }

        /// <summary>
        /// Generate a MST using Prim's Algorhtum 
        /// </summary>
        public Dictionary<int, List<int>> GetMST()
        {
            _parent = new Dictionary<int, List<int>>();

            float[] key = new float[_numberOfVertices];
            bool[] inSet = new bool[_numberOfVertices];

            for (int i = 0; i < _numberOfVertices; i++)
            {
                _parent.Add(i, new List<int>());
                key[i] = Mathf.Infinity;
                inSet[i] = false;
            }

            int firstIndex = Random.Range(0, _numberOfVertices);
            key[firstIndex] = 0;

            for (int i = 0; i < _numberOfVertices; i++)
            {
                int nextVertex = GetMinimumKey(key, inSet);

                if (nextVertex == -1) break;

                inSet[nextVertex] = true;

                for (int v = 0; v < _numberOfVertices; v++)
                {
                    if (_adjacencyMatrix[nextVertex, v] != 0 && !inSet[v] && _adjacencyMatrix[nextVertex, v] < key[v])
                    {
                        if (_parent[v].Count == 0) _parent[v].Add(nextVertex);
                        else _parent[v][0] = nextVertex;

                        key[v] = _adjacencyMatrix[nextVertex, v];
                    }
                }
            }

            return _parent;
        }

        /// <summary>
        /// Draws a MST for debugging
        /// </summary>
        public void DrawMST(Color? color = null, float duration = Mathf.Infinity)
        {
            for (int i = 0; i < _numberOfVertices; i++)
            {
                if (_parent[i].Count == 0) continue;

                Vector3 ptParnet = new(_vertexPositions[_parent[i][0]].x, 0, _vertexPositions[_parent[i][0]].y);
                Vector3 pt = new(_vertexPositions[i].x, 0, _vertexPositions[i].y);

                Debug.DrawLine(ptParnet, pt, color ?? Color.red, duration);
            }
        }
    }
}
