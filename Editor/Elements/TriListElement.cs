using System;
using System.Collections;
using System.Linq;
using TriInspector.Resolvers;
using TriInspectorUnityInternalBridge;
using TriInspector.Utilities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TriInspector.Elements
{
    public class TriListElement : TriElement
    {
        private const float ListExtraWidth = 7f;
        private const float DraggableAreaExtraWidth = 14f;

        private readonly TriProperty _property;
        private readonly ReorderableList _reorderableListGui;
        private readonly bool _alwaysExpanded;
        private readonly bool _showElementLabels;

        private float _lastContentWidth;

        protected ReorderableList ListGui => _reorderableListGui;

        public TriListElement(TriProperty property)
        {
            property.TryGetAttribute(out ListDrawerSettingsAttribute settings);

            _property = property;
            _alwaysExpanded = settings?.AlwaysExpanded ?? false;

            #region カスタマイズ: 要素のラベルを任意に指定可能にする

            // _showElementLabels = settings?.ShowElementLabels ?? false;
            _showElementLabels = true;

            #endregion

            _reorderableListGui = new ReorderableList(null, _property.ArrayElementType)
            {
                draggable = settings?.Draggable ?? true,
                displayAdd = settings == null || !settings.HideAddButton,
                displayRemove = settings == null || !settings.HideRemoveButton,
                drawHeaderCallback = DrawHeaderCallback,
                elementHeightCallback = ElementHeightCallback,
                drawElementCallback = DrawElementCallback,
                onAddCallback = AddElementCallback,
                onRemoveCallback = RemoveElementCallback,
                onReorderCallbackWithDetails = ReorderCallback,
            };

            if (!_reorderableListGui.displayAdd && !_reorderableListGui.displayRemove)
            {
                _reorderableListGui.footerHeight = 0f;
            }
        }

        public override bool Update()
        {
            var dirty = false;

            if (_property.TryGetSerializedProperty(out var serializedProperty) && serializedProperty.isArray)
            {
                _reorderableListGui.serializedProperty = serializedProperty;
            }
            else if (_property.Value != null)
            {
                _reorderableListGui.list = (IList) _property.Value;
            }
            else if (_reorderableListGui.list == null)
            {
                _reorderableListGui.list = (IList) (_property.FieldType.IsArray
                    ? Array.CreateInstance(_property.ArrayElementType, 0)
                    : Activator.CreateInstance(_property.FieldType));
            }

            if (_alwaysExpanded && !_property.IsExpanded)
            {
                _property.IsExpanded = true;
            }

            if (_property.IsExpanded)
            {
                dirty |= GenerateChildren();
            }
            else
            {
                dirty |= ClearChildren();
            }

            dirty |= base.Update();

            if (dirty)
            {
                ReorderableListProxy.ClearCacheRecursive(_reorderableListGui);
            }

            return dirty;
        }

        public override float GetHeight(float width)
        {
            if (!_property.IsExpanded)
            {
                return _reorderableListGui.headerHeight + 4f;
            }

            _lastContentWidth = width;

            return _reorderableListGui.GetHeight();
        }

        public override void OnGUI(Rect position)
        {
            if (!_property.IsExpanded)
            {
                ReorderableListProxy.DoListHeader(_reorderableListGui, new Rect(position)
                {
                    yMax = position.yMax - 4,
                });
                return;
            }

            var labelWidthExtra = ListExtraWidth + DraggableAreaExtraWidth;

            using (TriGuiHelper.PushLabelWidth(EditorGUIUtility.labelWidth - labelWidthExtra))
            {
                _reorderableListGui.DoList(position);
            }
        }

        private void AddElementCallback(ReorderableList reorderableList)
        {
            AddElementCallback(reorderableList, null);
        }

        private void AddElementCallback(ReorderableList reorderableList, Object addedReferenceValue)
        {
            if (_property.TryGetSerializedProperty(out _))
            {
                ReorderableListProxy.DoAddButton(reorderableList, addedReferenceValue);
                _property.NotifyValueChanged();
                return;
            }

            var template = CloneValue(_property);

            _property.SetValues(targetIndex =>
            {
                var value = (IList) _property.GetValue(targetIndex);

                if (_property.FieldType.IsArray)
                {
                    var array = Array.CreateInstance(_property.ArrayElementType, template.Length + 1);
                    Array.Copy(template, array, template.Length);

                    if (addedReferenceValue != null)
                    {
                        array.SetValue(addedReferenceValue, array.Length - 1);
                    }

                    value = array;
                }
                else
                {
                    if (value == null)
                    {
                        value = (IList) Activator.CreateInstance(_property.FieldType);
                    }

                    var newElement = addedReferenceValue != null
                        ? addedReferenceValue
                        : CreateDefaultElementValue(_property);

                    value.Add(newElement);
                }

                return value;
            });
        }

        private void RemoveElementCallback(ReorderableList reorderableList)
        {
            if (_property.TryGetSerializedProperty(out _))
            {
                ReorderableListProxy.defaultBehaviours.DoRemoveButton(reorderableList);
                _property.NotifyValueChanged();
                return;
            }

            var template = CloneValue(_property);
            var ind = reorderableList.index;

            _property.SetValues(targetIndex =>
            {
                var value = (IList) _property.GetValue(targetIndex);

                if (_property.FieldType.IsArray)
                {
                    var array = Array.CreateInstance(_property.ArrayElementType, template.Length - 1);
                    Array.Copy(template, 0, array, 0, ind);
                    Array.Copy(template, ind + 1, array, ind, array.Length - ind);
                    value = array;
                }
                else
                {
                    value?.RemoveAt(ind);
                }

                return value;
            });
        }

        private void ReorderCallback(ReorderableList list, int oldIndex, int newIndex)
        {
            if (_property.TryGetSerializedProperty(out _))
            {
                _property.NotifyValueChanged();
                return;
            }

            var mainValue = _property.Value;

            _property.SetValues(targetIndex =>
            {
                var value = (IList) _property.GetValue(targetIndex);

                if (value == mainValue)
                {
                    return value;
                }

                var element = value[oldIndex];
                for (var index = 0; index < value.Count - 1; ++index)
                {
                    if (index >= oldIndex)
                    {
                        value[index] = value[index + 1];
                    }
                }

                for (var index = value.Count - 1; index > 0; --index)
                {
                    if (index > newIndex)
                    {
                        value[index] = value[index - 1];
                    }
                }

                value[newIndex] = element;

                return value;
            });
        }

        private bool GenerateChildren()
        {
            var count = _reorderableListGui.count;

            if (ChildrenCount == count)
            {
                return false;
            }

            while (ChildrenCount < count)
            {
                var property = _property.ArrayElementProperties[ChildrenCount];
                AddChild(CreateItemElement(property));
            }

            while (ChildrenCount > count)
            {
                RemoveChildAt(ChildrenCount - 1);
            }

            return true;
        }

        private bool ClearChildren()
        {
            if (ChildrenCount == 0)
            {
                return false;
            }

            RemoveAllChildren();

            return true;
        }

        protected virtual TriElement CreateItemElement(TriProperty property)
        {
            return new TriPropertyElement(property, new TriPropertyElement.Props
            {
                forceInline = !_showElementLabels,
            });
        }

        private void DrawHeaderCallback(Rect rect)
        {
            var labelRect = new Rect(rect);

            #region カスタマイズ: 要素数を変更するためのフィールドを描画

            var arraySizeRect = new Rect(rect)
            {
                // xMin = rect.xMax - 100,
                xMin = rect.xMax - 40,
                xMax = rect.xMax - 4
            };

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                var arraySize = EditorGUI.IntField(arraySizeRect, GUIContent.none, _reorderableListGui.count);
                if (changeCheckScope.changed)
                {
                    // Add
                    if (arraySize > _reorderableListGui.count)
                    {
                        for (var i = _reorderableListGui.count; i < arraySize; ++i)
                        {
                            AddElementCallback(_reorderableListGui);
                        }
                    }
                    // Remove
                    else if (arraySize < _reorderableListGui.count)
                    {
                        for (var i = _reorderableListGui.count; i > arraySize; --i)
                        {
                            RemoveElementCallback(_reorderableListGui);
                        }
                    }
                }
            }

            #endregion

            if (_alwaysExpanded)
            {
                EditorGUI.LabelField(labelRect, _property.DisplayNameContent);
            }
            else
            {
                TriEditorGUI.Foldout(labelRect, _property);
            }

            #region カスタマイズ: 要素数はラベルとしては表示しない

            // var label = _reorderableListGui.count == 0 ? "Empty" : $"{_reorderableListGui.count} items";
            // GUI.Label(arraySizeRect, label, Styles.ItemsCount);

            #endregion

            if (Event.current.type == EventType.DragUpdated && rect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDrop.objectReferences.All(obj => TryGetDragAndDropObject(obj, out _))
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Rejected;

                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform && rect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.AcceptDrag();

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (TryGetDragAndDropObject(obj, out var addedReferenceValue))
                    {
                        AddElementCallback(_reorderableListGui, addedReferenceValue);
                    }
                }

                Event.current.Use();
            }
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= ChildrenCount)
            {
                return;
            }

            if (!_reorderableListGui.draggable)
            {
                rect.xMin += DraggableAreaExtraWidth;
            }

            #region カスタマイズ: 上端にスペースを追加

            rect.yMin += 1;

            #endregion

            using (TriPropertyOverrideContext.BeginOverride(ListPropertyOverrideContext.Instance))
            {
                GetChild(index).OnGUI(rect);
            }
        }

        private float ElementHeightCallback(int index)
        {
            if (index >= ChildrenCount)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            return GetChild(index).GetHeight(_lastContentWidth);
        }

        private static object CreateDefaultElementValue(TriProperty property)
        {
            var canActivate = property.ArrayElementType.IsValueType ||
                              property.ArrayElementType.GetConstructor(Type.EmptyTypes) != null;

            return canActivate ? Activator.CreateInstance(property.ArrayElementType) : null;
        }

        private static Array CloneValue(TriProperty property)
        {
            var list = (IList) property.Value;
            var template = Array.CreateInstance(property.ArrayElementType, list?.Count ?? 0);
            list?.CopyTo(template, 0);
            return template;
        }

        private bool TryGetDragAndDropObject(Object obj, out Object result)
        {
            if (obj == null)
            {
                result = null;
                return false;
            }

            var elementType = _property.ArrayElementType;
            var objType = obj.GetType();

            if (elementType == objType || elementType.IsAssignableFrom(objType))
            {
                result = obj;
                return true;
            }

            if (obj is GameObject go && typeof(Component).IsAssignableFrom(elementType) &&
                go.TryGetComponent(elementType, out var component))
            {
                result = component;
                return true;
            }

            result = null;
            return false;
        }

        private class ListPropertyOverrideContext : TriPropertyOverrideContext
        {
            public static readonly ListPropertyOverrideContext Instance = new ListPropertyOverrideContext();

            private readonly GUIContent _noneLabel = GUIContent.none;

            public override bool TryGetDisplayName(TriProperty property, out GUIContent displayName)
            {
                #region カスタマイズ: 要素のラベルを任意に指定可能にする

                // var showLabels = property.TryGetAttribute(out ListDrawerSettingsAttribute settings) &&
                //                  settings.ShowElementLabels;
                //
                // if (!showLabels)
                // {
                //     displayName = _noneLabel;
                //     return true;
                // }

                if (property.TryGetAttribute(out ListDrawerSettingsAttribute settings) &&
                    !string.IsNullOrEmpty(settings.ElementLabelMethod))
                {
                    var elementLabelResolver = ValueResolver.Resolve<string, int>(
                        property.Definition, settings.ElementLabelMethod, property.IndexInArray);
                    var label = elementLabelResolver.GetValue(property, property.IndexInArray);
                    if (!string.IsNullOrEmpty(label))
                    {
                        displayName = new GUIContent(label);
                        return true;
                    }
                }

                #endregion

                displayName = _noneLabel;
                return false;
            }
        }

        private static class Styles
        {
            public static readonly GUIStyle ItemsCount;

            static Styles()
            {
                ItemsCount = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin
                            ? new Color(0.6f, 0.6f, 0.6f)
                            : new Color(0.3f, 0.3f, 0.3f),
                    },
                };
            }
        }
    }
}