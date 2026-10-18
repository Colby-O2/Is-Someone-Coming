using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueGraph.SO
{
    public abstract class BaseSO : ScriptableObject
    {
        [Header("Global SO Settings")]
        public int id = -1;
        public bool includeInDatabase = true;
    }
}
