using System;
using System.Collections;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using UnityEditor;
using UnityEngine;

using static TransformsAI.Unity.Utilities.Editor.EditorUtils;

namespace TransformsAI.Unity.Protobuf.Editor
{
    public static class ProtoDraw
    {
        public delegate float DrawFieldDelegate(Rect offsetRect, IMessage message, FieldDescriptor field, OneofDescriptor currentOneOf, ProtoDrawerContext context);

        public static float DrawProto(Rect rect, IMessage message, GUIContent name, ProtoDrawerContext context, bool drawHeader)
        {
            var height = 0f;

            if (drawHeader)
            {
                context.Foldout = EditorGUI.Foldout(rect.Offset(height), context.Foldout, name);
                height += SingleLineHeight;
                if (!context.Foldout) return height;
            }

            OneofDescriptor currentOneOf = null;

            rect = rect.Indent();

            foreach (var field in message.Descriptor.Fields.InDeclarationOrder())
            {
                var prevOneOf = currentOneOf;
                currentOneOf = field.ContainingOneof;
                var ctxName = field.Name;

                if (currentOneOf != null)
                {
                    if (prevOneOf == currentOneOf) continue;
                    ctxName = currentOneOf.Name;
                }

                using (context.Enter(ctxName))
                {
                    var fieldDrawer = CustomProtoDrawer.GetDelegate(DrawField, context.ContainingType, context.SerializedFieldName, field );
                    height += fieldDrawer(rect.Offset(height), message, field, currentOneOf, context);

                }
            }

            return height;
        }

        public static float DrawField(Rect offsetRect, IMessage message, FieldDescriptor field, OneofDescriptor currentOneOf, ProtoDrawerContext context)
        {

            // special fields
            if (currentOneOf != null)
                return DrawProtoOneOf(offsetRect, currentOneOf, message, context);
            if (field.IsMap)
                return DrawProtoMap(offsetRect, field, message, context);
            if (field.IsRepeated)
                return DrawProtoFieldRepeated(offsetRect, field, message, context);
            if (field.FieldType == FieldType.Message)
                return DrawProtoMessage(offsetRect, message, field, context);
        
            // scalars
            var name = ObjectNames.NicifyVariableName(field.JsonName);
            object Getter() => field.Accessor.GetValue(message);
            void Setter(object obj) => field.Accessor.SetValue(message, obj);
            return DrawProtoField(offsetRect, field.FieldType, context, name, Getter, Setter, true);
        }

        private static float DrawProtoMessage(Rect offsetRect, IMessage message, FieldDescriptor field,
            ProtoDrawerContext context)
        {
            var innerMessage = field.Accessor.GetValue(message);

            context.Foldout = EditorGUI.Foldout(offsetRect, context.Foldout, string.Empty);

            var exists = innerMessage != null;
            var create = EditorGUI.Toggle(offsetRect.Indent(12), ObjectNames.NicifyVariableName(field.JsonName), exists);
            var foldoutRect = offsetRect.Offset(SingleLineHeight);
            var height = SingleLineHeight;

            if (exists && !create)
            {
                field.Accessor.Clear(message);
                innerMessage = null;
            }

            if (!exists && create)
            {
                innerMessage = CreateInstance(field);
                field.Accessor.SetValue(message, innerMessage);
            }

            if (!context.Foldout) return height;

            if (innerMessage == null)
            {
                EditorGUI.LabelField(foldoutRect.Indent(), "Field Not set. Click on the checkbox to set.");
                height += SingleLineHeight;
                return height;
            }

            height += DrawProtoField(
                foldoutRect.Indent(),
                field.FieldType,
                context,
                ObjectNames.NicifyVariableName(field.JsonName),
                () => field.Accessor.GetValue(message),
                obj => field.Accessor.SetValue(message, obj),
                false);

            return height;
        }

        public delegate T DrawerFunc<T>(Rect rect, string label, T currentValue);

        public static object CreateInstance(FieldDescriptor field)
        {
            var type = GetFieldType(field);
            return Activator.CreateInstance(type);
        }

        public static Type GetFieldType(FieldDescriptor field)
        {
            if (field.IsRepeated) throw new NotSupportedException();
            if (field.IsMap) throw new NotSupportedException();
            switch (field.FieldType)
            {
                case FieldType.Fixed32:
                case FieldType.SFixed32:
                case FieldType.Float:
                    return typeof(float);
                case FieldType.Double:
                case FieldType.Fixed64:
                case FieldType.SFixed64:
                    return typeof(double);
                case FieldType.Int64:
                case FieldType.SInt64:
                    return typeof(long);
                case FieldType.UInt64:
                    return typeof(ulong);
                case FieldType.SInt32:
                case FieldType.Int32:
                    return typeof(int);
                case FieldType.UInt32:
                    return typeof(uint);
                case FieldType.Bool:
                    return typeof(bool);
                case FieldType.String:
                    return typeof(string);
                case FieldType.Group:
                    throw new NotSupportedException();
                case FieldType.Message:
                    return field.MessageType.ClrType;
                case FieldType.Bytes:
                    return typeof(byte[]);
                case FieldType.Enum:
                    return field.EnumType.ClrType;
            }
            throw new ArgumentException("bad message");
        }

