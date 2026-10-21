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
    static readonly Color ActiveCard = new Color(0.78f, 0.93f, 1f, 1f);
    static readonly Color HighlightCard = new Color(0.62f, 0.86f, 1f, 1f);
    static readonly Color IdleCard = Color.white;

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
    RectTransform avatarViewport;
    RectTransform avatarRow;
    float avatarStep;

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
        PrepareViews();
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

        List<Button> buttons = new List<Button>();
        List<Image> faces = new List<Image>();
        for (int i = 0; i < 16; i++)
        {
            Transform slot = FindOptional("Avatar" + i);
            if (slot == null)
                break;

            Button button = slot.GetComponent<Button>();
            Transform face = slot.Find("Face");
            if (button == null || face == null)
                continue;

            buttons.Add(button);
            faces.Add(face.GetComponent<Image>());
        }

        avatarButtons = buttons.ToArray();
        avatarFaces = faces.ToArray();
        avatars = LoadAvatars();
        EnsureAvatarScroller();
        labelFont = statusText.font;
        labelMaterial = statusText.fontSharedMaterial;

        Image addImage = FindDeep("Add").GetComponent<Image>();
        cardSprite = addImage.sprite;
        keyboardRoot = FindOptional("Keyboard") != null ? FindOptional("Keyboard").gameObject : null;
        if (cardTemplate != null)
            cardTemplate.gameObject.SetActive(false);
        nameInput = BuildNameField(FindDeep("NameField"));
        BindMainMenu();
    }

    void PrepareViews()
    {
        EnsureContinueButton();
        if (createView != null)
            createView.SetActive(false);
        if (confirmRoot != null)
            confirmRoot.SetActive(false);
        if (keyboardRoot != null)
            keyboardRoot.SetActive(false);
        if (cardTemplate != null)
            cardTemplate.gameObject.SetActive(false);
        if (listView != null)
            listView.SetActive(true);
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
        Hook("Select", OnSelect);
        Hook("Continue", OnSelect);
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

    void OnSelect()
    {
        UserProfile chosen = highlighted ?? (ProfileManager.Instance != null ? ProfileManager.Instance.CurrentProfile : null);
        if (chosen == null)
            return;

        ProfileManager.Instance.SelectProfile(chosen);
        highlighted = chosen;
        ShowCurrentOnButton();
        RefreshList();
        Close();
    }

    void RefreshAvatars()
    {
        int count = avatarButtons == null ? 0 : avatarButtons.Length;
        if (count == 0)
            return;

        EnsureAvatarScroller();

        const int visibleSlots = 4;
        int maxStart = Mathf.Max(0, count - visibleSlots);
        int start = Mathf.Clamp(selectedAvatar - 1, 0, maxStart);

        for (int i = 0; i < count; i++)
        {
            avatarButtons[i].gameObject.SetActive(true);
            bool selected = i == selectedAvatar;
            avatarButtons[i].GetComponent<Image>().color = selected ? SelectedPlate : IdlePlate;
            avatarButtons[i].onClick.RemoveAllListeners();
            int captured = i;
            avatarButtons[i].onClick.AddListener(() =>
            {
                selectedAvatar = captured;
                RefreshAvatars();
            });
        }

        if (avatarRow != null)
            avatarRow.anchoredPosition = new Vector2(-start * avatarStep, 0f);

        if (avatarNameText != null)
            avatarNameText.text = AvatarNames[Mathf.Clamp(selectedAvatar, 0, AvatarNames.Length - 1)].ToUpperInvariant();
    }

    void NudgeAvatar(int direction)
    {
        int count = avatarButtons != null && avatarButtons.Length > 0
            ? avatarButtons.Length
            : Mathf.Max(1, avatars.Length);
        selectedAvatar = Mathf.Clamp(selectedAvatar + direction, 0, count - 1);
        RefreshAvatars();
    }

    void EnsureAvatarScroller()
    {
        if (createView == null || avatarButtons == null || avatarButtons.Length == 0)
            return;

        if (avatarRow != null && avatarButtons[0] != null && avatarButtons[0].transform.parent == avatarRow)
        {
            MeasureAvatarStep();
            return;
        }

        bool authoredViewport = false;
        if (avatarViewport == null)
        {
            Transform existing = FindOptional("AvatarViewport");
            avatarViewport = existing as RectTransform;
            authoredViewport = avatarViewport != null;
        }

        if (avatarViewport == null)
        {
            GameObject viewportObject = new GameObject("AvatarViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(createView.transform, false);
            avatarViewport = viewportObject.GetComponent<RectTransform>();
            Image mask = viewportObject.GetComponent<Image>();
            mask.color = new Color(1f, 1f, 1f, 0f);
            mask.raycastTarget = false;
        }

        if (avatarRow == null)
        {
            Transform existingRow = avatarViewport.Find("AvatarRow");
            if (existingRow != null)
                avatarRow = existingRow as RectTransform;
        }

        if (avatarRow == null)
        {
            GameObject rowObject = new GameObject("AvatarRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            rowObject.transform.SetParent(avatarViewport, false);
            avatarRow = rowObject.GetComponent<RectTransform>();
        }

        if (!authoredViewport)
            PlaceAvatarViewport();

        avatarRow.anchorMin = new Vector2(0f, 0.5f);
        avatarRow.anchorMax = new Vector2(0f, 0.5f);
        avatarRow.pivot = new Vector2(0f, 0.5f);
        avatarRow.anchoredPosition = Vector2.zero;
        avatarRow.sizeDelta = new Vector2(0f, avatarButtons[0].GetComponent<RectTransform>().sizeDelta.y);

        HorizontalLayoutGroup layout = avatarRow.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = avatarRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        if (!authoredViewport)
            layout.spacing = AvatarSpacing();

        ContentSizeFitter fitter = avatarRow.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = avatarRow.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            RectTransform slot = avatarButtons[i].transform as RectTransform;
            if (slot.parent != avatarRow)
                slot.SetParent(avatarRow, false);
            slot.localScale = Vector3.one;
            slot.localRotation = Quaternion.identity;
            avatarButtons[i].gameObject.SetActive(true);

            MaskableGraphic plate = avatarButtons[i].GetComponent<MaskableGraphic>();
            if (plate != null)
                plate.maskable = true;
            if (avatarFaces[i] != null)
                avatarFaces[i].maskable = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(avatarRow);
        MeasureAvatarStep();
    }

    void MeasureAvatarStep()
    {
        if (avatarRow == null || avatarButtons == null || avatarButtons.Length == 0)
            return;

        HorizontalLayoutGroup layout = avatarRow.GetComponent<HorizontalLayoutGroup>();
        float spacing = layout != null ? layout.spacing : 2f;
        avatarStep = avatarButtons[0].GetComponent<RectTransform>().sizeDelta.x + spacing;
        if (avatarButtons.Length < 2)
            return;

        float first = (avatarButtons[0].transform as RectTransform).anchoredPosition.x;
        float second = (avatarButtons[1].transform as RectTransform).anchoredPosition.x;
        float measured = Mathf.Abs(second - first);
        if (measured > 0.01f)
            avatarStep = measured;
    }

    void PlaceAvatarViewport()
    {
        RectTransform prev = FindOptional("Prev") as RectTransform;
        RectTransform next = FindOptional("Next") as RectTransform;
        RectTransform first = avatarButtons[0].transform as RectTransform;
        float height = first != null ? first.sizeDelta.y + 2f : 16f;
        float y = 28.77f;
        float left = -26f;
        float right = 26f;

        if (prev != null)
        {
            left = prev.anchoredPosition.x + prev.rect.width * 0.5f + 1.2f;
            y = prev.anchoredPosition.y;
        }

        if (next != null)
            right = next.anchoredPosition.x - next.rect.width * 0.5f - 1.2f;

        if (right <= left)
        {
            left = -26f;
            right = 26f;
        }

        avatarViewport.SetParent(createView.transform, false);
        avatarViewport.anchorMin = new Vector2(0.5f, 0.5f);
        avatarViewport.anchorMax = new Vector2(0.5f, 0.5f);
        avatarViewport.pivot = new Vector2(0.5f, 0.5f);
        avatarViewport.anchoredPosition = new Vector2((left + right) * 0.5f, y);
        avatarViewport.sizeDelta = new Vector2(right - left, height);
        avatarViewport.SetSiblingIndex(Mathf.Max(0, createView.transform.childCount - 1));
        if (prev != null)
            prev.SetAsLastSibling();
        if (next != null)
            next.SetAsLastSibling();
    }

    float AvatarSpacing()
    {
        if (avatarButtons.Length < 2)
            return 2f;

        RectTransform a = avatarButtons[0].transform as RectTransform;
        RectTransform b = avatarButtons[1].transform as RectTransform;
        float gap = b.anchoredPosition.x - a.anchoredPosition.x - a.sizeDelta.x;
        return gap > 0.1f && gap < 8f ? gap : 2f;
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
        for (int i = 0; i < avatarFaces.Length; i++)
        {
            if (avatarFaces[i] != null && avatarFaces[i].sprite != null)
                found.Add(avatarFaces[i].sprite);
        }

        if (found.Count > 0)
            return found.ToArray();

        for (int i = 0; i < AvatarNames.Length; i++)
        {
            Sprite sprite = FindSpriteByName(AvatarNames[i]);
            if (sprite != null)
                found.Add(sprite);
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

    void EnsureContinueButton()
    {
        Transform existing = FindOptional("Select");
        if (existing == null)
            existing = FindOptional("Continue");
        if (existing != null)
            continueButton = existing.gameObject;
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
