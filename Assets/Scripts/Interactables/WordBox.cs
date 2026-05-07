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
    private WordBox(ToolTipProperties? toolTipProps)
    {
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

    public WordBox() : this(null) {}
    public WordBox(in ToolTipProperties props) : this(new ToolTipProperties?(props)) { }
}