using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Aryx_MC260_Chimera
{
    internal static class AryxFieldAccess
    {
        internal static Func<T, TValue> CreateGetter<T, TValue>(FieldInfo field)
        {
            ParameterExpression instance = Expression.Parameter(typeof(T), "instance");
            return Expression.Lambda<Func<T, TValue>>(Expression.Field(instance, field), instance).Compile();
        }
    }
}
