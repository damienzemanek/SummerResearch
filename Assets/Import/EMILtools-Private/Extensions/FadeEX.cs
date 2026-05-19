using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EMILtools.Extensions
{

    [Serializable]
    public struct FadeSettings
    {
        public UnityEngine.Object targ;
        public float step;
        public float delay;
        public float delayToStartFading;

        public GameObject GetGO()
        {
            switch (targ)
            {
                case GameObject go: return go;
                case UnityEngine.Component c: return c.gameObject; 
                default: return null;
            }
        }

        public void SetColor(Color c)
        {
            switch (targ)
            {
                case Graphic g:
                    g.color = c;
                    break;
                case Material m:
                    m.color = c;
                    break;
                case Renderer r:
                    r.material.color = c;
                    break;
                case GameObject go:
                    if (go.TryGetComponent<Graphic>(out var gg))
                        gg.color = c;
                    else if (go.TryGetComponent<Renderer>(out var rr))
                        rr.material.color = c;
                    break;
            }
        }

        public Color GetColor()
        {
            switch (targ)
            {
                case Graphic g: return g.color;
                case Material m: return m.color;
                case Renderer r: return r.material.color;
                case GameObject go:
                    if (go.TryGetComponent<Graphic>(out var gg))
                        return gg.color;
                    if (go.TryGetComponent<Renderer>(out var rr))
                        return rr.material.color;
                    break;
            }
            return Color.red;
        }

        public void SetAlpha(float val)
        {
            switch(targ)
            {
                case Graphic g:
                    Color gc = g.color;
                    gc.a = val;
                    g.color = gc;
                    break;
                case Material m:
                    Color mc = m.color;
                    mc.a = val;
                    m.color = mc;
                    break;
                case Renderer r:
                    Color rc = r.material.color;
                    rc.a = val;
                    r.material.color = rc;
                    break;
            }
        }
    }

    public static class FadeEX
    {
        public static void EnableFadeOut(this MonoBehaviour host, FadeSettings fade)
        {
            ResetFade(fade, true);
            host.StartCoroutine(C_FadeToTransparent(fade, () => ResetFade(fade, false)));
        }

        public static void ResetFade(FadeSettings fade, bool _active)
        {
            Color color = fade.GetColor();
            color.a = 1f;
            fade.GetColor();

            fade.GetGO()?.gameObject.SetActive(_active);
        }

        public static IEnumerator C_FadeToTransparent(FadeSettings fade, Action postHook = null)
        {
            if (fade.delayToStartFading > 0)
                yield return new WaitForSeconds(fade.delayToStartFading);

            fade.GetGO()?.gameObject.SetActive(true);

            float fadeVal = 1f;
            Color currentColor = fade.GetColor();

            while (fadeVal > 0)
            {
                fadeVal -= fade.step;
                currentColor.a = fadeVal;

                fade.SetColor(currentColor);
                yield return new WaitForSeconds(fade.delay);
            }

            currentColor.a = 0;

            fade.SetColor(currentColor);

            postHook?.Invoke();
        }


        public static IEnumerator C_FadeToOpaque(FadeSettings fade, Action postHook = null)
        {
            if(fade.delayToStartFading > 0)
                yield return new WaitForSeconds(fade.delayToStartFading);


            fade.GetGO()?.gameObject.SetActive(true);

            float fadeVal = 0;
            Color currentColor = fade.GetColor();

            while (fadeVal < 1)
            {
                fadeVal += fade.step;
                currentColor.a = fadeVal;

                fade.SetColor(currentColor);
                yield return new WaitForSeconds(fade.delay);
            }

            currentColor.a = 1;
            fade.SetColor(currentColor);

            postHook?.Invoke();
        }
    }

}