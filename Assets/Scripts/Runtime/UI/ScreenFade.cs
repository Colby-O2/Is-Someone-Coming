using UnityEngine;
using UnityEngine.UI;

using System.Collections;

using PlazmaGames.Core;

using TMPro;
using ColbyO.SJ26;

namespace ColbyO.Sj26.UI
{
    public class ScreenFade : MonoBehaviour
    {
        [SerializeField] private Image _img;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private AudioSource _as;

        private Promise _promise;

        public Promise FadeOut(float duration)
        {
            Promise.CreateExisting(ref _promise);
            StartCoroutine(Fade(0f, 1f, duration));
            return _promise;
        }

        public Promise WriteFadeOutText(string text, float typeSpeed, float duration)
        {
            Promise.CreateExisting(ref _promise);
            StartCoroutine(WriteText(text, typeSpeed, duration));
            return _promise;
        }

        public Promise FadeIn(float duration)
        {
            Promise.CreateExisting(ref _promise);
            StartCoroutine(Fade(1f, 0f, duration));
            return _promise;
        }

        private IEnumerator WriteText(string textToType, float typeSpeed, float duration)
        {
            _text.text = string.Empty;
            _text.gameObject.SetActive(true);
            if (string.IsNullOrEmpty(textToType))
                yield break;

            WaitForSeconds wait = new WaitForSeconds(typeSpeed);

            if (_as != null && !_as.isPlaying) _as.Play();

            for (int i = 0; i < textToType.Length; i++)
            {
                while (SJ26GameManager.IsPaused)
                {
                    if (_as != null && _as.isPlaying) _as.Stop();
                    yield return null;
                }

                if (_as != null && !_as.isPlaying) _as.Play();

                _text.text += textToType[i];
                yield return wait;
            }

            if (_as != null && _as.isPlaying) _as.Stop();

            yield return new WaitForSeconds(duration);

            _text.gameObject.SetActive(false);

            Promise.ResolveExisting(ref _promise);
        }

        private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
        {
            float time = 0f;
            Color color = _img.color;

            while (time < duration)
            {
                float t = time / duration;
                if (startAlpha < endAlpha) t = 1f - t;
                t = Mathf.Pow(t, 3.0f);
                if (startAlpha < endAlpha) t = 1f - t;
                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                _img.color = color;

                time += Time.deltaTime;
                yield return null;
            }

            color.a = endAlpha;
            _img.color = color;
            Promise.ResolveExisting(ref _promise);
        }
    }
}