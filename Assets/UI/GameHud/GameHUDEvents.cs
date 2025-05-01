using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class GameHUDEvents : MonoBehaviour
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

    
    void Awake()
    {
        selfDocument = GetComponent<UIDocument>();
        hudContainer = selfDocument.rootVisualElement.Q("ScreenContainer");

        dictionaryContainer = selfDocument.rootVisualElement.Q("DictionaryContainer");
        dictionary = selfDocument.rootVisualElement.Q("Dictionary");

        // Add events to all buttons
        dictionaryButton = selfDocument.rootVisualElement.Q("DictionaryButton") as Button;
        dictionaryButton.RegisterCallback<ClickEvent>(OpenDictionary);
        dictionaryButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        settingsButton = selfDocument.rootVisualElement.Q("SettingsButton") as Button;
        settingsButton.RegisterCallback<ClickEvent>(OpenSettings);
        settingsButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);


        backButton = selfDocument.rootVisualElement.Q("BackButton") as Button;
        backButton.RegisterCallback<ClickEvent>(CloseDictionary);

        sh = GetComponent<SoundHandler>();

        dictionaryContainer.visible = false;
    }

    void OnDisable()
    {
        // Remove click events from all buttons
        dictionaryButton.UnregisterCallback<ClickEvent>(OpenDictionary);
        dictionaryButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
        settingsButton.UnregisterCallback<ClickEvent>(OpenSettings);
        settingsButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
        backButton.UnregisterCallback<ClickEvent>(CloseDictionary);
    }

    private void OpenDictionary(ClickEvent e)
    {
        StartCoroutine(EnterDictionary(dictionary, openImage));
    }

    private void OpenSettings(ClickEvent e)
    {
        // Disable Player movement/Interactions

        // Change the Display
        settingsDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void CloseDictionary(ClickEvent e)
    {
        StartCoroutine(ExitDictionary(dictionary, closedImage));
    }


    IEnumerator EnterDictionary(VisualElement dictionary, Texture2D closedImage)
    {
        dictionaryContainer.style.backgroundColor = new StyleColor(new Color(165,0,0, 0.85f));
        // Disable box collider to prevent interactions & freeze position to prevent movement
        PlayerController.Instance.GetComponent<BoxCollider2D>().enabled = false;
        PlayerController.Instance.GetComponent<Rigidbody2D>().constraints |= RigidbodyConstraints2D.FreezePositionX;
        
        // Enable Dictionary elements, Disable HUD
        dictionaryContainer.visible = true;
        hudContainer.visible = false;

        // Enter the screen
        sh.PlaySoundUI(openClip);
        yield return Translate(dictionary, backButton, -1500f, 0f, 1f);

        // Play transition animations
        yield return Fade(dictionary, 1f, 0f, 0.45f);
        dictionary.style.backgroundImage = new StyleBackground(openImage);
        yield return Fade(dictionary, 0f, 1f, 1.5f);

        backButton.SetEnabled(true);  
    }

    IEnumerator ExitDictionary(VisualElement dictionary, Texture2D closedImage)
    {
        backButton.SetEnabled(false);

        // Play transition animations
        sh.PlaySoundUI(closeClip);
        yield return Fade(dictionary, 1f, 0f, 0.45f);
        dictionary.style.backgroundImage = new StyleBackground(closedImage);
        yield return Fade(dictionary, 0f, 1f, 1.5f);


        // Leave the screen
        yield return Translate(dictionary, backButton, 0f, -1500f, 1f);

        // Disable Dictionary elements, Re-enable HUD
        dictionaryContainer.style.backgroundColor = new StyleColor(new Color(165,0,0, 0f));
        dictionaryContainer.visible = false;
        hudContainer.visible = true;
        
        // Restore movement & Re-enable box collider
        PlayerController.Instance.GetComponent<Rigidbody2D>().constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        PlayerController.Instance.GetComponent<BoxCollider2D>().enabled = true;
    }


    // Fade the alpha style property of a visual element
    IEnumerator Fade(VisualElement dict, float start, float end, float duration)
    {
        float timeElapsed = 0;

        while (duration > timeElapsed)
        {
            float t = (timeElapsed) / duration;
            float eased = Mathf.SmoothStep(start, end, t);
            dict.style.opacity = eased;
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        dict.style.opacity = end;

    }

    // Translate the left style property of a visual element and button
    IEnumerator Translate(VisualElement dict, Button btn, float start, float end, float duration)
    {
        float buttonStart = -start;
        float buttonEnd = -end;
        

        float timeElapsed = 0;

        while (duration > timeElapsed)
            {
                float t = (timeElapsed) / duration;

                float easedDictionary = Mathf.SmoothStep(start, end, t);
                float easedButton = Mathf.SmoothStep(buttonStart, buttonEnd, t);

                dict.style.left = easedDictionary;
                btn.style.left = easedButton;
                timeElapsed += Time.deltaTime;

                yield return null;
            }

            dict.style.left = end;
            btn.style.left = buttonEnd;
    }

    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }
}
