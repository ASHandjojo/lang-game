using UnityEngine;

/// <summary>
/// Avoids expensive null checks for Unity objects. Only checks on assignment.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class OptionalComponent<T> where T : Object
{
    private T obj;
    private bool isInitialized = false;

    private OptionalComponent() { }

    public OptionalComponent(T obj) => SetObject(obj);

    public void SetObject(T objIn)
    {
        obj = objIn;
        isInitialized = objIn != null;
    }

    public bool TryGet(out T objectOut)
    {
        objectOut = obj;
        return isInitialized;
    }

    public static implicit operator OptionalComponent<T>(T obj)
    {
        return new OptionalComponent<T>(obj);
    }
}