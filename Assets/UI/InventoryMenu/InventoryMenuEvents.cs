using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class InventoryMenuEvents : UIMenuController
{
    
    private UIDocument document;

    private VisualElement heroImageContainer;
    private List<VisualElement> tabSelectorItems;

    private List<Button> itemSelectorItemIcons;
    int itemSelectorMiddleIndex = 2;

    private VisualElement heroImage;
    private Label itemHeading;
    private Label itemDescription;

    private Button itemSelectorNextButton;
    private Button itemSelectorPrevButton;

    private SoundHandler soundHandler;

    ItemCategory curTab = 0;
    int[] tabItemSelectorIndices = new int[(int) ItemCategory.Count];

    [Header("Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        soundHandler = GetComponent<SoundHandler>();

        heroImageContainer = document.rootVisualElement.Q<VisualElement>(className: "item-hero-image-container");
        tabSelectorItems = document.rootVisualElement.Query<VisualElement>(className: "tab-selector__item-container").ToList();

        itemSelectorItemIcons = document.rootVisualElement.Query<Button>(className: "item-selector__item-icon").ToList();
        itemSelectorMiddleIndex = itemSelectorItemIcons.Count / 2;

        heroImage = document.rootVisualElement.Q<VisualElement>(className: "item-hero-image");
        itemHeading = document.rootVisualElement.Q<Label>(className: "item-text__heading");
        itemDescription = document.rootVisualElement.Q<Label>(className: "item-text__description");

        itemSelectorNextButton = document.rootVisualElement.Q<Button>(className: "item-selector__next-button");
        itemSelectorPrevButton = document.rootVisualElement.Q<Button>(className: "item-selector__prev-button");

        // Begin with settings menu not displayed
        document.rootVisualElement.style.display = DisplayStyle.None;
    }

    void Start()
    {
        SwitchTabTo(curTab);
    }

    void OnEnable()
    {
        //// Add events to back button
        //backButton = document.rootVisualElement.Q("BackButton") as Button;
        //backButton.RegisterCallback<ClickEvent>(e => MenuToggler.Instance.ClearAllMenus());

        //// Add sounds
        //backButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        document.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        int i = 0;
        foreach (VisualElement tabButton in tabSelectorItems)
        {
            int indexCopy = i; // make copy to make lambda capture the correct value
            tabButton.RegisterCallback<ClickEvent>((e) => SwitchTabTo((ItemCategory) indexCopy));
            i++;
        }

        itemSelectorNextButton.RegisterCallback<ClickEvent>((e) => MoveItemSelectorIndexBy(1));
        itemSelectorPrevButton.RegisterCallback<ClickEvent>((e) => MoveItemSelectorIndexBy(-1));

        int offset = -itemSelectorMiddleIndex;
        foreach (Button icon in itemSelectorItemIcons)
        {
            // make copies to make lambda capture the correct values
            int offsetCopy = offset;
            Button iconCopy = icon;

            icon.RegisterCallback<ClickEvent>((e) => {
                if (iconCopy.style.backgroundImage.ToString() != "Null") MoveItemSelectorIndexBy(offsetCopy);
                    // have to convert to string because backgroundImage == null doesn't work for some reason
            });
            offset++;
        }
    }

    // Get rid of button events
    void OnDisable()
    {
        //backButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        //backButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        Debug.Log($"Resized; new size: {evt.newRect.width} x {evt.newRect.height}");

        heroImageContainer.style.minWidth = heroImageContainer.resolvedStyle.height;
        heroImageContainer.style.maxWidth = heroImageContainer.resolvedStyle.height;

        foreach (Button icon in itemSelectorItemIcons)
        {
            icon.style.maxHeight = icon.resolvedStyle.width;
        }
    }

    void SwitchTabTo(ItemCategory tab)
    {
        Debug.Log("Switched to tab {index}");

        tabSelectorItems[(int) curTab].RemoveFromClassList("tab-selector__item-container--selected");

        tabSelectorItems[(int) tab].AddToClassList("tab-selector__item-container--selected");

        curTab = tab;
        SwitchItemSelectorIndexTo(tabItemSelectorIndices[(int)tab]);
    }

    void SwitchItemSelectorIndexTo(int index)
    {
        IReadOnlyList<InventoryItem> category = PlayerController.Instance.PlayerInventory.GetCategoryItems(curTab);

        index = (index % category.Count + category.Count) % category.Count;
            // formula ensures wrapping works for negative numbers

        int i = index - itemSelectorMiddleIndex;
        foreach (Button icon in itemSelectorItemIcons)
        {
            if (i < 0 || i >= category.Count)
            {
                icon.style.backgroundImage = null;
            }
            else
            {
                InventoryItem item = category[i];
                icon.style.backgroundImage = Background.FromSprite(ItemIcons.Instance.Icons[item]);
            }

            i++;
        }

        SwitchDisplayedItemTo(category[index]);

        tabItemSelectorIndices[(int) curTab] = index;
    }

    void MoveItemSelectorIndexBy(int amount)
    {
        SwitchItemSelectorIndexTo(tabItemSelectorIndices[(int)curTab] + amount);
    }

    void SwitchDisplayedItemTo(InventoryItem item)
    {
        ItemInfo info = Inventory.ItemInfoTable[item];

        itemHeading.text = info.Name;
        itemDescription.text = info.Description;

        Sprite heroImageSprite = Resources.Load<Sprite>("ItemHeroImages/" + info.HeroImageFilename);
        heroImage.style.backgroundImage = Background.FromSprite(heroImageSprite);
    }

    public override IEnumerator Open()
    {
        document.rootVisualElement.style.display = DisplayStyle.Flex;
        yield break;
    }

    // Return to main menu or gameHud
    public override IEnumerator Close()
    {
        //backButton.SetEnabled(false);
        document.rootVisualElement.style.display = DisplayStyle.None;

        //backButton.SetEnabled(true);

        yield break;
    }


    // Play sound when a button is clicked
    private void OnButtonClick(ClickEvent e)
    {
        soundHandler.PlaySoundUI(selectionClip);
        MenuToggler.Instance.UseMenu(this);
    }

    // Play sound when cursor is over a button
    private void OnButtonHover(MouseEnterEvent e)
    {
        soundHandler.PlaySoundUI(hoverClip);
    }

    
}
