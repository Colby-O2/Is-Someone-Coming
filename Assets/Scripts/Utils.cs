using System;
using UnityEngine;
using System.Collections;
using PlazmaGames.Core;
using PlazmaGames.Math;

namespace ColbyO.SJ26
{
    public static class Utils
    {
        public delegate ref TRef RefFunc<TRef>();
        
        public static IEnumerator DisableNextFrame(GameObject targetObj)
        {
            yield return null; 

            if (targetObj != null)
            {
                targetObj.SetActive(false);
            }
        }

        public static Promise Animate(Action<float> setter, float from, float to, float duration, float delay = 0)
        {
            float start = Time.time;
            return SJ26GameManager.GlobalScheduler.Every(0, () =>
            {
                float t = MathExt.Clamp01((Time.time - (start + delay)) / duration);
                setter(Mathf.Lerp(from, to, t));
                return t < 1;
            });
        }
        
        public static Promise Animate(Action<Quaternion> setter, Quaternion from, Quaternion to, float duration, float delay = 0)
        {
            float start = Time.time;
            return SJ26GameManager.GlobalScheduler.Every(0, () =>
            {
                float t = MathExt.Clamp01((Time.time - (start + delay)) / duration);
                setter(Quaternion.Lerp(from, to, t));
                return t < 1;
            });
        }

        public static bool CanCameraSee(Camera camera, Renderer obj)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, obj.bounds);
        }
    }
}