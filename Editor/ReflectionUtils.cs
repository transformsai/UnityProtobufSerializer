using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ReflectionUtils
{
    private static readonly Regex IndexMatcher = new Regex("^([^\\s\\[]+)\\[([0-9]+)\\]");

    private static readonly Dictionary<string, Func<object, object>> GetterCache = new Dictionary<string, Func<object, object>>();


    public static Func<object, object> MakeGetter(SerializedProperty prop)
    {
        object obj = prop.serializedObject.targetObject;
        if (obj == null) throw new Exception("Target object is null!");

        var path = prop.propertyPath.Replace(".Array.data[", "[");

        var type = obj.GetType();
        var tag = $"{type.FullName}:{path}";

        if (GetterCache.TryGetValue(tag, out var getter)) return getter;

        foreach (var element in path.Split('.'))
        {
            var indexMatch = IndexMatcher.Match(element);

            Func<object, object> newGetter;
            if (indexMatch.Success)
            {
                var elementName = indexMatch.Groups[1].Value;
                var index = int.Parse(indexMatch.Groups[2].Value);
                newGetter = MakeGetter(type, elementName, index, out type);
            }
            else
            {
                newGetter = MakeGetter(type, element, out type);
            }

            // save old getter to capture in lambda
            var oldGetter = getter;
            getter = getter == null ? newGetter : source => newGetter(oldGetter(source));
        }

        GetterCache[tag] = getter;
        return getter;
    }

    private static Func<object, object> MakeGetter(Type type, string name, out Type newType)
    {
        var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        newType = f.FieldType;
        return source => f.GetValue(source);
    }

    private static Func<object, object> MakeGetter(Type type, string name, int index, out Type innerType)
    {
        var listGetter = (Func<object, IList>)MakeGetter(type, name, out var listType);
        innerType = listType.GenericTypeArguments[0];
        return obj => listGetter(obj)[index];

    }
}
