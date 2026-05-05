using UnityEngine;
using System.Collections;

public class SmokeCloud : MonoBehaviour
{
    public float lifetime        = 2.5f;
    public float fadeInDuration  = 0.15f;
    public float fadeOutDuration = 1.4f;
    public int   quadCount       = 12;
    public float size            = 2f;
    public Color cloudColor      = new Color(0.82f, 0.82f, 0.82f, 1f);

    private Material[] _materials;

    public static SmokeCloud Spawn(Vector3 position, float size = 2f, float fadeOutDuration = 1f, Color? color = null)
    {
        foreach (var existing in FindObjectsByType<SmokeCloud>(FindObjectsSortMode.None))
            Destroy(existing.gameObject);

        var go = new GameObject("SmokeCloud");
        go.transform.position = position;
        var cloud = go.AddComponent<SmokeCloud>();
        cloud.size            = size;
        cloud.fadeOutDuration = fadeOutDuration;
        cloud.lifetime        = 0.1f + fadeOutDuration;
        cloud.fadeInDuration  = 0.1f;
        if (color.HasValue) cloud.cloudColor = color.Value;
        return cloud;
    }

    private void Start()
    {
        var shader = Shader.Find("Custom/SmokeCloud");
        if (shader == null)
        {
            Debug.LogError("SmokeCloud: shader 'Custom/SmokeCloud' not found. Make sure the shader is in the project.");
            Destroy(gameObject);
            return;
        }

        _materials = new Material[quadCount];

        for (int i = 0; i < quadCount; i++)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform);
            Destroy(quad.GetComponent<MeshCollider>());

            // Random spread — large circle, ~4x wider than before
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist  = Random.Range(0f, size * 1.2f);
            float yOff  = Random.Range(-size * 0.4f, size * 0.4f);
            quad.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * dist, yOff, Mathf.Sin(angle) * dist);

            float s = size * Random.Range(0.8f, 1.2f);
            quad.transform.localScale = new Vector3(s, s, 1f);

            var mat = new Material(shader);
            mat.SetColor("_Color",       cloudColor);
            mat.SetFloat("_NoiseScale",  Random.Range(2.5f, 4f));
            mat.SetFloat("_ScrollSpeed", Random.Range(0.02f, 0.06f));
            mat.SetFloat("_Density",     Random.Range(0.65f, 0.82f));
            mat.SetVector("_NoiseOffset", new Vector4(
                Random.Range(0f, 100f), Random.Range(0f, 100f), 0f, 0f));
            mat.SetFloat("_Dissolve", 0.3f); // start slightly dispersed, not fully invisible

            var rend = quad.GetComponent<MeshRenderer>();
            rend.material            = mat;
            rend.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows      = false;

            _materials[i] = mat;
        }

        StartCoroutine(CloudLifetime());
    }

    private IEnumerator CloudLifetime()
    {
        float hold = Mathf.Max(0f, lifetime - fadeInDuration - fadeOutDuration);

        // Fade in: dissolve 0.3 → 0 (starts already slightly dispersed)
        for (float t = 0f; t < fadeInDuration; t += Time.deltaTime)
        {
            SetDissolve(Mathf.Lerp(0.3f, 0f, t / fadeInDuration));
            yield return null;
        }
        SetDissolve(0f);

        yield return new WaitForSeconds(hold);

        // Fade out: dissolve 0 → 1
        for (float t = 0f; t < fadeOutDuration; t += Time.deltaTime)
        {
            SetDissolve(Mathf.Lerp(0f, 1f, t / fadeOutDuration));
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetDissolve(float value)
    {
        foreach (var mat in _materials)
            if (mat != null) mat.SetFloat("_Dissolve", value);
    }

    private void OnDestroy()
    {
        foreach (var mat in _materials)
            if (mat != null) Destroy(mat);
    }
}
