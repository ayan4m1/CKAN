using CKAN.NetKAN.Model;
using CKAN.Types;

namespace CKAN.NetKAN.Validators
{
    internal sealed class IsCkanModuleValidator : IValidator
    {
        public void Validate(Metadata metadata)
        {
            CkanModule.FromJson(metadata.Json().ToString());
        }
    }
}
