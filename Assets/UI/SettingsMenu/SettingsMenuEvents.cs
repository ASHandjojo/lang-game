using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuEvents : MonoBehaviour
{
    private UIDocument selfDocument;
    [SerializeField] private UIDocument otherDocument;
    private Button backButton;
    private SoundHandler sh;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    void Awake()
    {
        selfDocument = GetComponent<UIDocument>();
        sh = GetComponent<SoundHandler>();

        // Add events to back button
        backButton = selfDocument.rootVisualElement.Q("BackButton") as Button;
        backButton.RegisterCallback<ClickEvent>(ToggleMenu);
        
        // Add sounds
        backButton.RegisterCallback<ClickEvent>(OnButtonClick);
        backButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        // Begin with settings menu not displayed
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;
    }    

    // Get rid of button events
    void OnDisable()
    {
        backButton.UnregisterCallback<ClickEvent>(ToggleMenu);

        backButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        backButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
        
    }

    // Return to main menu or gameHud
     private void ToggleMenu(ClickEvent e)
    {
        backButton.SetEnabled(false);

        otherDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;

        backButton.SetEnabled(true);

        // If a Player Instance is available (not in main menu), return movement and interactions
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


    // Change .hotkey-image based on assigned Keybind


    // Change dialogue text speed based on slider position 
}
