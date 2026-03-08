using UnityEngine;

public class Shotgun : PlayerGun
{
    public override void Fire()
    {
        Vector3 spawnPos = playerPos.position;
        Vector3 baseDir = playerPos.forward;
        baseDir.y = 0f;
        baseDir.Normalize();

        for (int i = 0; i < 7; i++)
        {
            GameObject bulletObj = Instantiate(mainProjectilePrefab, spawnPos, Quaternion.identity);

            float maxAngle = bulletSpread;
            float azimuth = Random.Range(0f, 360f);
            float angle = Random.Range(0f, maxAngle);

            Vector3 axis = baseDir;
            Vector3 ortho = Vector3.Cross(axis, Vector3.up);
            if (ortho.sqrMagnitude < 1e-6f) ortho = Vector3.Cross(axis, Vector3.right);
            ortho.Normalize();
            Vector3 ortho2 = Vector3.Cross(axis, ortho).normalized;
            Vector3 dir = (Quaternion.AngleAxis(angle, ortho * Mathf.Cos(azimuth * Mathf.Deg2Rad) + ortho2 * Mathf.Sin(azimuth * Mathf.Deg2Rad)) * baseDir).normalized;
            SetDirection(dir, bulletObj);   
        }
        
        
        AfterFire();
    }

    private void SetDirection(Vector3 initialDirection, GameObject bulletObj)
    {
        var direction = initialDirection.normalized;
        direction = CalculateRandomDirection(direction);
        bulletObj.transform.rotation = Quaternion.LookRotation(direction);
    }

    private Vector3 CalculateRandomDirection(Vector3 direction)
    {
        Vector2 randCircle = Random.insideUnitCircle * Mathf.Tan(bulletSpread * Mathf.Deg2Rad);

        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(direction, Vector3.right).normalized;
        Vector3 up = Vector3.Cross(right, direction).normalized;

        return (direction + right * randCircle.x + up * randCircle.y).normalized;
    }
}