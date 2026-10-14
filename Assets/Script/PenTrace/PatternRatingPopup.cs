using UnityEngine;

public class PatternRatingPopup : MonoBehaviour
{
    float hideAt;

    void OnEnable()
    {
        Restart();
    }

    public void Restart()
    {
        hideAt = Time.time + 1f;
    }

    void Update()
    {
        if (Time.time >= hideAt)
            gameObject.SetActive(false);
    }
}
