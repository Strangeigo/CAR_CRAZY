using UnityEngine;

public static class ExtensionMethods
{
    /// <summary>
    /// Remaps a value from one range to another.
    /// </summary>
    /// <param name="value">The value to remap</param>
    /// <param name="oldMin">The minimum of the old range</param>
    /// <param name="oldMax">The maximum of the old range</param>
    /// <param name="newMin">The minimum of the new range</param>
    /// <param name="newMax">The maximum of the new range</param>
    /// <returns>The remapped value</returns>
    public static float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        return (value - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
    }
}
