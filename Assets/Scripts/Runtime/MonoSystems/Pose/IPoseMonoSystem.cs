using ColbyO.SJ26.Rig;
using UnityEngine;

using PlazmaGames.Core;
using PlazmaGames.Core.MonoSystem;

namespace ColbyO.SJ26.Rig
{
    public interface IPoseMonoSystem : IMonoSystem
    {
        public Transform[] FindRigParts(Transform poseRoot);
        public void SavePose(string characterName, string poseName, Transform poseRoot, float transitionDuration = 1f);
        public void LoadPose(string characterName, string poseName);
        public void UpdatePoseTransition(string characterName, string poseName, float newDuration);
        public string[] GetAllPoses(string characterName);
        public void DeleteCharacter(string characterName);
        public void DeletePose(string characterName, string poseName);
        public string[] GetAllCharacters();
        public void CreateCharacter(string characterName, Transform poseRoot = null);
        public CharacterPoses GetCharacterData(string characterName);
        public void UpdateCharacter(string characterName, Transform newRoot);
        public Promise LerpPose(string characterName, string toPoseName, float? speedOverride = null);
    }
}