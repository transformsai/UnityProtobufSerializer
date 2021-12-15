using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using TransformsAI.Unity.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TransformsAI.Unity.Protobuf.Editor
{
    public class ProtoListDrawer
    {
        public ReorderableList ReorderableList { get; }
        public FieldDescriptor Field { get; }
        private ProtoDrawerContext _context;
        public List<float> Sizes { get; } = new List<float>();
        public bool CanEditElements { get; set; }

        public ProtoListDrawer(IMessage message, FieldDescriptor field)
        {
            var list = (IList)field.Accessor.GetValue(message);
            var innerType = list.GetType().GetGenericArguments()[0];

            Field = field;
            ReorderableList = new ReorderableList(list, innerType, true, false, true, true);

            ReorderableList.drawElementCallback += DrawListElement;

            ReorderableList.elementHeightCallback = GetElementHeight;

            // Todo: Bug: When reordering an element that is folded out, the internal foldout state
            // is attributed to the wrong element because the index is used to store the path
            // Use the following callback to search and replace the foldout strings.
            // listRef.ReorderableList.onReorderCallbackWithDetails +=
        }

        private float GetElementHeight(int index)
        {
            for (var i = 0; i < ReorderableList.count - Sizes.Count; i++) Sizes.Add(EditorUtils.SingleLineHeight);
            return Sizes[index];
        }

        public void Draw(Rect rect, ProtoDrawerContext ctx)
        {
            _context = ctx;
            ReorderableList.DoList(rect);
            _context = null;
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            using var disabledScope = new EditorGUI.DisabledScope(!CanEditElements);
            using var section = _context.Enter(index);

            var list = ReorderableList.list;
            var field = Field;
            string fieldName = null;
            var fieldType = field.FieldType;

            if (fieldType == FieldType.Message)
            {
                var item = (IMessage)list[index];

                var nameValue = item
                    ?.Descriptor.Fields.InDeclarationOrder()
                    .FirstOrDefault(it => it.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                    ?.Accessor.GetValue(item) as string;

                rect = rect.Indent();
                if (!string.IsNullOrWhiteSpace(nameValue)) fieldName = nameValue;
            }

            var label = fieldName != null ?

                $"{field.Name}[{index}]: {fieldName}" :
                $"{field.Name}[{index}]";

            var newSize = ProtoDraw.DrawProtoField(
                rect,
                fieldType,
                _context,
                label,
                () => list[index],
                v => list[index] = v,
                true);

            if (newSize != Sizes[index])
            {
                // Needed to invalidate height differences due to height caching in ReorderableList.
                // todo: ask unity to expose a Cache invalidation in ReorderableList.
                GUI.changed = true;
            }

            Sizes[index] = newSize;


        }

    }
}
