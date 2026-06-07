using Newtonsoft.Json.Serialization;

namespace Lus.Infrastructure.Common
{
    public class OriginalCaseNamingStrategy : NamingStrategy
    {
        public OriginalCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames)
        {
            ProcessDictionaryKeys = processDictionaryKeys;
            OverrideSpecifiedNames = overrideSpecifiedNames;
        }

        public OriginalCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames, bool processExtensionDataNames)
            : this(processDictionaryKeys, overrideSpecifiedNames)
        {
            ProcessExtensionDataNames = processExtensionDataNames;
        }

        public OriginalCaseNamingStrategy()
        {
        }

        protected override string ResolvePropertyName(string name)
        {
            return name;
        }
    }
}
