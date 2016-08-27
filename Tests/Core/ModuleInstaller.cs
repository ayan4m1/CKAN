using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using CKAN;
using CKAN.Relationships;
using CKAN.Types;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core
{
    [TestFixture]
    public class ModuleInstaller
    {
        private string _flagPath;
        private string _dogeZip;
        private CkanModule _dogeMod;

        private string _mmZip;
        private CkanModule _mmMod;

        [SetUp]
        public void Setup()
        {
            // By setting these for every test, we can make sure our tests can change
            // them any way they like without harming other tests.
            _flagPath = "DogeCoinFlag-1.01/GameData/DogeCoinFlag/Flags/dogecoin.png";
            _dogeZip = TestData.DogeCoinFlagZip();
            _dogeMod = TestData.DogeCoinFlag_101_module();

            _mmZip = TestData.ModuleManagerZip();
            _mmMod = TestData.ModuleManagerModule();
        }

        [Test]
        public void Sanity()
        {
            // Test our assumptions are right with the data we're using.

            // Our _find mod should have a find section, but not a file section.
            CkanModule mod = TestData.DogeCoinFlag_101_module_find();
            Assert.IsNull(mod.install[0].file);
            Assert.IsNotNull(mod.install[0].find);
        }

        [Test]
        public void GenerateDefaultInstall()
        {
            string filename = TestData.DogeCoinFlagZip();
            using (var zipfile = new ZipFile(filename))
            {
                ModuleInstallDescriptor stanza = ModuleInstallDescriptor.DefaultInstallStanza("DogeCoinFlag", zipfile);

                TestDogeCoinStanza(stanza);

                // Same again, but screwing up the case (we see this *all the time*)
                ModuleInstallDescriptor stanza2 = ModuleInstallDescriptor.DefaultInstallStanza("DogecoinFlag", zipfile);

                TestDogeCoinStanza(stanza2);

                // Now what happens if we can't find what to install?

                Assert.Throws<FileNotFoundKraken>(delegate
                {
                    ModuleInstallDescriptor.DefaultInstallStanza("Xyzzy", zipfile);
                });

                // Make sure the FNFKraken looks like what we expect.
                try
                {
                    ModuleInstallDescriptor.DefaultInstallStanza("Xyzzy", zipfile);
                }
                catch (FileNotFoundKraken kraken)
                {
                    Assert.AreEqual("Xyzzy", kraken.file);
                }
            }
        }

        // Test data: different ways to install the same file.
        public static CkanModule[] doge_mods =
        {
            TestData.DogeCoinFlag_101_module(),
            TestData.DogeCoinFlag_101_module_find()
        };

        [Test][TestCaseSource("doge_mods")]
        public void FindInstallableFiles(CkanModule mod)
        {
            List<InstallableFile> contents = CKAN.ModuleInstaller.FindInstallableFiles(mod, _dogeZip, null);
            List<string> filenames = new List<string>();

            Assert.IsNotNull(contents);

            // Make sure it's actually got files!
            Assert.IsTrue(contents.Count > 0);

            foreach (var file in contents)
            {
                // Make sure the destination paths are null, because we supplied no KSP instance.
                Assert.IsNull(file.Destination);

                // Make sure the source paths are not null, that would be silly!
                Assert.IsNotNull(file.Source);

                // And make sure our makeDir info is filled in.
                Assert.IsNotNull(file.MakeDir);

                filenames.Add(file.Source.Name);
            }

            // Ensure we've got an expected file
            Assert.Contains("DogeCoinFlag-1.01/GameData/DogeCoinFlag/Flags/dogecoin.png", filenames);
        }

        [Test][TestCaseSource("doge_mods")]
        public void FindInstallableFilesWithKSP(CkanModule mod)
        {
            using (var tidy = new DisposableKSP())
            {
                List<InstallableFile> contents = CKAN.ModuleInstaller.FindInstallableFiles(mod, _dogeZip, tidy.KSP);

                // See if we can find an expected estination path in the right place.
                string file = contents
                    .Select(x => x.Destination).FirstOrDefault(
                    x => Regex.IsMatch(x, "GameData/DogeCoinFlag/Flags/dogecoin\\.png$"));

                Assert.IsNotNull(file);
            }
        }

        // GH #315, all of these should result in the same output.
        // Even though they're not necessarily all spec-valid, we should accept them
        // nonetheless.
        public static readonly string[] SuchPaths =
        {
            "GameData/SuchTest",
            "GameData/SuchTest/",
            "GameData\\SuchTest",
            "GameData\\SuchTest\\",
            "GameData\\SuchTest/",
            "GameData/SuchTest\\"
        };

        [Test]
        [TestCaseSource("SuchPaths")]
        public void FindInstallbleFilesWithBonusPath(string path)
        {
            _dogeMod.install[0].install_to = path;
            using (var tidy = new DisposableKSP())
            {
                IEnumerable<InstallableFile> contents =
                    CKAN.ModuleInstaller.FindInstallableFiles(_dogeMod, _dogeZip, tidy.KSP);

                string file = contents
                    .Select(x => x.Destination).FirstOrDefault(
                    x => Regex.IsMatch(x, "GameData/SuchTest/DogeCoinFlag/Flags/dogecoin\\.png$"));

                Assert.IsNotNull(file);
            }
        }

        [Test]
        public void ModuleManagerInstall()
        {
            using (var tidy = new DisposableKSP())
            {
                List<InstallableFile> contents = CKAN.ModuleInstaller.FindInstallableFiles(_mmMod, _mmZip, tidy.KSP);

                string file = contents
                    .Select(x => x.Destination).FirstOrDefault(
                    x => Regex.IsMatch(x, @"ModuleManager\.2\.5\.1\.dll$"));

                Assert.IsNotNull(file, "ModuleManager install");
            }
        }

        [Test][TestCaseSource("doge_mods")]
        // Make sure all our filters work.
        public void FindInstallableFilesWithFilter(CkanModule mod)
        {
            var extraDoge = TestData.DogeCoinFlagZipWithExtras();
            var contents = CKAN.ModuleInstaller.FindInstallableFiles(mod, extraDoge, null);
            var files = contents.Select(x => x.Source.Name).ToList();

            Assert.IsTrue(files.Contains("DogeCoinFlag-1.01/GameData/DogeCoinFlag/Flags/dogecoin.png"), "dogecoin.png");
            Assert.IsFalse(files.Contains("DogeCoinFlag-1.01/GameData/DogeCoinFlag/README.md"), "Filtered README 1");
            Assert.IsFalse(files.Contains("DogeCoinFlag-1.01/GameData/DogeCoinFlag/Flags/README.md"), "Filtered README 2");
            Assert.IsFalse(files.Contains("DogeCoinFlag-1.01/GameData/DogeCoinFlag/notes.txt.bak"), "Filtered .bak file");
        }

        [Test]
        public void No_Installable_Files()
        {
            // This tests GH #93

            var dogezip = TestData.DogeCoinFlagZip();
            var buggedMod = TestData.DogeCoinFlag_101_bugged_module();

            Assert.Throws<BadMetadataKraken>(delegate
            {
                CKAN.ModuleInstaller.FindInstallableFiles(buggedMod, dogezip, null);
            });

            try
            {
                CKAN.ModuleInstaller.FindInstallableFiles(buggedMod, dogezip, null);
            }
            catch (BadMetadataKraken ex)
            {
                // Make sure our module information is attached.
                Assert.IsNotNull(ex.module);
                Assert.AreEqual(buggedMod.identifier, ex.module.identifier);
            }
        }

#pragma warning disable 0414

        // All of these targets should fail.
        public static readonly string[] BadTargets = {
            "GameDataIsTheBestData", "Shups", "GameData/../../../../etc/pwned",
            "Ships/Foo", "GameRoot/saves", "GameRoot/CKAN", "GameData/..",
            @"GameData\..\..\etc\pwned", @"GameData\.."
        };

#pragma warning restore 0414

        [Test]
        [TestCaseSource("BadTargets")]
        public void FindInstallableFilesWithBadTarget(string location)
        {
            // This install location? It shouldn't be valid.
            _dogeMod.install[0].install_to = location;

            Assert.Throws<BadInstallLocationKraken>(delegate
            {
                CKAN.ModuleInstaller.FindInstallableFiles(_dogeMod, _dogeZip, null);
            });
        }

        [Test]
        // GH #205, make sure we write in *binary*, not text.
        public void BinaryNotText_205()
        {
            // Use CopyZipEntry (via CopyDogeFromZip) and make sure it
            // comes out the right size.
            string tmpfile = CopyDogeFromZip();
            long size = new FileInfo(tmpfile).Length;

            try
            {
                // Compare recorded length against what we expect.
                Assert.AreEqual(52043, size);
            }
            finally
            {
                // Tidy up.
                File.Delete(tmpfile);
            }
        }

        [Test]
        // Make sure when we roll-back a transaction, files written with CopyZipEntry go
        // back to their pre-transaction state.
        public void FileSysRollBack()
        {
            string file;

            using (var scope = new TransactionScope())
            {
                file = CopyDogeFromZip();
                Assert.IsTrue(new FileInfo(file).Length > 0);
                scope.Dispose(); // Rollback
            }

            // And now, our file should be gone!
            Assert.IsFalse(File.Exists(file));
        }

        [Test]
        // We don't allow overwriting of files when doing installs. Hooray!
        public void DontOverWrite_208()
        {
            using (ZipFile zipfile = new ZipFile(TestData.DogeCoinFlagZip()))
            {
                var entry = zipfile.GetEntry(_flagPath);
                var tmpfile = Path.GetTempFileName();

                Assert.Throws<FileExistsKraken>(delegate
                {
                    CKAN.ModuleInstaller.CopyZipEntry(zipfile, entry, tmpfile, false);
                });

                // Cleanup
                File.Delete(tmpfile);
            }
        }

        [Test]
        [Category("TODO")]
        [Explicit]
        //Test how we handle corrupt data
        public void CorruptZip_242()
        {
            var corruptDogeZip = TestData.DogeCoinFlagZipCorrupt();

            using (var zipfile = new ZipFile(corruptDogeZip))
            {
                // GenerateDefault Install
                ModuleInstallDescriptor.DefaultInstallStanza("DogeCoinFlag", zipfile);

                // FindInstallableFiles
                CkanModule dogemod = TestData.DogeCoinFlag_101_module();
                CKAN.ModuleInstaller.FindInstallableFiles(dogemod, corruptDogeZip, null);
            }
        }

        [TestCase("GameData/kOS", "GameData/kOS/Plugins/kOS.dll", "GameData", null, "GameData/kOS/Plugins/kOS.dll")]
        [TestCase("kOS-1.1/GameData/kOS", "kOS-1.1/GameData/kOS/Plugins/kOS.dll", "GameData", null, "GameData/kOS/Plugins/kOS.dll")]
        [TestCase("ModuleManager.2.5.1.dll", "ModuleManager.2.5.1.dll", "GameData", null, "GameData/ModuleManager.2.5.1.dll")]
        [TestCase("Ships", "Ships/SPH/FAR Firehound.craft", "SomeDir/Ships", null, "SomeDir/Ships/SPH/FAR Firehound.craft")]
        [TestCase("GameData/kOS", "GameData/kOS/Plugins/kOS.dll", "GameData", "kOS-Renamed", "GameData/kOS-Renamed/Plugins/kOS.dll")]
        [TestCase("kOS-1.1/GameData/kOS", "kOS-1.1/GameData/kOS/Plugins/kOS.dll", "GameData", "kOS-Renamed", "GameData/kOS-Renamed/Plugins/kOS.dll")]
        [TestCase("ModuleManager.2.5.1.dll", "ModuleManager.2.5.1.dll", "GameData", "ModuleManager-Renamed.dll", "GameData/ModuleManager-Renamed.dll")]
        public void TransformOutputName(string file, string outputName, string installDir, string @as, string expected)
        {
            // Act
            var result = CKAN.ModuleInstaller.TransformOutputName(file, outputName, installDir, @as);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("GameData", "GameData/kOS/Plugins/kOS.dll", "GameData", "GameData-Renamed")]
        [TestCase("Ships", "Ships/SPH/FAR Firehound.craft", "SomeDir/Ships", "Ships-Renamed")]
        [TestCase("GameData/kOS", "GameData/kOS/Plugins/kOS.dll", "GameData", "kOS/Renamed")]
        [TestCase("kOS-1.1/GameData/kOS", "kOS-1.1/GameData/kOS/Plugins/kOS.dll", "GameData", "kOS/Renamed")]
        [TestCase("ModuleManager.2.5.1.dll", "ModuleManager.2.5.1.dll", "GameData", "Renamed/ModuleManager.dll")]
        public void TransformOutputNameThrowsOnInvalidParameters(string file, string outputName, string installDir, string @as)
        {
            // Act
            TestDelegate act = () => CKAN.ModuleInstaller.TransformOutputName(file, outputName, installDir, @as);

            // Assert
            Assert.That(act, Throws.Exception);
        }

        private string CopyDogeFromZip()
        {
            string dogezip = TestData.DogeCoinFlagZip();
            ZipFile zipfile = new ZipFile(dogezip);

            ZipEntry entry = zipfile.GetEntry(_flagPath);
            string tmpfile = Path.GetTempFileName();

            // We have to delete our temporary file, as CZE refuses to overwrite; huzzah!
            File.Delete(tmpfile);
            CKAN.ModuleInstaller.CopyZipEntry(zipfile, entry, tmpfile, false);

            return tmpfile;
        }

        [Test]
        public void UninstallModNotFound()
        {
            using (var tidy = new DisposableKSP())
            {
                KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(tidy.KSP)){CurrentInstance = tidy.KSP};

                Assert.Throws<ModNotInstalledKraken>(delegate
                {
                    // This should throw, as our tidy KSP has no mods installed.
                    CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).UninstallList("Foo");
                });

                manager.CurrentInstance = null; // I weep even more.
            }
        }

        [Test]
        public void CanInstallMod()
        {
            string modFileName = "DogeCoinFlag/Flags/dogecoin.png";

            // Create a new disposable KSP instance to run the test on.
            using (DisposableKSP ksp = new DisposableKSP())
            {
                // Make sure the mod is not installed.
                var dataDir = CKAN.KSPPathUtils.GetGameDirectory(ksp.KSP.GameDir, GameDirectory.GameData);
                var modFilePath = Path.Combine(dataDir, modFileName);

                Assert.IsFalse(File.Exists(modFilePath));

                // Copy the zip file to the cache directory.
                Assert.IsFalse(ksp.KSP.Cache.IsCachedZip(TestData.DogeCoinFlag_101_module().download));

                var cachePath = ksp.KSP.Cache.Store(TestData.DogeCoinFlag_101_module().download, TestData.DogeCoinFlagZip());

                Assert.IsTrue(ksp.KSP.Cache.IsCachedZip(TestData.DogeCoinFlag_101_module().download));
                Assert.IsTrue(File.Exists(cachePath));

                // Mark it as available in the registry.
                Assert.AreEqual(0, ksp.KSP.Registry.Available(ksp.KSP.Version).Count());

                ksp.KSP.Registry.AddAvailable(TestData.DogeCoinFlag_101_module());

                Assert.AreEqual(1, ksp.KSP.Registry.Available(ksp.KSP.Version).Count());

                // Attempt to install it.
                List<string> modules = new List<string> {TestData.DogeCoinFlag_101_module().identifier};

                CKAN.ModuleInstaller.GetInstance(ksp.KSP, NullUser.User).InstallList(modules, new RelationshipResolverOptions());

                // Check that the module is installed.
                Assert.IsTrue(File.Exists(modFilePath));
            }
        }

        [Test]
        public void CanUninstallMod()
        {
            string mod_file_name = "DogeCoinFlag/Flags/dogecoin.png";

            // Create a new disposable KSP instance to run the test on.
            using (var ksp = new DisposableKSP())
            {
                KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(ksp.KSP)){CurrentInstance = ksp.KSP};

                var cacheDir = CKAN.KSPPathUtils.GetGameDirectory(ksp.KSP.GameDir, GameDirectory.DownloadCacheDir);
                Assert.IsTrue(Directory.Exists(cacheDir));

                Assert.IsTrue(Directory.Exists(ksp.KSP.DownloadCacheDir));

                var dataDir = CKAN.KSPPathUtils.GetGameDirectory(ksp.KSP.GameDir, GameDirectory.GameData);
                var modFilePath = Path.Combine(dataDir, modFileName);

                // Install the test mod.
                ksp.KSP.Cache.Store(TestData.DogeCoinFlag_101_module().download, TestData.DogeCoinFlagZip());
                ksp.KSP.Registry.AddAvailable(TestData.DogeCoinFlag_101_module());

                List<string> modules = new List<string> {TestData.DogeCoinFlag_101_module().identifier};

                CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).InstallList(modules, new RelationshipResolverOptions());

                // Check that the module is installed.
                Assert.IsTrue(File.Exists(modFilePath));

                // Attempt to uninstall it.
                CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).UninstallList(modules);

                // Check that the module is not installed.
                Assert.IsFalse(File.Exists(mod_file_path));
            }
        }

        [Test]
        public void UninstallEmptyDirs()
        {
            string emptyFolderName = "DogeCoinFlag";

            // Create a new disposable KSP instance to run the test on.
            using (var ksp = new DisposableKSP())
            {
                KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(ksp.KSP)){CurrentInstance = ksp.KSP};

                Assert.IsTrue(Directory.Exists(ksp.KSP.DownloadCacheDir));

                string directoryPath = Path.Combine(ksp.KSP.GameData, emptyFolderName);

                // Install the base test mod.

                var registry = ksp.KSP.Registry;
                ksp.KSP.Cache.Store(TestData.DogeCoinFlag_101_module().download, TestData.DogeCoinFlagZip());
                registry.AddAvailable(TestData.DogeCoinFlag_101_module());

                List<string> modules = new List<string> {TestData.DogeCoinFlag_101_module().identifier};

                CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).InstallList(modules, new RelationshipResolverOptions());

                modules.Clear();

                // Install the plugin test mod.
                ksp.KSP.Cache.Store(TestData.DogeCoinPlugin_module().download, TestData.DogeCoinPluginZip());
                registry.AddAvailable(TestData.DogeCoinPlugin_module());

                modules.Add(TestData.DogeCoinPlugin_module().identifier);

                CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).InstallList(modules, new RelationshipResolverOptions());

                modules.Clear();

                // Check that the directory is installed.
                Assert.IsTrue(Directory.Exists(directoryPath));

                // Uninstall both mods.

                modules.Add(TestData.DogeCoinFlag_101_module().identifier);
                modules.Add(TestData.DogeCoinPlugin_module().identifier);

                CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).UninstallList(modules);

                // Check that the directory has been deleted.
                Assert.IsFalse(Directory.Exists(directoryPath));
            }
        }

        [Test]
        public void ModuleManagerInstancesAreDecoupled()
        {
            string modFileName = "DogeCoinFlag/Flags/dogecoin.png";

            // Create a new disposable KSP instance to run the test on.
            Assert.DoesNotThrow(delegate
            {
                for (int i = 0; i < 5; i++)
                {
                    using (DisposableKSP ksp = new DisposableKSP())
                    {
                        // Copy the zip file to the cache directory.
                        ksp.KSP.Cache.Store(TestData.DogeCoinFlag_101_module().download, TestData.DogeCoinFlagZip());

                        // Mark it as available in the registry.
                        ksp.KSP.Registry.AddAvailable(TestData.DogeCoinFlag_101_module());

                        // Attempt to install it.
                        List<string> modules = new List<string> {TestData.DogeCoinFlag_101_module().identifier};

                        CKAN.ModuleInstaller.GetInstance(ksp.KSP, NullUser.User).InstallList(modules, new RelationshipResolverOptions());

                        // Check that the module is installed.
                        var dataDir = CKAN.KSPPathUtils.GetGameDirectory(ksp.KSP.GameDir, GameDirectory.GameData);
                        var modFilePath = Path.Combine(dataDir, modFileName);

                        Assert.IsTrue(File.Exists(modFilePath));
                    }
                }
            });
        }

        [TestCase("Ships")]
        [TestCase("Ships/VAB")]
        [TestCase("Ships/SPH")]
        [TestCase("Ships/@thumbs")]
        [TestCase("Ships/@thumbs/VAB")]
        [TestCase("Ships/@thumbs/SPH")]
        public void AllowsInstallsToShipsDirectories(string directory)
        {
            // Arrange
            var zip = ZipFile.Create(new MemoryStream());
            zip.BeginUpdate();
            zip.AddDirectory("ExampleShips");
            zip.Add(new ZipEntry("/ExampleShips/AwesomeShip.craft") { Size = 0, CompressedSize = 0 });
            zip.CommitUpdate();

            var mod = CkanModule.FromJson(string.Format(@"
            {{
                ""spec_version"": 1,
                ""identifier"": ""AwesomeMod"",
                ""version"": ""1.0.0"",
                ""download"": ""https://awesomemod.example/AwesomeMod.zip"",
                ""install"": [
                    {{
                        ""file"": ""ExampleShips/AwesomeShip.craft"",
                        ""install_to"": ""{0}""
                    }}
                ]
            }}
            ", directory));

            // Act
            List<InstallableFile> results;
            using (var ksp = new DisposableKSP())
            {
                results = CKAN.ModuleInstaller.FindInstallableFiles(mod.install.First(), zip, ksp.KSP);
            }

            // Assert
            Assert.That(
                results.Count(i => i.Destination.EndsWith(string.Format("/{0}/AwesomeShip.craft", directory))) == 1,
                Is.True
            );
        }

        // TODO: It would be nice to merge this and the above function into one super
        // test.
        [Test]
        public void AllowInstallsToScenarios()
        {
            // Bogus zip with example to install.
            var zip = ZipFile.Create(new MemoryStream());
            zip.BeginUpdate();
            zip.AddDirectory("saves");
            zip.AddDirectory("saves/scenarios");
            zip.Add(new ZipEntry("/saves/scenarios/AwesomeRace.sfs") { Size = 0, CompressedSize = 0 });
            zip.CommitUpdate();

            var mod = CkanModule.FromJson(@"
                {
                    ""spec_version"": ""v1.14"",
                    ""identifier"": ""AwesomeMod"",
                    ""version"": ""1.0.0"",
                    ""download"": ""https://awesomemod.example/AwesomeMod.zip"",
                    ""install"": [
                        {
                            ""file"": ""saves/scenarios/AwesomeRace.sfs"",
                            ""install_to"": ""Scenarios""
                        }
                    ]
                }")
            ;

            using (var ksp = new DisposableKSP())
            {
                var results = CKAN.ModuleInstaller.FindInstallableFiles(mod.install.First(), zip, ksp.KSP);
                var scenarioDir = CKAN.KSPPathUtils.GetGameDirectory(ksp.KSP.GameDir, GameDirectory.Scenarios);

                Assert.AreEqual(
                    Path.Combine(scenarioDir, "AwesomeRace.sfs"),
                    results.First().Destination
                );
            }
        }

        private static void TestDogeCoinStanza(ModuleInstallDescriptor stanza)
        {
            Assert.AreEqual("GameData", stanza.install_to);
            Assert.AreEqual("DogeCoinFlag-1.01/GameData/DogeCoinFlag", stanza.file);
        }

    }
}

