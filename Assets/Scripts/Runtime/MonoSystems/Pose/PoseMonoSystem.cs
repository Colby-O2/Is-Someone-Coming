using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using PlazmaGames.Animation;
using PlazmaGames.Core;

namespace ColbyO.SJ26.Rig
{
    public class PoseMonoSystem : MonoBehaviour, IPoseMonoSystem
    {
        public string filePath = "Poses.json";
        private PoseDatabase database = new PoseDatabase();

        public bool IsLoaded { get; private set; }

        private void Awake()
        {
            StartCoroutine(LoadDatabase((bool sucessful) =>
            {
                Debug.Log(sucessful ? "Poses Sucessful Loaded." : "Poses Not Loaded.");
                IsLoaded = sucessful;
            }));
        }

        public Transform[] FindRigParts(Transform poseRoot)
        {
            if (poseRoot == null) return new Transform[0];

            return poseRoot.GetComponentsInChildren<Transform>(true)
                           .Where(t => t.name.Contains("_target") || t.name.Contains("_hint"))
                           .ToArray();
        }

        public void SavePose(string characterName, string poseName, Transform poseRoot, float transitionDuration)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Save Pose, Pose Database is Not Loaded!");
                return;
            }

            if (poseRoot == null)
            {
                Debug.LogWarning("Pose root is null!");
                return;
            }

            Transform[] parts = FindRigParts(poseRoot);
            Rig.Pose newPose = new Rig.Pose { PoseName = poseName, TransitionDuration = transitionDuration };

            foreach (Transform t in parts)
            {
                newPose.Targets.Add(new Rig.PoseTarget
                {
                    TargetName = t.name,
                    LocalPosition = t.localPosition,
                    LocalRotation = t.localRotation,
                    LocalScale = t.localScale
                });
            }

            CharacterPoses character = database.Characters.FirstOrDefault(c => c.CharacterName == characterName);
            if (character == null)
            {
                character = new CharacterPoses { CharacterName = characterName };
                database.Characters.Add(character);
            }

            Rig.Pose existingPose = character.Poses.FirstOrDefault(p => p.PoseName == poseName);
            if (existingPose != null)
            {
                existingPose.Targets = newPose.Targets;
                existingPose.TransitionDuration = newPose.TransitionDuration;
            }
            else
            {
                character.Poses.Add(newPose);
            }

            SaveDatabase();

            Debug.Log($"Saved pose '{poseName}' for character '{characterName}' with transition duration {transitionDuration}");
        }

        public void LoadPose(string characterName, string poseName)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Load Pose, Pose Database is Not Loaded!");
                return;
            }

            CharacterPoses character = database.Characters.FirstOrDefault(c => c.CharacterName == characterName);
            if (character == null)
            {
                Debug.LogWarning($"Character '{characterName}' not found.");
                return;
            }

            Transform rigRoot = null;
            if (!string.IsNullOrEmpty(character.PoseRootPath))
            {
                GameObject go = GameObject.Find(character.PoseRootPath);
                if (go != null) rigRoot = go.transform;
            }

            if (rigRoot == null)
            {
                Debug.LogWarning($"PoseRoot for character '{characterName}' not found in the scene.");
                return;
            }

            Rig.Pose pose = character.Poses.FirstOrDefault(p => p.PoseName == poseName);
            if (pose == null)
            {
                Debug.LogWarning($"Pose '{poseName}' not found for character '{characterName}'");
                return;
            }

            Transform[] parts = FindRigParts(rigRoot);

            foreach (PoseTarget data in pose.Targets)
            {
                Transform t = parts.FirstOrDefault(p => p.name == data.TargetName);
                if (t != null)
                {
                    t.localPosition = data.LocalPosition;
                    t.localRotation = data.LocalRotation;
                    t.localScale = data.LocalScale;
                }
            }

            Debug.Log($"Loaded pose '{poseName}' for character '{characterName}'");
        }

        public void UpdatePoseTransition(string characterName, string poseName, float newDuration)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Update Pose Transition, Pose Database is Not Loaded!");
                return;
            }

            CharacterPoses character = database.Characters.FirstOrDefault(c => c.CharacterName == characterName);
            if (character == null) return;

            Rig.Pose pose = character.Poses.FirstOrDefault(p => p.PoseName == poseName);
            if (pose == null) return;

            pose.TransitionDuration = newDuration;

            SaveDatabase();
            Debug.Log($"Updated transition duration for pose '{poseName}' of character '{characterName}' to {newDuration}");
        }

        public void CreateCharacter(string characterName, Transform poseRoot = null)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Create Character, Pose Database is Not Loaded!");
                return;
            }

            if (string.IsNullOrEmpty(characterName))
                return;

            if (database.Characters.Any(c => c.CharacterName == characterName))
                return;

            CharacterPoses character = new CharacterPoses
            {
                CharacterName = characterName,
                PoseRootPath = poseRoot != null ? GetTransformPath(poseRoot) : ""
            };

            database.Characters.Add(character);
            SaveDatabase();
            Debug.Log($"Created new character: {characterName}.");
        }

        public void UpdateCharacter(string characterName, Transform newRoot)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Update Character, Pose Database is Not Loaded!");
                return;
            }

            CharacterPoses character = database.Characters.FirstOrDefault(c => c.CharacterName == characterName);
            if (character != null)
            {
                character.PoseRootPath = newRoot != null ? GetTransformPath(newRoot) : "";

                SaveDatabase();
                Debug.Log($"Updated PoseRoot for character '{characterName}'.");
            }
        }

        public CharacterPoses GetCharacterData(string characterName)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Get Character Data, Pose Database is Not Loaded!");
                return null;
            }

            return database.Characters.FirstOrDefault(c => c.CharacterName == characterName);
        }

        private string GetTransformPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        public string[] GetAllPoses(string characterName)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot All Poses, Pose Database is Not Loaded!");
                return null;
            }

            CharacterPoses character = database.Characters.FirstOrDefault(c => c.CharacterName == characterName);
            return character?.Poses.Select(p => p.PoseName).ToArray() ?? new string[0];
        }

        public string[] GetAllCharacters()
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Get All Characters, Pose Database is Not Loaded!");
                return null;
            }

            return database.Characters.Select(c => c.CharacterName).ToArray();
        }

        public void DeletePose(string characterName, string poseName)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Delete Pose, Pose Database is Not Loaded!");
                return;
            }

            CharacterPoses character = database.Characters.FirstOrDefault(c => c.CharacterName == characterName);
            if (character == null) return;

            character.Poses.RemoveAll(p => p.PoseName == poseName);
            SaveDatabase();

            Debug.Log($"Deleted pose '{poseName}' from character '{characterName}'");
        }

        public void DeleteCharacter(string characterName)
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("Cannot Delete Character, Pose Database is Not Loaded!");
                return;
            }

            database.Characters.RemoveAll(c => c.CharacterName == characterName);
            SaveDatabase();

            Debug.Log($"Deleted character '{characterName}' and all its poses");
        }

        private IEnumerator LoadDatabase(System.Action<bool> onComplete = null)
        {
            bool sucessful;

#if UNITY_WEBGL && !UNITY_EDITOR
    string path = Path.Combine(Application.streamingAssetsPath, filePath);
    using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path))
    {
        yield return www.SendWebRequest();

        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Failed to load poses from {path}: {www.error}");
            database = new PoseDatabase();
            sucessful = false;
        }
        else
        {
            database = JsonUtility.FromJson<PoseDatabase>(www.downloadHandler.text) ?? new PoseDatabase();
            sucessful = true;
        }
    }
