// -------------------------------------------------------------------------------------------------
// <copyright file="ExternalObjectTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2017-2025 Starion Group S.A.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
// </copyright>
// ------------------------------------------------------------------------------------------------

// Modification notice (Apache License 2.0, section 4(b)):
//   This file was modified in the miles-team/reqifsharp fork of ReqIFSharp.
//   Change: tests for the zip entry resolution were added.

namespace ReqIFSharp.Tests.AttributeValueTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="ExternalObject"/> class.
    /// </summary>
    [TestFixture]
    public class ExternalObjectTestFixture
    {
        private ExternalObject externalObject;

        [SetUp]
        public void SetUp()
        {
            this.externalObject = new ExternalObject(null);
        }

        [Test]
        public void Verify_that_expected_exceptions_are_raised_when_QueryLocalData_is_called()
        {
            string reqifPath = null;
            Assert.Throws<ArgumentException>(() => this.externalObject.QueryLocalData(reqifPath, null));

            Stream sourceStream = null;
            Assert.Throws<ArgumentNullException>(() => this.externalObject.QueryLocalData(sourceStream, null));

            reqifPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "requirements-and-objects.reqifz");
            Assert.Throws<ArgumentNullException>(() => this.externalObject.QueryLocalData(reqifPath, null));

            using (sourceStream = new FileStream(reqifPath, FileMode.Open))
            {
                Assert.Throws<ArgumentNullException>(() => this.externalObject.QueryLocalData(sourceStream, null));
            }

            var targetStream = new MemoryStream();
            
            using (sourceStream = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(() => this.externalObject.QueryLocalData(sourceStream, null));
            }

            this.externalObject.Uri = "http";
            Assert.Throws<InvalidOperationException>(() => this.externalObject.QueryLocalData(reqifPath, targetStream));

            using (sourceStream = new FileStream(reqifPath, FileMode.Open))
            {
                Assert.Throws<InvalidOperationException>(() => this.externalObject.QueryLocalData(sourceStream, targetStream));
            }
        }

        [Test]
        public void Verify_that_QueryLocalData_resolves_entry_by_exact_match()
        {
            var payload = new byte[] { 1, 2, 3, 4, 5 };
            var other = new byte[] { 9, 9, 9 };

            using (var zipStream = CreateZipArchiveStream(new Dictionary<string, byte[]>
                   {
                       { "images/picture.png", payload },
                       { "package/images/picture.png", other }
                   }))
            using (var target = new MemoryStream())
            {
                this.externalObject.Uri = "images/picture.png";
                this.externalObject.QueryLocalData(zipStream, target);

                Assert.That(target.ToArray(), Is.EqualTo(payload));
            }
        }

        [Test]
        public void Verify_that_QueryLocalData_resolves_entry_with_leading_folder_prefix()
        {
            var payload = new byte[] { 1, 2, 3, 4, 5 };

            using (var zipStream = CreateZipArchiveStream(new Dictionary<string, byte[]>
                   {
                       { "package/images/picture.png", payload }
                   }))
            using (var target = new MemoryStream())
            {
                this.externalObject.Uri = "images/picture.png";
                this.externalObject.QueryLocalData(zipStream, target);

                Assert.That(target.ToArray(), Is.EqualTo(payload));
            }
        }

        [Test]
        public void Verify_that_QueryLocalData_resolves_entry_with_backslash_directory_separators()
        {
            var payload = new byte[] { 1, 2, 3, 4, 5 };

            using (var zipStream = CreateZipArchiveStream(new Dictionary<string, byte[]>
                   {
                       { @"images\picture.png", payload }
                   }))
            using (var target = new MemoryStream())
            {
                this.externalObject.Uri = "images/picture.png";
                this.externalObject.QueryLocalData(zipStream, target);

                Assert.That(target.ToArray(), Is.EqualTo(payload));
            }
        }

        [Test]
        public void Verify_that_QueryLocalData_resolves_url_encoded_uri()
        {
            var payload = new byte[] { 1, 2, 3, 4, 5 };

            using (var zipStream = CreateZipArchiveStream(new Dictionary<string, byte[]>
                   {
                       { "package/Boundary Scan/picture.png", payload }
                   }))
            using (var target = new MemoryStream())
            {
                this.externalObject.Uri = "Boundary%20Scan/picture.png";
                this.externalObject.QueryLocalData(zipStream, target);

                Assert.That(target.ToArray(), Is.EqualTo(payload));
            }
        }

        [Test]
        public void Verify_that_QueryLocalData_does_not_resolve_ambiguous_suffix_matches()
        {
            var payloadA = new byte[] { 1, 1, 1 };
            var payloadB = new byte[] { 2, 2, 2 };

            using (var zipStream = CreateZipArchiveStream(new Dictionary<string, byte[]>
                   {
                       { "a/picture.png", payloadA },
                       { "b/picture.png", payloadB }
                   }))
            using (var target = new MemoryStream())
            {
                this.externalObject.Uri = "picture.png";
                this.externalObject.QueryLocalData(zipStream, target);

                Assert.That(target.Length, Is.EqualTo(0));
            }
        }

        [Test]
        public void Verify_that_QueryLocalData_does_not_resolve_suffix_across_directory_boundary()
        {
            var payload = new byte[] { 1, 2, 3 };

            using (var zipStream = CreateZipArchiveStream(new Dictionary<string, byte[]>
                   {
                       { "images/some-picture.png", payload }
                   }))
            using (var target = new MemoryStream())
            {
                this.externalObject.Uri = "picture.png";
                this.externalObject.QueryLocalData(zipStream, target);

                Assert.That(target.Length, Is.EqualTo(0));
            }
        }

        /// <summary>
        /// Creates an in-memory zip archive containing the provided entries
        /// </summary>
        /// <param name="entries">
        /// The entries as a name to content mapping
        /// </param>
        /// <returns>
        /// A readable <see cref="MemoryStream"/> positioned at the start of the archive
        /// </returns>
        private static MemoryStream CreateZipArchiveStream(Dictionary<string, byte[]> entries)
        {
            var stream = new MemoryStream();

            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var kvp in entries)
                {
                    var entry = archive.CreateEntry(kvp.Key);
                    using (var entryStream = entry.Open())
                    {
                        entryStream.Write(kvp.Value, 0, kvp.Value.Length);
                    }
                }
            }

            stream.Position = 0;
            return stream;
        }
    }
}
