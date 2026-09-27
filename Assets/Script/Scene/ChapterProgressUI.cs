using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChapterProgressUI : MonoBehaviour
{
    
    public TMP_Text chapter1ProgressText;
    public TMP_Text chapter2ProgressText;
    public TMP_Text chapter3ProgressText;
    public TMP_Text chapter4ProgressText;


   
    public Slider progressSlider;


   
    public int minigamesPerChapter = 5;
    public int totalChapters = 4;


    private void OnEnable()
    {
        RefreshProgress();
    }


    public void RefreshProgress()
    {
        int chapter1 =
            Mathf.Clamp(
                SessionProgress.GetProgress(1),
                0,
                minigamesPerChapter
            );


        int chapter2 =
            Mathf.Clamp(
                SessionProgress.GetProgress(2),
                0,
                minigamesPerChapter
            );


        int chapter3 =
            Mathf.Clamp(
                SessionProgress.GetProgress(3),
                0,
                minigamesPerChapter
            );


        int chapter4 =
            Mathf.Clamp(
                SessionProgress.GetProgress(4),
                0,
                minigamesPerChapter
            );


        
        if (chapter1ProgressText != null)
        {
            chapter1ProgressText.text =
                $"{chapter1}/{minigamesPerChapter}";
        }


        if (chapter2ProgressText != null)
        {
            chapter2ProgressText.text =
                $"{chapter2}/{minigamesPerChapter}";
        }


        if (chapter3ProgressText != null)
        {
            chapter3ProgressText.text =
                $"{chapter3}/{minigamesPerChapter}";
        }


        if (chapter4ProgressText != null)
        {
            chapter4ProgressText.text =
                $"{chapter4}/{minigamesPerChapter}";
        }


     

        int completedMinigames =
            chapter1 +
            chapter2 +
            chapter3 +
            chapter4;


        int totalMinigames =
            minigamesPerChapter *
            totalChapters;


        float overallProgress = 0f;


        if (totalMinigames > 0)
        {
            overallProgress =
                (float)completedMinigames /
                totalMinigames;
        }


        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;

            progressSlider.value =
                overallProgress;

            progressSlider.interactable =
                false;
        }
    }
}