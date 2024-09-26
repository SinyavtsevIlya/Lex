using System;
using System.Collections.Generic;

namespace Nanory.Lex.Conversion
{
    public static class AuthoringEntityOps
    {
        public static AuthoringEntity CreateEmptyAuthoringEntity(string id)
        {
            var entity = new AuthoringEntity 
            {
                Components = new List<AuthoringComponent>(),
                Id = id,
            };

            return entity;
        }

        public static void SetId(this AuthoringEntity authoringEntity, string id)
        {
            if (authoringEntity.Id == id) 
                return;

            var previousId = authoringEntity.Id;
            authoringEntity.Id = id;
            authoringEntity.IdChanged?.Invoke(previousId, id);
        }

        public static void Convert(this AuthoringEntity authoringEntity, int entity, ConvertToEntitySystem convertToEntitySystem)
        {
            foreach (var component in authoringEntity.Components) 
                component.Convert(entity, convertToEntitySystem);
        }

        public static TComponent Get<TComponent>(this AuthoringEntity authoringEntity) where TComponent : AuthoringComponent
        {
            foreach (var currentComponent in authoringEntity.Components)
            {
                if (currentComponent is not TComponent tComponent)
                    continue;
                
                return tComponent;
            }

            throw new Exception(
                $"{nameof(AuthoringEntity)} {authoringEntity.Id} doesn't have a {typeof(TComponent)} component.");
        }

        public static bool TryGet<TComponent>(this AuthoringEntity authoringEntity, out TComponent component)
            where TComponent : AuthoringComponent
        {
            foreach (var currentComponent in authoringEntity.Components)
            {
                if (currentComponent is not TComponent tComponent)
                    continue;
                
                component = tComponent;
                return true;
            }

            component = default;
            return false;
        }

        public static bool Has<TComponent>(this AuthoringEntity authoringEntity)
        {
            foreach (var currentComponent in authoringEntity.Components)
                if (currentComponent is TComponent)
                    return true;

            return false;
        }
        
        public static bool Has(this AuthoringEntity authoringEntity, Type componentType)
        {
            foreach (var currentComponent in authoringEntity.Components)
                if (currentComponent.GetType() == componentType)
                    return true;

            return false;
        }

        public static bool Add<TComponent>(this AuthoringEntity authoringEntity, TComponent component)
            where TComponent : AuthoringComponent
        {
            foreach (var currentComponent in authoringEntity.Components)
                if (currentComponent is TComponent)
                    return false;

            authoringEntity.AddUnsafe(component);
            return true;
        }
        
        public static void AddUnsafe<TComponent>(this AuthoringEntity authoringEntity, TComponent component)
            where TComponent : AuthoringComponent
        {
            authoringEntity.Components.Add(component);
        }
    }
}