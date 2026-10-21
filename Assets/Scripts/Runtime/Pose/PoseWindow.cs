#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

using System.Linq;

using PlazmaGames.Core;

using ColbyO.SJ26.Rig;

namespace ColbyO.SJ26
{
    public class PoseWindow : EditorWindow
    {
        private IPoseMonoSystem _poseSystem;
        private string _poseName = "NewPose";
        private string _newCharacterName = "NewCharacter";
        private Transform _poseRoot;

        private Vector2 _scrollPos;
        private int _selectedCharacterIndex = 0;
        private int _prevSelectedCharacterIndex = -1;
        private string[] _characterNames = new string[0];

        private float _transitionDuration = 1f;


        [MenuItem("Tools/Pose Manager")]
        public static void ShowWindow()
        {
            GetWindow<PoseWindow>("Pose Manager");
        }

        private void OnGUI()
        {
            GUILayout.Label("Pose Manager Tool", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Must be in Play Mode first.", MessageType.Warning);
                return;
            }

            _poseSystem = GameManager.GetMonoSystem<IPoseMonoSystem>();
            if (_poseSystem == null)
            {
                EditorGUILayout.HelpBox("Assign a PoseMonoSystem in GameManager first.", MessageType.Warning);
                return;
            }

            _characterNames = _poseSystem.GetAllCharacters();
            if (_characterNames.Length == 0)
                _selectedCharacterIndex = -1;
            else
                _selectedCharacterIndex = Mathf.Clamp(_selectedCharacterIndex, 0, _characterNames.Length - 1);

            if (_characterNames.Length > 0)
                _selectedCharacterIndex = EditorGUILayout.Popup("Select Character", _selectedCharacterIndex, _characterNames);
            else
                EditorGUILayout.LabelField("No characters available.");

            string currentChar = (_selectedCharacterIndex >= 0 && _characterNames.Length > 0) ? _characterNames[_selectedCharacterIndex] : null;

            EditorGUILayout.BeginHorizontal();
            _newCharacterName = EditorGUILayout.TextField("New Character Name", _newCharacterName);
            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(_newCharacterName))
                {
                    _poseSystem.CreateCharacter(_newCharacterName, null);

                    _newCharacterName = "New Character";
                    _poseName = "NewPose";
                    _transitionDuration = 1f;
                    _poseRoot = null;

                    _characterNames = _poseSystem.GetAllCharacters();
                    _selectedCharacterIndex = _characterNames.Length - 1;
                    currentChar = (_selectedCharacterIndex >= 0 && _characterNames.Length > 0) ? _characterNames[_selectedCharacterIndex] : null;
                }
            }
            EditorGUILayout.EndHorizontal();

            _poseName = EditorGUILayout.TextField("Pose Name", _poseName);
            _transitionDuration = EditorGUILayout.FloatField("Transition Duration", _transitionDuration);

            if (_selectedCharacterIndex != _prevSelectedCharacterIndex)
            {
                _prevSelectedCharacterIndex = _selectedCharacterIndex;
                if (!string.IsNullOrEmpty(currentChar))
                {
                    CharacterPoses characterData = _poseSystem.GetCharacterData(currentChar);
                    if (characterData != null && !string.IsNullOrEmpty(characterData.PoseRootPath))
                    {
                        GameObject go = GameObject.Find(characterData.PoseRootPath);
                        _poseRoot = (go != null) ? go.transform : null;
                    }
                    else _poseRoot = null;
                }
            }

            Transform newRoot = (Transform)EditorGUILayout.ObjectField("Pose Root", _poseRoot, typeof(Transform), true);
            if (newRoot != _poseRoot)
            {
                _poseRoot = newRoot;
                GameManager.GetMonoSystem<IPoseMonoSystem>().UpdateCharacter(_characterNames[_selectedCharacterIndex], newRoot);
            }

            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(currentChar) || _poseRoot == null)
            {
                EditorGUILayout.HelpBox("Select a character and assign a Pose Root.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Save Pose"))
            {
                _poseSystem.SavePose(currentChar, _poseName, _poseRoot, _transitionDuration);
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Load Pose"))
            {
                _poseSystem.LoadPose(currentChar, _poseName);

                Rig.Pose pose = _poseSystem.GetCharacterData(currentChar)?
                                     .Poses.FirstOrDefault(p => p.PoseName == _poseName);
                if (pose != null)
                    _transitionDuration = pose.TransitionDuration;
            }

            EditorGUILayout.Space();
            GUILayout.Label("Saved Characters and Poses", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));

            foreach (string character in _poseSystem.GetAllCharacters())
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Character: " + character, EditorStyles.boldLabel);

                string[] poses = _poseSystem.GetAllPoses(character);
                if (poses.Length == 0)
                    EditorGUILayout.LabelField("  No poses for this character.");
                else
                {
                    foreach (string pose in poses)
                    {
                        EditorGUILayout.BeginHorizontal();

                        Rig.Pose savedPose = _poseSystem.GetCharacterData(character)?
                                                .Poses.FirstOrDefault(p => p.PoseName == pose);

                        EditorGUILayout.LabelField("  Pose: " + pose);

                        if (savedPose != null)
                        {
                            float newDuration = EditorGUILayout.FloatField(savedPose.TransitionDuration, GUILayout.Width(60));
                            if (!Mathf.Approximately(newDuration, savedPose.TransitionDuration))
                            {
                                savedPose.TransitionDuration = newDuration;
                                _poseSystem.UpdatePoseTransition(character, pose, newDuration);
                            }
                        }

                        if (GUILayout.Button("Load", GUILayout.Width(50)))
                        {
                            _poseSystem.LoadPose(character, pose);
                            _poseName = pose;
                            if (savedPose != null)
                                _transitionDuration = savedPose.TransitionDuration;
                        }

                        if (GUILayout.Button("Switch", GUILayout.Width(60)))
                        {
                            _poseSystem.LerpPose(character, pose);
                        }

                        if (GUILayout.Button("Delete", GUILayout.Width(60)))
                            _poseSystem.DeletePose(character, pose);

                        EditorGUILayout.EndHorizontal();
                    }
                }

                if (GUILayout.Button("Delete Character"))
                {
                    if (EditorUtility.DisplayDialog("Delete Character",
                        $"Are you sure you want to delete all poses for '{character}'?", "Yes", "No"))
                    {
                        _poseSystem.DeleteCharacter(character);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
