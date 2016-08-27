using CKAN.Types;
using NUnit.Framework;

namespace Tests.Core.Types
{
    [TestFixture]
    public class License
    {
        [Test]
        public void LicenseGood()
        {
            var license = new CKAN.Types.License("GPL-3.0");
            Assert.IsInstanceOf<CKAN.Types.License>(license);
            Assert.AreEqual("GPL-3.0", license.ToString());
        }

        [Test]
        public void LicenseBad()
        {
            Assert.Throws<BadMetadataKraken>(delegate
            {
                // Not a valid license string, contains spaces.
                new CKAN.Types.License("GPL 3.0");
            });
        }
    }
}

