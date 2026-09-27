using UnityEngine;

public class MainMenuReturnController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject checkProgressPanel;


    private void Start()
    {
        bool shouldOpenChapterSelect =
            PlayerPrefs.GetInt(
                "OpenChapterSelectOnLoad",
                0
            ) == 1;


      

        if (shouldOpenChapterSelect)
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            if (checkProgressPanel != null)
            {
                checkProgressPanel.SetActive(true);
            }


            
            PlayerPrefs.DeleteKey(
                "OpenChapterSelectOnLoad"
            );

            PlayerPrefs.Save();
        }

     
        else
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }

            if (checkProgressPanel != null)
            {
                checkProgressPanel.SetActive(false);
            }
        }
    }
}