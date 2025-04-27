using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuEvents : MonoBehaviour
{
    private UIDocument _document;
    private Button _startButton;
    private Button _settingsButton;
    private Button _exitButton;

    private List<Button> _buttonList = new List<Button>();

    private SceneLoader sl;
    private SoundHandler sh;
    [SerializeField] public AudioClip hoverClip;
    [SerializeField] public AudioClip selectionClip;

    void Awake()
    {
        sl = FindFirstObjectByType<SceneLoader>();
        _document = GetComponent<UIDocument>();
        sh = GetComponent<SoundHandler>();

        // Add click events to all buttons
        _startButton = _document.rootVisualElement.Q("StartButton") as Button;
        _startButton.RegisterCallback<ClickEvent>(StartGame);
        _settingsButton = _document.rootVisualElement.Q("SettingsButton") as Button;
        _settingsButton.RegisterCallback<ClickEvent>(OpenSettings);
        _exitButton = _document.rootVisualElement.Q("ExitButton") as Button;
        _exitButton.RegisterCallback<ClickEvent>(ExitGame);

        // Add sounds to all buttons
        _buttonList = _document.rootVisualElement.Query<Button>().ToList();
        for(int i = 0; i < _buttonList.Count; i++)
        {
            _buttonList[i].RegisterCallback<ClickEvent>(OnButtonClick);
            _buttonList[i].RegisterCallback<MouseEnterEvent>(OnButtonHover);
        }
    }

    void OnDisable()
    {
        _startButton.UnregisterCallback<ClickEvent>(StartGame);
        _settingsButton.UnregisterCallback<ClickEvent>(OpenSettings);
        _exitButton.UnregisterCallback<ClickEvent>(ExitGame);
        
        for(int i = 0; i < _buttonList.Count; i++)
        {
            _buttonList[i].UnregisterCallback<ClickEvent>(OnButtonClick);
            _buttonList[i].UnregisterCallback<MouseEnterEvent>(OnButtonHover);
        }
    }

    private void StartGame(ClickEvent e)
    {
        sl.LoadNextLevel();
    }

     private void OpenSettings(ClickEvent e)
    {
        Debug.Log("Open Settings.");
    }


    private void ExitGame(ClickEvent e)
    {
        Debug.Log("Exit game.");
        Application.Quit();
    }

    private void OnButtonClick(ClickEvent e)
    {
        sh.PlaySoundUI(selectionClip);
    }

    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }

    

    
}
