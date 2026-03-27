using System;

using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public sealed class EncodingEntry : IEquatable<EncodingEntry>, IEquatable<string>
{
    public string phoneticsStr;
    public string line; // For unicode

    public bool Equals(string rhs)          => line.Equals(rhs);
    public bool Equals(EncodingEntry rhs)   => line == rhs?.line;
    public override bool Equals(object obj) => obj is EncodingEntry rhs && Equals(rhs);

    public override int GetHashCode() => line.GetHashCode();

    public override string ToString() => line;

    public static implicit operator string(EncodingEntry obj) => obj?.line;

    public static bool operator==(EncodingEntry lhs, EncodingEntry rhs)
    {
        if (lhs is null)
        {
            return rhs is null;
        }
        return lhs.Equals(rhs);
    }
    public static bool operator!=(EncodingEntry lhs, EncodingEntry rhs) => !(lhs == rhs);

    public static bool operator==(EncodingEntry lhs, string rhs) => lhs.Equals(rhs);
    public static bool operator!=(EncodingEntry lhs, string rhs) => !lhs.Equals(rhs);

    public static bool operator==(string lhs, EncodingEntry rhs) => rhs.Equals(lhs);
    public static bool operator!=(string lhs, EncodingEntry rhs) => !rhs.Equals(lhs);
}

[Serializable]
public struct DialogueEntry
{
    public EncodingEntry line;
    public OptionalComponent<AudioClip> sound;

    public bool hasResponse;
    public EncodingEntry responseData;
}