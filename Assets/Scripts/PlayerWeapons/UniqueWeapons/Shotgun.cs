using UnityEngine;

public class Shotgun : PlayerGun
{
    public override void Fire()
    {
        Vector3 spawnPos = playerPos.position;
        Vector3 baseDir = playerPos.forward;
        baseDir.y = 0f;
        baseDir.Normalize();

        for (int i = 0; i < 14; i++)
        {
            Vector3 dir = CalculateSpreadDirection(baseDir);
            Quaternion spawnRot = Quaternion.LookRotation(dir);

            Projectile.Spawn(mainProjectilePrefab, spawnPos, spawnRot, gameObject);
        }

        AfterFire();
    }

    private Vector3 CalculateSpreadDirection(Vector3 baseDir)
    {
        Vector2 randCircle = Random.insideUnitCircle * Mathf.Tan(bulletSpread * Mathf.Deg2Rad);

        Vector3 right = Vector3.Cross(baseDir, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(baseDir, Vector3.right).normalized;

        Vector3 up = Vector3.Cross(right, baseDir).normalized;

        return (baseDir + right * randCircle.x + up * randCircle.y).normalized;
    }

    public override string GetDescription()
    {
        return "-";
    }

    public override string GetGunName()
    {
        return "Shotgun 'Okami'";
    }
}