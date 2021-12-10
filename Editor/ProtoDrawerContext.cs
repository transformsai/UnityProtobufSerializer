using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using Object = UnityEngine.Object;

public class ProtoDrawerContext
{
    public readonly Type ContainingType;
    public readonly string SerializedFieldName;
    private readonly Dictionary<string, ProtoListDrawer> _listReferences = new Dictionary<string, ProtoListDrawer>();
    private readonly StringBuilder _builder = new StringBuilder();
    private string _fullPath = null;
    private bool? _foldout = null;

    public string FullPath
    {
        get
        {
            if (_fullPath== null) _fullPath = _builder.ToString();
            return _fullPath;
        }
    }

    public ProtoDrawerContext(string key, Type containingType, string serializedFieldName){
        ContainingType = containingType;
        SerializedFieldName = serializedFieldName;
        _builder.Append(key);
        Cache[key] = this;
    }    

    private void NotifyPathChanged(){
        _fullPath = null;
        _foldout = null;
    }

    public PathContext Enter(string fieldName)
    {
        var length = _builder.Length;
        if (_builder.Length == 0) _builder.Append(fieldName);
        else _builder.Append('.').Append(fieldName);
        NotifyPathChanged();
        return new PathContext { context = this, SectionLength = _builder.Length - length };
    }

    public PathContext Enter(int arrayElement)
    {
        var length = _builder.Length;
        _builder.Append('[').Append(arrayElement).Append(']');
        NotifyPathChanged();
        return new PathContext { context = this, SectionLength = _builder.Length - length };
    }

    public bool Foldout
    {
        get
        {
            if (_foldout == null) _foldout = EditorPrefs.GetBool(FullPath, false);
            return _foldout.Value;
        }

        set
        {
            if(_foldout == value) return;
            _foldout = value;
            EditorPrefs.SetBool(FullPath, value);
        }
    }

    public ProtoListDrawer CurrentList
    {
        get => _listReferences.TryGetValue(FullPath, out var value) ? value : null;
        set => _listReferences[FullPath] = value;
    }

    public struct PathContext : IDisposable
    {
        public int SectionLength;
        public ProtoDrawerContext context;
        public void Dispose()
        {
            context._builder.Remove(context._builder.Length - SectionLength, SectionLength);
            context.NotifyPathChanged();
        }
    }


    private static readonly Dictionary<string, ProtoDrawerContext> Cache = new Dictionary<string, ProtoDrawerContext>();

    public static ProtoDrawerContext FromCache(SerializedProperty property)
    {
        return FromCache(property.serializedObject.targetObject, property.name);
    }

    public static ProtoDrawerContext FromCache(Object obj, string propertyName = null)
    {
        var key = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
        if (propertyName != null) key = $"{key}.{propertyName}";
        
        return FromCache(key, obj.GetType(), propertyName );
    }

    public static ProtoDrawerContext FromCache(string key, Type containingType, string fieldName)
    {
        return Cache.TryGetValue(key, out var ctx) ? ctx : new ProtoDrawerContext(key, containingType, fieldName);
    }

}
