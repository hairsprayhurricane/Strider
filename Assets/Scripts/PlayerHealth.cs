using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public PlayerController playerController;
    public const short maxHealth = 100;
    public short health;
    public short maxArmor;
    public short armor;
    public byte maxHealthInjectors;
    public byte healthInjectors;
    public byte injectorLeftValue;
    public AudioSource audioSource;
    public AudioClip deathSound;
    public AudioClip deathNoise;
//    private DamageDirectionIndicator indicator;
    public GameObject synthPuddle;
    public bool isDead;
    public bool isGod;
    public bool isUsingInjector;

    public enum LastDamageType
    {
        normal = 0,
        explosion = 1
    }

    void Start()
    {
        health = maxHealth;
        armor = 0;
        //CanvasManager.Instance.UpdateHealth(health, false);
        //CanvasManager.Instance.UpdateArmor(armor, false);
        //CanvasManager.Instance.UpdateHealthInjectors(healthInjectors, maxHealthInjectors, false);
        isDead = false;
        //indicator = CanvasManager.Instance.damageDirectionIndicator;
        StartCoroutine(HealPlayer());
    }

    public IEnumerator HealPlayer()
    {
        while (!isDead)
        {
            if(health < maxHealth)
            {
                health += 3;
                if (health > maxHealth) health = maxHealth;
                var currentHealth = Mathf.Clamp(health, 0, 100);

                ShaderVolumeManager.Instance.SetColorAdjustmentColorByValue(currentHealth);
                ShaderVolumeManager.Instance.SetFilmGrainByValue(currentHealth);
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void DamagePlayer(short damage, Transform otherTransform = default, LastDamageType lastDamageType = 0)
    {

        if (lastDamageType == LastDamageType.explosion)
        {
            StartCoroutine(ShaderVolumeManager.Instance.DramaticallyIncreaseBleedingColors(damage));
            //StartCoroutine(ShaderVolumeManager.Instance.DramaticallyIncreaseChromaticAberration());
            StartCoroutine(ShaderVolumeManager.Instance.DramaticallyChangeColorAdjustments(Color.red, 1.5f));
            StartCoroutine(ShaderVolumeManager.Instance.DramaticallyIncreaseDistortion(damage));

        }

        if (armor <= 0)
        {
            if (!isGod)
            {
                health -= damage;
            }
        }
        else
        {
            if (!isGod)
            {
                if (armor >= damage)
                {
                    armor -= damage;
                }
                else
                {
                    short remainingDamage;
                    remainingDamage = (short)(damage - armor);
                    armor = 0;
                    if (!isGod)
                    {
                        health -= remainingDamage;
                    }
                }
            }
        }
        if (health <= 0 && !isDead)
        {
            Kill(lastDamageType);
        }

        //CanvasManager.Instance.UpdateHealth(health, false);
        //CanvasManager.Instance.UpdateArmor(armor, false);

        //indicator.OnPlayerDamaged(otherTransform);

        ShaderVolumeManager.Instance.SetColorAdjustmentColorByValue(health);
        ShaderVolumeManager.Instance.SetFilmGrainByValue(health);

        playerController.ShakeCamera(magnitude:1);
    }

    public void DamagePlayerThroughArmor(short damage)
    {
        if (!isGod)
        {
            health -= damage;
        }
        if (health <= 0 && !isDead)
        {
            Kill();
        }
        //CanvasManager.Instance.UpdateHealth(health, false);
        //CanvasManager.Instance.UpdateArmor(armor, false);
    }

    public void UseInjector(byte amount = 50, float effectForce = 5f)
    {
        healthInjectors -= 1;
        //CanvasManager.Instance.UpdateHealthInjectors(healthInjectors, maxHealthInjectors);
        StartCoroutine(HealPlayerForTime(amount));
        StartCoroutine(ShaderVolumeManager.Instance.DramaticallyIncreaseGain(effectForce));
        StartCoroutine(ShaderVolumeManager.Instance.DramaticallyIncreaseChromaticAberration(effectForce));
    }

    public IEnumerator HealPlayerForTime(byte amount, float speed = 0.07f)
    {
        amount += 1;
        isUsingInjector = true;
        injectorLeftValue = amount;
        while (health != maxHealth)
        {
            health++;
            amount--;
            injectorLeftValue--;
            if (amount == 0)
            {
                break;
            }
            //CanvasManager.Instance.UpdateHealth(health, true);
            yield return new WaitForSeconds(speed);
        }
        isUsingInjector = false;
    }

    public void Kill(LastDamageType lastDamageType = 0)
    {
        Debug.Log("Dead");
        audioSource.clip = deathNoise;
        audioSource.loop = true;
        audioSource.Play();
        audioSource.PlayOneShot(deathSound);

        EnemyManager.DestroyAllAI();
        playerController.OnDeath();
        isDead = true;
        GunController.Instance.DeactivateAll();
        StartCoroutine(ShaderVolumeManager.Instance.DramaticallyIncreaseAndStopDistortion());
        StartCoroutine(WaitForRestart());

    }

    IEnumerator WaitForRestart()
    {
        while(isDead)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            yield return null;
        }
    }

    /*public void GiveHealth(byte amount, ItemPickup pickup)
    {
        if (health < maxHealth)
        {
            health += amount;
            pickup.PlaySound();
            Destroy(pickup);
            PickupEffect.resetEffect();
            
            LogInterface.AddNewRecord("Picked " + amount + " Synthetic Blood");
        }
        else
        {
            if (healthInjectors < maxHealthInjectors)
            {
                healthInjectors += 1;
                //CanvasManager.Instance.UpdateHealthInjectors(healthInjectors, maxHealthInjectors);
                pickup.PlaySound();
                Destroy(pickup.gameObject);
                PickupEffect.resetEffect();
                
                LogInterface.AddNewRecord("Blood Pack added to your Injectors");
            }
        }
        if (health > maxHealth)
        {
            health = maxHealth;
        }
        //CanvasManager.Instance.UpdateHealth(health);
        //CanvasManager.Instance.UpdateArmor(armor, false);
    }
    public void GiveArmor(byte amount, ItemPickup pickup)
    {
        if (armor < maxArmor)
        {
            armor += amount;
            pickup.PlaySound();
            Destroy(pickup.gameObject);
            PickupEffect.resetEffect();
            
            LogInterface.AddNewRecord("Picked " + amount + " Armor Restore Tools");

        }
        if (armor > maxArmor)
        {
            armor = maxArmor;
        }
        //CanvasManager.Instance.UpdateHealth(health, false);
        //CanvasManager.Instance.UpdateArmor(armor);
    }
    public void GiveHealth(byte amount)
    {
        if (health < maxHealth)
        {
            health += amount;
            LogInterface.AddNewRecord("Picked " + amount + " Synthetic Blood");
        }
        if (health > maxHealth)
        {
            health = maxHealth;
        }
        //CanvasManager.Instance.UpdateHealth(health);
        //CanvasManager.Instance.UpdateArmor(armor, false);
    }
    public void GiveArmor(byte amount)
    {
        if (armor < maxArmor)
        {
            armor += amount;
            LogInterface.AddNewRecord("Picked " + amount + " Armor Restore Tools");
        }
        if (armor > maxArmor)
        {
            armor = maxArmor;
        }
        //CanvasManager.Instance.UpdateHealth(health, false);
        //CanvasManager.Instance.UpdateArmor(armor);
    }

    public void GiveHealthWithoutCheck(byte amount)
    {
        health += amount;
        LogInterface.AddNewRecord("Picked " + amount + " Synthetic Blood");
        //CanvasManager.Instance.UpdateHealth(health);
        //CanvasManager.Instance.UpdateArmor(armor, false);
    }
    public void GiveArmorWithoutCheck(byte amount)
    {
        armor += amount;
        LogInterface.AddNewRecord("Picked " + amount + " Armor Restore Tools");
        //CanvasManager.Instance.UpdateHealth(health);
        //CanvasManager.Instance.UpdateArmor(armor, false);
    }

    */

    /*public void SpawnSynthPuddle(bool desize = false)
    {
        if (synthPuddle == null)
        {
            return;
        }

        GameObject bloodPuddle = Instantiate(synthPuddle, transform.position, synthPuddle.transform.rotation);
        Counters.enemyBloodSpilled++;
        if (desize)
        {
            bloodPuddle.GetComponent<DecalProjector>().size /= 4f;
        }
    }


    private void EnableDeadPose(LastDamageType reason = 0)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y - 3f, transform.position.z);
        GetComponent<CharacterController>().enabled = false;
        GetComponentInChildren<Animator>().SetBool("isWalking", false);
        GetComponentInChildren<Animator>().enabled = false;
        UIWeaponSway.isEnabled = false;
        //EnemyManager.DisableAllAI();
        PlayerController.isSaveActive = false;
        LogInterface.ClearRecords();
        LogInterface.maxWaitTime = 999;
        EnableDeadPoseVisuals(reason);
        StartCoroutine(SideEffects());
        GunController.Instance.TurnOffVolume();
        GunController.Instance.StopAllPlayerWeaponCoroutines();
    }

    private void EnableDeadPoseVisuals(LastDamageType reason = 0)
    {
        BarText.Down.ChangeText($"PRESS {SettingsParams.Kick.ToUpper()} FOR RESTART");
        LogInterface.AddNewRecord("CRITICAL MISTAKE OCCURRED");
        LogInterface.AddNewRecord("FURTHER CONTINUATION IS IMPOSSIBLE");
        LogInterface.AddNewRecord("SYSTEM WILL BE TERMINATED MOMENTARILY...");

        if (reason == LastDamageType.normal)
        {
            ShaderVolumeManager.Instance.ChangeColorAdjustmentColorFilter(new Color(r: 0.5f, g: 0, b: 0));
        }
        else if (reason == LastDamageType.explosion)
        {
            //CanvasManager.Instance.StopAllCoroutines();
            ShaderVolumeManager.Instance.ChangeColorAdjustmentColorFilter(new Color(r: 1f, g: 0, b: 0));
            StartCoroutine(ShaderVolumeManager.Instance.DistortionRandomizeOverTime(999));
            StartCoroutine(ShaderVolumeManager.Instance.ChromaticAberrationRandomizeOverTime(999));
        }

        SpawnSynthPuddle();
        
        _ = EnemyManager.DestroyAllAIAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError(t.Exception);
            }
        }, System.Threading.Tasks.TaskScheduler.Default);
    }
    
    private IEnumerator SideEffects()
    {
        while (true)
        {
            BugsStringController.SpawnHorizontalString(7);
            BugsStringController.SpawnVerticalString(7);
            yield return new WaitForEndOfFrame();
        }
    }*/
}
