using UnityEngine;

using PlazmaGames.Core.MonoSystem;

namespace ColbyO.SJ26.Enviroment
{
    public interface ILightMonoSystem : IMonoSystem
    {
        public void SetLightIntensity(float value, Color? color = null);
        public void TriggerPowerOutage(float baseFlickerDuration = 1.2f, float maxStaggerDelay = 0.6f, Vector3? waveOrigin = null, float waveSpeed = 12f);
        public void TriggerOutageWithEmergencyBackup(float blackoutPause = 2.5f, float baseFlickerDuration = 1.2f, float maxStaggerDelay = 0.6f, Vector3? waveOrigin = null, float waveSpeed = 12f);
        public void Mute(bool state);
        public void ResetLights();
    }
}