        public static float DrawProtoOneOf(Rect rect, OneofDescriptor oneOf, IMessage message, ProtoDrawerContext context)
        {

            var height = 0f;
            var accessor = oneOf.Accessor;

            context.Foldout = EditorGUI.Foldout(rect.Offset(height), context.Foldout, ObjectNames.NicifyVariableName(oneOf.Name));
            height += SingleLineHeight;

            if (!context.Foldout) return height;

            foreach (var field in oneOf.Fields)
            {
                var currentItem = accessor.GetCaseFieldDescriptor(message);
                var isSelected = field.FieldNumber == currentItem?.FieldNumber;
                var newSelected = EditorGUI.ToggleLeft(rect.Offset(height), field.Name, isSelected);
                height += SingleLineHeight;

                if (newSelected != isSelected)
                {
                    oneOf.Accessor.Clear(message);
                    if (newSelected) field.Accessor.SetValue(message, CreateInstance(field));
                }
                if (!newSelected) continue;

                using var section = context.Enter(field.Name);
                height += DrawProtoField(rect.Offset(height).Indent(), field.FieldType, context, ObjectNames.NicifyVariableName(field.JsonName),
                    () => field.Accessor.GetValue(message), obj => field.Accessor.SetValue(message, obj), false);
            }

            return height;
        }

        public static float DrawProtoMap(Rect rect, FieldDescriptor field, IMessage message, ProtoDrawerContext context)
        {
            //todo
            EditorGUI.LabelField(rect, $"{context.FullPath}: Maps not done yet");
            return SingleLineHeight;
        }

        public static float DrawProtoFieldRepeated(Rect rect, FieldDescriptor field, IMessage message, ProtoDrawerContext context)
        {
            var list = (IList)field.Accessor.GetValue(message);
            var height = 0f;

            context.Foldout = EditorGUI.Foldout(rect.Offset(height), context.Foldout, ObjectNames.NicifyVariableName(field.JsonName));
            height += SingleLineHeight;

            if (!context.Foldout) return height;

            context.CurrentList ??= new ProtoListDrawer(message, field);
            if (!ReferenceEquals(list, context.CurrentList.ReorderableList.list)) context.CurrentList.ReorderableList.list = list;

            context.CurrentList.Draw(rect.Offset(height), context);
            return context.CurrentList.ReorderableList.GetHeight() + height;
        }


        public static float DrawProtoField(Rect rect, FieldType fieldType, ProtoDrawerContext context, string name, Func<object> getter, Action<object> setter, bool drawProtoHeader)
        {
            switch (fieldType)
            {
                case FieldType.SFixed64:
                case FieldType.Fixed64:
                case FieldType.Double:
                    DrawProtoField<double>(rect, EditorGUI.DelayedDoubleField, name, getter, setter);
                    return SingleLineHeight;
                case FieldType.SFixed32:
                case FieldType.Fixed32:
                case FieldType.Float:
                    DrawProtoField<float>(rect, EditorGUI.DelayedFloatField, name, getter, setter);
                    return SingleLineHeight;
                case FieldType.UInt64:
                case FieldType.SInt64:
                case FieldType.Int64:
                    DrawProtoField<long>(rect, EditorGUI.LongField, name, getter, setter);
                    return SingleLineHeight;
                case FieldType.SInt32:
                case FieldType.UInt32:
                case FieldType.Int32:
                    DrawProtoField<int>(rect, EditorGUI.DelayedIntField, name, getter, setter);
                    return SingleLineHeight;
                case FieldType.Bool:
                    DrawProtoField<bool>(rect, EditorGUI.Toggle, name, getter, setter);
                    return SingleLineHeight;
                case FieldType.String:
                    DrawProtoField<string>(rect, EditorGUI.DelayedTextField, name, getter, setter);
                    return SingleLineHeight;
                case FieldType.Enum:
                    DrawProtoField<Enum>(rect, EditorGUI.EnumPopup, name, getter, setter);
                    return SingleLineHeight;
                case FieldType.Message:
                    var innerMessage = getter() as IMessage;
                    return DrawProto(rect, innerMessage, new GUIContent(name), context, drawProtoHeader);
                case FieldType.Bytes:
                    EditorGUI.LabelField(rect, $"{context.FullPath}: This type ({fieldType}) is unsupported");
                    return SingleLineHeight; // Todo
                case FieldType.Group:
                    EditorGUI.LabelField(rect, $"{context.FullPath}: This type ({fieldType}) is unsupported");
                    return SingleLineHeight;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void DrawProtoField<T>(Rect rect, DrawerFunc<T> drawer, string name, Func<object> getter, Action<object> setter)
        {
            var rawValue = (T)getter();
            var value = rawValue;
            var newValue = drawer(rect, name, value);
            if (!newValue.Equals(value)) setter(newValue);
        }
    
    }


}
