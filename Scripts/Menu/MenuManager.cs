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
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
        levelsMenu.SetActive(false);
    }

    public void OpenLevelsMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        levelsMenu.SetActive(true);
    }

    public void LoadLevel(int index)
    {
        SceneManager.LoadScene(index);
    }
}
