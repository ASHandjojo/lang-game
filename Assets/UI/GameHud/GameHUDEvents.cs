using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class GameHUDEvents : UIMenuController
{
    [Header("Audio")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip hoverClip;
    private SoundHandler sh;

    [Header("Sprites")]
    [SerializeField] private Texture2D closedImage;
    [SerializeField] private Texture2D openImage;

    private UIDocument selfDocument;
    [SerializeField] private UIDocument settingsDocument;

    private VisualElement hudContainer;
    private VisualElement dictionaryContainer;
    private VisualElement dictionary;
    private Button dictionaryButton;
    private Button settingsButton;

    private Button backButton;

    public override IEnumerator Open()
    {
        yield return EnterDictionary(dictionary, openImage);
        PlayerController.Instance.context |= PlayerContext.InDictionary;
    }

    public override IEnumerator Close() 
    {
        PlayerController.Instance.context &= ~PlayerContext.InDictionary;
        yield return ExitDictionary(dictionary, closedImage);
    }

    void Awake()
    {
        selfDocument = GetComponent<UIDocument>();
        sh           = GetComponent<SoundHandler>();

        hudContainer = selfDocument.rootVisualElement.Q("ScreenContainer");

        dictionaryContainer = selfDocument.rootVisualElement.Q("DictionaryContainer");
        dictionary = selfDocument.rootVisualElement.Q("Dictionary");

        // Add events to all buttons
        dictionaryButton = selfDocument.rootVisualElement.Q<Button>("DictionaryButton");
        dictionaryButton.RegisterCallback<ClickEvent>((e) => MenuToggler.Instance.UseMenu(this));
        dictionaryButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);
        // Back button for dictionary
        backButton = selfDocument.rootVisualElement.Q<Button>("BackButton");
        backButton.RegisterCallback<ClickEvent>((e) => MenuToggler.Instance.ClearAllMenus());

        var settingsComponent = settingsDocument.gameObject.GetComponent<SettingsMenuEvents>();
        settingsButton = selfDocument.rootVisualElement.Q<Button>("SettingsButton");
        settingsButton.RegisterCallback<ClickEvent>((e) => MenuToggler.Instance.UseMenu(settingsComponent));
        settingsButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        dictionaryContainer.visible = false;
    }

    //public void OpenSettings(ClickEvent e) => OpenSettings();

    private IEnumerator EnterDictionary(VisualElement dictionary, Texture2D closedImage)
    {
        dictionaryContainer.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.08f, 0.8f));
        // Disable box collider to prevent interactions & freeze position to prevent movement
        //DisableWorldActions();
        
        // Enable Dictionary elements, Disable HUD
        dictionaryContainer.visible = true;
        hudContainer.visible        = false;

        // Enter the screen
        sh.PlaySoundUI(openClip);
        yield return Translate(dictionary, backButton, -1500.0f, -350.0f, 1.0f);
        // Play transition animations
        yield return Fade(dictionary, 1.0f, 0.0f, 0.45f);
        dictionary.style.backgroundImage = new StyleBackground(openImage);
        yield return Fade(dictionary, 0.0f, 1.0f, 1.5f);

        backButton.SetEnabled(true);  
    }

    private IEnumerator ExitDictionary(VisualElement dictionary, Texture2D closedImage)
    {
        backButton.SetEnabled(false);

        // Play transition animations
        sh.PlaySoundUI(closeClip);
        yield return Fade(dictionary, 1.0f, 0.0f, 0.45f);
        dictionary.style.backgroundImage = new StyleBackground(closedImage);
        yield return Fade(dictionary, 0.0f, 1.0f, 1.5f);

        // Leave the screen
        yield return Translate(dictionary, backButton, -350f, -1500f, 1f);

        // Disable Dictionary elements, Re-enable HUD
        dictionaryContainer.style.backgroundColor = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0f));
        dictionaryContainer.visible = false;
        hudContainer.visible        = true;
        
        // Restore movement, Re-enable box collider, listen for menu keys
        //EnableWorldActions();
    }

    // Fade the alpha style property of a visual element
    private IEnumerator Fade(VisualElement dict, float start, float end, float duration)
    {
        float timeElapsed = 0.0f;
        while (duration > timeElapsed)
        {
            float t      = timeElapsed / duration;
            float eased  = Mathf.SmoothStep(start, end, t);
            timeElapsed += Time.deltaTime;

            dict.style.opacity = eased;
            yield return null;
        }
        dict.style.opacity = end;
    }

    // Translate the left style property of a visual element and button
    private IEnumerator Translate(VisualElement dict, Button btn, float start, float end, float duration)
    {
        float buttonStart;
        float buttonEnd;

        if (start == -1500)
        {
            buttonStart = -start;
            buttonEnd   = 0.0f;
        }
        else
        {
            buttonStart = 0.0f;
            buttonEnd   = -end;
        }

        float timeElapsed = 0.0f;
        while (duration > timeElapsed)
        {
            float t = timeElapsed / duration;

            float easedDictionary = Mathf.SmoothStep(start, end, t);
            float easedButton     = Mathf.SmoothStep(buttonStart, buttonEnd, t);

            dict.style.left = easedDictionary;
            btn.style.left  = easedButton;
            timeElapsed    += Time.deltaTime;

            yield return null;
        }

        dict.style.left = end;
        btn.style.left  = buttonEnd;
    }

    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }
}