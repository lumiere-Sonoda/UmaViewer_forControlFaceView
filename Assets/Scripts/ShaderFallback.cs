using UnityEngine;

// Runtime safety net: if a material references a shader that the current GPU/graphics API
// cannot run (e.g., DX11-only Gallop shaders on Metal), swap it to a simple built-in
// alternative so meshes render instead of turning magenta.
public class ShaderFallback : MonoBehaviour
{
    private Shader _standard;
    private float _nextScanTime;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _standard = Shader.Find("Standard");
        _nextScanTime = Time.realtimeSinceStartup + 1f;
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup < _nextScanTime) return;
        _nextScanTime = Time.realtimeSinceStartup + 5f; // throttle
        ApplyFallback();
    }

    private void ApplyFallback()
    {
        if (_standard == null) return;

        foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
        {
            // Skip assets in Project view to avoid dirtying them; only touch runtime copies.
            if (mat == null || mat.shader == null) continue;
            if (mat.shader.isSupported) continue;

            var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

            mat.shader = _standard;
            if (mainTex) mat.SetTexture("_MainTex", mainTex);
            mat.SetColor("_Color", color);

            // Preserve alpha intent heuristically.
            if (mat.renderQueue >= 2450 && mat.renderQueue <= 2550)
            {
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetFloat("_Mode", 1); // Cutout
            }
            else if (mat.renderQueue >= 2950)
            {
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.SetFloat("_Mode", 2); // Fade
            }
        }
    }
}
