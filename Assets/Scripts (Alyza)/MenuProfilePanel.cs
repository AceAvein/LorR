using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuProfilePanel : MonoBehaviour
{
    static readonly string[] AvatarNames =
    {
        "Bunny", "Sheep", "Cat", "Penguin", "Bear", "Fox"
    };

    static readonly Color Ink = new Color(0.08f, 0.08f, 0.08f, 1f);
    static readonly Color SelectedPlate = new Color(0.22f, 0.78f, 0.36f, 1f);
    static readonly Color IdlePlate = new Color(0.9f, 0.94f, 0.98f, 1f);

    GameObject mainMenu;
    Button profileButton;
    TMP_Text profileLabel;
    Transform content;
    Transform cardTemplate;
    GameObject listView;
    GameObject createView;
    GameObject confirmRoot;
    GameObject backButton;
    GameObject emptyText;
    TMP_Text statusText;
    TMP_Text confirmText;
    TMP_Text errorText;
    TMP_Text avatarNameText;
    TMP_InputField nameInput;
    Image leftPlate;
    Image rightPlate;
    Button[] avatarButtons;
    Image[] avatarFaces;
    Sprite[] avatars;

    readonly List<GameObject> spawned = new List<GameObject>();
    int selectedAvatar;
    bool nonDominantIsLeft = true;
    bool ready;

    void Awake()
    {
        Cache();
        Wire();
        EnsureManager();
        if (ProfileManager.Instance != null)
            ShowCurrentOnButton();
        ready = true;
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (!ready)
            return;

        ShowList();
    }

    public void Open()
    {
        EnsureManager();
        if (mainMenu != null)
            mainMenu.SetActive(false);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    void Cache()
    {
        listView = FindDeep("List").gameObject;
        createView = FindDeep("Create").gameObject;
        confirmRoot = FindDeep("Confirm").gameObject;
        backButton = FindDeep("Back").gameObject;
        emptyText = FindDeep("Empty").gameObject;
        statusText = FindDeep("Status").GetComponent<TMP_Text>();
        confirmText = FindDeep("Question").GetComponent<TMP_Text>();
        errorText = FindDeep("Error").GetComponent<TMP_Text>();
        avatarNameText = FindDeep("AvatarName").GetComponent<TMP_Text>();
        content = FindDeep("Content");
        cardTemplate = FindDeep("CardTemplate");

        leftPlate = FindDeep("Left").GetComponent<Image>();
        rightPlate = FindDeep("Right").GetComponent<Image>();

        avatarButtons = new Button[4];
        avatarFaces = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            Transform slot = FindDeep("Avatar" + i);
            avatarButtons[i] = slot.GetComponent<Button>();
            avatarFaces[i] = slot.Find("Face").GetComponent<Image>();
        }

        Transform library = FindDeep("AvatarLibrary");
        avatars = new Sprite[library.childCount];
        for (int i = 0; i < library.childCount; i++)
            avatars[i] = library.GetChild(i).GetComponent<Image>().sprite;

        nameInput = BuildNameField(FindDeep("NameField"));

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name != "Profile")
                continue;

            profileButton = buttons[i];
            profileLabel = profileButton.GetComponentInChildren<TMP_Text>();
            mainMenu = profileButton.transform.root.gameObject;
            break;
        }
    }

    void Wire()
    {
        Hook("Close", Close);
        Hook("Back", ShowList);
        Hook("Add", ShowCreate);
        Hook("Prev", () => NudgeAvatar(-1));
        Hook("Next", () => NudgeAvatar(1));
        Hook("Left", () => SetHand(true));
        Hook("Right", () => SetHand(false));
        Hook("Ok", OnCreate);
        Hook("Yes", ConfirmDelete);
        Hook("No", () => confirmRoot.SetActive(false));

        if (profileButton != null)
            profileButton.onClick.AddListener(Open);

        if (profileLabel != null)
        {
            profileLabel.enableAutoSizing = true;
            profileLabel.fontSizeMin = 20f;
            profileLabel.fontSizeMax = 36f;
            profileLabel.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    void Hook(string objectName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindDeep(objectName).GetComponent<Button>();
        if (button.targetGraphic == null)
            button.targetGraphic = button.GetComponent<Image>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    TMP_InputField BuildNameField(Transform field)
    {
        TMP_InputField input = field.GetComponent<TMP_InputField>();
        if (input == null)
            input = field.gameObject.AddComponent<TMP_InputField>();

        TMP_Text text = FindChild(field, "NameText").GetComponent<TMP_Text>();
        TMP_Text placeholder = FindChild(field, "NamePlaceholder").GetComponent<TMP_Text>();
        Transform area = FindChild(field, "TextArea");
        input.textViewport = area as RectTransform;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.targetGraphic = field.GetComponent<Image>();
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 16;
        input.caretWidth = 3;
        input.customCaretColor = true;
        input.caretColor = Ink;
        input.onValueChanged.RemoveAllListeners();
        input.onValueChanged.AddListener(_ =>
        {
            if (errorText != null)
                errorText.text = "";
        });

        VrNameKeyboard keyboard = field.GetComponent<VrNameKeyboard>();
        if (keyboard == null)
            keyboard = field.gameObject.AddComponent<VrNameKeyboard>();
        keyboard.field = input;
        return input;
    }

    void ShowList()
    {
        createView.SetActive(false);
        confirmRoot.SetActive(false);
        backButton.SetActive(false);
        listView.SetActive(true);
        RefreshList();
        ShowCurrentOnButton();
    }

    void ShowCreate()
    {
        listView.SetActive(false);
        confirmRoot.SetActive(false);
        createView.SetActive(true);
        backButton.SetActive(true);
        selectedAvatar = 0;
        nameInput.text = "";
        errorText.text = "";
        SetHand(true);
        RefreshAvatars();
    }

    void RefreshList()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            if (child == cardTemplate)
                continue;
            Destroy(child.gameObject);
        }
        spawned.Clear();

        if (ProfileManager.Instance == null)
            return;

        List<UserProfile> profiles = ProfileManager.Instance.Profiles;
        emptyText.SetActive(profiles.Count == 0);

        UserProfile current = ProfileManager.Instance.CurrentProfile;
        statusText.text = current == null ? "CHOOSE A PLAYER" : "PLAYING AS " + current.name.ToUpper();

        for (int i = 0; i < profiles.Count; i++)
        {
            GameObject card = Instantiate(cardTemplate.gameObject, content);
            card.name = "Card";
            card.SetActive(true);
            spawned.Add(card);
            FillCard(card.transform, profiles[i], current == profiles[i]);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    }

    void FillCard(Transform card, UserProfile profile, bool selected)
    {
        Image plate = card.GetComponent<Image>();
        if (plate != null)
            plate.color = selected ? new Color(0.8f, 0.93f, 1f, 1f) : Color.white;

        Image face = card.Find("CardFace").GetComponent<Image>();
        face.sprite = Portrait(profile);
        face.preserveAspect = true;

        card.Find("CardName").GetComponent<TMP_Text>().text = profile.name;
        string hand = profile.nonDominantIsLeft ? "Left" : "Right";
        card.Find("CardHand").GetComponent<TMP_Text>().text = "Non-dominant: " + hand;

        Button select = card.GetComponent<Button>();
        if (select.targetGraphic == null)
            select.targetGraphic = plate;
        select.onClick.RemoveAllListeners();
        select.onClick.AddListener(() => SelectProfile(profile));

        Button delete = card.Find("CardDelete").GetComponent<Button>();
        delete.onClick.RemoveAllListeners();
        delete.onClick.AddListener(() => AskDelete(profile));
    }

    void RefreshAvatars()
    {
        int count = avatars.Length;
        int start = Mathf.Clamp(selectedAvatar - 1, 0, Mathf.Max(0, count - 4));
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = Mathf.Min(start + i, count - 1);
            avatarFaces[i].sprite = avatars[index];
            bool selected = index == selectedAvatar;
            avatarButtons[i].GetComponent<Image>().color = selected ? SelectedPlate : IdlePlate;
            avatarButtons[i].transform.localScale = Vector3.one;
            avatarButtons[i].onClick.RemoveAllListeners();
            int captured = index;
            avatarButtons[i].onClick.AddListener(() =>
            {
                selectedAvatar = captured;
                RefreshAvatars();
            });
        }

        avatarNameText.text = AvatarNames[Mathf.Clamp(selectedAvatar, 0, AvatarNames.Length - 1)].ToUpper();
    }

    void NudgeAvatar(int direction)
    {
        int count = Mathf.Max(1, avatars.Length);
        selectedAvatar = (selectedAvatar + direction + count) % count;
        RefreshAvatars();
    }

    void SetHand(bool left)
    {
        nonDominantIsLeft = left;
        if (leftPlate != null)
            leftPlate.color = left ? SelectedPlate : IdlePlate;
        if (rightPlate != null)
            rightPlate.color = left ? IdlePlate : SelectedPlate;
        SetMark(leftPlate, left);
        SetMark(rightPlate, !left);
    }

    static void SetMark(Image plate, bool on)
    {
        if (plate == null)
            return;

        Transform mark = plate.transform.Find("Check");
        if (mark != null)
            mark.gameObject.SetActive(on);
    }

    void OnCreate()
    {
        string playerName = nameInput.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            errorText.text = "ENTER A NAME";
            return;
        }

        if (ProfileManager.Instance.ProfileNameExists(playerName))
        {
            errorText.text = "THAT NAME IS ALREADY USED";
            return;
        }

        bool created = ProfileManager.Instance.AddProfile(playerName, "", selectedAvatar, nonDominantIsLeft);
        if (!created)
        {
            errorText.text = "COULD NOT SAVE";
            return;
        }

        List<UserProfile> profiles = ProfileManager.Instance.Profiles;
        for (int i = 0; i < profiles.Count; i++)
        {
            if (profiles[i].name == playerName)
            {
                ProfileManager.Instance.SelectProfile(profiles[i]);
                break;
            }
        }

        ShowList();
    }

    void SelectProfile(UserProfile profile)
    {
        ProfileManager.Instance.SelectProfile(profile);
        ShowCurrentOnButton();
        Close();
    }

    UserProfile pendingDelete;

    void AskDelete(UserProfile profile)
    {
        pendingDelete = profile;
        confirmText.text = "DELETE " + profile.name.ToUpper() + "?";
        confirmRoot.SetActive(true);
    }

    void ConfirmDelete()
    {
        if (pendingDelete != null && ProfileManager.Instance != null)
            ProfileManager.Instance.DeleteProfile(pendingDelete);

        pendingDelete = null;
        confirmRoot.SetActive(false);
        ShowCurrentOnButton();
        RefreshList();
    }

    void ShowCurrentOnButton()
    {
        if (profileLabel == null || ProfileManager.Instance == null)
            return;

        UserProfile current = ProfileManager.Instance.CurrentProfile;
        profileLabel.text = current == null || string.IsNullOrEmpty(current.name) ? "Profile" : current.name;
        if (statusText != null && listView != null && listView.activeSelf)
            statusText.text = current == null ? "CHOOSE A PLAYER" : "PLAYING AS " + current.name.ToUpper();
    }

    Sprite Portrait(UserProfile profile)
    {
        if (profile != null && !string.IsNullOrEmpty(profile.imagePath) && File.Exists(profile.imagePath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(profile.imagePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(bytes))
                {
                    return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (IOException)
            {
            }
        }

        if (avatars == null || avatars.Length == 0)
            return null;

        int index = profile == null ? 0 : Mathf.Clamp(profile.avatarIndex, 0, avatars.Length - 1);
        return avatars[index];
    }

    void EnsureManager()
    {
        if (ProfileManager.Instance != null)
            return;

        GameObject manager = new GameObject("ProfileManager");
        manager.AddComponent<ProfileManager>();
    }

    static Transform FindChild(Transform root, string childName)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == childName)
                return all[i];
        }

        return null;
    }

    Transform FindDeep(string childName)
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == childName)
                return all[i];
        }

        Debug.LogError("Profile window is missing " + childName);
        return null;
    }
}

public class VrNameKeyboard : MonoBehaviour, ISelectHandler
{
    public TMP_InputField field;
    TouchScreenKeyboard keyboard;

    public void OnSelect(BaseEventData eventData)
    {
        if (field == null || !TouchScreenKeyboard.isSupported)
            return;

        keyboard = TouchScreenKeyboard.Open(field.text, TouchScreenKeyboardType.Default, false, false, false, false, "Name");
    }

    void Update()
    {
        if (keyboard == null || field == null || !TouchScreenKeyboard.isSupported)
            return;

        if (keyboard.text != field.text)
            field.text = keyboard.text;

        if (!keyboard.active)
            keyboard = null;
    }
}
