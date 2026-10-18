using UnityEngine;

namespace InteractionSystem.UI
{
    [RequireComponent(typeof(MeshCollider))]
    public class CameraScreenCollider : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _meshCollider = GetComponent<MeshCollider>();
            _mesh = new Mesh();
            _meshCollider.sharedMesh = _mesh;

            UpdateCollider();
        }

        void OnRectTransformDimensionsChange()
        {
            UpdateCollider();
        }

        private void UpdateCollider()
        {
            if (!_rectTransform || !_meshCollider) return;

            Vector3[] corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);

            _mesh.Clear();
            _mesh.vertices = new Vector3[]
            {
            transform.InverseTransformPoint(corners[0]), 
            transform.InverseTransformPoint(corners[1]), 
            transform.InverseTransformPoint(corners[2]), 
            transform.InverseTransformPoint(corners[3])  
            };

            _mesh.uv = new Vector2[]
            {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
            };

            _mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();

            _meshCollider.sharedMesh = null; 
            _meshCollider.sharedMesh = _mesh;
        }
    }
}
