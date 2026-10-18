using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Utilities
{
    public static class GameObjectUtilities
    {
        public static void SetGameLayerRecursive(this GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                child.gameObject.layer = layer;

                Transform _HasChildren = child.GetComponentInChildren<Transform>();
                if (_HasChildren != null)
                    SetGameLayerRecursive(child.gameObject, layer);

            }
        }
    }
}
