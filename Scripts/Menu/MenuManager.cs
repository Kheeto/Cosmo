using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Graphics Settings")]
    [SerializeField] private UniversalRenderPipelineAsset graphicsHigh;
    [SerializeField] private UniversalRenderPipelineAsset graphicsLow;
    [SerializeField] private TMP_Text graphicsText;

    [Header("References")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject levelsMenu;
    [SerializeField] private Animator animator;

    [SerializeField] private List<Slider> sliders = new List<Slider>();
    [SerializeField] private List<TMP_Text> sliderTexts = new List<TMP_Text>();

    private void Start()
    {
        OpenMainMenu();
    }

    public void OpenMainMenu()
    {
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        levelsMenu.SetActive(false);
    }

    public void OpenOptionsMenu()
    {
        animator.SetTrigger("MainToOptions");
    }

    public void OpenLevelsMenu()
    {
        animator.SetTrigger("MainToLevels");
    }

    public void OptionsToMain()
    {
        animator.SetTrigger("OptionsToMain");
    }

    public void LevelsToMain()
    {
        animator.SetTrigger("LevelsToMain");
    }

    public void LoadLevel(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void OnGraphicsSettingUpdated()
    {
        if (GraphicsSettings.renderPipelineAsset == graphicsHigh)
        {
            GraphicsSettings.renderPipelineAsset = graphicsLow;
            graphicsText.text = "LOW";
        }
        else if (GraphicsSettings.renderPipelineAsset == graphicsLow)
        {
            GraphicsSettings.renderPipelineAsset = graphicsHigh;
            graphicsText.text = "HIGH";
        }
    }

    public void OnSliderValueUpdated(int index)
    {
        sliderTexts[index].text = sliders[index].value.ToString("0.##");
    }
}
