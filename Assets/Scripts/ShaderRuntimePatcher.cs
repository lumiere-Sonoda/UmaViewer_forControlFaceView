using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Extra safety: walk all renderers in the scene periodically and swap unsupported shaders
// on their shared materials so meshes stop rendering magenta even if materials were created
// outside Resources or after asset load.
public class ShaderRuntimePatcher : MonoBehaviour
{
    private Shader _standard;
    private float _nextScanTime;
    private static readonly List<Renderer> _renderers = new List<Renderer>(512);

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _standard = Shader.Find("Standard");
        _nextScanTime = Time.realtimeSinceStartup + 1f;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        // throttle to once every 2 seconds
        if (Time.realtimeSinceStartup < _nextScanTime) return;
        _nextScanTime = Time.realtimeSinceStartup + 2f;
        if (_standard == null) return;

        _renderers.Clear();
        _renderers.AddRange(FindObjectsByType<Renderer>(FindObjectsSortMode.None));
        foreach (var r in _renderers)
        {
            if (!r) continue;
            var mats = r.sharedMaterials;
            var changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || m.shader == null) continue;
                if (m.shader.isSupported) continue;

                var mainTex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;

                m.shader = _standard;
                if (mainTex) m.SetTexture("_MainTex", mainTex);
                m.SetColor("_Color", color);
                changed = true;

                if (m.renderQueue >= 2450 && m.renderQueue <= 2550)
                {
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Mode", 1);
                }
                else if (m.renderQueue >= 2950)
                {
                    m.EnableKeyword("_ALPHABLEND_ON");
                    m.SetFloat("_Mode", 2);
                }
            }

            if (changed)
            {
                r.sharedMaterials = mats;
            }
        }
    }
}
