using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ProjectUtilities
{
    private static readonly Regex WhitespaceRegex = new Regex(@"\s+");

    public static void DestroyChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.Destroy(parent.GetChild(i).gameObject);
    }

    public static void DestroyComponents<T>(IList<T> components) where T : Component
    {
        if (components == null) return;

        for (int i = components.Count - 1; i >= 0; i--)
        {
            if (components[i] != null)
                Object.Destroy(components[i].gameObject);
        }

        components.Clear();
    }

    public static List<T> ShuffledCopy<T>(IList<T> source)
    {
        var copy = source == null ? new List<T>() : new List<T>(source);

        for (int i = copy.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }

        return copy;
    }

    public static string NormalizeAnswer(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return WhitespaceRegex.Replace(value.Trim().ToLowerInvariant(), " ");
    }

    public static bool SameAnswer(string left, string right)
    {
        return string.Equals(NormalizeAnswer(left), NormalizeAnswer(right), StringComparison.Ordinal);
    }

    public static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
