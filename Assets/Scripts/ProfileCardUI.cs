using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ProfileCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image handIcon;
    public Text playerName;
    public Button deleteButton;

    private UserProfile profile;
    private ProfileSelectUI profileSelectUI;

    public void Setup(UserProfile newProfile, ProfileSelectUI ui)
    {
        profile = newProfile;
        profileSelectUI = ui;

        // Display Name
        playerName.text = profile.name;

        Debug.Log("Image Path: " + profile.imagePath);

        // Display Image
        if (!string.IsNullOrEmpty(profile.imagePath) && File.Exists(profile.imagePath))
        {
            byte[] bytes = File.ReadAllBytes(profile.imagePath);

            Texture2D texture = new Texture2D(2, 2);

            texture.LoadImage(bytes);

            handIcon.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        // Select Profile
        Button cardButton = GetComponent<Button>();

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() =>
        {
            profileSelectUI.OnProfileSelected(profile);
        });

        // Delete Button
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(DeleteProfile);
        }
    }

    void DeleteProfile()
    {
        DeleteConfirmationUI.Instance.Show(() =>
        {
            ProfileManager.Instance.DeleteProfile(profile);

            profileSelectUI.RefreshProfileList();
        });
    }
}