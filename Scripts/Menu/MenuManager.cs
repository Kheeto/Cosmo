using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject levelsMenu;
    [SerializeField] private Animator animator;

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
}
