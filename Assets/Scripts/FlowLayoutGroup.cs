using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FlowLayoutGroup : LayoutGroup
{
    [Header("Flow Settings")]
    public float spacingX = 6f;
    public float spacingY = 8f;
    public bool centerRows = true; // центрировать каждую строку

    void OnEnable()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        SetLayoutInputForAxis(0f, rectTransform.rect.width, -1f, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        SetLayoutInputForAxis(0f, GetTotalHeight(), -1f, 1);
    }

    public override void SetLayoutHorizontal() => DoLayout();
    public override void SetLayoutVertical()   => DoLayout();

    void DoLayout()
    {
        float containerWidth = rectTransform.rect.width - padding.left - padding.right;
        if (containerWidth <= 0)
        {
            var p = transform.parent as RectTransform;
            if (p != null) containerWidth = p.rect.width - padding.left - padding.right;
        }
        if (containerWidth <= 0) return;

        // Разбиваем детей на строки
        var rows = new List<List<int>>();
        var currentRow = new List<int>();
        float x = 0f;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            float childW = LayoutUtility.GetPreferredWidth(rectChildren[i]);
            if (currentRow.Count > 0 && x + childW > containerWidth)
            {
                rows.Add(currentRow);
                currentRow = new List<int>();
                x = 0f;
            }
            currentRow.Add(i);
            x += childW + spacingX;
        }
        if (currentRow.Count > 0) rows.Add(currentRow);

        // Расставляем с центрированием
        float y = padding.top;
        foreach (var row in rows)
        {
            // Считаем ширину строки
            float rowWidth = 0f;
            foreach (int idx in row)
                rowWidth += LayoutUtility.GetPreferredWidth(rectChildren[idx]) + spacingX;
            rowWidth -= spacingX;

            float rowHeight = 0f;
            foreach (int idx in row)
                rowHeight = Mathf.Max(rowHeight, LayoutUtility.GetPreferredHeight(rectChildren[idx]));

            // Начальный X для центрирования
            float startX = centerRows
                ? padding.left + (containerWidth - rowWidth) / 2f
                : padding.left;

            float cx = startX;
            foreach (int idx in row)
            {
                var child = rectChildren[idx];
                float childW = LayoutUtility.GetPreferredWidth(child);
                float childH = LayoutUtility.GetPreferredHeight(child);
                // Вертикальное выравнивание по центру строки
                float childY = y + (rowHeight - childH) / 2f;
                SetChildAlongAxis(child, 0, cx, childW);
                SetChildAlongAxis(child, 1, childY, childH);
                cx += childW + spacingX;
            }

            y += rowHeight + spacingY;
        }
    }

    float GetTotalHeight()
    {
        float containerWidth = rectTransform.rect.width - padding.left - padding.right;
        if (containerWidth <= 0)
        {
            var p = transform.parent as RectTransform;
            if (p != null) containerWidth = p.rect.width - padding.left - padding.right;
        }
        if (containerWidth <= 0) return 0f;

        float x = 0f;
        float y = padding.top;
        float rowH = 0f;

        foreach (var child in rectChildren)
        {
            float childW = LayoutUtility.GetPreferredWidth(child);
            float childH = LayoutUtility.GetPreferredHeight(child);

            if (x > 0 && x + childW > containerWidth)
            {
                x = 0f;
                y += rowH + spacingY;
                rowH = 0f;
            }

            x += childW + spacingX;
            rowH = Mathf.Max(rowH, childH);
        }

        return y + rowH + padding.bottom;
    }
}