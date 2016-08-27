using System;
using System.IO;
using CKAN;
using CKAN.Types;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core
{
    [TestFixture]
    public class KSP
    {
        private CKAN.KSP ksp;
        private string ksp_dir;

        [SetUp]
        public void Setup()
        {
            ksp_dir = TestData.NewTempDir();
            TestData.CopyDirectory(TestData.good_ksp_dir(), ksp_dir);
            ksp = new CKAN.KSP(ksp_dir,NullUser.User);
        }

        [TearDown]
        public void TearDown()
        {
            if (ksp != null)
            {
                ksp.Dispose();
            }
                    
            Directory.Delete(ksp_dir, true);
        }

        [Test]
        public void IsGameDir()
        {
            // Our test data directory should be good.
            Assert.IsTrue(CKAN.KSP.IsKspDir(TestData.good_ksp_dir()));

            // As should our copied folder.
            Assert.IsTrue(CKAN.KSP.IsKspDir(ksp_dir));

            // And the one from our KSP instance.
            Assert.IsTrue(CKAN.KSP.IsKspDir(ksp.GameDir));

            // All these ones should be bad.
            foreach (string dir in TestData.bad_ksp_dirs())
            {
                Assert.IsFalse(CKAN.KSP.IsKspDir(dir));
            }
        }

        [Test]
        public void Training()
        {
            // Use Uri to force line separators to a consistent format
            var canonicalPath = new Uri(Path.Combine(ksp_dir, "saves", "training")).LocalPath;
            var testPath = CKAN.KSPPathUtils.GetGameDirectory(ksp.GameDir, GameDirectory.Tutorial);

            Assert.AreEqual(canonicalPath, new Uri(testPath).LocalPath);
        }

        [Test]
        public void ScanDlls()
        {
            var dataDir = CKAN.KSPPathUtils.GetGameDirectory(ksp.GameDir, GameDirectory.GameData);
            string path = Path.Combine(dataDir, "Example.dll");

            Assert.IsFalse(ksp.Registry.IsInstalled("Example"), "Example should start uninstalled");

            File.WriteAllText(path, "Not really a DLL, are we?");

            ksp.ScanGameData();

            Assert.IsTrue(ksp.Registry.IsInstalled("Example"), "Example installed");

            GameVersion version = ksp.Registry.InstalledVersion("Example");
            Assert.IsInstanceOf<DllVersion>(version, "DLL detected as a DLL, not full mod");

            // Now let's do the same with different case.
            string path2 = Path.Combine(dataDir, "NewMod.DLL");

            Assert.IsFalse(ksp.Registry.IsInstalled("NewMod"));
            File.WriteAllText(path2, "This text is irrelevant. You will be assimilated");

            ksp.ScanGameData();

            Assert.IsTrue(ksp.Registry.IsInstalled("NewMod"));
        }

        [Test]
        public void ToAbsolute()
        {
            Assert.AreEqual(
                CKAN.KSPPathUtils.NormalizePath(
                    Path.Combine(ksp_dir, "GameData/HydrazinePrincess")
                ),
                ksp.ToAbsoluteGameDir("GameData/HydrazinePrincess")
            );
        }

        [Test]
        public void ToRelative()
        {
            string absolute = Path.Combine(ksp_dir, "GameData/HydrazinePrincess");

            Assert.AreEqual(
                "GameData/HydrazinePrincess",
                ksp.ToRelativeGameDir(absolute)
            );
        }

    }
}
