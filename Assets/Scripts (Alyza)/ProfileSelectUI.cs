using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
public class ProfileSelectUI : MonoBehaviour
{
    public Transform profileContainer;
    public GameObject profileButtonPrefab;
    public GameObject addProfileButton;
    public GameObject creationPanel;
    public Button leftArrow;
    public Button rightArrow;
    private const int PROFILES_PER_PAGE = 3;
    private int currentPage = 0;
    private List<GameObject> spawnedCards = new List<GameObject>();
    void Start()
    {
        StartCoroutine(LoadProfiles());
    }
    void OnEnable()
    {
        StartCoroutine(LoadProfiles());
    }
    IEnumerator LoadProfiles()
    {
        yield return null;
        currentPage = 0;
        RefreshProfileList();
    }
    public void RefreshProfileList()
    {
        // Delete old cards
        foreach (GameObject card in spawnedCards)
        {
            if (card != null)
                Destroy(card);
        }
        spawnedCards.Clear();
        if (ProfileManager.Instance == null)
        {
            Debug.LogError("ProfileManager not found!");
            return;
        }
        List<UserProfile> allProfiles = ProfileManager.Instance.Profiles;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allProfiles.Count / PROFILES_PER_PAGE));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
        int startIndex = currentPage * PROFILES_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + PROFILES_PER_PAGE, allProfiles.Count);
        for (int i = startIndex; i < endIndex; i++)
        {
            UserProfile profile = allProfiles[i];
            GameObject cardObj = Instantiate(profileButtonPrefab, profileContainer);
            spawnedCards.Add(cardObj);
            ProfileCardUI cardUI = cardObj.GetComponent<ProfileCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(profile, this);
            }
            else
            {
                Debug.LogError("ProfileCardUI component missing on ProfileButtonPrefab!");
            }
        }
        if (addProfileButton != null)
            addProfileButton.transform.SetAsLastSibling();
        UpdateArrowVisibility(totalPages);
    }
    void UpdateArrowVisibility(int totalPages)
    {
        if (leftArrow != null)
            leftArrow.gameObject.SetActive(currentPage > 0);
        if (rightArrow != null)
            rightArrow.gameObject.SetActive(currentPage < totalPages - 1);
    }
    public void OnLeftArrowClicked()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshProfileList();
        }
    }
    public void OnRightArrowClicked()
    {
        int totalPages = Mathf.Max(1,
            Mathf.CeilToInt((float)ProfileManager.Instance.Profiles.Count / PROFILES_PER_PAGE));
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            RefreshProfileList();
        }
    }
    public void OnProfileSelected(UserProfile profile)
    {
        ProfileManager.Instance.SelectProfile(profile);
        Debug.Log("Selected Profile: " + profile.name);
        SceneManager.LoadScene("02_MainMenu");
    }
    public void OnAddProfileClicked()
    {
        if (creationPanel != null)
            creationPanel.SetActive(true);
    }
}