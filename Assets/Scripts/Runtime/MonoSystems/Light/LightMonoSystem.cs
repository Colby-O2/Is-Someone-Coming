using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ColbyO.SJ26.Enviroment
{
    public class LightMonoSystem : MonoBehaviour, ILightMonoSystem
    {
        [SerializeField] private string _lightTag = "OffceLight";

        [Header("Power Outage Settings")]
        [SerializeField] private Color _brownoutColor = new Color(1f, 0.45f, 0.15f);
        [SerializeField] private Color _emergencyColor = new Color(0.8f, 0.05f, 0.05f);

        private List<LightController> _lights;
        private Coroutine _outageRoutine;

        private void Start()
        {
            _lights =  FindObjectsByType<LightController>().ToList();
        }

        private void OnDestroy()
        {
            if (_outageRoutine != null)
            {
                StopCoroutine(_outageRoutine);
                _outageRoutine = null;
            }
        }

        public void SetLightIntensity(float intensity, Color? color = null)
        {
            foreach (var light in _lights)
            {
                light.SetIntensity(intensity);
                if (color.HasValue) light.SetColor(color.Value);
            }
        }

        public void ResetLights()
        {
            if (_outageRoutine != null) StopCoroutine(_outageRoutine);

            foreach (var light in _lights)
            {
                light.ResetLight();
            }
        }

        public void TriggerPowerOutage(float baseFlickerDuration = 1.2f, float maxStaggerDelay = 0.6f, Vector3? waveOrigin = null, float waveSpeed = 12f)
        {
            if (_outageRoutine != null) StopCoroutine(_outageRoutine);

            foreach (var light in _lights)
            {
                float delay = UnityEngine.Random.Range(0f, maxStaggerDelay);

                if (waveOrigin.HasValue)
                {
                    float distance = Vector3.Distance(waveOrigin.Value, light.transform.position);
                    delay = (distance / waveSpeed) + UnityEngine.Random.Range(0f, 0.15f);
                }

                float duration = baseFlickerDuration + UnityEngine.Random.Range(-0.3f, 0.4f);
                light.TriggerOutage(delay, Mathf.Max(0.2f, duration), _brownoutColor);
            }
        }

        public void TriggerOutageWithEmergencyBackup(float blackoutPause = 2.5f, float baseFlickerDuration = 1.2f, float maxStaggerDelay = 0.6f, Vector3? waveOrigin = null, float waveSpeed = 12f)
        {
            if (_outageRoutine != null) StopCoroutine(_outageRoutine);
            if (this == null) return;
            _outageRoutine = StartCoroutine(EmergencyBackupSequence(blackoutPause));
        }

        private IEnumerator EmergencyBackupSequence(float blackoutPause, float baseFlickerDuration = 1.2f, float maxStaggerDelay = 0.6f, Vector3? waveOrigin = null, float waveSpeed = 12f)
        {
            TriggerPowerOutage(baseFlickerDuration, maxStaggerDelay, waveOrigin, waveSpeed);

            yield return new WaitForSeconds(1.4f + blackoutPause);

            for (int i = 0; i < _lights.Count; i += 3)
            {
                _lights[i].SetColor(_emergencyColor);
                _lights[i].SetIntensityMultiplier(0.25f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.35f));
            }
        }
    }
}