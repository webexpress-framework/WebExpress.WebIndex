using System;

namespace WebExpress.WebIndex.WebAttribute
{
    /// <summary>
    /// Keeps a property out of WQL, for index items whose statements may come from untrusted
    /// callers. A filter, a sort or a function argument cannot reach the property or anything
    /// nested below it, so its value cannot be guessed through repeated conditions. The
    /// attribute is independent of <see cref="IndexIgnoreAttribute"/>, which only skips the
    /// reverse index and leaves the property queryable by a scan.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class WqlIgnoreAttribute : Attribute
    {
    }
}
