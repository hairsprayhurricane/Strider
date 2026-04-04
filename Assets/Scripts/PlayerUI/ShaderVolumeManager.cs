using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class ShaderVolumeManager : MonoBehaviour
{
    private Volume volume;
    private Vignette vignette;
    private LiftGammaGain lgg;
    private float gainOffsetDefaultValue;
    private ChromaticAberration chromaticAberration;
    private float chromaticAberrationDefaultIntensity;
    private BleedingColors bleedingColors;
    private float bleedingColorsDefaultIntensity;
    private float bleedingColorsDefaultShift;
    private ColorAdjustments colorAdjustments;
    private Color colorAdjustmentsDefaultColor;
    private Distortion distortion;
    private float distortionDefaultIntensity;
    private FilmGrain filmGrain;
    private float filmGrainIntensity;
    private float filmGrainResponse;
    /*private Exposure exposure;
    private float exposureIntensityDefault;
    private float exposureFixedExposure;
    private float exposureFixedExposureDefaultValue;
    private float exposureCompensation;
    private float exposureCompensationDefaultValue;*/
    private Scanlines scanlines;
    private float scanlinesIntensityDefault;

    private Color currentFilterColor;
    private float currentFilterIntensity = 1f;
    private float defaultFilterIntensity = 1f;
    private Coroutine intensityCoroutine;

    private Color vignetteDefaultColor;
    private Coroutine vignetteColorCoroutine;

    private static ShaderVolumeManager _instance;
    public static ShaderVolumeManager Instance { get { return _instance; } }

    //public Color enemyDefaultColor;
    public Color enemySearchColor = Color.orange;
    public Color enemyAttackColor = Color.red;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

    }
    void Start()
    {
        volume = GetComponent<Volume>();

        T GetEffect<T>() where T : VolumeComponent
        {
            T effect;
            volume.profile.TryGet(out effect);
            return effect;
        }

        vignette = GetEffect<Vignette>();
        lgg = GetEffect<LiftGammaGain>();
        chromaticAberration = GetEffect<ChromaticAberration>();
        bleedingColors = GetEffect<BleedingColors>();
        colorAdjustments = GetEffect<ColorAdjustments>();
        distortion = GetEffect<Distortion>();
        filmGrain = GetEffect<FilmGrain>();
        //exposure  = GetEffect<Exposure>();
        scanlines = GetEffect<Scanlines>();

        if (vignette != null)
        {
            vignette.active = true;
            vignette.color.overrideState = true;
            vignetteDefaultColor = vignette.color.value;
        }
        if (lgg != null)
        {
            gainOffsetDefaultValue = lgg.gain.value.w;
        }
        if (chromaticAberration != null)
        {
            chromaticAberrationDefaultIntensity = chromaticAberration.intensity.value;
        }
        if (bleedingColors != null)
        {
            bleedingColorsDefaultIntensity = bleedingColors.intensity.value;
            bleedingColorsDefaultShift = bleedingColors.shift.value;
        }
        if (colorAdjustments != null)
    {
        Color hdrColor = colorAdjustments.colorFilter.value;
        float ev = Mathf.Log(Mathf.Max(hdrColor.maxColorComponent, 1e-5f), 2f);
        currentFilterColor = ev > 0f ? hdrColor / Mathf.Pow(2f, ev) : hdrColor;
        currentFilterIntensity = ev;
        defaultFilterIntensity = ev;
        colorAdjustmentsDefaultColor = hdrColor;
    }
        if (distortion != null)
        {
            distortionDefaultIntensity = distortion.intensity.value;
        }
        if (filmGrain != null)
        {
            filmGrainIntensity = filmGrain.intensity.value;
            filmGrainResponse = filmGrain.response.value;
        }
        /*if(exposure != null)
        {
            exposureFixedExposureDefaultValue = exposure.fixedExposure.value;
            exposureCompensationDefaultValue = exposure.compensation.value;
        }*/
        if(scanlines != null)
        {
            scanlinesIntensityDefault = scanlines.intensity.value;
        }

        EnemyFSM.OnStateChanged += OnEnemyStateChanged;
    }

    private void OnDestroy()
    {
        EnemyFSM.OnStateChanged -= OnEnemyStateChanged;
    }

    private void OnEnemyStateChanged(EnemyFSM.GlobalState state)
    {
        switch (state)
        {
            case EnemyFSM.GlobalState.Search:
                SetVignetteColor(enemySearchColor, 1f);
                break;
            case EnemyFSM.GlobalState.Attack:
                SetVignetteColor(enemyAttackColor, 1f);
                break;
            case EnemyFSM.GlobalState.Calm:
                SetVignetteColor(vignetteDefaultColor, 1f);
                break;
        }
    }

    public void SetVignetteColor(Color target, float duration = 1f)
    {
        if (vignette == null) return;
        if (vignetteColorCoroutine != null) StopCoroutine(vignetteColorCoroutine);
        vignetteColorCoroutine = StartCoroutine(VignetteColorCoroutine(target, duration));
    }

    private IEnumerator VignetteColorCoroutine(Color target, float duration)
    {
        Color start = vignette.color.value;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            vignette.color.value = Color.Lerp(start, target, k);
            yield return null;
        }

        vignette.color.value = target;
        vignetteColorCoroutine = null;
    }

    public void SetVignetteColorByValue(int value)
    {
        if (vignette == null)
        {
            Start();
        }
        value = Mathf.Clamp(value, 0, 100);
        float t = value / 100f;
        Color lerpedColor = Color.Lerp(Color.red, Color.black, t);
        vignette.color.value = lerpedColor;
    }

    public void SetColorAdjustmentColorByValue(int value)
    {
        if (colorAdjustments == null) Start();

        value = Mathf.Clamp(value, 0, 100);
        float t = value / 100f;
        currentFilterColor = Color.Lerp(Color.red, colorAdjustmentsDefaultColor, t);

        ApplyColorFilter();
    }

    public void SetColorAdjustmentDefaultIntensity(float value)
    {
        defaultFilterIntensity = value;
        currentFilterIntensity = value;

        ApplyColorFilter();
    }

    public void SetColorAdjustmentColorIntensity(float targetEV, float duration)
    {
        if (colorAdjustments == null) Start();

        if (intensityCoroutine != null)
        {
            StopCoroutine(intensityCoroutine);
        }

        intensityCoroutine = StartCoroutine(ColorFilterIntensityCoroutine(targetEV, duration));
    }

    public IEnumerator ColorFilterIntensityCoroutine(float targetEV, float duration)
    {
        float startEV = currentFilterIntensity;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.SmoothStep(0f, 1f, k);

            currentFilterIntensity = Mathf.Lerp(startEV, targetEV, s);
            ApplyColorFilter();

            yield return null;
        }

        currentFilterIntensity = targetEV;
        ApplyColorFilter();
        intensityCoroutine = null;
    }

    private void ApplyColorFilter()
    {
        colorAdjustments.colorFilter.value = currentFilterColor * Mathf.Pow(2f, currentFilterIntensity);
    }

    public void SetFilmGrainByValue(int value)
    {
        if (filmGrain == null)
        {
            Start();
        }
        value = Mathf.Clamp(value, 0, 100);
        float t = value / 100f;
        filmGrain.intensity.value = Mathf.Lerp(1f, 0f, t);
    }
    public void SetFilmGrainByValue(float value)
    {
        if (filmGrain == null)
        {
            Start();
        }
        filmGrain.intensity.value = value;
    }

    public IEnumerator DramaticallyIncreaseGain(float force = 5f)
    {
        if (lgg == null)
        {
            Start();
        }
        Vector4 gain = lgg.gain.value;
        gain.w = force;
        lgg.gain.value = gain;

        while (gain.w > gainOffsetDefaultValue)
        {
            gain.w -= 0.1f;
            lgg.gain.value = gain;
            yield return new WaitForSeconds(0.01f);
        }
        gain.w = gainOffsetDefaultValue;
        lgg.gain.value = gain;
    }

    public IEnumerator DramaticallyIncreaseChromaticAberration(float force = 5f)
    {
        if (chromaticAberration == null)
        {
            Start();
        }
        float maxIntensity = force;
        chromaticAberration.intensity.overrideState = true;
        chromaticAberration.intensity.value = maxIntensity;

        float currentIntensity = maxIntensity;
        while (currentIntensity > chromaticAberrationDefaultIntensity)
        {
            currentIntensity -= 0.1f;
            chromaticAberration.intensity.value = currentIntensity;
            yield return new WaitForSeconds(0.01f);
        }
        chromaticAberration.intensity.value = chromaticAberrationDefaultIntensity;
    }

    public IEnumerator DramaticallyIncreaseBleedingColors(float damage = 30)
    {
        if (bleedingColors == null)
        {
            Start();
        }

        damage = Mathf.Clamp(damage, 0f, 100f);

        float maxIntensity = 7f * (damage / 100f);
        float maxShift = 7f * (damage / 100f);

        bleedingColors.intensity.overrideState = true;

        bleedingColors.intensity.value = maxIntensity;
        bleedingColors.shift.value = maxShift;

        float duration = 1f;
        float elapsedTime = 0f;

        float prevOsc = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float damp = 1f - (elapsedTime / duration);
            float sharpSin = Mathf.Sin(elapsedTime * 80f);
            float sharpened = Mathf.Sign(sharpSin) * Mathf.Pow(Mathf.Abs(sharpSin), 0.5f);
            float osc = sharpened * damp * 0.3f;

            osc = Mathf.Lerp(prevOsc, osc, 0.8f);
            prevOsc = osc;

            bleedingColors.intensity.value = maxIntensity * (1f + osc);
            bleedingColors.shift.value = maxShift * (1f + osc);

            yield return new WaitForEndOfFrame();
        }

        float returnDuration = 0.5f;
        float returnElapsed = 0f;

        float startIntensity = bleedingColors.intensity.value;
        float startShift = bleedingColors.shift.value;

        while (returnElapsed < returnDuration)
        {
            returnElapsed += Time.deltaTime;
            float tReturn = Mathf.Clamp01(returnElapsed / returnDuration);
            bleedingColors.intensity.value = Mathf.Lerp(startIntensity, bleedingColorsDefaultIntensity, tReturn);
            bleedingColors.shift.value = Mathf.Lerp(startShift, bleedingColorsDefaultShift, tReturn);

            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator DramaticallyChangeColorAdjustments(Color color, float duration)
    {
        if (colorAdjustments == null)
        {
            Start();
        }

        // Останавливаем корутину интенсивности чтобы не было конфликта
        if (intensityCoroutine != null)
        {
            StopCoroutine(intensityCoroutine);
            intensityCoroutine = null;
        }

        float halfDuration = duration / 2f;

        Color startColor = colorAdjustments.colorFilter.value;
        Color defaultColor = colorAdjustmentsDefaultColor;

        float time = 0f;
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            float t = time / halfDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            colorAdjustments.colorFilter.value = Color.Lerp(startColor, color, smoothT);
            yield return new WaitForEndOfFrame();
        }

        time = 0f;
        startColor = color;
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            float t = time / halfDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            colorAdjustments.colorFilter.value = Color.Lerp(startColor, defaultColor, smoothT);
            yield return new WaitForEndOfFrame();
        }

        colorAdjustments.colorFilter.value = defaultColor;

        // Восстанавливаем currentFilterColor после анимации
        currentFilterColor = defaultColor;
        currentFilterIntensity = defaultFilterIntensity;
    }

    public void ChangeColorAdjustmentColorFilter(Color color)
    {
        colorAdjustments.colorFilter.value = color;
    }

    public IEnumerator DramaticallyIncreaseDistortion(float maxIntensity = 0.5f)
    {
        if (distortion == null)
        {
            Start();
        }
        
        distortion.intensity.overrideState = true;
        distortion.intensity.value = maxIntensity;

        float currentIntensity = maxIntensity;
        while (currentIntensity > distortionDefaultIntensity)
        {
            currentIntensity -= 0.01f;
            distortion.intensity.value = currentIntensity;
            yield return new WaitForSeconds(0.01f);
        }
        distortion.intensity.value = distortionDefaultIntensity;
    }
    public IEnumerator DramaticallyIncreaseAndStopDistortion(float maxIntensity = 0.5f)
    {
        if (distortion == null)
        {
            Start();
        }
        
        distortion.intensity.overrideState = true;

        float currentIntensity = distortion.intensity.value;
        while (currentIntensity < maxIntensity)
        {
            currentIntensity += 0.01f;
            distortion.intensity.value = currentIntensity;
            yield return new WaitForSeconds(0.01f);
        }
        //distortion.intensity.value = distortionDefaultIntensity;
    }

    public IEnumerator DramaticallyIncreaseDistortion(int damage)
    {
        if (distortion == null)
        {
            Start();
        }

        float maxIntensity = (damage / 100f) * 0.5f;

        distortion.intensity.overrideState = true;
        distortion.intensity.value = maxIntensity;

        float currentIntensity = maxIntensity;
        while (currentIntensity > distortionDefaultIntensity)
        {
            currentIntensity -= 0.01f;
            distortion.intensity.value = currentIntensity;
            yield return new WaitForSeconds(0.01f);
        }
        distortion.intensity.value = distortionDefaultIntensity;
    }

    public IEnumerator DistortionRandomizeOverTime(float totalTime, float minIntensity = 0f, float maxIntensity = 0.5f, float changeEvery = 0.1f, float returnDuration = 0.25f)
    {
        if (distortion == null)
        {
            Start();
        }

        distortion.intensity.overrideState = true;

        minIntensity = Mathf.Max(0f, minIntensity);
        maxIntensity = Mathf.Max(minIntensity, maxIntensity);

        float elapsed = 0f;

        float current = distortion.intensity.value;
        float target = Random.Range(minIntensity, maxIntensity);
        float timeToNext = 0f;

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            timeToNext -= Time.deltaTime;

            if (timeToNext <= 0f)
            {
                target = Random.Range(minIntensity, maxIntensity);
                timeToNext = changeEvery;
            }

            current = Mathf.Lerp(current, target, 0.2f);
            distortion.intensity.value = current;

            yield return new WaitForEndOfFrame();
        }

        float def = distortionDefaultIntensity;
        float t = 0f;
        float start = distortion.intensity.value;

        while (t < returnDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / returnDuration);
            float s = Mathf.SmoothStep(0f, 1f, k);
            distortion.intensity.value = Mathf.Lerp(start, def, s);
            yield return new WaitForEndOfFrame();
        }

        distortion.intensity.value = def;

    }

    public IEnumerator ChromaticAberrationRandomizeOverTime(float totalTime, float minIntensity = 0f, float maxIntensity = 0.5f, float changeEvery = 0.1f, float returnDuration = 0.25f)
    {
        if (chromaticAberration == null)
        {
            Start();
        }

        chromaticAberration.intensity.overrideState = true;

        minIntensity = Mathf.Max(0f, minIntensity);
        maxIntensity = Mathf.Max(minIntensity, maxIntensity);

        float elapsed = 0f;

        float current = chromaticAberration.intensity.value;
        float target = Random.Range(minIntensity, maxIntensity);
        float timeToNext = 0f;

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            timeToNext -= Time.deltaTime;

            if (timeToNext <= 0f)
            {
                target = Random.Range(minIntensity, maxIntensity);
                timeToNext = changeEvery;
            }

            current = Mathf.Lerp(current, target, 0.2f);
            chromaticAberration.intensity.value = current;

            yield return new WaitForEndOfFrame();
        }

        float def = chromaticAberrationDefaultIntensity;
        float t = 0f;
        float start = chromaticAberration.intensity.value;

        while (t < returnDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / returnDuration);
            float s = Mathf.SmoothStep(0f, 1f, k);
            chromaticAberration.intensity.value = Mathf.Lerp(start, def, s);
            yield return new WaitForEndOfFrame();
        }

        chromaticAberration.intensity.value = def;

    }



    public IEnumerator ColorAdjustmentsFadeFromBlackToDefault(float duration) // !
    {
        if (colorAdjustments == null)
        {
            Start();
        }

        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.contrast.overrideState = true;

        float defaultExposure = colorAdjustments.postExposure.value;
        float defaultContrast = colorAdjustments.contrast.value;

        float startExposure = -8f;
        float startContrast = Mathf.Clamp(defaultContrast - 50f, -100f, 100f);

        colorAdjustments.postExposure.value = startExposure;
        colorAdjustments.contrast.value = startContrast;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.SmoothStep(0f, 1f, k);

            colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, defaultExposure, s);
            colorAdjustments.contrast.value = Mathf.Lerp(startContrast, defaultContrast, s);

            yield return null;
        }

        colorAdjustments.postExposure.value = defaultExposure;
        colorAdjustments.contrast.value = defaultContrast;
    }


    public IEnumerator FilmGrainPeakThenReturn(float duration)
    {
        if (filmGrain == null)
        {
            Start();
        }

        float defIntensity = filmGrainIntensity;
        float defResponse = filmGrainResponse;

        filmGrain.intensity.overrideState = true;
        filmGrain.response.overrideState = true;

        float maxIntensity = 1f;
        float maxResponse = 1f;

        filmGrain.intensity.value = maxIntensity;
        filmGrain.response.value = maxResponse;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.SmoothStep(0f, 1f, k);

            filmGrain.intensity.value = Mathf.Lerp(maxIntensity, defIntensity, s);
            filmGrain.response.value = Mathf.Lerp(maxResponse, defResponse, s);

            yield return new WaitForEndOfFrame();
        }

        filmGrain.intensity.value = defIntensity;
        filmGrain.response.value = defResponse;
    }

    public IEnumerator FilmGrainPeakCoroutine(float duration)
    {
        if (filmGrain == null)
        {
            Start();
        }

        float defIntensity = filmGrainIntensity;
        float defResponse = filmGrainResponse;

        filmGrain.intensity.overrideState = true;
        filmGrain.response.overrideState = true;

        float maxIntensity = 1f;
        float maxResponse = 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.SmoothStep(0f, 1f, k);

            filmGrain.intensity.value = Mathf.Lerp(defIntensity, maxIntensity, s);
            filmGrain.response.value = Mathf.Lerp(defResponse, maxResponse, s);

            yield return new WaitForEndOfFrame();
        }

        filmGrain.intensity.value = defIntensity;
        filmGrain.response.value = defResponse;
    }
    public IEnumerator ScanlinesPeakCoroutine(float duration)
    {
        if (scanlines == null)
        {
            Start();
        }

        float defIntensity = scanlinesIntensityDefault;

        scanlines.intensity.overrideState = true;

        float maxIntensity = 1f;
        scanlines.intensity.value = maxIntensity;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.SmoothStep(0, 1, k);

            scanlines.intensity.value = Mathf.Lerp(maxIntensity, defIntensity, s);

            yield return new WaitForEndOfFrame();
        }

        scanlines.intensity.value = defIntensity;
    }

    public void SetScanlinesIntensity(float value)
    {
        if (!scanlines) Start();

        scanlines.intensity.value = value;
    }
    public void SetScanlinesDefaultIntensity()
    {
        if (!scanlines) Start();

        scanlines.intensity.value = scanlinesIntensityDefault;
    }
    
    public IEnumerator FilmGrainResetCoroutine(float resetDuration)
    {
        if (filmGrain == null)
        {
            yield break;
        }

        filmGrain.intensity.overrideState = true;
        filmGrain.response.overrideState = true;

        float defIntensity = filmGrainIntensity;
        float defResponse = filmGrainResponse;

        float currentIntensity = filmGrain.intensity.value;
        float currentResponse = filmGrain.response.value;

        float t = 0f;
        while (t < resetDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / resetDuration);
            float s = Mathf.SmoothStep(0f, 1f, k);

            filmGrain.intensity.value = Mathf.Lerp(currentIntensity, defIntensity, s);
            filmGrain.response.value = Mathf.Lerp(currentResponse, defResponse, s);

            yield return new WaitForEndOfFrame();
        }

        filmGrain.intensity.value = defIntensity;
        filmGrain.response.value = defResponse;
    }

    public void ChangeColorAdjustmentColor(Color newColor)
    {
        colorAdjustments.colorFilter.value = newColor;
    }
    public void SetDefaultColorColorAdjustment()
    {
        colorAdjustments.colorFilter.value = colorAdjustmentsDefaultColor;
        currentFilterColor = colorAdjustmentsDefaultColor;
        currentFilterIntensity = defaultFilterIntensity;
    }

    /*public void SetExposureCompensation(float newCompensationValue)
    {
        exposure.compensation.value = newCompensationValue;
    }
    public void SetExposureCompensationDefaultValue()
    {
        exposure.compensation.value = exposureCompensationDefaultValue;
    }*/
}