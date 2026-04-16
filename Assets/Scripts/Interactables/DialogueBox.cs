using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

public sealed class DialogueBox : VisualElement
{
    public struct DictionaryData
    {
        public Label dictWords;
        public Label dictNotes;
    }

    public struct CharacterData
    {
        public string    name;
        public Texture2D image;
    }

    private float textSpeed;
    private float bounceHeight = 30.0f;
    private float bounceSpeed  = 30.0f;
    protected IVisualElementScheduledItem bounceSchedule;

    private DictionaryData dictData;

    private Label         npcName;
    private VisualElement npcImage;

    private VisualElement textContainer, wordTooltip, nextLinePrompt;
    private WordBox hoverPrefab, noHoverPrefab;

    private StyleColor originalColor;

    public DialogueBox(VisualTreeAsset asset, in DictionaryData dictData, in CharacterData charData)
    {
        Debug.Assert(asset != null);
        asset.CloneTree(this);

        Debug.Assert(dictData.dictWords != null);
        Debug.Assert(dictData.dictNotes != null);

        this.dictData = dictData;

        npcName  = this.Q<Label>("NpcName");
        npcImage = this.Q<VisualElement>("NpcImage");
        Debug.Assert(npcName  != null);
        Debug.Assert(npcImage != null);

        npcName.text                   = charData.name;
        npcImage.style.backgroundImage = charData.image;

        wordTooltip    = this.Q("WordTooltip");
        textContainer  = this.Q("TextContainer");
        nextLinePrompt = this.Q("NextLinePrompt");
        Debug.Assert(wordTooltip    != null);
        Debug.Assert(textContainer  != null);
        Debug.Assert(nextLinePrompt != null);

        hoverPrefab   = new WordBox(asset, MetaHover());
        noHoverPrefab = new WordBox(asset);

        originalColor = style.color;
    }

    // Iteration Methods
    public void Display(in DialogueEntry entry)
    {

    }

    // Animation Methods
    private void StartBounce(float startTime)
    {
        nextLinePrompt.visible = true;

        bounceSchedule = nextLinePrompt.schedule.Execute(() =>
        {
            // Animate up and down movement
            float t = Time.time - startTime;

            float yOffset = Mathf.PingPong(t * bounceSpeed, bounceHeight);
            nextLinePrompt.style.translate = new Translate(0.0f, yOffset);

            // Animate size increase-decrease
            float normalizedT = yOffset / bounceHeight;
            float scaleFactor = Mathf.Lerp(1.3f, 1.0f, normalizedT);
            // float scaleFactor = Mathf.Lerp(0.7f, 1.3f, normalizedT);
            nextLinePrompt.transform.scale = new Vector3(scaleFactor, scaleFactor, 1.0f);
        }).Every(16);
    }

    private void StopBounce()
    {
        nextLinePrompt.visible = false;
        bounceSchedule?.Pause();

        // Reset position and scale
        nextLinePrompt.style.translate = new Translate(0.0f, 0.0f);
        nextLinePrompt.transform.scale = Vector3.one;
    }

    // Dictionary Call (for per-word tooltip)
    private string GetPlayerNotes(string word)
    {
        PlayerController player = PlayerController.Instance;
        Dictionary dictionary = player.dictionary;

        foreach (DictionaryEntry entry in dictionary.dictionaryList)
        {
            if (LanguageTable.PhoneticProcessor.Translate(entry.Word) == word)
            {
                if (entry.Notes.Length == 0)
                {
                    return "No Notes Available For This Word";
                }
                return entry.Notes;
            }
        }

        return "No Notes Available For This Word";
    }

    // Callback Impls
    private void ShowTooltip(string name, Vector2 mousePosition, StyleFontDefinition font)
    {
        dictData.dictWords.text = name;
        dictData.dictWords.style.unityFontDefinition = font;

        dictData.dictNotes.text = GetPlayerNotes(name);

        MoveTooltip(mousePosition);
        wordTooltip.style.display = DisplayStyle.Flex;
    }

    private void MoveTooltip(Vector2 mousePosition)
    {
        float offsetX = 12.0f;
        float offsetY = 12.0f;

        wordTooltip.style.left = mousePosition.x + offsetX;
        wordTooltip.style.top  = mousePosition.y + offsetY;
    }

    private void HideTooltip() => wordTooltip.style.display = DisplayStyle.None;

    private ToolTipProperties MetaHover() => new()
    {
        onPointerEnter = (evt) =>
        {
            Label target = (Label)evt.target;
            target.style.color = new StyleColor(Color.red);
            ShowTooltip(name, evt.position, target.style.unityFontDefinition);
        },
        onPointerMove = (evt) => MoveTooltip(evt.position),
        onPointerLeave = (evt) =>
        {
            Label target = (Label)evt.target;
            target.style.color = originalColor;
            HideTooltip();
        },
        onPanelDetach = (evt) => HideTooltip()
    };
}