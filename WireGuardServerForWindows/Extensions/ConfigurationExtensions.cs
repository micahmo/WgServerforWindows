using SharpConfig;

namespace WireGuardServerForWindows.Extensions
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Merges two <see cref="Configuration"/>s together. Handles the case where one or both is null.
        /// </summary>
        public static Configuration Merge(this Configuration first, Configuration second)
        {
            Configuration result = default;

            if (first is { } && second is { })
            {
                foreach (Section section in second)
                {
                    // Can't use indexer to add new sections in case there are duplicate names during the merge.
                    Section newSection = new Section(section.Name) {PreComment = section.PreComment};
                    
                    foreach (Setting setting in section)
                    {
                        newSection[setting.Name].RawValue = setting.RawValue;
                    }

                    first.Add(newSection);
                }

                result = first;
            }
            else if (first is { })
            {
                result = first;
            }
            else if (second is { })
            {
                result = second;
            }

            return result;
        }
    }
}
