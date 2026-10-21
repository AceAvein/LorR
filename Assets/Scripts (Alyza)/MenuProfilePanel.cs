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
    static readonly Color TitleInk = new Color(0.95f, 0.97f, 1f, 1f);
    static readonly Color StatusInk = new Color(0.16f, 0.32f, 0.55f, 1f);
    static readonly Color SelectedPlate = new Color(0.22f, 0.78f, 0.36f, 1f);
    static readonly Color IdlePlate = new Color(0.9f, 0.94f, 0.98f, 1f);
    static readonly Color ActiveCard = new Color(0.78f, 0.93f, 1f, 1f);
    static readonly Color HighlightCard = new Color(0.62f, 0.86f, 1f, 1f);
    static readonly Color IdleCard = Color.white;
    static readonly Color CreateGreen = new Color(0.2f, 0.72f, 0.38f, 1f);
    static readonly Color ContinueBlue = new Color(0.18f, 0.48f, 0.86f, 1f);

    [SerializeField] GameObject mainMenu;
    [SerializeField] Button profileButton;

    TMP_Text profileLabel;
    Transform content;
    Transform cardTemplate;
    GameObject listView;
    GameObject createView;
    GameObject confirmRoot;
    GameObject backButton;
    GameObject emptyText;
    GameObject continueButton;
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
    UserProfile highlighted;
    UserProfile pendingDelete;
    int selectedAvatar;
    bool nonDominantIsLeft = true;
    bool ready;
    bool shiftOn;

    void Awake()
    {
        Cache();
        Relayout();
        Wire();
        EnsureManager();
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
        HideKeyboard();
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
        nameInput = BuildNameField(FindDeep("NameField"));
        BindMainMenu();
    }

    void BindMainMenu()
    {
        if (mainMenu == null)
        {
            GameObject buttons = GameObject.Find("MainMenu_Buttons");
            if (buttons != null)
                mainMenu = buttons;
        }

        if (profileButton == null && mainMenu != null)
        {
            Transform named = mainMenu.transform.Find("Profile");
            if (named != null)
                profileButton = named.GetComponent<Button>();
        }

        if (profileButton == null)
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "Profile")
                {
                    profileButton = buttons[i];
                    break;
                }
            }
        }

        if (profileButton != null)
        {
            profileLabel = profileButton.GetComponentInChildren<TMP_Text>(true);
            if (mainMenu == null)
                mainMenu = profileButton.transform.parent != null
                    ? profileButton.transform.parent.gameObject
                    : profileButton.gameObject;
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
        Hook("No", CancelDelete);
        EnsureContinueButton();
        Hook("Continue", OnContinue);
        WireKeyboard();

        if (profileButton != null)
        {
            profileButton.onClick.RemoveListener(Open);
            profileButton.onClick.AddListener(Open);
        }

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
        Transform found = FindOptional(objectName);
        if (found == null)
            return;

        Button button = found.GetComponent<Button>();
        if (button == null)
            return;

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
        input.characterLimit = ProfileManager.MaxNameLength;
        input.caretWidth = 3;
        input.customCaretColor = true;
        input.caretColor = Ink;
        input.shouldHideMobileInput = true;
        input.shouldHideSoftKeyboard = true;
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
            windowTitle.text = "PLAYER PROFILES";
        if (highlighted == null && ProfileManager.Instance != null)
            highlighted = ProfileManager.Instance.CurrentProfile;
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
            windowTitle.text = "CREATE NEW PLAYER";
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
        statusText.text = current == null ? "CHOOSE A PLAYER" : "PLAYING AS " + current.name.ToUpperInvariant();
        if (continueButton != null)
        {
            bool canContinue = highlighted != null || current != null;
            continueButton.GetComponent<Button>().interactable = canContinue;
        }

        for (int i = 0; i < profiles.Count; i++)
        {
            GameObject card = cardTemplate != null
                ? Instantiate(cardTemplate.gameObject, content)
                : CreateCard();
            card.name = "Card";
            card.SetActive(true);
            spawned.Add(card);
            FillCard(card.transform, profiles[i], current, highlighted);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    }

    void FillCard(Transform card, UserProfile profile, UserProfile current, UserProfile focus)
    {
        bool isCurrent = SameProfile(profile, current);
        bool isFocus = SameProfile(profile, focus);
        Image plate = card.GetComponent<Image>();
        if (plate != null)
            plate.color = isFocus ? HighlightCard : isCurrent ? ActiveCard : IdleCard;

        Image face = card.Find("CardFace").GetComponent<Image>();
        face.sprite = Portrait(profile);
        face.preserveAspect = true;

        card.Find("CardName").GetComponent<TMP_Text>().text = profile.name;
        string hand = profile.nonDominantIsLeft ? "Left" : "Right";
        string suffix = isCurrent ? "  •  ACTIVE" : "";
        card.Find("CardHand").GetComponent<TMP_Text>().text = "Non-dominant: " + hand + suffix;

        Button select = card.GetComponent<Button>();
        if (select.targetGraphic == null)
            select.targetGraphic = plate;
        select.onClick.RemoveAllListeners();
        UserProfile captured = profile;
        select.onClick.AddListener(() => HighlightProfile(captured));

        Button delete = card.Find("CardDelete").GetComponent<Button>();
        delete.onClick.RemoveAllListeners();
        delete.onClick.AddListener(() => AskDelete(captured));
    }

    void HighlightProfile(UserProfile profile)
    {
        highlighted = profile;
        RefreshList();
    }

    void OnContinue()
    {
        UserProfile chosen = highlighted ?? (ProfileManager.Instance != null ? ProfileManager.Instance.CurrentProfile : null);
        if (chosen == null)
            return;

        ProfileManager.Instance.SelectProfile(chosen);
        ShowCurrentOnButton();
        Close();
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

        avatarNameText.text = AvatarNames[Mathf.Clamp(selectedAvatar, 0, AvatarNames.Length - 1)].ToUpperInvariant();
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
        HideKeyboard();
        string playerName = ProfileManager.SanitizeName(nameInput.text);
        nameInput.text = playerName;
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

        UserProfile createdProfile = ProfileManager.Instance.FindProfile(playerName);
        if (createdProfile != null)
        {
            ProfileManager.Instance.SelectProfile(createdProfile);
            highlighted = createdProfile;
        }

        ShowList();
    }

    void AskDelete(UserProfile profile)
    {
        pendingDelete = profile;
        confirmText.text = "DELETE " + profile.name.ToUpperInvariant() + "?";
        confirmRoot.SetActive(true);
        confirmRoot.transform.SetAsLastSibling();
    }

    void CancelDelete()
    {
        pendingDelete = null;
        confirmRoot.SetActive(false);
    }

    void ConfirmDelete()
    {
        if (pendingDelete != null && ProfileManager.Instance != null)
        {
            if (SameProfile(highlighted, pendingDelete))
                highlighted = null;
            ProfileManager.Instance.DeleteProfile(pendingDelete);
        }

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
            statusText.text = current == null ? "CHOOSE A PLAYER" : "PLAYING AS " + current.name.ToUpperInvariant();
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
                    return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
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

    static bool SameProfile(UserProfile a, UserProfile b)
    {
        if (a == null || b == null)
            return false;
        if (ReferenceEquals(a, b))
            return true;
        return !string.IsNullOrEmpty(a.name) &&
               a.name.Equals(b.name, System.StringComparison.OrdinalIgnoreCase);
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
        List<Sprite> found = new List<Sprite>();
        for (int i = 0; i < AvatarNames.Length; i++)
        {
            Sprite sprite = FindSpriteByName(AvatarNames[i]);
            if (sprite != null)
                found.Add(sprite);
        }

        if (found.Count > 0)
            return found.ToArray();

        for (int i = 0; i < avatarFaces.Length; i++)
        {
            if (avatarFaces[i] != null && avatarFaces[i].sprite != null)
                found.Add(avatarFaces[i].sprite);
        }

        return found.ToArray();
    }

    static Sprite FindSpriteByName(string spriteName)
    {
        Sprite[] loaded = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] != null && loaded[i].name == spriteName)
                return loaded[i];
        }

        return null;
    }

    public void ShowKeyboard()
    {
        if (keyboardRoot == null)
            return;

        keyboardRoot.SetActive(true);
        keyboardRoot.transform.SetAsLastSibling();
        if (nameInput != null)
        {
            nameInput.ActivateInputField();
            nameInput.caretPosition = nameInput.text.Length;
        }
    }

    public void HideKeyboard()
    {
        if (keyboardRoot != null)
            keyboardRoot.SetActive(false);
        if (nameInput != null)
            nameInput.DeactivateInputField();
    }

    void WireKeyboard()
    {
        if (keyboardRoot == null)
            return;

        EnsureKey("Key_Shift", "SHIFT", new Color(0.78f, 0.86f, 0.96f, 1f));
        EnsureKey("Key_Clear", "CLEAR", new Color(0.95f, 0.62f, 0.28f, 1f));
        LayoutKeyboard();

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
            else if (token == "Clear")
                keys[i].onClick.AddListener(ClearName);
            else if (token == "Shift")
                keys[i].onClick.AddListener(ToggleShift);
            else if (token == "Done")
                keys[i].onClick.AddListener(HideKeyboard);
            else
            {
                string captured = token;
                keys[i].onClick.AddListener(() => TypeKey(captured));
            }
        }

        RefreshShiftLabels();
    }

    void TypeKey(string token)
    {
        if (string.IsNullOrEmpty(token))
            return;

        string value = token.Length == 1 && char.IsLetter(token[0])
            ? (shiftOn ? token.ToUpperInvariant() : token.ToLowerInvariant())
            : token;
        TypeName(value);
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
        if (errorText != null)
            errorText.text = "";
    }

    void BackspaceName()
    {
        if (nameInput == null || string.IsNullOrEmpty(nameInput.text))
            return;

        nameInput.text = nameInput.text.Substring(0, nameInput.text.Length - 1);
        nameInput.caretPosition = nameInput.text.Length;
    }

    void ClearName()
    {
        if (nameInput == null)
            return;

        nameInput.text = "";
        nameInput.caretPosition = 0;
    }

    void ToggleShift()
    {
        shiftOn = !shiftOn;
        RefreshShiftLabels();
    }

    void RefreshShiftLabels()
    {
        if (keyboardRoot == null)
            return;

        Transform[] all = keyboardRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].name.StartsWith("Key_"))
                continue;

            string token = all[i].name.Substring(4);
            if (token.Length != 1 || !char.IsLetter(token[0]))
            {
                if (token == "Shift")
                {
                    Image plate = all[i].GetComponent<Image>();
                    if (plate != null)
                        plate.color = shiftOn ? SelectedPlate : new Color(0.78f, 0.86f, 0.96f, 1f);
                }

                continue;
            }

            TMP_Text label = all[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = shiftOn ? token.ToUpperInvariant() : token.ToLowerInvariant();
        }
    }

    void Relayout()
    {
        RectTransform window = transform as RectTransform;
        SetRect(window, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, window.anchoredPosition, new Vector2(80f, 112f));
        window.localScale = Vector3.one * 0.012f;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.worldCamera == null && Camera.main != null)
            canvas.worldCamera = Camera.main;

        RectTransform board = FindDeep("Board") as RectTransform;
        Stretch(board, Vector2.zero);
        board.localScale = Vector3.one;
        DisableRaycast(board);

        RectTransform inner = FindDeep("Inner") as RectTransform;
        Stretch(inner, new Vector2(4f, 4f));
        DisableRaycast(inner);

        SetRect(FindDeep("Title") as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(52f, 8f));
        StyleTitle(windowTitle, "PLAYER PROFILES", 5.4f);

        SetRect(FindDeep("Close") as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-5f, -4f), new Vector2(9f, 9f));
        SetRect(FindDeep("Back") as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(5f, -4f), new Vector2(9f, 9f));

        RectTransform list = listView.transform as RectTransform;
        Stretch(list, new Vector2(6f, 8f));
        list.offsetMin = new Vector2(6f, 8f);
        list.offsetMax = new Vector2(-6f, -14f);
        list.localScale = Vector3.one;

        SetRect(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1f), new Vector2(64f, 6f));
        StyleLabel(statusText, 2.6f, StatusInk);

        SetRect(emptyText.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(56f, 6f));

        RectTransform scroll = FindDeep("Scroll") as RectTransform;
        SetRect(scroll, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        scroll.offsetMin = new Vector2(0f, 12f);
        scroll.offsetMax = new Vector2(0f, -8f);
        ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.scrollSensitivity = 12f;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }

        TMP_Text emptyLabel = emptyText.GetComponent<TMP_Text>();
        if (emptyLabel != null)
        {
            emptyLabel.raycastTarget = false;
            emptyLabel.text = "NO PROFILES YET";
        }

        RectTransform add = FindDeep("Add") as RectTransform;
        SetRect(add, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(-1.5f, 10f));
        TMP_Text addLabel = FindDeep("AddLabel").GetComponent<TMP_Text>();
        addLabel.text = "CREATE NEW PLAYER";
        addLabel.fontSize = 2.6f;
        addLabel.enableAutoSizing = true;
        addLabel.fontSizeMin = 2f;
        addLabel.fontSizeMax = 2.8f;

        EnsureContinueButton();
        RectTransform cont = continueButton.transform as RectTransform;
        SetRect(cont, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(-1.5f, 10f));

        RelayoutCreate();
        RelayoutConfirm();
        RelayoutCardTemplate();
        RelayoutKeyboard();
        createView.SetActive(false);
        confirmRoot.SetActive(false);
        if (keyboardRoot != null)
            keyboardRoot.SetActive(false);
        if (cardTemplate != null)
            cardTemplate.gameObject.SetActive(false);
        listView.SetActive(true);
    }

    void RelayoutCreate()
    {
        RectTransform create = createView.transform as RectTransform;
        Stretch(create, new Vector2(6f, 8f));
        create.offsetMin = new Vector2(6f, 8f);
        create.offsetMax = new Vector2(-6f, -14f);
        create.localScale = Vector3.one;

        SetRect(FindDeep("Prev") as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(8f, 10f));
        SetRect(FindDeep("Next") as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(8f, 10f));
        Image prevImage = FindDeep("Prev").GetComponent<Image>();
        Image nextImage = FindDeep("Next").GetComponent<Image>();
        if (prevImage != null) prevImage.color = IdlePlate;
        if (nextImage != null) nextImage.color = IdlePlate;

        float[] xs = { -18f, -6f, 6f, 18f };
        for (int i = 0; i < 4; i++)
            SetRect(FindDeep("Avatar" + i) as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(xs[i], -3f), new Vector2(10f, 10f));

        SetRect(avatarNameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(40f, 5f));
        SetRect(FindDeep("NameField") as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(58f, 10f));
        SetRect(errorText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -33f), new Vector2(58f, 5f));
        SetRect(FindDeep("Left") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-2f, 14f), new Vector2(24f, 10f));
        SetRect(FindDeep("Right") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(2f, 14f), new Vector2(24f, 10f));
        SetRect(FindDeep("Ok") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 1f), new Vector2(36f, 11f));
        TMP_Text okLabel = FindOptional("OkLabel") != null ? FindOptional("OkLabel").GetComponent<TMP_Text>() : null;
        if (okLabel != null)
            okLabel.text = "SAVE PLAYER";
    }

    void RelayoutConfirm()
    {
        RectTransform confirm = confirmRoot.transform as RectTransform;
        Stretch(confirm, Vector2.zero);
        confirm.localScale = Vector3.one;
        SetRect(FindDeep("ConfirmCard") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 28f));
        SetRect(FindDeep("Question") as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(46f, 10f));
        SetRect(FindDeep("Yes") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-2f, 4f), new Vector2(18f, 9f));
        SetRect(FindDeep("No") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(2f, 4f), new Vector2(18f, 9f));
        TMP_Text noLabel = FindOptional("NoLabel") != null ? FindOptional("NoLabel").GetComponent<TMP_Text>() : null;
        TMP_Text yesLabel = FindOptional("YesLabel") != null ? FindOptional("YesLabel").GetComponent<TMP_Text>() : null;
        if (noLabel != null) noLabel.text = "CANCEL";
        if (yesLabel != null) yesLabel.text = "DELETE";
    }

    void RelayoutCardTemplate()
    {
        if (cardTemplate == null)
            return;

        if (cardTemplate.parent != content)
            cardTemplate.SetParent(content, false);

        RectTransform rect = cardTemplate as RectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 16f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        LayoutElement layout = cardTemplate.GetComponent<LayoutElement>();
        if (layout == null)
            layout = cardTemplate.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 16f;
        layout.preferredHeight = 16f;
        layout.flexibleWidth = 1f;

        SetRect(cardTemplate.Find("CardFace") as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(9f, 0f), new Vector2(11f, 11f));
        SetRect(cardTemplate.Find("CardName") as RectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(17f, 2.4f), new Vector2(-34f, 5f));
        SetRect(cardTemplate.Find("CardHand") as RectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(17f, -2.6f), new Vector2(-34f, 4f));
        SetRect(cardTemplate.Find("CardDelete") as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-8f, 0f), new Vector2(8f, 8f));
    }

    void RelayoutKeyboard()
    {
        if (keyboardRoot == null)
            return;

        RectTransform keyboard = keyboardRoot.transform as RectTransform;
        SetRect(keyboard, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(78f, 42f));
        keyboard.localPosition = new Vector3(keyboard.localPosition.x, keyboard.localPosition.y, -2f);
        LayoutKeyboard();
    }

    void LayoutKeyboard()
    {
        if (keyboardRoot == null)
            return;

        PlaceKeyRow(new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" }, 15.5f, 6.8f, 7.2f);
        PlaceKeyRow(new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" }, 8.3f, 6.8f, 7.2f);
        PlaceKeyRow(new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" }, 1.1f, 6.8f, 7.2f);
        PlaceKeyRow(new[] { "Z", "X", "C", "V", "B", "N", "M" }, -6.1f, 6.8f, 7.2f);
        PlaceNamedKey("Key_Back", 31.2f, -6.1f, 12f, 7.2f);
        PlaceNamedKey("Key_Shift", -24f, -14.2f, 14f, 7.6f);
        PlaceNamedKey("Key_Space", 0f, -14.2f, 28f, 7.6f);
        PlaceNamedKey("Key_Clear", 20f, -14.2f, 10f, 7.6f);
        PlaceNamedKey("Key_Done", 31.5f, -14.2f, 11f, 7.6f);
    }

    void PlaceKeyRow(string[] tokens, float y, float width, float height)
    {
        float spacing = width + 0.7f;
        float start = -((tokens.Length - 1) * spacing) * 0.5f;
        for (int i = 0; i < tokens.Length; i++)
            PlaceNamedKey("Key_" + tokens[i], start + spacing * i, y, width, height);
    }

    void PlaceNamedKey(string keyName, float x, float y, float width, float height)
    {
        Transform key = FindOptional(keyName);
        if (key == null)
            return;

        SetRect(key as RectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(x, y), new Vector2(width, height));
        TMP_Text label = key.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.enableAutoSizing = true;
            label.fontSizeMin = 1.8f;
            label.fontSizeMax = 3.2f;
            label.raycastTarget = false;
        }
    }

    void EnsureContinueButton()
    {
        Transform existing = FindOptional("Continue");
        if (existing != null)
        {
            continueButton = existing.gameObject;
            return;
        }

        continueButton = CreateTextButton(listView.transform, "Continue", "SELECT / CONTINUE", ContinueBlue);
    }

    void EnsureKey(string keyName, string label, Color color)
    {
        if (FindOptional(keyName) != null)
            return;

        GameObject key = CreateTextButton(keyboardRoot.transform, keyName, label, color);
        key.name = keyName;
    }

    GameObject CreateTextButton(Transform parent, string objectName, string label, Color color)
    {
        GameObject plate = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        plate.transform.SetParent(parent, false);
        Image image = plate.GetComponent<Image>();
        image.sprite = cardSprite;
        image.type = cardSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = true;
        Button button = plate.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateLabel(plate.transform, objectName + "Label", Vector2.zero, Vector2.zero, 2.6f, Color.white, FontStyles.Bold);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 2f;
        text.fontSizeMax = 3f;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return plate;
    }

    GameObject CreateCard()
    {
        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        card.transform.SetParent(content, false);

        LayoutElement layout = card.GetComponent<LayoutElement>();
        layout.preferredHeight = 16f;
        layout.minHeight = 16f;

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
        faceRect.anchoredPosition = new Vector2(9f, 0f);
        faceRect.sizeDelta = new Vector2(11f, 11f);
        face.preserveAspect = true;

        CreateLabel(card.transform, "CardName", new Vector2(17f, 2.4f), new Vector2(-34f, 5f), 3.2f, Ink, FontStyles.Bold);
        CreateLabel(card.transform, "CardHand", new Vector2(17f, -2.6f), new Vector2(-34f, 4f), 2.2f, new Color(0.35f, 0.45f, 0.58f, 1f), FontStyles.Normal);

        Image deletePlate = CreateImage(card.transform, "CardDelete", true);
        deletePlate.sprite = cardSprite;
        deletePlate.type = cardSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        deletePlate.color = new Color(0.95f, 0.35f, 0.38f, 1f);
        RectTransform deleteRect = deletePlate.rectTransform;
        deleteRect.anchorMin = new Vector2(1f, 0.5f);
        deleteRect.anchorMax = new Vector2(1f, 0.5f);
        deleteRect.pivot = new Vector2(0.5f, 0.5f);
        deleteRect.anchoredPosition = new Vector2(-8f, 0f);
        deleteRect.sizeDelta = new Vector2(8f, 8f);
        Button delete = deletePlate.gameObject.AddComponent<Button>();
        delete.targetGraphic = deletePlate;
        TMP_Text deleteLabel = CreateLabel(deletePlate.transform, "DeleteLabel", Vector2.zero, Vector2.zero, 2.8f, Color.white, FontStyles.Bold);
        deleteLabel.text = "X";
        deleteLabel.alignment = TextAlignmentOptions.Center;
        RectTransform deleteLabelRect = deleteLabel.rectTransform;
        deleteLabelRect.anchorMin = Vector2.zero;
        deleteLabelRect.anchorMax = Vector2.one;
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

    static void StyleTitle(TMP_Text title, string text, float size)
    {
        if (title == null)
            return;
        title.text = text;
        title.fontSize = size;
        title.color = TitleInk;
        title.alignment = TextAlignmentOptions.Center;
        title.enableWordWrapping = false;
        title.raycastTarget = false;
    }

    static void StyleLabel(TMP_Text label, float size, Color color)
    {
        if (label == null)
            return;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    static void DisableRaycast(Component target)
    {
        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = false;
    }

    static void Stretch(RectTransform rect, Vector2 padding)
    {
        if (rect == null)
            return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = padding;
        rect.offsetMax = -padding;
        rect.localScale = Vector3.one;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        if (rect == null)
            return;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
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
            field.shouldHideMobileInput = true;
            field.shouldHideSoftKeyboard = true;
            field.ActivateInputField();
            field.caretPosition = field.text.Length;
        }

        MenuProfilePanel panel = GetComponentInParent<MenuProfilePanel>();
        if (panel != null)
            panel.ShowKeyboard();
    }
}
