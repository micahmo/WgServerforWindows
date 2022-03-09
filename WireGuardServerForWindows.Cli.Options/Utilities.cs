using System;
using System.Linq;
using CommandLine;

namespace WireGuardServerForWindows.Cli.Options
{
    /// <summary>
    /// Static utilities related to the CLI options
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// If the given <paramref name="verbType"/> has the <see cref="VerbAttribute"/>, returns the <see cref="VerbAttribute.Name"/> value.
        /// Otherwise, returns null.
        /// </summary>
        /// <param name="verbType"></param>
        public static string GetVerb(this Type verbType)
        {
            return verbType.GetCustomAttributes(inherit: true).OfType<VerbAttribute>().FirstOrDefault()?.Name;
        }

        /// <summary>
        /// If the given <paramref name="verbType"/> has a property with the <see cref="OptionAttribute"/>, returns the <see cref="OptionAttribute.LongName"/> value.
        /// Otherwise, returns null.
        /// </summary>
        public static string GetOption(this Type verbType, string optionName)
        {
            return verbType.GetProperties().FirstOrDefault(p => p.Name == optionName)?.GetCustomAttributes(inherit: true).OfType<OptionAttribute>().FirstOrDefault()?.LongName;
        }
    }
}
