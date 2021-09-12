// ----------------------------------------------------------------------------
// The MIT License
// UnityEditor integration https://github.com/Leopotam/ecslite-unityeditor
// for LeoECS Lite https://github.com/Leopotam/ecslite
// Copyright (c) 2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Nanory.Lex.UnityEditor {
    public static class EditorExtensions {
        public static string GetCleanGenericTypeName (Type type) {
            if (!type.IsGenericType) {
                return type.Name;
            }
            var constraints = "";
            foreach (var constraint in type.GetGenericArguments ()) {
                constraints += constraints.Length > 0 ? $", {GetCleanGenericTypeName (constraint)}" : constraint.Name;
            }
            return $"{type.Name.Substring (0, type.Name.LastIndexOf ("`", StringComparison.Ordinal))}<{constraints}>";
        }
    }

    public sealed class EcsEntityDebugView : MonoBehaviour {
        [NonSerialized]
        public EcsWorld World;
        [NonSerialized]
        public int Entity;
    }
}
#endif