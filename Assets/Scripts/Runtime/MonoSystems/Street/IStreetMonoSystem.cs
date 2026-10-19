using PlazmaGames.Core.MonoSystem;
using System.Collections.Generic;
using UnityEngine;

namespace ColbyO.SJ26.Street
{
    public interface IStreetMonoSystem : IMonoSystem
    {
        List<StreetGraphNode> GetNodes();
        List<StreetGraphEdge> GetEdges();
        List<NamedStreet> GetNamedStreets();
        Transform GetSplineContainerTransform();
    }
}