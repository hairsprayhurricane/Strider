using UnityEngine;

// Генератор случайных чисел в стиле Doom (1993/1996).
// Использует предопределенную таблицу из 256 случайных чисел для быстрой генерации.
// Адаптировано под Ludere: Talos.
public static class ClassicRandom
{
    private static readonly byte[] randomTable = new byte[256]
    {
        0, 8, 109, 220, 222, 241, 149, 107, 75, 248, 254, 140, 16, 66,
        74, 21, 211, 47, 80, 242, 154, 27, 205, 128, 161, 89, 77, 36,
        95, 110, 85, 48, 212, 140, 211, 249, 22, 79, 200, 50, 28, 188,
        52, 140, 202, 120, 68, 145, 62, 70, 184, 190, 91, 197, 152, 224,
        149, 104, 25, 178, 252, 182, 202, 182, 141, 197, 4, 81, 181, 242,
        145, 42, 39, 227, 156, 198, 225, 193, 219, 93, 122, 175, 249, 0,
        175, 143, 70, 239, 46, 246, 163, 53, 163, 109, 168, 135, 2, 235,
        25, 92, 20, 145, 138, 77, 69, 166, 78, 176, 173, 212, 166, 113,
        94, 161, 41, 50, 239, 49, 111, 164, 70, 60, 2, 37, 171, 75,
        136, 156, 11, 56, 42, 146, 138, 229, 73, 146, 77, 61, 98, 196,
        135, 106, 63, 197, 195, 86, 96, 203, 113, 101, 170, 247, 181, 113,
        80, 250, 108, 7, 255, 237, 129, 226, 79, 107, 112, 166, 103, 241,
        24, 223, 239, 120, 198, 58, 60, 82, 128, 3, 184, 66, 143, 224,
        145, 224, 81, 206, 163, 45, 63, 90, 168, 114, 59, 33, 159, 95,
        28, 139, 123, 98, 125, 196, 15, 70, 194, 253, 54, 14, 109, 226,
        71, 17, 161, 93, 186, 87, 244, 138, 20, 52, 123, 251, 26, 36,
        17, 46, 52, 231, 232, 76, 31, 221, 84, 37, 216, 165, 212, 106,
        197, 242, 98, 43, 39, 175, 254, 145, 190, 84, 118, 222, 187, 136,
        120, 163, 236, 249
    };

    private static int rndindex = 0;
    public static int GetIndex()
    {
        return rndindex;
    }
    public static void SetIndex(int indx)
    {
        rndindex = indx;
    }
    private static int P_Random()
    {
        rndindex = (rndindex + 1) & 0xFF;
        return randomTable[rndindex];
    }

    
    // Возвращает случайное число от -255 до 255.
    public static int M_Random()
    {
        return P_Random() - P_Random();
    }
    
    // Возвращает следующее случайное значение из таблицы (byte)
    private static byte NextByte()
    {
        rndindex = (rndindex + 1) & 0xFF; // Циклический индекс от 0 до 255
        return randomTable[rndindex];
    }


    
    // Работает с int и float перегрузками.
    public static int Range(int min, int max)
    {
        if (min >= max)
            return min;

        int range = max - min;
        int randomValue = P_Random();
        return min + (randomValue * range / 256);
    }
    public static float Range(float min, float max)
    {
        if (min >= max)
            return min;

        float range = max - min;
        float randomValue = P_Random() / 255f; // Нормализация к [0, 1]
        return min + (randomValue * range);
    }

    
    // Возвращает случайное short значение в диапазоне [0, max)
    public static short RangeShort(short max)
    {
        if (max <= 0)
        {
            Debug.LogError("max должен быть больше 0");
            return 0;
        }

        return (short)(NextByte() % max);
    }

    
    // Возвращает случайное short значение в диапазоне [min, max)
    public static short RangeShort(short min, short max)
    {
        if (min >= max)
        {
            Debug.LogError("min должен быть меньше max");
            return min;
        }

        int range = max - min;
        return (short)(min + (NextByte() % range));
    }

    
    // Сбрасывает индекс таблицы (для детерминированности).
    public static void SetSeed(int seed)
    {
        rndindex = seed & 0xFF;
    }

    
    // Возвращает случайное значение от 0.0 до 1.0 (аналог .value).
    public static float value
    {
        get { return P_Random() / 255f; }
    }

    // Возвращает случайное значение позиции в сфере (3D)
    public static Vector3 insideUnitSphere
    {
        get
        {
            Vector3 point;
            do
            {
                float x = Range(-1f, 1f);
                float y = Range(-1f, 1f);
                float z = Range(-1f, 1f);
                point = new Vector3(x, y, z);
            }
            while (point.sqrMagnitude > 1f);
            return point;
        }
    }
    // Возвращает случайное значение позиции в круге (2D)
    public static Vector2 insideUnitCircle
    {
        get
        {
            Vector2 point;
            do
            {
                float x = Range(-1f, 1f);
                float y = Range(-1f, 1f);
                point = new Vector2(x, y);
            }
            while (point.sqrMagnitude > 1f);
            return point;
        }
    }

}
