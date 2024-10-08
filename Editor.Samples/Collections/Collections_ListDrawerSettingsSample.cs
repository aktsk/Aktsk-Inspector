﻿using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;

public class Collections_ListDrawerSettingsSample : ScriptableObject
{
    [ListDrawerSettings(Draggable = true,
        HideAddButton = false,
        HideRemoveButton = false,
        AlwaysExpanded = false)]
    public List<Material> list;

    [ListDrawerSettings(Draggable = false, AlwaysExpanded = true)]
    public Vector3[] vectors;

    public MyStruct[] namedStructs = new MyStruct[]
    {
        new MyStruct {name = "First", value = 1},
        new MyStruct {name = "Second", value = 2,},
    };

    [Serializable]
    public struct MyStruct
    {
        public string name;
        public int value;

        #region カスタマイズ: ToStringメソッドがoverrideされている場合、リスト要素のラベルとして利用する

        public override string ToString()
        {
            return $"name={name}, value={value}";
        }

        #endregion
    }
}