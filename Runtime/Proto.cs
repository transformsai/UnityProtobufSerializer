using System;
using Google.Protobuf;
using UnityEngine;

[Serializable]
public class Proto<T> : IProto, ISerializationCallbackReceiver where T : IMessage<T>, new()
{
    public static MessageParser<T> Parser = new MessageParser<T>(() => new T());

    [Tooltip("Using text encoding allows for readable diffs in source control, but introduces a dependency on proto file variable names.")]
    public bool UseBinaryEncoding = true;
    [SerializeField] private byte[] binaryValue;
    [SerializeField] private string textValue;
    public T Value { get; set; }
    [SerializeField] private long protoHash;

    IMessage IProto.Value => Value;

    bool IProto.UseBinaryEncoding
    {
        get => UseBinaryEncoding;
        set => UseBinaryEncoding = value;
    }
    
    public Proto() : this(new T()){}

    public Proto(T value, bool binary = true)
    {
        Value = value;
    }

    public void OnBeforeSerialize()
    {
        binaryValue =  Value.ToByteArray();
        textValue = JsonFormatter.Default.Format(Value);
    }

    public void OnAfterDeserialize()
    {
        if (UseBinaryEncoding)
        {
            try
            {
                Value = Parser.ParseFrom(binaryValue);                
            }
            catch (Exception e)
            {
                Debug.LogError("Error in deserializing proto file. Using text backup.");
                Debug.LogException(e);
                Value = JsonParser.Default.Parse<T>(textValue);
            }
        }
        else
        {
            Value = JsonParser.Default.Parse<T>(textValue);
        }
    }
}

public interface IProto
{
    public IMessage Value { get; }
    public bool UseBinaryEncoding { get; set; }
}

public class CustomProtoDrawerAttribute : Attribute {

    public Type type;

    public CustomProtoDrawerAttribute(Type type) {
        this.type = type;
    }
}
