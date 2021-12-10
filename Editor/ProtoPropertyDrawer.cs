using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Proto<>))]
public class ProtoPropertyDrawer : PropertyDrawer
{
    private float _objectHeight = EditorUtils.SingleLineHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var ctx = ProtoDrawerContext.FromCache(property);
        var getter = ReflectionUtils.MakeGetter(property);
        var target = property.serializedObject.targetObject;
        var proto = (IProto)getter(target);
        var value = proto.Value;
        Event e = Event.current;

        if (e.type == EventType.ContextClick && position.Contains(e.mousePosition))
        {
            GenericMenu context = new GenericMenu();
            context.AddItem(new GUIContent("Use Binary Serialization"), proto.UseBinaryEncoding, () =>
            {
                proto.UseBinaryEncoding = !proto.UseBinaryEncoding;
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);

            });
            context.ShowAsContext();
        }

        EditorGUI.BeginChangeCheck();

        _objectHeight = ProtoDraw.DrawProto(position, value, ObjectNames.NicifyVariableName(property.name), ctx, true);

        if (EditorGUI.EndChangeCheck())
        {
            property.serializedObject.ApplyModifiedProperties();            
            property.serializedObject.Update();
            property.FindPropertyRelative("protoHash").longValue = value.GetHashCode();
            EditorUtility.SetDirty(target);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => _objectHeight;

    public override bool CanCacheInspectorGUI(SerializedProperty property) => true;

}
