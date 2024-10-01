using System;
using System.Diagnostics;

namespace TriInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class ListDrawerSettingsAttribute : Attribute
    {
        public bool Draggable { get; set; } = true;
        public bool HideAddButton { get; set; }
        public bool HideRemoveButton { get; set; }
        public bool AlwaysExpanded { get; set; }

        #region カスタマイズ: 要素のラベルを任意に指定可能にする

        // public bool ShowElementLabels { get; set; }
        public string ElementLabelMethod { get; set; }

        #endregion
    }
}