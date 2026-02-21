using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary> Handles menu opening and closing, as well as input action map toggling </summary>
/// <remarks>
/// NOTE: input action map toggling is a somewhat separate concern;
///   should be refactored out into its own class if more features are added
/// </remarks>
[DisallowMultipleComponent]
public sealed class MenuToggler : MonoBehaviour
{
    public static MenuToggler Instance { get; private set; }

    private InputActionMap uiActionMap;
    private InputActionMap prevActionMap;
    // SideScrolling is the default starting action map for now; could change later

    private InputAction returnAction;

    private InputAction settingsAction;
    private InputAction dictionaryAction;

    [SerializeField] private SettingsMenuEvents settingsMenu;
    [SerializeField] private GameHUDEvents dictionaryMenu;

    private OptionalComponent<UIMenuController> currentMenu = new(); // Starts uninitialized

    public bool IsTransitioning { get; private set; } = false;

    // For caching
    private Rigidbody2D playerRB;
    private Collider2D  playerCollider;

    public void ClearAllMenus()
    {
        Debug.Log("Closing Menu");
        StartCoroutine(ClearAllMenusCoroutine());
    }

    private IEnumerator ClearAllMenusCoroutine()
    {
        if (!currentMenu.TryGet(out UIMenuController menu)) yield break;

        // ignore if still in the process of playing opening/closing animation
        if (IsTransitioning) yield break;
        IsTransitioning = true;

        yield return menu.Close();
        currentMenu.Unset();

        //uiActionMap.Disable();
        prevActionMap.Enable();

        EnableWorldActions();

        PlayerController.Instance.context &= ~PlayerContext.Menu;

        IsTransitioning = false;
    }

    public void UseMenu(UIMenuController menu)
    {
        StartCoroutine(UseMenuCoroutine(menu));
    }

    private IEnumerator UseMenuCoroutine(UIMenuController menu)
    {
        // ignore if still in the process of playing opening/closing animation
        if (IsTransitioning) yield break;

        // Closes active menu
        if (currentMenu.TryGet(out UIMenuController prevMenu))
        {
            // break out of the whole thing if the menu to open is already open
            if (menu == prevMenu) yield break;

            Debug.Log("Closing Previous Menu");
            yield return ClearAllMenusCoroutine();
        }

        Debug.Log("Opening Menu");

        IsTransitioning = true;

        //uiActionMap.Enable();
        prevActionMap.Disable();
        DisableWorldActions();

        PlayerController.Instance.context |= PlayerContext.Menu;

        currentMenu.SetNonNull(menu);
        yield return menu.Open();

        IsTransitioning = false;
    }

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(MenuToggler)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;
    }

    void Start()
    {
        uiActionMap = InputSystem.actions.FindActionMap("UI");
        uiActionMap.Enable();

        // Set initial state of action maps
        InputSystem.actions.Disable();

        // This should always be enabled
        InputSystem.actions.FindActionMap("MenuToggles").Enable();

        // Enable default starting action map ("SideScrolling" for now)
        prevActionMap = InputSystem.actions.FindActionMap("SideScrolling");
        prevActionMap.Enable();

        // Log which action maps are currently enabled
        foreach (var map in InputSystem.actions.actionMaps)
        {
            Debug.Log($"Map: {map.name} is {(map.enabled ? "ACTIVE" : "OFF")}");
        }

        returnAction = InputSystem.actions.FindAction("Return");

        settingsAction   = InputSystem.actions.FindAction("Settings");
        dictionaryAction = InputSystem.actions.FindAction("Dictionary");

        playerRB       = PlayerController.Instance.GetComponent<Rigidbody2D>();
        playerCollider = PlayerController.Instance.GetComponent<Collider2D>();
    }

    void Update()
    {   
        if (returnAction.WasCompletedThisFrame())
        {
            ClearAllMenus();
        }

        if (settingsAction.WasPerformedThisFrame())
        {
            HandleMenuButton(settingsMenu);
        }
        if (dictionaryAction.WasPerformedThisFrame())
        {
            HandleMenuButton(dictionaryMenu);
        }
    }

    private void HandleMenuButton(UIMenuController menu)
    {
        if (currentMenu.TryGet(out UIMenuController prevMenu) && prevMenu == menu)
        {
            // if menu is already open, pressing the button again closes it
            ClearAllMenus();
        }
        else
        {
            UseMenu(menu);
        }
    }

    // Disable box collider to prevent further interaction & freeze position to prevent movement
    private void DisableWorldActions()
    {
        playerRB.constraints  |= RigidbodyConstraints2D.FreezePositionX;
        playerCollider.enabled = false;
    }

    // Restore movement & Re-enable box collider
    private void EnableWorldActions()
    {
        playerRB.constraints  &= ~RigidbodyConstraints2D.FreezePositionX;
        playerCollider.enabled = true;
    }
}