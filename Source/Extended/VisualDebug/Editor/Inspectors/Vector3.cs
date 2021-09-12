// ----------------------------------------------------------------------------
// The MIT License
// UnityEditor integration https://github.com/Leopotam/ecslite-unityeditor
// for LeoECS Lite https://github.com/Leopotam/ecslite
// Copyright (c) 2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using UnityEditor;
using UnityEngine;

namespace Nanory.Lex.UnityEditor.Inspectors {
    sealed class Vector3Inspector : IEcsComponentInspector {
        public Type GetFieldType () {
            return typeof (Vector3);
        }

        public void OnGUI (string label, object value, EcsWorld world, int entityId) {
            EditorGUILayout.Vector3Field (label, (Vector3) value);
        }
    }
}