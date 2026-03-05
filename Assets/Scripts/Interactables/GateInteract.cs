using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class GateInteract : Interactable
{
    private UIDocument document;
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private SceneLoader sceneLoader;

    // private bool isZoomed;
    private float timeElapsed;

    private Vector3 cameraPos;

    protected override void Awake()
    {
        base.Awake();
        document        = GetComponent<UIDocument>();
        worldPromptIcon = GetComponentsInChildren<SpriteRenderer>(true)[1];
        //keybindIcon     = Keybinds.Instance.getKeyImage(Keybinds.Instance.getIntersKey());
        soundHandler    = GetComponent<SoundHandler>();

        // Initialize UI images, while hiding UI screen until interacted with
        // document.rootVisualElement.Q("ViewImage").style.backgroundImage   = new StyleBackground(zoomImage);
        //document.rootVisualElement.Q("PromptImage").style.backgroundImage = new StyleBackground(keybindIcon);

        // document.rootVisualElement.style.visibility = Visibility.Hidden;
        // document.rootVisualElement.style.display    = DisplayStyle.None;
        // isZoomed = false;
    }

    // Activates next scene loading trigger
    protected override IEnumerator InteractLogic(PlayerController player)
    {
        sceneLoader.LoadNextLevel();
        yield return null;
        // yield break;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        InputSystem.onActionChange += HandleActionChange;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        InputSystem.onActionChange -= HandleActionChange;
    }

    private void HandleActionChange(object actionOrMap, InputActionChange change)
    {
        // ignore if not a rebinding update
        if (change != InputActionChange.BoundControlsChanged) return;
        UpdateRebindBindingIcon();
    }
    private void UpdateRebindBindingIcon()
    {
        string controlPath = SettingsMenuEvents.RebindableInputPaths[worldPromptInput]
            .GetBinding().effectivePath;

        if (MenuToggler.BindingIcons.Icons.TryGetValue(controlPath, out Sprite iconSprite))
        {
            Debug.Log($"Prompt Image: {document.rootVisualElement.Q("PromptImage")}");
            document.rootVisualElement.Q("PromptImage").style.backgroundImage
                = Background.FromSprite(iconSprite);
        }
        else
        {
            Debug.LogWarning("No icon found for control path '" + controlPath + "'");
        }
    }
}