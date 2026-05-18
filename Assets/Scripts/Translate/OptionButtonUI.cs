using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionButtonUI : MonoBehaviour
{
    private static readonly Color CorrectColor = new Color(0.6f, 1f, 0.6f);
    private static readonly Color WrongColor = new Color(1f, 0.6f, 0.6f);

    private Button button;
    private Image background;
    private TextMeshProUGUI label;
    private Action<OptionButtonUI> onClick;
    private Color defaultColor;

    public string Value { get; private set; }

    private void Awake()
    {
        button = GetComponent<Button>();
        background = GetComponent<Image>();
        label = GetComponentInChildren<TextMeshProUGUI>(true);

        if (background != null)
            defaultColor = background.color;

        button?.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        button?.onClick.RemoveListener(OnButtonClicked);
    }

    public void Setup(string value, Action<OptionButtonUI> clickCallback)
    {
        Value = value;
        onClick = clickCallback;

        ProjectUtilities.SetText(label, value);
        ResetColor();
        SetInteractable(true);
    }

    public void SetCorrect()
    {
        SetBackgroundColor(CorrectColor);
    }

    public void SetWrong()
    {
        SetBackgroundColor(WrongColor);
    }

    public void ResetColor()
    {
        SetBackgroundColor(defaultColor);
    }

    public void SetInteractable(bool state)
    {
        if (button != null)
            button.interactable = state;
    }

    private void OnButtonClicked()
    {
        onClick?.Invoke(this);
    }

    private void SetBackgroundColor(Color color)
    {
        if (background != null)
            background.color = color;
    }
}
