using System;

using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public struct DialogueEntry
{
    [Serializable]
    public sealed class ResponseData
    {
        public string expectedInput;
    }

    public string line;
    public OptionalComponent<AudioClip> sound;

    public bool hasResponse;
    public ResponseData responseData;
}