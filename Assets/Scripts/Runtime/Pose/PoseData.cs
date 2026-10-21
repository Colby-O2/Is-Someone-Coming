using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColbyO.SJ26.Rig
{
    [Serializable]
    public class PoseTarget
    {
        public string TargetName;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }

    [Serializable]
    public class Pose
    {
        public string PoseName;
        public float TransitionDuration = 1f;
        public List<PoseTarget> Targets = new();
    }

    [Serializable]
    public class CharacterPoses
    {
        public string CharacterName;
        public string PoseRootPath;
        public List<Pose> Poses = new();
    }

    [Serializable]
    public class PoseDatabase
    {
        public List<CharacterPoses> Characters = new();
    }
}