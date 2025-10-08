using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private static MenuController _instance;
    public static MenuController Instance {  get { return _instance; } }

    public GameObject _mainMenu;
    public Button _startButton;
    public Button _optionsButton;
    public Button _exitButton;

    public GameObject _optionsMenu;
    private OptionsController _optionsController;

    // Start is called before the first frame update
    void Start()
    {
        if(_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            _instance = this;
        }

        if(_optionsMenu != null)
        {
            GameObject options = Instantiate(_optionsMenu);
            _optionsController = options.GetComponent<OptionsController>();
        }

        if( _mainMenu == null )
        {
            _mainMenu = gameObject.transform.Find("MainMenu").gameObject;
        }
        BindMenuButtons();

        if(_optionsMenu == null )
        {
            _optionsMenu = gameObject.transform.Find("OptionsMenu").gameObject;
        }

    }
    
    private void BindMenuButtons()
    {
        if (_mainMenu == null) return;

        if(_startButton == null)
        {
            _startButton = _mainMenu.transform.Find("StartGameButton").GetComponent<Button>();
        }
        _startButton.onClick.AddListener(StartGame);

        if (_optionsButton == null)
        {
            _optionsButton = _mainMenu.transform.Find("OptionsButton").GetComponent<Button>();
        }
        _optionsButton.onClick.AddListener(OpenOptionsMenu);

        if (_exitButton == null)
        {
            _exitButton = _mainMenu.transform.Find("ExitButton").GetComponent<Button>();
        }
        _exitButton.onClick.AddListener(QuitGame);

    }


    // Main Menu Functions
    
    private void StartGame()
    {
        _mainMenu.SetActive(false);
        _optionsMenu.SetActive(false);

        SceneManager.LoadSceneAsync("GameplayScene");
    }

    private void OpenOptionsMenu()
    {
        if(_optionsMenu == null) return;

        _mainMenu.SetActive(false);
        _optionsMenu.SetActive(true);
    }

    private void ExitOptionsMenu()
    {
        if (_optionsMenu == null) return;

        _mainMenu.SetActive(true);
        _optionsMenu.SetActive(false);
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}
