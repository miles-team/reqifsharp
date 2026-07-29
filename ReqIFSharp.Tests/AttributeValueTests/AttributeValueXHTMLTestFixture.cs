// -------------------------------------------------------------------------------------------------
// <copyright file="AttributeValueXHTMLTestFixture.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Tests
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.Extensions.Logging;

    using NUnit.Framework;

    using ReqIFSharp;

    using Serilog;

    /// <summary>
    /// Suite of tests for the <see cref="AttributeValueXHTML"/>
    /// </summary>
    [TestFixture]
    public class AttributeValueXHTMLTestFixture
    {
        private ILoggerFactory loggerFactory;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            this.loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            });
        }

        [Test]
        public void Verify_that_constructor_does_not_throw_exception()
        {
            Assert.That(() => new AttributeValueXHTML(null), Throws.Nothing);
            Assert.That(() => new AttributeValueXHTML(this.loggerFactory), Throws.Nothing);
        }

        [Test]
        public void Verify_That_The_AttributeDefinition_Can_Be_Set_Or_Get()
        {
            var attributeDefinitionXhtml = new AttributeDefinitionXHTML();

            var attributeValueXhtml = new AttributeValueXHTML();
            attributeValueXhtml.Definition = attributeDefinitionXhtml;

            var attributeValue = (AttributeValue)attributeValueXhtml;

            Assert.That(attributeValue.AttributeDefinition, Is.EqualTo(attributeDefinitionXhtml));

            attributeValue.AttributeDefinition = attributeDefinitionXhtml;

            Assert.That(attributeValue.AttributeDefinition, Is.EqualTo(attributeDefinitionXhtml));
        }

        [Test]
        public void Verify_That_Exception_Is_Raised_When_Invalid_AttributeDefinition_Is_Set()
        {
            var attributeDefinitionString = new AttributeDefinitionReal();
            var attributeValueXhtml = new AttributeValueXHTML();
            var attributeValue = (AttributeValue)attributeValueXhtml;

            Assert.Throws<ArgumentException>(() => attributeValue.AttributeDefinition = attributeDefinitionString);
        }

        [Test]
        public void Verify_That_Exception_Is_Raised_When_AttributeDefinition_Is_Null()
        {
            var attributeValueXhtml = new AttributeValueXHTML();
            var attributeValue = (AttributeValue)attributeValueXhtml;

            Assert.That(
                () => attributeValue.AttributeDefinition = null,
                Throws.Exception.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("attributeDefinition"));
        }

        [Test]
        public void Verify_That_WriteXml_Without_Definition_Set_Throws_SerializationException()
        {
            using var memoryStream = new MemoryStream();
            using var writer = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true });

            var attributeValueXhtml = new AttributeValueXHTML();

            Assert.That(() => attributeValueXhtml.WriteXml(writer),
                Throws.Exception.TypeOf<SerializationException>());
        }

        [Test]
        public void Verify_That_WriteXmlAsync_Without_Definition_Set_Throws_SerializationException()
        {
            using var memoryStream = new MemoryStream();
            using var writer = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true });

            var attributeValueXhtml = new AttributeValueXHTML();

            var cancellationTokenSource = new CancellationTokenSource();

            Assert.That(() => attributeValueXhtml.WriteXmlAsync(writer, cancellationTokenSource.Token),
                Throws.Exception.TypeOf<SerializationException>());
        }

        [Test]
        public void Verify_That_WriteXmlAsync_Throws_Exception_when_cancelled()
        {
            using var memoryStream = new MemoryStream();
            using var writer = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true });

            var attributeValueXhtml = new AttributeValueXHTML
            {
                Definition = new AttributeDefinitionXHTML()
            };

            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.That(
                async () => await attributeValueXhtml.WriteXmlAsync(writer, cts.Token),
                Throws.Exception.TypeOf<OperationCanceledException>());
        }

        [Test]
        public void Verify_Convenience_Value_Property()
        {
            var attributeValue = new AttributeValueXHTML();

            var val = "testetestes";
            attributeValue.ObjectValue = val;

            Assert.That(val, Is.EqualTo(attributeValue.TheValue));
            Assert.That(val, Is.EqualTo(attributeValue.ObjectValue));
        }

        [Test]
        public void Verify_that_when_ObjectValue_is__set_to_null_exception_is_raised_when_setter_is_called()
        {
            var attributeValue = new AttributeValueXHTML();

            Assert.That(() => attributeValue.ObjectValue = null, Throws.TypeOf<ArgumentNullException>()); 
        }

        [Test]
        public void Verify_that_raw_text_can_be_extracted_from_xtml_value()
        {
            var attributeValue = new AttributeValueXHTML();

            attributeValue.TheValue = null;
            Assert.That(attributeValue.ExtractUnformattedTextFromValue(), Is.Empty);

            attributeValue.TheValue = "";
            Assert.That(attributeValue.ExtractUnformattedTextFromValue(), Is.Empty);
            
            var val = "<xhtml:div>Description of the SpecObject that includes formatted tables and/or style:<xhtml:ul class=\"noindent\"><xhtml:li>Element 1</xhtml:li><xhtml:li>Element 2</xhtml:li></xhtml:ul></xhtml:div>";
            attributeValue.ObjectValue = val;

            var unformattedText = attributeValue.ExtractUnformattedTextFromValue();

            Assert.That(unformattedText, Is.EqualTo("Description of the SpecObject that includes formatted tables and/or style: Element 1 Element 2"));
        }

        [Test]
        public void Verify_that_when_IsSimpe_is_incorrect_bool_exception_is_thrown()
        {
            var reqifPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Datatype-Demo-XhtML-Fault.reqif");
            var deserializer = new ReqIFDeserializer(this.loggerFactory);

            Assert.That(() => deserializer.Deserialize(reqifPath, false),
                Throws.TypeOf<SerializationException>()
                    .With.Message.StartsWith("The AttributeValueXHTML.IS-SIMPLIFIED"));

        }

        [Test]
        public void Verify_that_external_objects_are_created_for_object_element_in_default_namespace()
        {
            const string xml = "<ATTRIBUTE-VALUE-XHTML><THE-VALUE><div xmlns=\"http://www.w3.org/1999/xhtml\"><object data=\"images/picture.png\" type=\"image/png\" width=\"100\" height=\"50\" /></div></THE-VALUE></ATTRIBUTE-VALUE-XHTML>";

            var attributeValueXhtml = DeserializeAttributeValueXhtml(xml);

            Assert.That(attributeValueXhtml.ExternalObjects, Has.Count.EqualTo(1));
            Assert.That(attributeValueXhtml.ExternalObjects[0].Uri, Is.EqualTo("images/picture.png"));
            Assert.That(attributeValueXhtml.ExternalObjects[0].MimeType, Is.EqualTo("image/png"));
            Assert.That(attributeValueXhtml.ExternalObjects[0].Width, Is.EqualTo(100));
            Assert.That(attributeValueXhtml.ExternalObjects[0].Height, Is.EqualTo(50));
        }

        [Test]
        public void Verify_that_external_objects_are_created_for_prefixed_xhtml_object_element()
        {
            const string xml = "<ATTRIBUTE-VALUE-XHTML><THE-VALUE><xhtml:div xmlns:xhtml=\"http://www.w3.org/1999/xhtml\"><xhtml:object data=\"images/picture.png\" type=\"image/png\" /></xhtml:div></THE-VALUE></ATTRIBUTE-VALUE-XHTML>";

            var attributeValueXhtml = DeserializeAttributeValueXhtml(xml);

            Assert.That(attributeValueXhtml.ExternalObjects, Has.Count.EqualTo(1));
            Assert.That(attributeValueXhtml.ExternalObjects[0].Uri, Is.EqualTo("images/picture.png"));
        }

        [Test]
        public void Verify_that_external_objects_are_created_when_type_attribute_precedes_data_attribute()
        {
            const string xml = "<ATTRIBUTE-VALUE-XHTML><THE-VALUE><div xmlns=\"http://www.w3.org/1999/xhtml\"><object type=\"image/png\" data=\"images/picture.png\" /></div></THE-VALUE></ATTRIBUTE-VALUE-XHTML>";

            var attributeValueXhtml = DeserializeAttributeValueXhtml(xml);

            Assert.That(attributeValueXhtml.ExternalObjects, Has.Count.EqualTo(1));
            Assert.That(attributeValueXhtml.ExternalObjects[0].Uri, Is.EqualTo("images/picture.png"));
        }

        [Test]
        public void Verify_that_url_encoded_data_attribute_is_decoded()
        {
            const string xml = "<ATTRIBUTE-VALUE-XHTML><THE-VALUE><div xmlns=\"http://www.w3.org/1999/xhtml\"><object data=\"Boundary%20Scan/picture.png\" type=\"image/png\" /></div></THE-VALUE></ATTRIBUTE-VALUE-XHTML>";

            var attributeValueXhtml = DeserializeAttributeValueXhtml(xml);

            Assert.That(attributeValueXhtml.ExternalObjects, Has.Count.EqualTo(1));
            Assert.That(attributeValueXhtml.ExternalObjects[0].Uri, Is.EqualTo("Boundary Scan/picture.png"));
        }

        [Test]
        public void Verify_that_no_external_objects_are_created_for_value_without_object_element()
        {
            const string xml = "<ATTRIBUTE-VALUE-XHTML><THE-VALUE><div xmlns=\"http://www.w3.org/1999/xhtml\">plain text that mentions an object and some data</div></THE-VALUE></ATTRIBUTE-VALUE-XHTML>";

            var attributeValueXhtml = DeserializeAttributeValueXhtml(xml);

            Assert.That(attributeValueXhtml.ExternalObjects, Is.Empty);
        }

        [Test]
        public void Verify_that_elements_whose_name_merely_starts_with_object_are_not_detected()
        {
            const string xml = "<ATTRIBUTE-VALUE-XHTML><THE-VALUE><div xmlns=\"http://www.w3.org/1999/xhtml\"><objectxxxx data=\"images/picture.png\" type=\"image/png\" /></div></THE-VALUE></ATTRIBUTE-VALUE-XHTML>";

            var attributeValueXhtml = DeserializeAttributeValueXhtml(xml);

            Assert.That(attributeValueXhtml.ExternalObjects, Is.Empty);
        }

        [Test]
        public async Task Verify_that_external_objects_are_created_when_reading_asynchronously()
        {
            const string xml = "<ATTRIBUTE-VALUE-XHTML><THE-VALUE><div xmlns=\"http://www.w3.org/1999/xhtml\"><object data=\"images/picture.png\" type=\"image/png\" /></div></THE-VALUE></ATTRIBUTE-VALUE-XHTML>";

            var attributeValueXhtml = new AttributeValueXHTML(new SpecObject(), null);

            var settings = new XmlReaderSettings { Async = true };
            using (var reader = XmlReader.Create(new StringReader(xml), settings))
            {
                await reader.MoveToContentAsync();
                await attributeValueXhtml.ReadXmlAsync(reader, CancellationToken.None);
            }

            Assert.That(attributeValueXhtml.ExternalObjects, Has.Count.EqualTo(1));
            Assert.That(attributeValueXhtml.ExternalObjects[0].Uri, Is.EqualTo("images/picture.png"));
        }

        /// <summary>
        /// Deserializes an ATTRIBUTE-VALUE-XHTML snippet using the internal ReadXml method
        /// </summary>
        /// <param name="xml">
        /// The xml snippet to deserialize
        /// </param>
        /// <returns>
        /// The resulting <see cref="AttributeValueXHTML"/>
        /// </returns>
        private static AttributeValueXHTML DeserializeAttributeValueXhtml(string xml)
        {
            var attributeValueXhtml = new AttributeValueXHTML(new SpecObject(), null);

            using (var reader = XmlReader.Create(new StringReader(xml)))
            {
                reader.MoveToContent();
                attributeValueXhtml.ReadXml(reader);
            }

            return attributeValueXhtml;
        }
    }
}
