using System.Dynamic;
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
        CreateInstance();
    }

    private void CreateInstance()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void OpenLoginPanel()
    {
        ClearUI();
        loginPanel.SetActive(true);
    }

    public void OpenRegistrationPanel()
    {
        ClearUI();
        registrationPanel.SetActive(true);
    }

    public void ShowVerificationResponce(bool isEmailSent, string emailId, string errorMessage)
    {
        ClearUI();
        emailVerificationPanel.SetActive(true);

        if (isEmailSent)
        {
            emailVerificationText.text = $"Пожалуйста, подтвердите свою почту \n Подтверждение было отправлено на почту {emailId}";
        }
        else
        {
            emailVerificationText.text = $"Невозможно отправить подтверждение : {errorMessage}";
        }
    }

    private void ClearUI()
    {
        loginPanel.SetActive(false);
        registrationPanel.SetActive(false);
        emailVerificationPanel.SetActive(false);
    }
}
