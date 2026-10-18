using PlazmaGames.Core;
using System;
using UnityEngine;

namespace PlazmaGames.Animation
{
	public class AnimationRequest
	{
		public MonoBehaviour value;
		public float duration;
		public Action<float> animationFunction;
		public Promise promise;

		public bool hasStarted;
		public bool hasCompleted;
		public Coroutine coroutine;

		public AnimationRequest(MonoBehaviour value, float duration, Action<float> animationFunction)
		{
			this.value = value;
			this.duration = duration;
			this.animationFunction = animationFunction;
			this.hasStarted = false;
			this.hasCompleted = false;
			this.coroutine = null;
		}
	}
}
