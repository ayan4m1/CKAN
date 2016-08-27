using CKAN.Versioning;

namespace CKAN.Types.GameComparator
{
    /// <summary>
    /// You're On Your Own (YOYO) game compatibility comparison.
    /// This claims everything is compatible with everything.
    /// </summary>
    public class YoyoGameComparator : IGameComparator
    {
        public bool Compatible(KspVersion gameVersion, CkanModule module)
        {
            return true;
        }
    }
}