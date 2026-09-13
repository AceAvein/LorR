using TMPro;
using UnityEngine;

public class ProgressionUItrace : MonoBehaviour
{
    public TMP_Text accuracyText;
    public TMP_Text rankText;

    public void Populate(float accuracy)
    {
        accuracyText.text = $"ACCURACY: {accuracy:0}%";
        rankText.text = $"RANK: {RankCalculator.GetRank(accuracy)}";
    }
}