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

        /*gunController.AddWeapon(playerPistolPrefab, 1);
        gunController.AddWeapon(shotgunPrefab, 2);
        gunController.AddWeapon(machineGunPrefab, 3);
        gunController.AddWeapon(rocketLauncherPrefab, 4);
        
        gunController.AddWeapon(plasmaGunPrefab, 5);
        gunController.AddWeapon(bfgPrefab, 6);
        gunController.AddWeapon(unmakerPrefab, 7);
        */
        gunController.AddWeapon(thunderBolt, 1);
        gunController.AddWeapon(fireThrower, 2);
    }
}
