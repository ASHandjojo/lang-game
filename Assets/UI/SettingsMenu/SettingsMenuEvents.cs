using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuEvents : MonoBehaviour
{
    private UIDocument _selfDocument;
    [SerializeField] private UIDocument _otherDocument;
    private Button _backButton;
    private SoundHandler sh;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    void Awake()
    {
        _selfDocument = GetComponent<UIDocument>();
        sh = GetComponent<SoundHandler>();

        // Add events to back button
        _backButton = _selfDocument.rootVisualElement.Q("BackButton") as Button;
        _backButton.RegisterCallback<ClickEvent>(ToggleMainMenu);
        
        // Add sounds
        _backButton.RegisterCallback<ClickEvent>(OnButtonClick);
        _backButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        // Begin with settings menu not displayed
        _selfDocument.rootVisualElement.style.display = DisplayStyle.None;
    }    

    // Get rid of button events
    void OnDisable()
    {
        _backButton.UnregisterCallback<ClickEvent>(ToggleMainMenu);

        _backButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        _backButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
        
    }

    // Return to main menu
     private void ToggleMainMenu(ClickEvent e)
    {
        _backButton.SetEnabled(false);

        _otherDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        _selfDocument.rootVisualElement.style.display = DisplayStyle.None;
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
