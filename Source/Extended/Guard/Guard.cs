using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public class Guard
    {
#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIfNull<T>(T obj, string paramName, string message)
        {
            if (obj == null)
                throw new GuardArgumentNullException(paramName, message);
        }

#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIfNull<T, T1>(T obj, string paramName, string message, T1 arg1)
        {
            if (obj == null)
                throw new GuardArgumentNullException(paramName, string.Format(message, arg1));
        }

#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIfNull<T, T1, T2>(T obj, string paramName, string message, T1 arg1, T2 arg2)
        {
            if (obj == null)
                throw new GuardArgumentNullException(paramName, string.Format(message, arg1, arg2));
        }
        
#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIfAnyNull<T1, T2>(T1 arg1, T2 arg2, string message)
        {
            if (arg1 == null)
                throw new GuardArgumentNullException(nameof(arg1), message);

            if (arg2 == null)
                throw new GuardArgumentNullException(nameof(arg2), message);
        }
        
#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIf(bool value, string paramName, string message)
        {
            if (value)
                throw new GuardBadConditionException(message, paramName);
        }
        
#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIf<T>(bool value, string paramName, string message, T arg)
        {
            if (value)
                throw new GuardBadConditionException(string.Format(message, arg), paramName);
        }

#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIf<T1, T2>(bool value, string paramName, string message, T1 arg1, T2 arg2)
        {
            if (value)
                throw new GuardBadConditionException(string.Format(message, arg1, arg2), paramName);
        }

#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIf<T1, T2, T3>(bool value, string paramName, string message, T1 arg1, T2 arg2,
            T3 arg3)
        {
            if (value)
                throw new GuardBadConditionException(string.Format(message, arg1, arg2, arg3),
                    paramName);
        }
        
#if GUARD_DISABLED
        [Conditional("FALSE")]
#endif
        public static void ThrowIfNullOrEmpty(string str, string paramName, string message)
        {
            if (string.IsNullOrEmpty(str))
                throw new GuardArgumentNullException(paramName, message);
        }
    }
    
    public class GuardBadConditionException : ArgumentException
    {
        public GuardBadConditionException(string paramName, string message) : base(message, paramName)
        {
            
        }
    }
    
    public class GuardArgumentNullException : ArgumentNullException
    {
        public GuardArgumentNullException(string paramName, string message) : base(paramName, message)
        {
            
        }
    }

    public class EntityDisposedException : Exception
    {
        private readonly Entity _entity;
        private readonly string _operationContext;
        
        public EntityDisposedException(Entity entity, string operationContext = null)
        {
            _entity = entity;
            _operationContext = operationContext;
        }

        public override string Message
        {
            get
            {
                var arguments = new List<string>
                {
                    $"Disposed entity {_entity}"
                };
                
                if (_operationContext != null)
                {
                    arguments.Add($"doing {_operationContext}");
                }
                
                return string.Join(" while ", arguments);
            }
        }
    }
}