using UnityEngine;

using System.Collections.Generic;
using PlazmaGames.Core.MonoSystem;


namespace ColbyO.SJ26.GPS
{
    public interface IGPSMonoSystem : IMonoSystem
    {
        List<Vector3> CalculatePath(Vector3 startPosition, string targetAddress);
    }
}