using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using UnityEngine;

public abstract class CustomProtoDrawer
{

    public virtual Type ClassFilter => null;
    public virtual string SerializedFieldNameFilter => null;
    public virtual string ProtoFieldNameFilter => null;
    public virtual FieldType? FieldTypeFilter => null;
    public virtual bool? IsRepeatedFilter => null;
    public virtual float Priority => 0f;

    public abstract float DrawField(ProtoDraw.DrawFieldDelegate nextDrawer, Rect offsetRect, IMessage message,
        FieldDescriptor field, OneofDescriptor oneOf, ProtoDrawerContext context);

    public static ProtoDraw.DrawFieldDelegate GetDelegate(ProtoDraw.DrawFieldDelegate innerDelegate, Type containingClass, string fieldName, FieldDescriptor descriptor)
    {
        foreach (var drawer in Drawers)
        {
            if(drawer.ClassFilter != null && drawer.ClassFilter!= containingClass) continue;
            if(drawer.FieldTypeFilter != null && drawer.FieldTypeFilter != descriptor.FieldType) continue;
            if(drawer.IsRepeatedFilter != null && drawer.IsRepeatedFilter != descriptor.IsRepeated) continue;
            if(drawer.SerializedFieldNameFilter != null && drawer.SerializedFieldNameFilter!= fieldName) continue;

            if (drawer.ProtoFieldNameFilter != null &&
               !drawer.ProtoFieldNameFilter.Equals(descriptor.Name,StringComparison.InvariantCultureIgnoreCase) &&
               !drawer.ProtoFieldNameFilter.Equals(descriptor.JsonName, StringComparison.InvariantCultureIgnoreCase)) continue;

            var oldDelegate = innerDelegate;
            innerDelegate = (rect, message, field, of, context) =>
                drawer.DrawField(oldDelegate, rect, message, field, of, context);
        }

        return innerDelegate;
    }

    private static readonly List<CustomProtoDrawer> Drawers = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(it => it.GetTypes())
        .Where(it => it.IsSubclassOf(typeof(CustomProtoDrawer)))
        .Where(it=>!it.IsAbstract)
        .Select(it => it.GetConstructor(new Type[0]))
        .Where(it => it != null)
        .Select(it => it.Invoke(new object[0]) as CustomProtoDrawer)
        .Where(it => it != null)
        .OrderBy(it => it.Priority)
        .ToList();

    internal static IReadOnlyList<CustomProtoDrawer> RegisteredDrawers => Drawers;

    public static void Register(CustomProtoDrawer drawer)
    {
        Drawers.Add(drawer);
        Drawers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }
}
