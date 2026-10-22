using UnityEngine;
using Unity.AI.Navigation;

using System.Collections.Generic;

using PlazmaGames.Core;

namespace ColbyO.SJ26.Entities
{
    public class ActorMonoSystem : MonoBehaviour, IActorMonoSystem
    {
        private NavMeshSurface _surface;

        private Dictionary<string, Actor> _actors = new Dictionary<string, Actor>();
        private Dictionary<string, Transform> _locations = new Dictionary<string, Transform>();

        private void Start()
        {
            _surface = FindAnyObjectByType<NavMeshSurface>();
            //_surface.BuildNavMesh();

            RefreshActorList();
            RefreshLocationsList();
        }

        public Promise SendActorToWith(Transform withTransform, string actorName, string locationName, float speed, string farDialogue = null, bool rebuildNavMesh = false)
        {
            if (_surface == null)
            {
                Debug.LogWarning("NavMesh Surface is null.");
                return null;
            }

            if (rebuildNavMesh) _surface.BuildNavMesh();

            Actor actor = GetActor(actorName);
            Transform loc = GetLocation(locationName);

            actor.SetMoveSpeed(speed);

            return actor.GotoWith(loc, withTransform, farDialogue);
        }

        public Promise SendActorTo(string actorName, string locationName, float speed, bool rebuildNavMesh = false)
        {
            if (_surface == null)
            {
                Debug.LogWarning("NavMesh Surface is null.");
                return null;
            }

            if (rebuildNavMesh) _surface.BuildNavMesh();

            Actor actor = GetActor(actorName);
            Transform loc = GetLocation(locationName);

            actor.SetMoveSpeed(speed);

            return actor.Goto(loc);
        }

        public Promise TellActorToSitAt(string actorName, string locationName, float speed, bool rebuildNavMesh = false)
        {
            if (_surface == null)
            {
                Debug.LogWarning("NavMesh Surface is null.");
                return null;
            }

            if (rebuildNavMesh) _surface.BuildNavMesh();

            Actor actor = GetActor(actorName);
            Transform loc = GetLocation(locationName);

            actor.SetMoveSpeed(speed);

            return actor.SitAt(loc);
        }

        public Promise TellActorToUnsit(string actorName)
        {
            Actor actor = GetActor(actorName);
            return actor.Unsit();
        }

        public Transform GetLocation(string name)
        {

            if (_locations == null)
            {
                Debug.LogWarning("Location list is null.");
                return null;
            }

            if (!_locations.ContainsKey(name))
            {
                Debug.LogWarning($"There is no location with name '{name}'");
                return null;
            }

            return _locations[name];
        }


        public Actor GetActor(string name)
        {
            if (_actors == null)
            {
                Debug.LogWarning("Actor list is null.");
                return null;
            }

            if (!_actors.ContainsKey(name))
            {
                Debug.LogWarning($"There is no actor with name '{name}'");
                return null;
            }

            return _actors[name];
        }

        private void RefreshLocationsList()
        {
            if (_locations == null) _locations = new Dictionary<string, Transform>();
            else _locations.Clear();

            GameObject[] locs = GameObject.FindGameObjectsWithTag("Location");

            foreach (GameObject go in locs)
            {
                string name = go.name;

                if (_locations.ContainsKey(name))
                {
                    Debug.LogWarning($"Duplicate location name found: '{name}'. Skipping.");
                    continue;
                }

                _locations.Add(name, go.transform);
            }
        }

        private void RefreshActorList()
        {
            if (_actors == null) _actors = new Dictionary<string, Actor>();
            else _actors.Clear();

            Actor[] actors = FindObjectsByType<Actor>();

            foreach (Actor actor in actors)
            {
                if (_actors.ContainsKey(actor.ActorName))
                {
                    Debug.LogWarning($"Duplicate actor name found: '{actor.ActorName}'. Skipping.");
                    continue;
                }

                _actors.Add(actor.ActorName, actor);

                Debug.Log($"Added '{actor.ActorName}'.");
            }
        }
    }
}