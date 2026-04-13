using UnityEngine;

public class Unmaker : PlayerGun
{
    public override void FireAction()
    {
        audioSource.Stop();
        //audioSource.PlayOneShot(shotSound);

        Vector3 spawnPos = playerPos.position;
        Vector3 baseDir = playerPos.forward;
        baseDir.y = 0f;
        baseDir.Normalize();

        float maxAngle = bulletSpread;
        float azimuth = ClassicRandom.Range(0f, 360f);
        float angle = ClassicRandom.Range(0f, maxAngle);

        Vector3 axis = baseDir;
        Vector3 ortho = Vector3.Cross(axis, Vector3.up);
        if (ortho.sqrMagnitude < 1e-6f)
            ortho = Vector3.Cross(axis, Vector3.right);

        ortho.Normalize();
        Vector3 ortho2 = Vector3.Cross(axis, ortho).normalized;

        Vector3 spreadAxis =
            ortho * Mathf.Cos(azimuth * Mathf.Deg2Rad) +
            ortho2 * Mathf.Sin(azimuth * Mathf.Deg2Rad);

        Vector3 offsetDir =
            (Quaternion.AngleAxis(angle, spreadAxis) * baseDir).normalized;

        Quaternion spawnRot = Quaternion.LookRotation(offsetDir);

        GameObject bulletObj = Instantiate(mainProjectilePrefab, spawnPos, spawnRot);
        //LeadProjectile bullet = bulletObj.GetComponent<LeadProjectile>();
        //bullet.damage = damage;

    }

    public override string GetDescription()
    {
        return "-";
    }

    public override string GetGunName()
    {
        throw new System.NotImplementedException();
    }
}