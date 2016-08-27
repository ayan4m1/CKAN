using CKAN.Types;
using Newtonsoft.Json.Linq;

namespace CKAN.NetKAN.Extensions
{
    internal static class VersionExtensions
    {
        public static JToken ToSpecVersionJson(this GameVersion specVersion)
        {
            if (specVersion.IsEqualTo(new GameVersion("v1.0")))
            {
                return 1;
            }
            return specVersion.ToString();
        }
    }
}
