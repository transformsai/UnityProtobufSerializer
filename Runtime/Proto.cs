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
        private int _lastHash;
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
            // We know the hashcode of IMessage<T> is semantically valid
            // because IMessage<T> implements IEquatable<T>.
            var hash = Value?.GetHashCode();
            if(hash == _lastHash) return;
            _lastHash = hash ?? 0;

#pragma warning disable CS0612
            // legacy
            if (binaryValue != null && binaryValue.Length > 0)
            {
                BinaryValue = Convert.ToBase64String(binaryValue);
                binaryValue = null;
            }
#pragma warning restore CS0612


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
#pragma warning disable CS0612
            // legacy
            if (binaryValue != null && binaryValue.Length > 0)
            {
                BinaryValue = Convert.ToBase64String(binaryValue);
                binaryValue = null;
            }
#pragma warning restore CS0612
            // If we only have one type of data, use that to update the value
            if (string.IsNullOrEmpty(TextValue) && !string.IsNullOrEmpty(BinaryValue))
            {
                Value = Parser.ParseFrom(Convert.FromBase64String(BinaryValue));
            }
            else if (string.IsNullOrEmpty(BinaryValue) && !string.IsNullOrEmpty(TextValue))
            {
                Value = JsonParser.Default.Parse<T>(TextValue);
                
            }
            else switch (EncodingFormat)
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

            _lastHash = Value.GetHashCode();
        }

        IMessage IProto.Value => Value;
        ProtoFormat IProto.EncodingFormat
        {
            get => EncodingFormat;
            set => EncodingFormat = value;
        }
        public static implicit operator T(Proto<T> proto) => proto.Value;
        public static implicit operator Proto<T>(T proto) => new Proto<T>(proto);
    }


    public interface IProto
    {
        public IMessage Value { get; }
        public ProtoFormat EncodingFormat { get; set; }
    }
}