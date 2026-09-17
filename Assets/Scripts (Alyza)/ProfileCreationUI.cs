
using UnityEngine;
using UnityEngine.UI;

public class ProfileCreationUI : MonoBehaviour
{
    [Header("UI References")]
    public InputField nameInput;
    public ChangeProfileImage changeProfileImage;
    public ProfileSelectUI profileSelectUI;

    // I-drag dito ang mismong Profile Select panel
    public GameObject profileSelectPanel;

    [Header("Duplicate Name Message")]
    public Text duplicateNameMessage;


    // OPEN CREATE ACCOUNT UI
    public void OpenCreationUI()
    {
        // Itago lamang ang Profile Select panel
        if (profileSelectPanel != null)
        {
            profileSelectPanel.SetActive(false);
        }

        // Ipakita ang Create Account UI
        gameObject.SetActive(true);

        // Itago ang duplicate message
        if (duplicateNameMessage != null)
        {
            duplicateNameMessage.text = "";
            duplicateNameMessage.gameObject.SetActive(false);
        }
    }


    // CREATE PROFILE BUTTON
    public void OnCreateClicked()
    {
        string playerName = nameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Please enter a profile name.");
            return;
        }


        // CHECK DUPLICATE NAME
        if (ProfileManager.Instance.ProfileNameExists(playerName))
        {
            Debug.LogWarning(
                "Profile name already exists. Please choose another name.");

            if (duplicateNameMessage != null)
            {
                duplicateNameMessage.text =
                    "Profile name already exists!";

                duplicateNameMessage.gameObject.SetActive(true);
            }

            return;
        }


        // GET SELECTED PROFILE IMAGE
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


        // ADD PROFILE
        bool profileCreated =
            ProfileManager.Instance.AddProfile(
                playerName,
                imagePath);


        // SAFETY CHECK
        if (!profileCreated)
        {
            if (duplicateNameMessage != null)
            {
                duplicateNameMessage.text =
                    "Profile creation failed!";

                duplicateNameMessage.gameObject.SetActive(true);
            }

            return;
        }


        // CLEAR INPUT FIELD
        nameInput.text = "";


        // HIDE CREATE ACCOUNT UI
        gameObject.SetActive(false);


        // SHOW PROFILE SELECT PANEL
        if (profileSelectPanel != null)
        {
            profileSelectPanel.SetActive(true);
        }


        // REFRESH PROFILE LIST
        if (profileSelectUI != null)
        {
            profileSelectUI.RefreshProfileList();
        }
    }


    // CANCEL BUTTON
    public void OnCancelClicked()
    {
        if (nameInput != null)
        {
            nameInput.text = "";
        }

        if (duplicateNameMessage != null)
        {
            duplicateNameMessage.text = "";
            duplicateNameMessage.gameObject.SetActive(false);
        }


        // HIDE CREATE ACCOUNT UI
        gameObject.SetActive(false);


        // SHOW PROFILE SELECT PANEL
        if (profileSelectPanel != null)
        {
            profileSelectPanel.SetActive(true);
        }
    }


    // CLOSE DUPLICATE MESSAGE
    public void CloseDuplicateMessage()
    {
        if (duplicateNameMessage != null)
        {
            duplicateNameMessage.text = "";
            duplicateNameMessage.gameObject.SetActive(false);
        }
    }
}