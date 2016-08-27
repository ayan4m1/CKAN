using System;
using System.IO;
using System.Threading;
using CKAN;

namespace Tests.Data
{
    /// <summary>
    /// A disposable KSP instance. Use the `.KSP` property to access, will
    /// be automatically cleaned up on DisposableKSP falling out of using() scope.
    /// </summary>
    public class DisposableKSP : IDisposable
    {
        private readonly string _goodKsp = TestData.good_ksp_dir();
        private readonly string _disposableDir;

        public KSP KSP { get; private set; }

        /// <summary>
        /// Creates a copy of the provided argument, or a known-good KSP install if passed null.
        /// Use .KSP to access the KSP object itself.
        /// </summary>
        public DisposableKSP(string directoryToClone = null, string registryFile = null)
        {
            directoryToClone = directoryToClone ?? _goodKsp;
            _disposableDir = TestData.NewTempDir();
            TestData.CopyDirectory(directoryToClone, _disposableDir);

            // If we've been given a registry file, then copy it into position before
            // creating our KSP object.
            if (registryFile != null)
            {
                var registryDir = Path.Combine(_disposableDir, "CKAN");
                var registryPath = Path.Combine(registryDir, "registry.json");

                Directory.CreateDirectory(registryDir);
                File.Copy(registryFile, registryPath, true);
            }

            KSP = new KSP(_disposableDir, NullUser.User);
        }

        public void Dispose()
        {
            FinalizeDirectory();
            KSP.Dispose();
            KSP = null; // In case .Dispose() was called manually.
        }

        private void FinalizeDirectory()
        {
            // todo: fix two stupid magic numbers
            for (var x = 0; x < 6; x++)
            {
                try
                {
                    Directory.Delete(_disposableDir, true);
                    break;
                }
                catch (IOException)
                {
                    // todo: logger
                    Thread.Sleep(100);
                }
            }
        }
    }
}

