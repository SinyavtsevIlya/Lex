using System;
using System.Collections.Generic;

namespace Nanory.Lex.Conversion
{
    public static class AuthoringComponentExtensions
    {
        public static void MergeNonAllocNonDestructive(this List<AuthoringComponent> mergedResult, 
            List<AuthoringComponent> original, List<AuthoringComponent> overrides)
        {
            if (mergedResult.Count > 0)
                throw new ArgumentException(
                    $"Non empty ({mergedResult.Count}) merge-pool passed. Make sure it's empty.");
            
            foreach (var component in original) 
                mergedResult.Add(component);

            MergeNonAllocDestructive(mergedResult, overrides);
        }

        public static void MergeNonAllocDestructive(this List<AuthoringComponent> mergedResult, List<AuthoringComponent> overrides)
        {
            if (overrides == null || overrides.Count == 0)
                return;
            
            foreach (var overrideComponent in overrides)
            {
                var hasFound = false;
                for (var idx = 0; idx < mergedResult.Count; idx++)
                {
                    var component = mergedResult[idx];
                    var isSameType = component.GetType() == overrideComponent.GetType();
                    var isReplacementType =
                        overrideComponent is IReplaceAuthoringComponent replacement &&
                        replacement.GetAuthoringTypeToReplace() == component.GetType();

                    if (isSameType || isReplacementType)
                    {
                        mergedResult[idx] = overrideComponent;
                        hasFound = true;
                        break;
                    }
                }

                if (!hasFound)
                    mergedResult.Add(overrideComponent);
            }
        }

        public static string ToShortenedAuthoringName(this Type authoringType, bool addSpaces = true)
        {
            var typeName = authoringType.Name;
            if (!typeName.Contains("Authoring"))
                throw new Exception($"{authoringType} has a wrong naming. It should contain an \"Authoring\" postfix");

            typeName = typeName.Replace("Authoring", string.Empty);

            if (!addSpaces)
                return typeName;
            
            const string pattern = "(\\B[A-Z])";
            typeName = System.Text.RegularExpressions.Regex.Replace(typeName, pattern, " $1");
            
            return typeName;
        }
    }
}