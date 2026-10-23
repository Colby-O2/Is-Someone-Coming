using UnityEngine;

namespace ColbyO.SJ26
{
    public class PitchOffset : MonoBehaviour
    {
        private AudioSource windAudio;

        [Header("Pitch Settings")]
        public float minPitch = 0.8f;
        public float maxPitch = 1.25f;

        [Header("Volume Settings")]
        public float minVolume = 0.4f;
        public float maxVolume = 1.0f;

        [Header("Gust")]
        public float gustSpeed = 0.25f;

        private float noiseOffset;

        void Start()
        {
            windAudio = GetComponent<AudioSource>();

            windAudio.loop = true;
            if (!windAudio.isPlaying)
            {
                windAudio.Play();
            }

            noiseOffset = Random.Range(0f, 1000f);
        }

        void Update()
        {
            float noise = Mathf.PerlinNoise(Time.time * gustSpeed, noiseOffset);

            windAudio.pitch = Mathf.Lerp(minPitch, maxPitch, noise);
            windAudio.volume = Mathf.Lerp(minVolume, maxVolume, noise);
        }
    }
}