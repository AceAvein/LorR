using UnityEngine;
using UnityEngine.UI;

public class ProfileCreationUI : MonoBehaviour
{
    [Header("UI References")]
    public InputField nameInput;
    public ChangeProfileImage changeProfileImage;
    public ProfileSelectUI profileSelectUI;

    [Header("Duplicate Name Message")]
    public GameObject duplicateNameMessage;

    public void OnCreateClicked()
    {
        string playerName = nameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Please enter a profile name.");
            return;
        }

        if (ProfileManager.Instance.ProfileNameExists(playerName))
        {
            Debug.LogWarning(
                "Profile name already exists. Please choose another name.");

            if (duplicateNameMessage != null)
            {
                duplicateNameMessage.SetActive(true);
            }

            return;
        }

        string imagePath = "";

        if (changeProfileImage != null)
        {
            imagePath = changeProfileImage.SelectedImagePath;
        }

        Debug.Log(
            "changeProfileImage is null? " +
            (changeProfileImage == null));

        Debug.Log(
            "imagePath about to be saved: '" +
            imagePath + "'");

        bool profileCreated =
            ProfileManager.Instance.AddProfile(
                playerName,
                imagePath);

        // Safety check
        if (!profileCreated)
        {
            if (duplicateNameMessage != null)
            {
                duplicateNameMessage.SetActive(true);
            }

            return;
        }

        nameInput.text = "";

        gameObject.SetActive(false);

        if (profileSelectUI != null)
        {
            profileSelectUI.RefreshProfileList();
        }
    }

    public void OnCancelClicked()
    {
        nameInput.text = "";

        if (duplicateNameMessage != null)
        {
            duplicateNameMessage.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    public void CloseDuplicateMessage()
    {
        if (duplicateNameMessage != null)
        {
            duplicateNameMessage.SetActive(false);
        }
    }
}