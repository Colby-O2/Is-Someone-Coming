using PlazmaGames.Attribute;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColbyO.SJ26.Utilities
{
    public class AudioCyclePlayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource _as;
        [SerializeField] private List<AudioClip> _clips;

        [Header("Settings")]
        [SerializeField] private float _delayBetween;

        [SerializeField, ReadOnly] private bool _isPlaying;

        public bool IsPlaying { get => _isPlaying; }

        private void Awake()
        {
            if (!_as) _as = GetComponent<AudioSource>();

            _isPlaying = false;
        }

        private IEnumerator StartAudioLoop()
        {
            _isPlaying = true;

            while (_isPlaying)
            {
                AudioClip clip = _clips[Random.Range(0, _clips.Count)];
                _as.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length + _delayBetween);
            }
        }

        public void Play()
        {
            if (_clips == null || _clips.Count == 0) return;
            StartCoroutine(StartAudioLoop());
        }

        public void Stop()
        {
            _isPlaying = false;
            StopAllCoroutines();
        }
    }
}
