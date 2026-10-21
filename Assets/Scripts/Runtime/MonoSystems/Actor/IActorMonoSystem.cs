using UnityEngine;

using PlazmaGames.Core;
using PlazmaGames.Core.MonoSystem;

namespace ColbyO.SJ26.Entities
{
    public interface IActorMonoSystem : IMonoSystem
    {
        public Transform GetLocation(string name);
        public Actor GetActor(string name);
        public Promise SendActorTo(string actorName, string locationName, float speed = 1f, bool rebuildNavMesh = false);
        public Promise SendActorToWith(Transform withTransform, string actorName, string locationName, float speed, string farDialogue = null, bool rebuildNavMesh = false);
        public Promise TellActorToSitAt(string actorName, string locationName, float speed, bool rebuildNavMesh = false);
        public Promise TellActorToUnsit(string actorName);
    }
}