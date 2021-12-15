using System;
using System.Collections.Generic;
using TransformsAI.Unity.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace TransformsAI.Unity.Protobuf.Editor
{
    [CustomPropertyDrawer(typeof(Proto<>))]
    public class ProtoPropertyDrawer : PropertyDrawer
    {
        private float _objectHeight = EditorUtils.SingleLineHeight;

        static ProtoPropertyDrawer()
        {
            EditorApplication.contextualPropertyMenu += OnContextualPropertyMenu;
        }

        private static readonly HashSet<SerializedProperty> Props = new HashSet<SerializedProperty>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var ctx = ProtoDrawerContext.FromCache(property);
            var proto = (IProto)property.ReflectedValue();
            var value = proto.Value;
            EditorGUI.BeginChangeCheck();

            var nameContent = new GUIContent(ObjectNames.NicifyVariableName(property.name));
            Props.Add(property);
            var propContent = EditorGUI.BeginProperty(position, nameContent, property);

            _objectHeight = ProtoDraw.DrawProto(position, value, propContent, ctx, true);

            EditorGUI.EndProperty();
            Props.Remove(property);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

        }

        static void OnContextualPropertyMenu(GenericMenu menu, SerializedProperty serializedProperty)
        {
            if (!Props.Contains(serializedProperty)) return;

            var relativeProp = serializedProperty.FindPropertyRelative(nameof(IProto.EncodingFormat));
            var fmt = (ProtoFormat)relativeProp.intValue;

            foreach (ProtoFormat value in Enum.GetValues(typeof(ProtoFormat)))
                menu.AddItem(new GUIContent("Encoding/" + value), fmt == value, Func, value);

            void Func(object newFmt)
            {
                var format = (ProtoFormat)newFmt;
                relativeProp.intValue = (int)format;
                var serializedObject = serializedProperty.serializedObject;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => _objectHeight;

        public override bool CanCacheInspectorGUI(SerializedProperty property) => true;

    }
}
