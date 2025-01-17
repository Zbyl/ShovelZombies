using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class Hud : MonoBehaviour
{
    private GameObject hud;
    private GameObject background;
    private GameObject menu;
    private GameObject mainMenu;
    private GameObject credits;
    private GameObject gameOverScreen;
    private GameObject winScreen;
    private GameObject resumeButton;
    private GameObject exitButton;
    private TMP_Text skeletonsKilledLabel;
    private Image powerCircle;

    private List<Transform> heartsGood = new List<Transform>();
    private List<Transform> heartsBad = new List<Transform>();
    private List<Transform> shovelPower = new List<Transform>();
    bool endScreenShown = false;

    public bool isInLevelMenu = true;    /// False if the menu is before starting the level, true if it is inside the level.

#if UNITY_WEBGL
    public static bool isWebGLBuild = true;
#else
    public static bool isWebGLBuild = false;
#endif

    // Start is called before the first frame update
    void Start()
    {
        hud = transform.Find("Canvas/Hud").gameObject;
        background = transform.Find("Canvas/Menu/Background").gameObject;
        menu = transform.Find("Canvas/Menu").gameObject;
        mainMenu = transform.Find("Canvas/Menu/MainMenu").gameObject;
        resumeButton = transform.Find("Canvas/Menu/MainMenu/ResumeButton").gameObject;
        exitButton = transform.Find("Canvas/Menu/MainMenu/ExitButton").gameObject;
        credits = transform.Find("Canvas/Menu/Credits").gameObject;
        gameOverScreen = transform.Find("Canvas/Menu/GameOver").gameObject;
        winScreen = transform.Find("Canvas/Menu/WinScreen").gameObject;
        powerCircle = transform.Find("Canvas/Hud/PowerCircle").GetComponent<Image>();

        skeletonsKilledLabel = transform.Find("Canvas/Hud/SkeletonsKilledLabel").GetComponent<TMP_Text>();

        foreach (Transform child in transform.Find("Canvas/Hud/HeartsGood"))
        {
            heartsGood.Add(child);
        }
        foreach (Transform child in transform.Find("Canvas/Hud/HeartsBad"))
        {
            heartsBad.Add(child);
        }
        foreach (Transform child in transform.Find("Canvas/Hud/ShovelPower"))
        {
            shovelPower.Add(child);
        }

        ShowMenu(!isInLevelMenu);
        resumeButton.SetActive(isInLevelMenu);
        exitButton.SetActive(!isWebGLBuild);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHealth();
        UpdateShovelPower();
        skeletonsKilledLabel.text = $"{GameState.Instance.skeletonsKilled}/{GameState.Instance.gravesNumber}";
        powerCircle.fillAmount = GameState.Instance.powerShovelStrength;

        if (isInLevelMenu && Input.GetKeyDown(KeyCode.Escape) && (GameState.Instance.gameResult == GameState.GameResult.PLAYING))
        {
            ShowMenu(!menu.activeInHierarchy);
        }

        if (Input.GetKeyDown(KeyCode.F5)) GameState.Instance.gameResult = GameState.GameResult.WON;
        if (Input.GetKeyDown(KeyCode.F6)) GameState.Instance.gameResult = GameState.GameResult.LOST;
        if (Input.GetKeyDown(KeyCode.F7)) GameState.Instance.powerShovelStrength += 0.2f;

        if (GameState.Instance.gameResult == GameState.GameResult.WON)
        {
            ShowGameOver(true);
        }
        if (GameState.Instance.gameResult == GameState.GameResult.LOST)
        {
            ShowGameOver(false);
        }
    }

    public void ShowGameOver(bool won)
    {
        if (endScreenShown) return;
        endScreenShown = true;

        ShowMenu(true);
        mainMenu.SetActive(false);
        gameOverScreen.SetActive(!won);
        winScreen.SetActive(won);
        background.SetActive(false);
        resumeButton.SetActive(false);
    }

    public void ShowMenu(bool show)
    {
        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            hud.SetActive(false);
            menu.SetActive(true);
            background.SetActive(true);
            mainMenu.SetActive(true);
            credits.SetActive(false);
            gameOverScreen.SetActive(false);
            winScreen.SetActive(false);
            GameState.Instance.isPaused = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            hud.SetActive(true);
            menu.SetActive(false);
            GameState.Instance.isPaused = false;
        }
    }

    public void StartNewGame()
    {
        SceneManager.LoadScene(1);
        ShowMenu(false);
    }

    public void ExitGame()
    {
        Debug.Log("Exit game.");
        if (Application.isEditor)
        {
            SceneManager.LoadScene(0);
        }
        else if (!isWebGLBuild)
        {
            Application.Quit();
        }
    }

    public void UpdateHealth()
    {
        var playerHealth = GameState.Instance.playerHealth;
        for (int i = 0; i < heartsGood.Count; ++i)
        {
            bool activate = i < playerHealth;
            heartsGood[i].gameObject.SetActive(activate);
            heartsBad[i].gameObject.SetActive(!activate);
        }
    }

    public void UpdateShovelPower()
    {
        var shovelPowerInt = (int)(GameState.Instance.powerShovelStrength * 5.05f);
        for (int i = 0; i < shovelPower.Count; ++i)
        {
            bool activate = i <= shovelPowerInt;
            shovelPower[i].gameObject.SetActive(activate);
        }
    }
}
