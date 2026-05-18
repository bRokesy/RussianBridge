using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private GameObject emailVerificationPanel;
    [SerializeField] private TextMeshProUGUI emailVerificationText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OpenLoginPanel()
    {
        ClearUI();
        loginPanel?.SetActive(true);
    }

    public void OpenRegistrationPanel()
    {
        ClearUI();
        registrationPanel?.SetActive(true);
    }

    public void ShowVerificationResponce(bool isEmailSent, string emailId, string errorMessage)
    {
        ClearUI();
        emailVerificationPanel?.SetActive(true);

        if (emailVerificationText == null)
            return;

        emailVerificationText.text = isEmailSent
            ? $"Пожалуйста, подтвердите свою почту\nПодтверждение было отправлено на {emailId}"
            : $"Невозможно отправить подтверждение: {errorMessage}";
    }

    private void ClearUI()
    {
        loginPanel?.SetActive(false);
        registrationPanel?.SetActive(false);
        emailVerificationPanel?.SetActive(false);
    }
}
