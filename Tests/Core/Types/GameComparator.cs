using System;
using CKAN.Types;
using CKAN.Types.GameComparator;
using CKAN.Versioning;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core.Types
{
    [TestFixture]
    public class GameComparator
    {
        static readonly KspVersion gameVersion = KspVersion.Parse("1.0.4");
        CkanModule gameMod;

        [SetUp]
        public void Setup()
        {
            // Refresh our mod every time since our tests will hack its version and things.
            gameMod = TestData.kOS_014_module();
        }

        [Test]
        [TestCase(typeof(StrictGameComparator), true)]
        [TestCase(typeof(GrasGameComparator), true)]
        [TestCase(typeof(YoyoGameComparator), true)]
        public void TotallyCompatible(Type type, bool expected)
        {
            var comparator = (IGameComparator) Activator.CreateInstance(type);

            // Mark the mod as being for 1.0.4
            gameMod.ksp_version = gameMod.ksp_version_min = gameMod.ksp_version_max
                = KspVersion.Parse("1.0.4");

            // Now test!
            Assert.AreEqual(expected, comparator.Compatible(gameVersion, gameMod));
        }

        [Test]
        [TestCase(typeof(StrictGameComparator), false)]
        [TestCase(typeof(GrasGameComparator), true)]
        [TestCase(typeof(YoyoGameComparator), true)]
        public void GenerallySafeLax(Type type, bool expected)
        {
            var comparator = (IGameComparator) Activator.CreateInstance(type);

            // We're going to tweak compatibly to mark the mod as being for 1.0.3
            gameMod.ksp_version = gameMod.ksp_version_min = gameMod.ksp_version_max
                = KspVersion.Parse("1.0.3");

            // Now test!
            Assert.AreEqual(expected, comparator.Compatible(gameVersion, gameMod));
        }

        [Test]
        [TestCase(typeof(StrictGameComparator), false)]
        [TestCase(typeof(GrasGameComparator), false)]
        [TestCase(typeof(YoyoGameComparator), true)]
        public void GenerallySafeStrict(Type type, bool expected)
        {
            var comparator = (IGameComparator) Activator.CreateInstance(type);

            // We're going to tweak compatibly to mark the mod as being for 1.0.3 ONLY
            gameMod.ksp_version = gameMod.ksp_version_min = gameMod.ksp_version_max
                = KspVersion.Parse("1.0.3");

            gameMod.ksp_version_strict = true;

            // Now test!
            Assert.AreEqual(expected, comparator.Compatible(gameVersion, gameMod));
        }

        [Test]
        [TestCase(typeof(StrictGameComparator), false)]
        [TestCase(typeof(GrasGameComparator), false)]
        [TestCase(typeof(YoyoGameComparator), true)]
        public void Incompatible(Type type, bool expected)
        {
            var comparator = (IGameComparator) Activator.CreateInstance(type);

            // The mod already starts off being incompatible, so just do the test. :)
            Assert.AreEqual(expected, comparator.Compatible(gameVersion, gameMod));
        }
    }
}

