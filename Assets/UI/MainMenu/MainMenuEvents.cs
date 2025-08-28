using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument), typeof(SoundHandler))]
public sealed class MainMenuEvents : MonoBehaviour
{
    private UIDocument selfDocument;
    [SerializeField] private UIDocument otherDocument;
    private Button startButton;
    private Button settingsButton;
    private Button exitButton;

    private List<Button> buttonList;

    private SceneLoader  sl;
    private SoundHandler sh;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    void Awake()
    {
        sl           = FindFirstObjectByType<SceneLoader>();
        selfDocument = GetComponent<UIDocument>();
        sh           = GetComponent<SoundHandler>();

        // Add click events to all buttons
        startButton = selfDocument.rootVisualElement.Q("StartButton") as Button;
        startButton.RegisterCallback<ClickEvent>(StartGame);
        settingsButton = selfDocument.rootVisualElement.Q("SettingsButton") as Button;
        settingsButton.RegisterCallback<ClickEvent>(ToggleSettings);
        exitButton = selfDocument.rootVisualElement.Q("ExitButton") as Button;
        exitButton.RegisterCallback<ClickEvent>(ExitGame);

        // Add sounds to all buttons
        buttonList = selfDocument.rootVisualElement.Query<Button>().ToList();
        for(int i = 0; i < buttonList.Count; i++)
        {
            buttonList[i].RegisterCallback<ClickEvent>(OnButtonClick);
            buttonList[i].RegisterCallback<MouseEnterEvent>(OnButtonHover);
        }
    }

    private void StartGame(ClickEvent e)
    {
        startButton.SetEnabled(false);
        settingsButton.SetEnabled(false);
        exitButton.SetEnabled(false);

        sl.LoadNextLevel();
    }

    // Switch between menus
    private void ToggleSettings(ClickEvent e)
    {
        otherDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        selfDocument.rootVisualElement.style.display  = DisplayStyle.None;
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
