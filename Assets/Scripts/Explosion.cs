using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;

public class Explosion
{
    private const short emmisionIntensityMax = 20; 
    private const short lightIntensityMax = 12000; 
    private const byte explosionRadius = 30; 
    private const string ExplosionGameObjectPath = "Prefabs/Explosives/ExplosionObj";
    private const string ParticlesPath = "Prefabs/Explosives/ExplosionParticles";
    private const string ExplosionPathSFX = "Audio/SFX/Explosives/HugeShortExplosion";
    private static GameObject ExplosionObj;
    private static AudioClip ExplosionAudioClip;
    private static List<GameObject> particlesGameObjects = new List<GameObject>();

    public static void CreateExplosion(float radius, Transform explosionCenter, bool damageOnlyPlayer = false, float kickForce = 40, short damage = -1)
    {
        if (Mathf.Approximately(radius, 0f)) return;

        Collider[] colliders = Physics.OverlapSphere(explosionCenter.position, radius);
        foreach (Collider other in colliders)
        {
            ProcessCollider(other, explosionCenter, damageOnlyPlayer, kickForce, damage);
        }

        if (!ExplosionObj) ExplosionObj = Resources.Load<GameObject>(ExplosionGameObjectPath);

        CreateVisuals(ExplosionObj, explosionCenter);
        PlayerController.Instance.ShakeCamera();

        if (PlayerController.Instance.GetDistanceToPlayer(explosionCenter.position) < 25)
            PlayerController.Instance.StartCoroutine(ShaderVolumeManager.Instance.ScanlinesPeakCoroutine(1));

        //CreateAndPlaySFX(ExplosionObj.transform.position);
    }

    private static void ProcessCollider(Collider target, Transform explosionCenter, bool damageOnlyPlayer, float kickForce, short damage)
    {
        if (!IsTargetInScope(target)) return;

        Vector3 directionToTarget = (target.transform.position - explosionCenter.position).normalized;
        float distanceToTarget = Vector3.Distance(explosionCenter.position, target.transform.position);

        RaycastHit[] hits = Physics.RaycastAll(
            explosionCenter.position,
            directionToTarget,
            distanceToTarget
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == target || IsIgnore(hit.collider))
                continue;

            if (hit.collider.CompareTag("Environment") || hit.collider.CompareTag("Cover"))
                return;
        }