#else
            string fullPath = Path.Combine(Application.streamingAssetsPath, filePath);
            if (!File.Exists(fullPath))
            {
                database = new PoseDatabase();
            }
            else
            {
                string json = File.ReadAllText(fullPath);
                database = JsonUtility.FromJson<PoseDatabase>(json) ?? new PoseDatabase();
            }
            sucessful = true;
#endif

            onComplete?.Invoke(sucessful);

            yield return null;
        }

        private void SaveDatabase()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, filePath);
            string json = JsonUtility.ToJson(database, true);
            File.WriteAllText(fullPath, json);
        }

        public Promise LerpPose(string characterName, string toPoseName, float? speedOverride = null)
        {
            Promise promise = new Promise();

            CharacterPoses character = GetCharacterData(characterName);
            if (character == null)
            {
                Debug.LogWarning($"Character '{characterName}' not found.");
                promise.Resolve();
                return promise;
            }

            Transform poseRoot = null;
            if (!string.IsNullOrEmpty(character.PoseRootPath))
            {
                GameObject go = GameObject.Find(character.PoseRootPath);
                if (go != null) poseRoot = go.transform;
            }

            if (poseRoot == null)
            {
                Debug.LogWarning($"PoseRoot for character '{characterName}' not found in scene.");
                promise.Resolve();
                return promise;
            }

            Rig.Pose toPose = character.Poses.FirstOrDefault(p => p.PoseName == toPoseName);
            if (toPose == null)
            {
                Debug.LogWarning($"Pose '{toPoseName}' not found for character '{characterName}'.");
                promise.Resolve();
                return promise;
            }

            float duration = speedOverride ?? toPose.TransitionDuration;

            Transform[] targets = poseRoot.GetComponentsInChildren<Transform>(true)
                                          .Where(t => t.name.Contains("_target") || t.name.Contains("_hint"))
                                          .ToArray();

            List<Vector3> startPos = targets.Select(t => t.localPosition).ToList();
            List<Quaternion> startRot = targets.Select(t => t.localRotation).ToList();
            List<Vector3> startScale = targets.Select(t => t.localScale).ToList();

            List<Vector3> endPos = new List<Vector3>();
            List<Quaternion> endRot = new List<Quaternion>();
            List<Vector3> endScale = new List<Vector3>();

            for (int i = 0; i < targets.Length; i++)
            {
                PoseTarget end = toPose.Targets.FirstOrDefault(t => t.TargetName == targets[i].name);
                endPos.Add(end != null ? end.LocalPosition : targets[i].localPosition);
                endRot.Add(end != null ? end.LocalRotation : targets[i].localRotation);
                endScale.Add(end != null ? end.LocalScale : targets[i].localScale);
            }

            GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(
                this,
                duration,
                (float t) => PoseLerpStep(t, targets, startPos, startRot, startScale, endPos, endRot, endScale)
            ).Then((_) =>
            {
                promise.Resolve();
            });

            return promise;
        }

        private void PoseLerpStep(
            float t,
            Transform[] targets,
            List<Vector3> startPos,
            List<Quaternion> startRot,
            List<Vector3> startScale,
            List<Vector3> endPos,
            List<Quaternion> endRot,
            List<Vector3> endScale
        )
        {
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].localPosition = Vector3.Lerp(startPos[i], endPos[i], t);
                targets[i].localRotation = Quaternion.Slerp(startRot[i], endRot[i], t);
                targets[i].localScale = Vector3.Lerp(startScale[i], endScale[i], t);
            }
        }
    }
}