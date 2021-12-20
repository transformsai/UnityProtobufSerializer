using System;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.Serialization;


namespace TransformsAI.Unity.Protobuf
{
    [Serializable]
    public class Proto<T> : IProto, ISerializationCallbackReceiver where T : IMessage<T>, new()
    {
        public static MessageParser<T> Parser = new MessageParser<T>(() => new T());

        
        [Tooltip("Using text encoding allows for readable diffs in source control, but introduces a dependency on proto file variable names.")]
        public ProtoFormat EncodingFormat;

        [SerializeField, Obsolete] private byte[] binaryValue;
        [SerializeField] private string BinaryValue;
        [SerializeField, FormerlySerializedAs("textValue")] private string TextValue;
        public T Value { get; set; }

        public Proto() : this(new T()) { }

        public Proto(T value, ProtoFormat format = ProtoFormat.BinaryWithFallback)
        {
            Value = value;
            EncodingFormat = format;
        }

        public void OnBeforeSerialize()
        {
            if (binaryValue != null && binaryValue.Length > 0)
            {
                BinaryValue = Convert.ToBase64String(binaryValue);
                binaryValue = null;
            }
            switch (EncodingFormat)
            {
                case ProtoFormat.BinaryWithFallback:
                case ProtoFormat.TextWithFallback:
                    BinaryValue = Convert.ToBase64String(Value.ToByteArray());
                    TextValue = JsonFormatter.Default.Format(Value);
                    break;
                case ProtoFormat.Binary:
                    BinaryValue = Convert.ToBase64String(Value.ToByteArray());
                    TextValue = null;
                    break;
                case ProtoFormat.Text:
                    BinaryValue = null;
                    TextValue = JsonFormatter.Default.Format(Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnAfterDeserialize()
        {
            if (binaryValue != null && binaryValue.Length > 0)
            {
                BinaryValue = Convert.ToBase64String(binaryValue);
                binaryValue = null;
            }
            // If we only have one type of data, use that to update the value
            if (string.IsNullOrEmpty(TextValue) && !string.IsNullOrEmpty(BinaryValue))
            {
                Value = Parser.ParseFrom(Convert.FromBase64String(BinaryValue));
                return;
            }
            if (string.IsNullOrEmpty(BinaryValue) && !string.IsNullOrEmpty(TextValue))
            {
                Value = JsonParser.Default.Parse<T>(TextValue);
                return;
            }

            
            switch (EncodingFormat)
            {
                case ProtoFormat.TextWithFallback:
                case ProtoFormat.Text:
                    try
                    {
                        Value = JsonParser.Default.Parse<T>(TextValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in deserializing proto file. Using text backup.");
                        Debug.LogException(e);
                        Value = Parser.ParseFrom(Convert.FromBase64String(BinaryValue));
                    }
                    break;
                case ProtoFormat.Binary:
                case ProtoFormat.BinaryWithFallback:
                    try
                    {
                        Value = Parser.ParseFrom(Convert.FromBase64String(BinaryValue));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in deserializing proto file. Using text backup.");
                        Debug.LogException(e);
                        Value = JsonParser.Default.Parse<T>(TextValue);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        IMessage IProto.Value => Value;
        ProtoFormat IProto.EncodingFormat
        {
            get => EncodingFormat;
            set => EncodingFormat = value;
        }
    }

    public interface IProto
    {
        public IMessage Value { get; }
        public ProtoFormat EncodingFormat { get; set; }
    }
}