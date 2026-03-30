using UnityEngine;

public class GunData : MonoBehaviour
{
    public PlayerPistol playerPistolPrefab;
    public Shotgun shotgunPrefab;
    public MachineGun machineGunPrefab;
    public RocketLauncher rocketLauncherPrefab;
    public PlasmaGun plasmaGunPrefab;
    public BFG bfgPrefab;
    public Unmaker unmakerPrefab;
    
    public Thunderbolt thunderBolt;
    public FireThrower fireThrower;

    GunController gunController;

    void Start()
    {
        gunController = GunController.Instance;

        /*
        */
        gunController.AddWeapon(playerPistolPrefab);
        gunController.AddWeapon(shotgunPrefab);
        gunController.AddWeapon(machineGunPrefab);
        gunController.AddWeapon(rocketLauncherPrefab);
        
        /*
        gunController.AddWeapon(plasmaGunPrefab, 5);
        gunController.AddWeapon(bfgPrefab, 6);
        gunController.AddWeapon(unmakerPrefab, 7);
        */

        /*
        gunController.AddWeapon(thunderBolt);
        gunController.AddWeapon(fireThrower);
        */
    }
}
