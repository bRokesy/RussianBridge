using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FlowLayoutGroup : LayoutGroup
{
    [Header("Flow Settings")]
    public float spacingX = 6f;
    public float spacingY = 8f;
    public bool centerRows = true;

    private void OnEnable()
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

    public override void SetLayoutHorizontal()
    {
        DoLayout();
    }

    public override void SetLayoutVertical()
    {
        DoLayout();
    }

    private void DoLayout()
    {
        float containerWidth = GetContainerWidth();
        if (containerWidth <= 0f)
            return;

        List<List<int>> rows = BuildRows(containerWidth);

        float y = padding.top;
        foreach (List<int> row in rows)
        {
            float rowWidth = GetRowWidth(row);
            float rowHeight = GetRowHeight(row);
            float x = GetStartX(containerWidth, rowWidth);

            foreach (int index in row)
            {
                RectTransform child = rectChildren[index];
                float childWidth = GetPreferredWidth(child);
                float childHeight = GetPreferredHeight(child);
                float childY = y + (rowHeight - childHeight) / 2f;

                SetChildAlongAxis(child, 0, x, childWidth);
                SetChildAlongAxis(child, 1, childY, childHeight);

                x += childWidth + spacingX;
            }

            y += rowHeight + spacingY;
        }
    }

    private List<List<int>> BuildRows(float containerWidth)
    {
        var rows = new List<List<int>>();
        var currentRow = new List<int>();
        float rowWidth = 0f;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            float childWidth = GetPreferredWidth(rectChildren[i]);

            if (currentRow.Count > 0 && rowWidth + childWidth > containerWidth)
            {
                rows.Add(currentRow);
                currentRow = new List<int>();
                rowWidth = 0f;
            }

            currentRow.Add(i);
            rowWidth += childWidth + spacingX;
        }

        if (currentRow.Count > 0)
            rows.Add(currentRow);

        return rows;
    }

    private float GetTotalHeight()
    {
        float containerWidth = GetContainerWidth();
        if (containerWidth <= 0f)
            return 0f;

        float rowWidth = 0f;
        float rowHeight = 0f;
        float totalHeight = padding.top;

        foreach (RectTransform child in rectChildren)
        {
            float childWidth = GetPreferredWidth(child);
            float childHeight = GetPreferredHeight(child);

            if (rowWidth > 0f && rowWidth + childWidth > containerWidth)
            {
                rowWidth = 0f;
                totalHeight += rowHeight + spacingY;
                rowHeight = 0f;
            }

            rowWidth += childWidth + spacingX;
            rowHeight = Mathf.Max(rowHeight, childHeight);
        }

        return totalHeight + rowHeight + padding.bottom;
    }

    private float GetContainerWidth()
    {
        float width = rectTransform.rect.width - padding.left - padding.right;
        if (width > 0f)
            return width;

        RectTransform parent = transform.parent as RectTransform;
        return parent != null
            ? parent.rect.width - padding.left - padding.right
            : 0f;
    }

    private float GetStartX(float containerWidth, float rowWidth)
    {
        return centerRows
            ? padding.left + (containerWidth - rowWidth) / 2f
            : padding.left;
    }

    private float GetRowWidth(List<int> row)
    {
        float width = 0f;

        foreach (int index in row)
            width += GetPreferredWidth(rectChildren[index]) + spacingX;

        return Mathf.Max(0f, width - spacingX);
    }

    private float GetRowHeight(List<int> row)
    {
        float height = 0f;

        foreach (int index in row)
            height = Mathf.Max(height, GetPreferredHeight(rectChildren[index]));

        return height;
    }

    private static float GetPreferredWidth(RectTransform child)
    {
        return LayoutUtility.GetPreferredWidth(child);
    }

    private static float GetPreferredHeight(RectTransform child)
    {
        return LayoutUtility.GetPreferredHeight(child);
    }
}
