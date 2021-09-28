// ----------------------------------------------------------------------------
// The MIT License
// UnityEditor integration https://github.com/Leopotam/ecslite-unityeditor
// for LeoECS Lite https://github.com/Leopotam/ecslite
// Copyright (c) 2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using UnityEditor;
using UnityEngine;

namespace Nanory.Lex.UnityEditorIntegration.Inspectors {
    sealed class QuaternionInspector : IEcsComponentInspector {
        public Type GetFieldType () {
            return typeof (Quaternion);
        }

        public void OnGUI (string label, object value, EcsWorld world, int entityId) {
            EditorGUILayout.Vector3Field (label, ((Quaternion) value).eulerAngles);
        }
    }
}