        ApplyEffectsToTarget(target, explosionCenter, distanceToTarget, damageOnlyPlayer, kickForce, damage);
    }

    private static bool IsTargetInScope(Collider target)
    {
        return target.CompareTag("Player") || target.CompareTag("Enemy");
    }

    private static bool IsIgnore(Collider collider)
    {
        return !collider.CompareTag("IgnoreCollider");
    }

    private static void ApplyEffectsToTarget(Collider target, Transform explosionCenter, float distance, bool damageOnlyPlayer, float kickForce, short straightDamage)
    {
        float radius = explosionRadius;
        float normalizedDistance = Mathf.Clamp01(distance / radius);


        if (target.CompareTag("Player"))
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                short damage = straightDamage < 0
                    ? (short)Mathf.Lerp(100, 10, normalizedDistance)
                    : straightDamage;
                playerHealth.DamagePlayer(damage, explosionCenter, PlayerHealth.LastDamageType.explosion);
            }
        }
        else if (target.CompareTag("Enemy") && !damageOnlyPlayer)
        {
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.KickEnemy(explosionCenter, 10);
                enemy.TakeDamage(200);
            }
        }
        else if (target.CompareTag("ExplosiveObject"))
        {
            target.GetComponent<RedBarrel>().Boom(ClassicRandom.Range(0.3f, 4f));
        }
    }

    private static void CreateVisuals(GameObject creatableExplosion, Transform explosionCenter)
    {
        byte explosionCount = (byte)Random.Range(6, 11);

        for (short i = 0; i < explosionCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * 8f;
            Vector3 spawnPosition = explosionCenter.position + randomOffset;

            List<GameObject> instianedParticles = new List<GameObject>();

            GameObject explosionInstance = Object.Instantiate(creatableExplosion, spawnPosition, Quaternion.identity);

            if (particlesGameObjects.Count == 0)
            {
                Object[] particles = Resources.LoadAll(ParticlesPath, typeof(GameObject));

                foreach (Object prefab in particles)
                {
                    if (prefab is GameObject gameObject)
                    {
                        particlesGameObjects.Add(gameObject);
                    }
                }
            }

            for (short j = 0; j < Random.Range(6, 13); j++)
            {
                GameObject explosionParticle = Object.Instantiate(particlesGameObjects[Random.Range(0, particlesGameObjects.Count)], spawnPosition, Quaternion.identity);
                float size = Random.Range(1, 6);
                explosionParticle.transform.localScale = new Vector3(
                    explosionParticle.transform.localScale.x * size,
                    explosionParticle.transform.localScale.y * size,
                    explosionParticle.transform.localScale.z
                );
                explosionParticle.GetComponent<Rigidbody>().AddForce(Random.insideUnitSphere.normalized * 20, ForceMode.Impulse);
                instianedParticles.Add(explosionParticle);

                Object.Destroy(explosionParticle, 1);
            }

            Animator explosionAnimator = explosionInstance.GetComponent<Animator>();
            if (explosionAnimator != null)
            {
                float randomBlend = Random.Range(0f, 2f);
                explosionAnimator.SetFloat("Blend", randomBlend);
            }

            AnimatorController animatorController = explosionAnimator.runtimeAnimatorController as AnimatorController;
            if (animatorController != null && animatorController.layers.Length > 0)
            {
                AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
                ChildAnimatorState[] states = stateMachine.states;

                if (states.Length > 0)
                {
                    float clipLength = GetClipLengthFromMotion(states[0].state.motion);

                    if (clipLength > 0f)
                    {
                        var instanceMaterial = explosionInstance.GetComponent<Renderer>().material;
                        var light = explosionInstance.GetComponent<Light>();
                        var instanceObject = ShaderVolumeManager.Instance;

                        instanceObject.StartCoroutine(EmissionIntensityBehavior(instanceMaterial, clipLength, instianedParticles));
                        instanceObject.StartCoroutine(LightBehavior(light, clipLength));

                        Object.Destroy(explosionInstance, clipLength);
                    }
                }
            }
        }
    }

    private static IEnumerator LightBehavior(Light light, float clipDuration)
    {
        if (light == null) yield break;

        light.intensity = lightIntensityMax;

        float elapsedTime = 0f;
        while (light != null && elapsedTime < clipDuration && light.intensity > 0f)
        {
            elapsedTime += 0.01f;
            light.intensity = Mathf.Lerp(lightIntensityMax, 0f, elapsedTime / clipDuration);
            yield return new WaitForSeconds(0.01f);
        }

        if (light != null)
            light.intensity = 0f;
    }

    private static IEnumerator EmissionIntensityBehavior(Material instanceMaterial, float clipDuration, List<GameObject> particles)
    {
        if (instanceMaterial == null) yield break;

        Color baseEmissiveColor = Color.white;

        instanceMaterial.EnableKeyword("_EMISSION");
        instanceMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        instanceMaterial.SetColor("_EmissionColor", baseEmissiveColor * emmisionIntensityMax);

        float elapsedTime = 0f;
        while (elapsedTime < clipDuration)
        {
            elapsedTime += Time.deltaTime;
            float intensity = Mathf.Lerp(emmisionIntensityMax, 0f, elapsedTime / clipDuration);
            instanceMaterial.SetColor("_EmissionColor", baseEmissiveColor * intensity);
            yield return null;
        }

        instanceMaterial.SetColor("_EmissionColor", Color.black);

        if (particles == null) yield break;

        foreach (var particle in particles)
        {
            var partMat = particle.GetComponent<Renderer>().material;
            partMat.EnableKeyword("_EMISSION");
            partMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            partMat.SetColor("_EmissionColor", baseEmissiveColor * emmisionIntensityMax);
        }
    }

    private static IEnumerator ScaleChangeBehavior(Transform instanceTransform, float clipDuration)
    {
        if (instanceTransform == null) yield break;

        var defaultScale = instanceTransform.localScale;
        var maxScale = defaultScale * 1.5f;

        instanceTransform.localScale = maxScale;

        float elapsedTime = 0f;
        while (instanceTransform != null && elapsedTime < clipDuration)
        {
            elapsedTime += 0.01f;
            instanceTransform.localScale = Vector3.Lerp(maxScale, defaultScale, elapsedTime / clipDuration);
            yield return new WaitForSeconds(0.01f);
        }

        if (instanceTransform != null)
            instanceTransform.localScale = defaultScale;
    }

    private static float GetClipLengthFromMotion(Motion motion)
    {
        if (motion == null) return 0f;

        if (motion is AnimationClip clip)
            return clip.length;

        if (motion is BlendTree blendTree)
        {
            foreach (ChildMotion child in blendTree.children)
            {
                float length = GetClipLengthFromMotion(child.motion);
                if (length > 0f) return length;
            }
        }

        return 0f;
    }

    private static void CreateAndPlaySFX(Vector3 explosionPos)
    {
        var soundObj = new GameObject("ExplosionAudioPlayer");
        soundObj.transform.position = explosionPos;
        var audioSource = soundObj.AddComponent<AudioSource>();
        if (ExplosionAudioClip == null) ExplosionAudioClip = Resources.Load<AudioClip>(ExplosionPathSFX);
        audioSource.clip = ExplosionAudioClip;
        audioSource.Play();
        Object.Destroy(soundObj, audioSource.clip.length);
    }
}