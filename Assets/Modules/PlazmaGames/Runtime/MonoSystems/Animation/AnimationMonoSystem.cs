using PlazmaGames.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlazmaGames.Animation
{
	public sealed class AnimationMonoSystem : MonoBehaviour, IAnimationMonoSystem
	{
		private Dictionary<MonoBehaviour, Queue<AnimationRequest>> _animationQueue;

		/// <summary>
		/// Ensure an animation is completely stoped.
		/// </summary>
		/// <param name="request">An animation request.</param>
		private void EnsureCoroutineHasStopped(AnimationRequest request)
		{
			if (request.coroutine != null)
			{
				if (request.value != null) request.value.StopCoroutine(request.coroutine);
				request.coroutine = null;
			}
		}

		/// <summary>
		/// Generic animation coroutine.
		/// </summary>
		/// <param name="request">An animation request.</param>
		/// <returns></returns>
		private IEnumerator GenrticAnimationCoroutine(AnimationRequest request)
		{
			float elapsedTime = 0.0f;
			float progress = 0.0f;

			while (progress <= 1.0f)
			{
				request.animationFunction(progress);
				elapsedTime += Time.unscaledDeltaTime;
				progress = elapsedTime / request.duration;
				yield return null;
			}

			request.animationFunction(1.0f);
			request.hasCompleted = true;
            Promise.ResolveExisting(ref request.promise);
        }

		/// <summary>
		/// Start an animations.
		/// </summary>
		/// <param name="request">An animation request.</param>
		private void StartAnimationCoroutine(AnimationRequest request)
		{
			request.hasStarted = true;
			request.hasCompleted = false;
			request.coroutine = request.value.StartCoroutine(GenrticAnimationCoroutine(request));
		}

		/// <summary>
		/// Request an animation to be played.
		/// </summary>
		/// <param name="request">An animation request.</param>
		/// <param name="force">If the animation should be ran right away or queued.</param>
		public Promise RequestAnimation(AnimationRequest request, bool force = false)
		{
            request.promise = Promise.CreateExisting(ref request.promise);

            if (force)
			{
				StartAnimationCoroutine(request);
				return request.promise;
			}

			if (!_animationQueue.ContainsKey(request.value))
			{
				_animationQueue.Add(request.value, new Queue<AnimationRequest>());
			}

			_animationQueue[request.value].Enqueue(request);
			if (_animationQueue[request.value].Count == 1)
			{
				StartAnimationCoroutine(request);
			}

            return request.promise;
        }

		/// <summary>
		/// Request an animation to be played.
		/// </summary>
		/// <param name="value">MonoBehaviour to attached coroutine to.</param>
		/// <param name="duration">The duration of the animation</param>
		/// <param name="animationFunction">Callback to run on each interation of the animation.</param>
		/// <param name="onComplete">Callback to be called when the animation is complete</param>
		/// <param name="force">If the animation should be ran right away or queued.</param>
		public Promise RequestAnimation(MonoBehaviour value, float duration, Action<float> animationFunction, bool force = false)
		{
			AnimationRequest request = new AnimationRequest(value, duration, animationFunction);
            return RequestAnimation(request, force);
		}

		/// <summary>
		/// Stops all animations attached to a MonoBehaviour object.
		/// </summary>
		/// <param name="value">MonoBehaviour to attached coroutine to.</param>
		public void StopAllAnimations(MonoBehaviour value)
		{
			if (value == null || !_animationQueue.ContainsKey(value)) return;

			foreach (AnimationRequest request in _animationQueue[value])
			{
				if (request.coroutine != null) value.StopCoroutine(request.coroutine);
				Promise.ResolveExisting(ref request.promise);
			}

			_animationQueue[value].Clear();
		}
		
		public Promise GetRunningPromise(MonoBehaviour value)
		{
            if (value == null || !_animationQueue.ContainsKey(value) || _animationQueue[value].Count == 0)
            {
                return null;
            }

            return _animationQueue[value].Peek().promise;
        }

		public bool HasAnimationRunning(MonoBehaviour value)
		{
			if (value == null || !_animationQueue.ContainsKey(value)) return false;

			return _animationQueue[value].Count > 0 && 
				_animationQueue[value].Peek().hasStarted && 
				!_animationQueue[value].Peek().hasCompleted;
		}

		public bool HasQuededAnimations(MonoBehaviour value)
		{
			if (value == null || !_animationQueue.ContainsKey(value)) return false;

			return _animationQueue[value].Count > 0;
		}

		private void Awake()
		{
			_animationQueue = new Dictionary<MonoBehaviour, Queue<AnimationRequest>>();
		}

		private void Update()
		{
			if (_animationQueue == null || _animationQueue.Count == 0) return;

			List<MonoBehaviour> keysToRemove = new List<MonoBehaviour>();

			foreach (KeyValuePair<MonoBehaviour, Queue<AnimationRequest>> animator in _animationQueue)
			{
				if (animator.Value != null && animator.Value.Any())
				{
					if (!animator.Value.Peek().hasStarted) StartAnimationCoroutine(animator.Value.Peek());
					else if (animator.Value.Peek().hasCompleted) EnsureCoroutineHasStopped(animator.Value.Dequeue());
				}
				else keysToRemove.Add(animator.Key);
			}

			foreach (MonoBehaviour key in keysToRemove)
			{
				_animationQueue.Remove(key);
			}
		}
	}
}
