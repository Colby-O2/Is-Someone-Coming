using PlazmaGames.Core;
using PlazmaGames.Core.MonoSystem;

using ColbyO.VNTG.CRT;
using ColbyO.VNTG.PSX;

namespace ColbyO.Sj26.Effects
{
    public interface IVisualEffectMonoSystem : IMonoSystem
    {
        public PSXEffectSettings GetPSXSettings();
        public CRTSettings GetCRTSettings();
        public Promise FadeIn(float duration);
        public Promise FadeOut(float duration);
        public Promise WriteFadeOutText(string textToType, float typeSpeed, float duration);
        public void ResetPSXSettings();
        public void ResetCRTSettings();
    }
}