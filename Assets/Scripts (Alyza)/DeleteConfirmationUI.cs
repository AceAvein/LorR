using UnityEngine;
using UnityEngine.UI;

public class DeleteConfirmationUI : MonoBehaviour
{
    public static DeleteConfirmationUI Instance;

    public GameObject fadeBackground;
    public GameObject window;

    public Button yesButton;
    public Button noButton;

    private System.Action confirmAction;

    private void Awake()
    {
        Instance = this;

        Hide();

        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClicked);
    }

    public void Show(System.Action action)
    {
        confirmAction = action;

        fadeBackground.SetActive(true);
        window.SetActive(true);
    }

    public void Hide()
    {
        fadeBackground.SetActive(false);
        window.SetActive(false);
    }

    void OnYesClicked()
    {
        Hide();

        confirmAction?.Invoke();
    }

    void OnNoClicked()
    {
        Hide();
    }
}