using System;

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

    private InputAction settingsAction;
    private InputAction dictionaryAction;

    [SerializeField] private SettingsMenuEvents settingsMenu;
    [SerializeField] private GameHUDEvents dictionaryMenu;

    private OptionalComponent<OpenClosable> currentMenu = new(); // Starts uninitialized

    // For caching
    private Rigidbody2D playerRB;
    private Collider2D  playerCollider;

    public void UseMenu(OpenClosable closable)
    {
        Debug.Assert(closable != null);
        // Closes active menu
        if (currentMenu.TryGet(out OpenClosable menu))
        {
            menu.Close();
            uiActionMap.Enable();
            prevActionMap.Disable();

            DisableWorldActions();
        }
        currentMenu.SetNonNull(closable);
        closable.Open();

        PlayerController.Instance.context |= PlayerContext.Menu;

        Debug.Log("Adding menu!");
    }

    public void ClearAllMenus()
    {
        if (currentMenu.TryGet(out OpenClosable menu))
        {
            Debug.Log("Cleared!");
            uiActionMap.Disable();
            prevActionMap.Enable();

            EnableWorldActions();

            menu.Close();
            currentMenu.Unset();
        }

        PlayerController.Instance.context &= ~PlayerContext.Menu;
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

        settingsAction   = InputSystem.actions.FindAction("Settings");
        dictionaryAction = InputSystem.actions.FindAction("Dictionary");

        playerRB       = PlayerController.Instance.GetComponent<Rigidbody2D>();
        playerCollider = PlayerController.Instance.GetComponent<Collider2D>();
    }

    void Update()
    {
        if (settingsAction.WasPerformedThisFrame())
        {
            UseMenu(settingsMenu);
        }
        if (dictionaryAction.WasPerformedThisFrame())
        {
            UseMenu(dictionaryMenu);
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