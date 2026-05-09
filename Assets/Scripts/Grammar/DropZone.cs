using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Feedback")]
    public Image background;
    public Color normalColor = new Color(0.9f, 0.9f, 0.95f, 1f);
    public Color hoverColor  = new Color(0.75f, 0.88f, 1f,   1f);

    void Start()
    {
        if (background) background.color = normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableWord chip = eventData.pointerDrag?.GetComponent<DraggableWord>();
        if (chip == null) return;
        
        int insertIndex = GetInsertIndex(eventData.position);

        chip.MoveToZone(transform, insertIndex);

        if (background) background.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging && background)
            background.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background) background.color = normalColor;
    }
    
    int GetInsertIndex(Vector2 screenPos)
    {
        int childCount = transform.childCount;
        if (childCount == 0) return 0;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null) continue;
            
            Vector2 childCenter = child.position;
            if (screenPos.x < childCenter.x)
                return i;
        }
        
        return childCount;
    }
}