﻿using CKAN.Types;
using NUnit.Framework;

namespace Tests.Core.Types
{
    [TestFixture]
    public class RelationshipDescriptor
    {

        GameVersion autodetected = new DllVersion();

        [Test]
        [TestCase("0.23","0.23", true)]
        [TestCase("wibble","wibble",true)]
        [TestCase("0.23", "0.23.1", false)]
        [TestCase("wibble","wobble", false)]
        public void VersionWithinBounds_ExactFalse(string version, string other_version, bool expected)
        {
            var rd = new CKAN.Types.RelationshipDescriptor { version = new GameVersion(version) };
            Assert.AreEqual(expected, rd.version_within_bounds(new GameVersion(other_version)));
        }

        [Test]
        [TestCase("0.20","0.23","0.21", true)]
        [TestCase("0.20","0.23","0.20", true)]
        [TestCase("0.20","0.23","0.23", true)]
        public void VersionWithinBounds_MinMax(string min, string max, string compare_to, bool expected)
        {
            var rd = new CKAN.Types.RelationshipDescriptor
            {
                min_version = new GameVersion(min),
                max_version = new GameVersion(max)
            };

            Assert.AreEqual(expected, rd.version_within_bounds(new GameVersion(compare_to)));
        }

        [Test]
        [TestCase("0.23")]
        public void VersionWithinBounds_vs_AutoDetectedMod(string version)
        {
            var rd = new CKAN.Types.RelationshipDescriptor { version = new GameVersion(version) };

            Assert.True(rd.version_within_bounds(autodetected));
        }

        [Test]
        [TestCase("0.20","0.23")]
        public void VersionWithinBounds_MinMax_vs_AutoDetectedMod(string min, string max)
        {
            var rd = new CKAN.Types.RelationshipDescriptor
            {
                min_version = new GameVersion(min),
                max_version = new GameVersion(max)
            };
            
            Assert.True(rd.version_within_bounds(autodetected));
        }

        [Test]
        [TestCase("wibble")]
        public void VersionWithinBounds_Null(string version)
        {
            var rd = new CKAN.Types.RelationshipDescriptor();

            Assert.True(rd.version_within_bounds(new GameVersion(version)));
        }

        [Test]
        public void VersionWithinBounds_AllNull()
        {
            var rd = new CKAN.Types.RelationshipDescriptor();

            Assert.True(rd.version_within_bounds(null));        }
    }
}

