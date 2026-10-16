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
    TMP_Text windowTitle;
    TMP_InputField nameInput;
    Sprite cardSprite;
    TMP_FontAsset labelFont;
    Material labelMaterial;
    Image leftPlate;
    Image rightPlate;
    GameObject keyboardRoot;
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
        windowTitle = FindDeep("Title").GetComponent<TMP_Text>();
        content = FindDeep("Content");
        cardTemplate = FindOptional("CardTemplate");

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

        avatars = LoadAvatars();
        labelFont = statusText.font;
        labelMaterial = statusText.fontSharedMaterial;

        Image addImage = FindDeep("Add").GetComponent<Image>();
        cardSprite = addImage.sprite;
        keyboardRoot = FindOptional("Keyboard") != null ? FindOptional("Keyboard").gameObject : null;
        if (cardTemplate != null)
            cardTemplate.gameObject.SetActive(false);

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
        WireKeyboard();

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
        HideKeyboard();
        if (windowTitle != null)
            windowTitle.text = "Profiles";
        RefreshList();
        ShowCurrentOnButton();
    }

    void ShowCreate()
    {
        listView.SetActive(false);
        confirmRoot.SetActive(false);
        createView.SetActive(true);
        backButton.SetActive(true);
        HideKeyboard();
        if (windowTitle != null)
            windowTitle.text = "Add User";
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
            child.SetParent(null, false);
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
            GameObject card = cardTemplate != null
                ? Instantiate(cardTemplate.gameObject, content)
                : CreateCard();
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
        int count = avatars == null ? 0 : avatars.Length;
        if (count == 0)
            return;

        int start = Mathf.Clamp(selectedAvatar - 1, 0, Mathf.Max(0, count - avatarButtons.Length));
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = start + i;
            bool visible = index < count;
            avatarButtons[i].gameObject.SetActive(visible);
            if (!visible)
                continue;

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
        Transform found = FindOptional(childName);
        if (found == null)
            Debug.LogError("Profile window is missing " + childName);
        return found;
    }

    Transform FindOptional(string childName)
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == childName)
                return all[i];
        }

        return null;
    }

    Sprite[] LoadAvatars()
    {
        Transform library = FindOptional("AvatarLibrary");
        if (library != null && library.childCount > 0)
        {
            Sprite[] fromLibrary = new Sprite[library.childCount];
            for (int i = 0; i < library.childCount; i++)
            {
                Image image = library.GetChild(i).GetComponent<Image>();
                if (image != null)
                    fromLibrary[i] = image.sprite;
            }

            return fromLibrary;
        }

        List<Sprite> found = new List<Sprite>();
        for (int i = 0; i < avatarFaces.Length; i++)
        {
            if (avatarFaces[i] != null && avatarFaces[i].sprite != null)
                found.Add(avatarFaces[i].sprite);
        }

        return found.ToArray();
    }

    public void ShowKeyboard()
    {
        if (keyboardRoot == null)
            return;

        keyboardRoot.SetActive(true);
        keyboardRoot.transform.SetAsLastSibling();
    }

    public void HideKeyboard()
    {
        if (keyboardRoot != null)
            keyboardRoot.SetActive(false);
    }

    void WireKeyboard()
    {
        if (keyboardRoot == null)
            return;

        Button[] keys = keyboardRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < keys.Length; i++)
        {
            string keyName = keys[i].gameObject.name;
            if (!keyName.StartsWith("Key_"))
                continue;

            string token = keyName.Substring(4);
            keys[i].onClick.RemoveAllListeners();
            if (keys[i].targetGraphic == null)
                keys[i].targetGraphic = keys[i].GetComponent<Image>();

            if (token == "Back")
                keys[i].onClick.AddListener(BackspaceName);
            else if (token == "Space")
                keys[i].onClick.AddListener(() => TypeName(" "));
            else if (token == "Done")
                keys[i].onClick.AddListener(HideKeyboard);
            else
            {
                string captured = token;
                keys[i].onClick.AddListener(() => TypeName(captured));
            }
        }
    }

    void TypeName(string value)
    {
        if (nameInput == null || string.IsNullOrEmpty(value))
            return;

        int limit = nameInput.characterLimit;
        if (limit > 0 && nameInput.text.Length + value.Length > limit)
            return;

        nameInput.text += value;
        nameInput.caretPosition = nameInput.text.Length;
    }

    void BackspaceName()
    {
        if (nameInput == null || string.IsNullOrEmpty(nameInput.text))
            return;

        nameInput.text = nameInput.text.Substring(0, nameInput.text.Length - 1);
        nameInput.caretPosition = nameInput.text.Length;
    }

    GameObject CreateCard()
    {
        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        card.transform.SetParent(content, false);

        LayoutElement layout = card.GetComponent<LayoutElement>();
        layout.preferredHeight = 12f;
        layout.minHeight = 12f;

        Image plate = card.GetComponent<Image>();
        plate.sprite = cardSprite;
        plate.type = cardSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        plate.color = Color.white;
        plate.raycastTarget = true;

        Button select = card.GetComponent<Button>();
        select.targetGraphic = plate;

        Image face = CreateImage(card.transform, "CardFace", false);
        RectTransform faceRect = face.rectTransform;
        faceRect.anchorMin = new Vector2(0f, 0.5f);
        faceRect.anchorMax = new Vector2(0f, 0.5f);
        faceRect.pivot = new Vector2(0.5f, 0.5f);
        faceRect.anchoredPosition = new Vector2(8f, 0f);
        faceRect.sizeDelta = new Vector2(8f, 8f);
        face.preserveAspect = true;

        CreateLabel(card.transform, "CardName", new Vector2(14f, 1.4f), new Vector2(-28f, 4f), 3.1f, Ink, FontStyles.Bold);
        CreateLabel(card.transform, "CardHand", new Vector2(14f, -2.2f), new Vector2(-28f, 3.2f), 2.1f, new Color(0.35f, 0.45f, 0.58f, 1f), FontStyles.Normal);

        Image deletePlate = CreateImage(card.transform, "CardDelete", true);
        deletePlate.sprite = cardSprite;
        deletePlate.type = cardSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        deletePlate.color = new Color(0.95f, 0.35f, 0.38f, 1f);
        RectTransform deleteRect = deletePlate.rectTransform;
        deleteRect.anchorMin = new Vector2(1f, 0.5f);
        deleteRect.anchorMax = new Vector2(1f, 0.5f);
        deleteRect.pivot = new Vector2(0.5f, 0.5f);
        deleteRect.anchoredPosition = new Vector2(-6f, 0f);
        deleteRect.sizeDelta = new Vector2(6f, 6f);
        Button delete = deletePlate.gameObject.AddComponent<Button>();
        delete.targetGraphic = deletePlate;
        TMP_Text deleteLabel = CreateLabel(deletePlate.transform, "DeleteLabel", Vector2.zero, Vector2.zero, 2.6f, Color.white, FontStyles.Bold);
        deleteLabel.text = "X";
        deleteLabel.alignment = TextAlignmentOptions.Center;
        RectTransform deleteLabelRect = deleteLabel.rectTransform;
        deleteLabelRect.anchorMin = Vector2.zero;
        deleteLabelRect.anchorMax = Vector2.one;
        deleteLabelRect.pivot = new Vector2(0.5f, 0.5f);
        deleteLabelRect.offsetMin = Vector2.zero;
        deleteLabelRect.offsetMax = Vector2.zero;

        return card;
    }

    Image CreateImage(Transform parent, string objectName, bool raycast)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.raycastTarget = raycast;
        return image;
    }

    TMP_Text CreateLabel(Transform parent, string objectName, Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text label = obj.GetComponent<TMP_Text>();
        label.font = labelFont;
        label.fontSharedMaterial = labelMaterial;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }
}

public class VrNameKeyboard : MonoBehaviour, ISelectHandler, IPointerClickHandler
{
    public TMP_InputField field;

    public void OnSelect(BaseEventData eventData)
    {
        OpenKeyboard();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenKeyboard();
    }

    void OpenKeyboard()
    {
        if (field != null)
        {
            field.ActivateInputField();
            field.caretPosition = field.text.Length;
        }

        MenuProfilePanel panel = GetComponentInParent<MenuProfilePanel>();
        if (panel != null)
            panel.ShowKeyboard();
    }
}
