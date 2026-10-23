using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

using PlazmaGames.Attribute;
using PlazmaGames.Core;

namespace ColbyO.SJ26.Enviroment
{
    public class LightController : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _mr;
        [SerializeField] private GameObject _volmetricLight;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioSource _audioSourcePowerDownPop;
        [SerializeField] private AudioClip _powerDownPopClip;

        [SerializeField, ReadOnly] private List<Light> _lights;
        [SerializeField, ReadOnly] private List<LightData> _defaultData;
        [SerializeField, ReadOnly] private float _defaultSaturationMultiplier;

        private Coroutine _activeRoutine;
        private static readonly int SaturationProp = Shader.PropertyToID("_SaturationMultiplier");

        private float _defaultVolume;
        private float _defaultPitch;

        private void Awake()
        {
            _lights = new List<Light>();
            _defaultData = new List<LightData>();

            var lights = GetComponentsInChildren<Light>();

            foreach (Light light in lights)
            {
                _lights.Add(light);
                _defaultData.Add(new LightData(light.intensity, light.color));
            }

            if (_mr != null)
            {
                _defaultSaturationMultiplier = _mr.material.GetFloat(SaturationProp);
            }

            if (_audioSource != null)
            {
                _defaultVolume = _audioSource.volume;
                _defaultPitch = _audioSource.pitch * UnityEngine.Random.Range(0.97f, 1.03f);
                _audioSource.pitch = _defaultPitch;
            }
        }

        public void Mute(bool state)
        {
            _audioSource.mute = state;
        }

        public void SetIntensityMultiplier(float multiplier)
        {
            multiplier = Mathf.Max(0f, multiplier);

            if (_mr != null)
            {
                _mr.material.SetFloat(SaturationProp, _defaultSaturationMultiplier * multiplier);
            }

            if (_volmetricLight != null)
            {
                _volmetricLight.SetActive(multiplier > 0.05f);
            }

            for (int i = 0; i < _lights.Count; i++)
            {
                _lights[i].intensity = _defaultData[i].intensity * multiplier;
            }

            if (_audioSource != null)
            {
                if (multiplier <= 0.01f && _audioSource.isPlaying)
                {
                    _audioSource.Stop();
                }
                else if (multiplier > 0.01f)
                {
                    if (!_audioSource.isPlaying) _audioSource.Play();
                    _audioSource.volume = _defaultVolume * multiplier;
                }
            }
        }

        public void SetIntensity(float intensity)
        {
            if (intensity < 0.01f)
            {
                if (_audioSource != null && _audioSource.isPlaying) _audioSource.Stop();
                SetIntensityMultiplier(0f);
                return;
            }

            if (_audioSource != null && !_audioSource.isPlaying) _audioSource.Play();

            if (_mr != null) _mr.material.SetFloat(SaturationProp, _defaultSaturationMultiplier);
            if (_volmetricLight != null) _volmetricLight.SetActive(true);

            foreach (Light light in _lights)
            {
                light.intensity = intensity;
            }
        }

        public void SetColor(Color color)
        {
            _volmetricLight.GetComponent<Renderer>().material.SetColor("_BaseColor", color);

            foreach (Light light in _lights)
            {
                light.color = color;
            }
        }

        public void ResetLight()
        {
            StopActiveRoutine();
            if (_mr != null) _mr.material.SetFloat(SaturationProp, _defaultSaturationMultiplier);
            if (_volmetricLight != null) _volmetricLight.SetActive(true);

            for (int i = 0; i < _lights.Count; i++)
            {
                _lights[i].intensity = _defaultData[i].intensity;
                _lights[i].color = _defaultData[i].color;
            }

            if (_audioSource != null)
            {
                _audioSource.volume = _defaultVolume;
                _audioSource.pitch = _defaultPitch;
                if (!_audioSource.isPlaying) _audioSource.Play();
            }
        }

        public void TriggerOutage(float delay, float flickerDuration, Color brownoutTint)
        {
            StopActiveRoutine();
            _activeRoutine = StartCoroutine(OutageRoutine(delay, flickerDuration, brownoutTint));
        }

        private void StopActiveRoutine()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
        }

        private IEnumerator OutageRoutine(float delay, float flickerDuration, Color brownoutTint)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            float noiseSeed = UnityEngine.Random.Range(0f, 100f);

            while (elapsed < flickerDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / flickerDuration;

                float noise = Mathf.PerlinNoise(Time.time * 25f, noiseSeed);
                float multiplier = noise > (progress * 0.75f) ? noise : 0f;

                SetIntensityMultiplier(multiplier);

                for (int i = 0; i < _lights.Count; i++)
                {
                    _lights[i].color = Color.Lerp(_defaultData[i].color, brownoutTint, 1f - multiplier);
                }

                if (_audioSource != null)
                {
                    float struggleBoost = Mathf.Sin(progress * Mathf.PI) * 1.4f;
                    _audioSource.volume = _defaultVolume * multiplier * Mathf.Max(1f, struggleBoost);

                    _audioSource.pitch = _defaultPitch * Mathf.Lerp(0.75f, 1.1f, multiplier) * (1f - (progress * 0.2f));
                }

                yield return null;
            }

            SetIntensityMultiplier(1.3f);
            if (_audioSource != null)
            {
                _audioSource.volume = Mathf.Min(1f, _defaultVolume * 1.6f);
                _audioSource.pitch = _defaultPitch * 1.15f;
            }
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.03f, 0.08f));

            SetIntensityMultiplier(0f);
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.volume = _defaultVolume;
                _audioSource.pitch = _defaultPitch;

                if (_audioSourcePowerDownPop != null && _powerDownPopClip != null)
                {
                    _audioSourcePowerDownPop.PlayOneShot(_powerDownPopClip, _defaultVolume);
                }
            }

            _activeRoutine = null;
        }

        [Serializable]
        private struct LightData
        {
            public float intensity;
            public Color color;

            public LightData(float intensity, Color color)
            {
                this.intensity = intensity;
                this.color = color;
            }
        }
    }
}