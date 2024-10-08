﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

/// <summary>
/// A dictionary that can be serialized by Unity.
///
/// How to use it:
///
/// For primitive keys and values:
///
/// Create a SerializableDictionary subclass
///  [System.Serializable]
///  public class StringStringDictionary : SerializableDictionary<string, string>  {}
///
/// Declare attributes of that type that are to be serialized as serialized field as follows:
///  [UnityEngine.SerializeField]
///  private StringStringDictionary stringDict = new StringStringDictionary();
///
/// For types that are not serialized by Unity directly:
///
///   E.g., to create a serializable dictionary of type <string, List<Color>>:
///
///   Create a SerializableDictionary.Storage subclass to hold the list
///     [Serializable]
///     public class ColorListStorage : SerializableDictionary.Storage<List<Color>> {}
///
///   Create a SerializableDictionary subclass using the previous subclass
///     [Serializable]
///     public class StringColorListDictionary : SerializableDictionary<string, List<Color>, ColorListStorage> {}
///
/// Note: This code stems from https://assetstore.unity.com/packages/tools/integration/serializabledictionary-90477
/// and was published under the MIT license.
///
/// </summary>
/// <typeparam name="TKey">key of the dictionary</typeparam>
/// <typeparam name="TValue">value of the dictionary</typeparam>
/// <typeparam name="TValueStorage">call back to Unity's serialization receiver</typeparam>
public abstract class SerializableDictionaryBase<TKey, TValue, TValueStorage> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private TKey[] keys;
    [SerializeField]
    private TValueStorage[] values;

    public SerializableDictionaryBase()
    {
    }

    public SerializableDictionaryBase(IDictionary<TKey, TValue> dict) : base(dict.Count)
    {
        foreach (KeyValuePair<TKey, TValue> kvp in dict)
        {
            this[kvp.Key] = kvp.Value;
        }
    }

    protected SerializableDictionaryBase(SerializationInfo info, StreamingContext context) : base(info, context) { }

    protected abstract void SetValue(TValueStorage[] storage, int i, TValue value);
    protected abstract TValue GetValue(TValueStorage[] storage, int i);

    public void CopyFrom(IDictionary<TKey, TValue> dict)
    {
        Clear();
        foreach (KeyValuePair<TKey, TValue> kvp in dict)
        {
            this[kvp.Key] = kvp.Value;
        }
    }

    public void OnAfterDeserialize()
    {
        if (keys != null && values != null && keys.Length == values.Length)
        {
            Clear();
            int n = keys.Length;
            for (int i = 0; i < n; ++i)
            {
                this[keys[i]] = GetValue(values, i);
            }

            keys = null;
            values = null;
        }

    }

    public void OnBeforeSerialize()
    {
        int n = Count;
        keys = new TKey[n];
        values = new TValueStorage[n];

        int i = 0;
        foreach (KeyValuePair<TKey, TValue> kvp in this)
        {
            keys[i] = kvp.Key;
            SetValue(values, i, kvp.Value);
            ++i;
        }
    }
}

public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase<TKey, TValue, TValue>
{
    public SerializableDictionary()
    {
    }

    public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict)
    {
    }

    protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

    protected override TValue GetValue(TValue[] storage, int i)
    {
        return storage[i];
    }

    protected override void SetValue(TValue[] storage, int i, TValue value)
    {
        storage[i] = value;
    }
}

public static class SerializableDictionary
{
    public class Storage<T>
    {
        public T Data;
    }
}

public class SerializableDictionary<TKey, TValue, TValueStorage> : SerializableDictionaryBase<TKey, TValue, TValueStorage> where TValueStorage : SerializableDictionary.Storage<TValue>, new()
{
    public SerializableDictionary()
    {
    }

    public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict)
    {
    }

    protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

    protected override TValue GetValue(TValueStorage[] storage, int i)
    {
        return storage[i].Data;
    }

    protected override void SetValue(TValueStorage[] storage, int i, TValue value)
    {
        storage[i] = new TValueStorage();
        storage[i].Data = value;
    }
}
