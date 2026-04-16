using System;

using UnityEngine;
using UnityEngine.UIElements;

public struct ToolTipProperties
{
    public Action<PointerEnterEvent>    onPointerEnter;
    public Action<PointerMoveEvent>     onPointerMove;
    public Action<PointerLeaveEvent>    onPointerLeave;
    public Action<DetachFromPanelEvent> onPanelDetach;
}

public sealed class WordBox : Label
{
    private WordBox(VisualTreeAsset asset, ToolTipProperties? toolTipProps)
    {
        Debug.Assert(asset != null);
        asset.CloneTree(this);

        enableRichText       = true;
        parseEscapeSequences = true;
        AddToClassList("dialogue-text");

        if (toolTipProps != null)
        {
            var propsValue = toolTipProps.Value;
            RegisterCallback<PointerEnterEvent>(new(propsValue.onPointerEnter));
            RegisterCallback<PointerMoveEvent>(new(propsValue.onPointerMove));
            RegisterCallback<PointerLeaveEvent>(new(propsValue.onPointerLeave));
            RegisterCallback<DetachFromPanelEvent>(new(propsValue.onPanelDetach));
        }
    }

    public WordBox(VisualTreeAsset asset) : this(asset, null) {}
    public WordBox(VisualTreeAsset asset, in ToolTipProperties props) :
        this(asset, new ToolTipProperties?(props)) { }
}