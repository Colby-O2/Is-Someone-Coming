using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlazmaGames.Core.MonoSystem;
using PlazmaGames.Core;

namespace PlazmaGames.Animation
{
	public interface IAnimationMonoSystem : IMonoSystem
	{
		public Promise RequestAnimation(AnimationRequest request, bool force = false);
		public Promise RequestAnimation(MonoBehaviour value, float duration, Action<float> animationFunction, bool force = false);
		public void StopAllAnimations(MonoBehaviour value);
		public bool HasAnimationRunning(MonoBehaviour value);
		public bool HasQuededAnimations(MonoBehaviour value);
		public Promise GetRunningPromise(MonoBehaviour value);

    }
}
