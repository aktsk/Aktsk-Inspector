﻿using System;
using TriInspector.Utilities;
using UnityEditor;
using UnityEngine;

namespace TriInspector.Elements
{
    internal class TriReferenceElement : TriPropertyCollectionBaseElement
    {
        private readonly Props _props;
        private readonly TriProperty _property;
        private readonly bool _showReferencePicker;
        private readonly bool _skipReferencePickerExtraLine;

        private Type _referenceType;

        [Serializable]
        public struct Props
        {
            public bool inline;
            public bool drawPrefixLabel;
            public float labelWidth;
        }

        public TriReferenceElement(TriProperty property, Props props = default)
        {
            _property = property;
            _props = props;
            _showReferencePicker = !property.TryGetAttribute(out HideReferencePickerAttribute _);
            _skipReferencePickerExtraLine = !_showReferencePicker && _props.inline;
        }

        public override bool Update()
        {
            var dirty = false;

            if (_props.inline || _property.IsExpanded)
            {
                dirty |= GenerateChildren();
            }
            else
            {
                dirty |= ClearChildren();
            }

            dirty |= base.Update();

            return dirty;
        }

        public override float GetHeight(float width)
        {
            #region カスタマイズ: 描画範囲調整

            // var height = _skipReferencePickerExtraLine ? 0f : EditorGUIUtility.singleLineHeight;
            var height = _skipReferencePickerExtraLine ? 0f : EditorGUIUtility.singleLineHeight + 2;

            #endregion

            if (_props.inline || _property.IsExpanded)
            {
                height += base.GetHeight(width);
            }

            return height;
        }

        public override void OnGUI(Rect position)
        {
            if (_props.drawPrefixLabel)
            {
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                position = EditorGUI.PrefixLabel(position, controlId, _property.DisplayNameContent);
            }

            var headerRect = new Rect(position)
            {
                height = _skipReferencePickerExtraLine ? 0f : EditorGUIUtility.singleLineHeight,
            };
            var headerLabelRect = new Rect(position)
            {
                height = headerRect.height,
                width = EditorGUIUtility.labelWidth,
            };
            var headerFieldRect = new Rect(position)
            {
                height = headerRect.height,

                #region カスタマイズ: 描画範囲調整

                // xMin = headerRect.xMin + EditorGUIUtility.labelWidth,
                xMin = headerRect.xMin + 15,

                #endregion
            };
            var contentRect = new Rect(position)
            {
                yMin = position.yMin + headerRect.height,
            };

            #region カスタマイズ: 描画範囲調整

            if (!_skipReferencePickerExtraLine)
            {
                contentRect.yMin += 2;
            }

            #endregion

            if (_props.inline)
            {
                if (_showReferencePicker)
                {
                    TriManagedReferenceGui.DrawTypeSelector(headerRect, _property);
                }

                using (TriGuiHelper.PushLabelWidth(_props.labelWidth))
                {
                    base.OnGUI(contentRect);
                }
            }
            else
            {
                #region カスタマイズ: 描画範囲調整

                var foldOutRect = new Rect(headerLabelRect)
                {
                    width = 20,
                };
                TriEditorGUI.Foldout(foldOutRect, _property);

                #endregion

                if (_showReferencePicker)
                {
                    TriManagedReferenceGui.DrawTypeSelector(headerFieldRect, _property);
                }

                if (_property.IsExpanded)
                {
                    #region カスタマイズ: 縦線表示

                    var lineRect = new Rect(position)
                    {
                        x = position.x + 6,
                        y = position.y + 13,
                        height = position.height - 13,
                        width = 1,
                    };
                    EditorGUI.DrawRect(lineRect, new Color(1, 1, 1, 0.1f));

                    #endregion

                    using (var indentedRectScope = TriGuiHelper.PushIndentedRect(contentRect, 1))
                    using (TriGuiHelper.PushLabelWidth(_props.labelWidth))
                    {
                        base.OnGUI(indentedRectScope.IndentedRect);
                    }
                }
            }
        }

        private bool GenerateChildren()
        {
            if (_property.ValueType == _referenceType)
            {
                return false;
            }

            _referenceType = _property.ValueType;

            RemoveAllChildren();

            ClearGroups();
            DeclareGroups(_property.ValueType);

            foreach (var childProperty in _property.ChildrenProperties)
            {
                AddProperty(childProperty);
            }

            return true;
        }

        private bool ClearChildren()
        {
            if (ChildrenCount == 0)
            {
                return false;
            }

            _referenceType = null;
            RemoveAllChildren();

            return true;
        }
    }
}