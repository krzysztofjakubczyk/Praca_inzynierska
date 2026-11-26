using UnityEngine;

/// <summary>
/// Strefa skrzyżowania wyznaczana automatycznie z linii zatrzymania (triggerów LightMonitorForCar):
/// prostokąt między wewnętrznymi krawędziami linii z czterech stron. Używana przez zasadę
/// "nie blokuj skrzyżowania" (pojazd wjeżdża tylko, gdy za strefą jest dla niego miejsce).
/// </summary>
public static class IntersectionZone
{
    private static bool computed;
    private static bool valid;
    private static Rect zone;
    private static int sceneHandle = -1;

    public static bool TryGet(out Rect rect)
    {
        int handle = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (!computed || handle != sceneHandle)
        {
            sceneHandle = handle;
            computed = true;
            valid = Compute(out zone);
            if (valid) Debug.Log($"[Ruch] Strefa skrzyżowania: x {zone.xMin:F1}..{zone.xMax:F1}, z {zone.yMin:F1}..{zone.yMax:F1}");
        }
        rect = zone;
        return valid;
    }

    private static bool Compute(out Rect rect)
    {
        rect = default;
        LightMonitorForCar[] monitors = Object.FindObjectsOfType<LightMonitorForCar>();
        if (monitors.Length < 2) return false;

        Vector3 center = Vector3.zero;
        foreach (var m in monitors) center += m.transform.position;
        center /= monitors.Length;

        float xMin = float.NegativeInfinity, xMax = float.PositiveInfinity;
        float zMin = float.NegativeInfinity, zMax = float.PositiveInfinity;
        bool any = false;
        foreach (var m in monitors)
        {
            Collider c = m.GetComponent<Collider>();
            if (c == null) continue;
            Bounds b = c.bounds;
            Vector3 offset = b.center - center;
            any = true;
            if (Mathf.Abs(offset.x) > Mathf.Abs(offset.z))
            {
                if (offset.x < 0) xMin = Mathf.Max(xMin, b.max.x); // linia z zachodu
                else xMax = Mathf.Min(xMax, b.min.x);              // linia ze wschodu
            }
            else
            {
                if (offset.z < 0) zMin = Mathf.Max(zMin, b.max.z); // linia z południa
                else zMax = Mathf.Min(zMax, b.min.z);              // linia z północy
            }
        }
        if (!any) return false;

        // Brakująca strona (np. skrzyżowanie typu T): przyjmij 15 m od środka.
        if (float.IsNegativeInfinity(xMin)) xMin = center.x - 15f;
        if (float.IsPositiveInfinity(xMax)) xMax = center.x + 15f;
        if (float.IsNegativeInfinity(zMin)) zMin = center.z - 15f;
        if (float.IsPositiveInfinity(zMax)) zMax = center.z + 15f;
        if (xMax <= xMin || zMax <= zMin) return false;

        rect = Rect.MinMaxRect(xMin, zMin, xMax, zMax);
        return true;
    }
}
