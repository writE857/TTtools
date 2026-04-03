using System;
using UnityEngine;

public static class AdeUIBindingUtility
{
    public static T FindInChildrenByName<T>(Transform root, params string[] names) where T : Component
    {
        if (root == null || names == null || names.Length == 0)
        {
            return null;
        }

        T[] components = root.GetComponentsInChildren<T>(true);
        foreach (string name in names)
        {
            foreach (T component in components)
            {
                if (string.Equals(component.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return component;
                }
            }
        }

        return null;
    }
}
