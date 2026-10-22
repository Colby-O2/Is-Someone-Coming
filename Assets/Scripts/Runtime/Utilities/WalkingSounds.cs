using UnityEngine;

using PlazmaGames.Audio;
using PlazmaGames.Core;

namespace ColbyO.SJ26.Utilities
{
    public class WalkingSound : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VelocityTracker _vel;
        [SerializeField] private AudioSource _as;
        [SerializeField] private AudioClip _indoorsClip;
        [SerializeField] private AudioClip _outdoorClip;

        [Header("Settings")]
        [SerializeField] private bool _isOutDoorsToStart = true;
        [Range(0f, 1f), SerializeField] private float _baseVolume;
        [SerializeField] private float _fadeSpeed = 5f;
        [SerializeField] private float _basePitch = 1f;
        [SerializeField] private float _pitchSpeedVariation = 0.05f;
        [SerializeField] private float _pitchRandomVariation = 0.02f;
        [SerializeField] private float _pitchSpeed = 0.1f;

        private float _targetVolume = 0f;
        private float _t = 0f;

        public bool Enabled { get; set; }

        private void Awake()
        {
            Enabled = true;
            _as.clip = _isOutDoorsToStart ? _outdoorClip : _indoorsClip;
            _as.loop = true;
            _as.playOnAwake = false;
            _as.volume = 0;
            _as.Play();
        }

        private void Update()
        {
            if (!Enabled)
            {
                if (_as.isPlaying) _as.Pause();
                return;
            }

            float speed = _vel.SpeedInPlane;
            bool isMoving = speed > 0.01f;

            float volume = _baseVolume * GameManager.GetMonoSystem<IAudioMonoSystem>().GetOverallVolume() * GameManager.GetMonoSystem<IAudioMonoSystem>().GetSfXVolume();

            _targetVolume = isMoving ? volume : 0f;

            float currentFadeSpeed = isMoving ? _fadeSpeed : _fadeSpeed * 5f;

            _as.volume = Mathf.MoveTowards(_as.volume, _targetVolume, currentFadeSpeed * Time.deltaTime);

            if (_as.volume > 0 && !_as.isPlaying)
            {
                _as.UnPause();
            }
            else if (_as.volume <= 0 && _as.isPlaying)
            {
                _as.Pause();
            }

            if (_as.isPlaying)
            {
                _t += Time.deltaTime;
                float pNoise = Mathf.PerlinNoise(0f, _t * _pitchSpeed);

                _as.pitch = _basePitch + (speed * _pitchSpeedVariation) + ((pNoise - 0.5f) * _pitchRandomVariation);
            }
        }

        public void SetIndoors() => ChangeClip(_indoorsClip);
        public void SetOutdoors() => ChangeClip(_outdoorClip);

        private void ChangeClip(AudioClip newClip)
        {
            if (_as.clip == newClip) return;
            _as.clip = newClip;
            if (_vel.SpeedInPlane > 0.01f) _as.Play();
        }
    }
}