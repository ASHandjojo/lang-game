using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuEvents : MonoBehaviour
{
    private UIDocument _selfDocument;
    [SerializeField] private UIDocument _otherDocument;
    private Button _startButton;
    private Button _settingsButton;
    private Button _exitButton;

    private List<Button> _buttonList = new List<Button>();

    private SceneLoader sl;
    private SoundHandler sh;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    void Awake()
    {
        sl = FindFirstObjectByType<SceneLoader>();
        _selfDocument = GetComponent<UIDocument>();
        sh = GetComponent<SoundHandler>();

        // Add click events to all buttons
        _startButton = _selfDocument.rootVisualElement.Q("StartButton") as Button;
        _startButton.RegisterCallback<ClickEvent>(StartGame);
        _settingsButton = _selfDocument.rootVisualElement.Q("SettingsButton") as Button;
        _settingsButton.RegisterCallback<ClickEvent>(ToggleSettings);
        _exitButton = _selfDocument.rootVisualElement.Q("ExitButton") as Button;
        _exitButton.RegisterCallback<ClickEvent>(ExitGame);

        // Add sounds to all buttons
        _buttonList = _selfDocument.rootVisualElement.Query<Button>().ToList();
        for(int i = 0; i < _buttonList.Count; i++)
        {
            _buttonList[i].RegisterCallback<ClickEvent>(OnButtonClick);
            _buttonList[i].RegisterCallback<MouseEnterEvent>(OnButtonHover);
        }
    }

    // Get rid of button events
    void OnDisable()
    {
        _startButton.UnregisterCallback<ClickEvent>(StartGame);
        _settingsButton.UnregisterCallback<ClickEvent>(ToggleSettings);
        _exitButton.UnregisterCallback<ClickEvent>(ExitGame);
        
        for(int i = 0; i < _buttonList.Count; i++)
        {
            _buttonList[i].UnregisterCallback<ClickEvent>(OnButtonClick);
            _buttonList[i].UnregisterCallback<MouseEnterEvent>(OnButtonHover);
        }
    }

    private void StartGame(ClickEvent e)
    {
        _startButton.SetEnabled(false);
        _settingsButton.SetEnabled(false);
        _exitButton.SetEnabled(false);

        sl.LoadNextLevel();
    }

    // Switch between menus
     private void ToggleSettings(ClickEvent e)
    {
        _otherDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        _selfDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    // Exit to desktop
    private void ExitGame(ClickEvent e)
    {
        Debug.Log("Exit game.");
        Application.Quit();
    }

    // Play sound when a button is clicked
    private void OnButtonClick(ClickEvent e)
    {
        sh.PlaySoundUI(selectionClip);
    }

    // Play sound when cursor is over a button
    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }
    
}
