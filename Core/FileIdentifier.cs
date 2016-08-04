﻿using ICSharpCode.SharpZipLib.GZip;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CKAN
{
    public static class FileIdentifier
    {
        /// <summary>
        /// Determine if there are any EASCII or Unicode characters in the supplied stream.
        /// </summary>
        /// <returns><c>true</c> if all characters are ASCII, <c>false</c> otherwise.</returns>
        /// <param name="stream">Stream to the file.</param>
        private static bool CheckASCII(Stream stream)
        {
            var rawByteCount = stream.Length;
            var asciiByteCount = -1L;

            using (var reader = new StreamReader(stream))
            {
                asciiByteCount = Encoding.ASCII.GetBytes(reader.ReadToEnd()).Length;
            }

            return rawByteCount == asciiByteCount;
        }

        /// <summary>
        /// Checks if the file is of type gzip.
        /// </summary>
        /// <returns><c>true</c>, if gzip, <c>false</c> otherwise.</returns>
        /// <param name="stream">Stream to the file.</param>
        private static bool CheckGZip(Stream stream)
        {
            // Rewind the stream to the origin of the file.
            stream.Seek(0, SeekOrigin.Begin);

            // Define the buffer and magic types to compare against.
            byte[] buffer = new byte[2];
            byte[] gzip_identifier = { 0x1F, 0x8B };

            // Read the first 2 bytes of the file into the buffer.
            int bytes_read = stream.Read(buffer, 0, buffer.Length);

            // Check if we reached EOF before reading enough bytes.
            if (bytes_read != buffer.Length)
            {
                return false;
            }

            // Compare against the magic numbers.
            if (buffer.SequenceEqual(gzip_identifier))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the file is of type tar.
        /// </summary>
        /// <returns><c>true</c>, if tar, <c>false</c> otherwise.</returns>
        /// <param name="stream">Stream to the file.</param>
        private static bool CheckTar(Stream stream)
        {
            if (stream.CanSeek)
            {
                // Rewind the stream to the origin of the file.
                stream.Seek(0, SeekOrigin.Begin);
            }

            // Define the buffer and magic types to compare against.
            byte[] buffer = new byte[5];
            byte[] tar_identifier = { 0x75, 0x73, 0x74, 0x61, 0x72 };

            // Advance the stream position to offset 257. This method circumvents stream which can't seek.
            for (int i = 0; i < 257; i++)
            {
                stream.ReadByte();
            }

            // Read 5 bytes into the buffer.
            int bytes_read = stream.Read(buffer, 0, buffer.Length);

            // Check if we reached EOF before reading enough bytes.
            if (bytes_read != buffer.Length)
            {
                return false;
            }

            // Compare against the magic numbers.
            if (buffer.SequenceEqual(tar_identifier))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the file is of type zip.
        /// </summary>
        /// <returns><c>true</c>, if zip, <c>false</c> otherwise.</returns>
        /// <param name="stream">Stream to the file.</param>
        private static bool CheckZip(Stream stream)
        {
            // Rewind the stream to the origin of the file.
            stream.Seek(0, SeekOrigin.Begin);

            // Define the buffer and magic types to compare against.
            byte[] buffer = new byte[4];
            byte[] zip_identifier = { 0x50, 0x4B, 0x03, 0x04 };
            byte[] zip_identifier_empty = { 0x50, 0x4B, 0x05, 0x06 };
            byte[] zip_identifier_spanned = { 0x50, 0x4B, 0x07, 0x08 };

            // Read the first 4 bytes of the file into the buffer.
            int bytes_read = stream.Read(buffer, 0, buffer.Length);

            // Check if we reached EOF before reading enough bytes.
            if (bytes_read != buffer.Length)
            {
                return false;
            }

            // Compare against the magic numbers.
            if (buffer.SequenceEqual(zip_identifier) || buffer.SequenceEqual(zip_identifier_empty) || buffer.SequenceEqual(zip_identifier_spanned))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Identifies the file using magic numbers.
        /// </summary>
        /// <returns>The filetype.</returns>
        /// <param name="stream">Open stream to the file.</param>
        public static FileType IdentifyFile(Stream stream)
        {
            var type = FileType.Unknown;

            // Check the input.
            if (stream == null || !stream.CanSeek)
            {
                return type;
            }

            // Start performing checks.
            if (CheckGZip(stream))
            {
                // This may contain a tar file inside, create a new stream and check.
                stream.Seek(0, SeekOrigin.Begin);
                using (var archive = new GZipInputStream(stream))
                {
                    type = CheckTar(archive) ? FileType.TarGz : FileType.GZip;
                }
            }
            else if (CheckTar(stream))
            {
                type = FileType.Tar;
            }
            else if (CheckZip(stream))
            {
                type = FileType.Zip;
            }
            else if (CheckASCII(stream))
            {
                type = FileType.ASCII;
            }

            return type;
        }

        /// <summary>
        /// Identifies the file using magic numbers.
        /// </summary>
        /// <returns>The filetype.</returns>
        /// <param name="path">Path to the file.</param>
        public static FileType IdentifyFile(string path)
        {
            FileType type = FileType.Unknown;

            // Check input.
            if (string.IsNullOrWhiteSpace(path))
            {
                return type;
            }

            // Check that the file exists.
            if (!File.Exists(path))
            {
                return type;
            }

            // Identify the file using the stream method.
            using (Stream stream = File.OpenRead(path))
            {
                type = IdentifyFile(stream);
            }

            return type;
        }
    }
}
