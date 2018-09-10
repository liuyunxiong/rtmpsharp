using System.Text.RegularExpressions;

// csharp: hina/extensions/stringextensions.cs [snipped]
namespace Hina
{
    static class StringExtensions
    {
        static readonly Regex CamelizeRegex1   = new Regex(@"(\-|_|\.|\s)+(.)?", RegexOptions.Compiled);
        static readonly Regex CamelizeRegex2   = new Regex(@"^(^|\/)([A-Z])",    RegexOptions.Compiled);

        public static string Camelize(this string str)
        {
            Check.NotNull(str);

            if (string.IsNullOrEmpty(str))
                return str;

            return CamelizeRegex2.Replace(
                CamelizeRegex1.Replace(str, x => x.Groups[2].Value.ToUpperInvariant()),
                x => x.Value.ToLowerInvariant());
        }
    }
}
