using UnityEngine;
using UnityEngine.UI;

public class ProfileCreationUI : MonoBehaviour
{
    [Header("UI References")]
    public InputField nameInput;
    public ChangeProfileImage changeProfileImage;
    public ProfileSelectUI profileSelectUI;

    public void OnCreateClicked()
    {
        string playerName = nameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Please enter a profile name.");
            return;
        }

        string imagePath = "";

        if (changeProfileImage != null)
            imagePath = changeProfileImage.SelectedImagePath;

        Debug.Log("changeProfileImage is null? " + (changeProfileImage == null));
        Debug.Log("imagePath about to be saved: '" + imagePath + "'");

        ProfileManager.Instance.AddProfile(playerName, imagePath);

        nameInput.text = "";

        gameObject.SetActive(false);

        if (profileSelectUI != null)
            profileSelectUI.RefreshProfileList();
    }

    public void OnCancelClicked()
    {
        nameInput.text = "";

        gameObject.SetActive(false);
    }
}