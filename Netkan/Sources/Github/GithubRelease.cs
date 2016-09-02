using System;
using CKAN.Types;

namespace CKAN.NetKAN.Sources.Github
{
    public sealed class GithubRelease
    {
        public string Author { get; private set; }
        public GameVersion Version { get; private set; }
        public Uri Download { get; private set; }

        public GithubRelease(string author, GameVersion version, Uri download)
        {
            Author = author;
            Version = version;
            Download = download;
        }
    }
}
