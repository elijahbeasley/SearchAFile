using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

public static class DataAnnotationHelpers
{
    public static int GetMaxLenFromAttr<T>(Expression<Func<T, object?>> selector)
    {
        if (selector.Body is not MemberExpression member)
        {
            if (selector.Body is UnaryExpression u && u.Operand is MemberExpression m)
                member = m;
            else
                return 0;
        }

        var prop = member.Member as PropertyInfo;
        if (prop == null) return 0;

        var strLen = prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;
        if (strLen is > 0) return strLen.Value;

        var maxLen = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length;
        return (maxLen is > 0) ? maxLen.Value : 0;
    }
}
