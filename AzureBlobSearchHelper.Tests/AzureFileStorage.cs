﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hyak.Common;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using NUnit.Framework;


namespace AzureBlobSearchHelper.Tests
{
    [TestFixture]
    public class AzureFileStorage
    {
        Dictionary<string, string> meta = new Dictionary<string, string>();
        private enum TestEnum
        {
            FirstValue,
            SecondValue,
            ThirdValue
        }

        private class ValidTestObjectGuidId
        {
            [MetaName]
            public Guid Id { get; set; }

            [MetaData]
            public string Description { get; set; }
        }

        private class ValidTestObject
        {
            [MetaName]
            public string Name { get; set; }

            [MetaData]
            public string Description { get; set; }

            [MetaData]
            public bool SomeBool { get; set; }

            [MetaData]
            public int SomeInt { get; set; }
            [MetaData]
            public long SomeLong { get; set; }
            [MetaData]
            public DateTime SomeDateTime { get; set; }
            
            [MetaData]
            public TestEnum SomeEnum { get; set; }

            [MetaData]
            public int? SomeNullableInt { get; set; }
            [MetaData]
            public DateTime? SomeNullableDateTime { get; set; }
            [MetaData]
            public bool? SomeNullableBool { get; set; }
        }

        private class InvalidTestObject
        {
            public string NoMetaTag { get; set; }
        }

        private Mock<ICloudBlobContainer> GetMetaContainer(string blobName)
        {
           meta = new Dictionary<string, string>();

            var cont = new Mock<ICloudBlobContainer>();
            var blockBlob = new Mock<ICloudBlockBlob>();
            var blob = new Mock<ICloudBlob>();

            blockBlob.SetupGet(bb => bb.Metadata).Returns(meta);
            blob.SetupGet(bb => bb.Metadata).Returns(meta);
            blob.Setup(cloudBlob => cloudBlob.ExistsAsync())
                .ReturnsAsync(true);
            
            
            

            cont.Setup(container => container.GetBlockBlobReference(blobName))
                .Returns(blockBlob.Object);

            cont.Setup(container => container.GetBlobReference(blobName))
                .Returns(blob.Object);

            

            return cont;
        }

        
        [Test]
        public async Task WhenClassHasMetaName_GetsUsingName()
        {
            var t = new ValidTestObject() {Name = "NameNameName", Description = "NotNameNotName"};

            var cont = GetMetaContainer(t.Name);


            var helper = new AzureFileStorage<ValidTestObject>(cont.Object,emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            cont.Verify(container => container.GetBlockBlobReference(t.Name), Times.Once);
        }

        [Test]
        public async Task WhenClassHasNoMetaName_ThrowsArgumentException()
        {
            var t = new InvalidTestObject();
            Assert.Throws<ArgumentException>(() =>
            {
                var helper = new AzureFileStorage<InvalidTestObject>(new Mock<ICloudBlobContainer>().Object,
                    o => new byte[0]);
            });

        }

        private Func<ValidTestObject,byte[]> emptyByteFunc => o => new byte[0];

        [Test]
        public async Task WhenRetrievingObject_HasOriginaStringlValue()
        {
            var t = new ValidTestObject() {Name = "StringMapTest", Description = "TestString"};
            

            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Name);


            Assert.AreEqual("TestString", res.Description);
        }

        [Test]
        public async Task WhenRetrievingObject_HasOriginaStringlValueNoDiacritic()
        {
            var t = new ValidTestObject() { Name = "StringMapTest", Description = "TêstŠtrïng" };


            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Name);


            Assert.AreEqual("TestString", res.Description);
        }

        [Test]
        public async Task WhenRetrievingObject_HasOriginalKeyValue()
        {
            var t = new ValidTestObject() { Name = "StringMapTest", Description = "TestString" };


            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Name);

            Assert.AreEqual("StringMapTest", res.Name);
        }

        [Test]
        public async Task WhenRetrievingObject_HasOriginalIntValue()
        {
            var t = new ValidTestObject() {Name = "IntMapTest", SomeInt = 42};
            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Name);

            Assert.AreEqual(42, res.SomeInt);
        }

        [Test]
        public async Task WhenRetrievingObject_HasOriginalLongValue()
        {
            var t = new ValidTestObject() { Name = "IntMapTest", SomeLong = 1215135 };
            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Name);

            Assert.AreEqual(1215135, res.SomeLong);
        }

        [Test]
        public async Task WhenRetrievingObject_HasOriginalDateTimeValue()
        {
            var t = new ValidTestObject() {Name = "DateTimeMapTest", SomeDateTime = new DateTime(2010, 6, 12)};
            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Name);
            Assert.AreEqual(new DateTime(2010, 6, 12), res.SomeDateTime);

        }

        [Test]
        public async Task WhenRetrievingObject_HasOriginalNullableDateTimeValue()
        {
            var t = new ValidTestObject() { Name = "NullableDateTimeMapTest", SomeNullableDateTime = new DateTime(2010, 6, 12) };
            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Name);
            Assert.AreEqual(new DateTime(2010, 6, 12), res.SomeNullableDateTime);
        }

        [Test]
        public async Task WhenStoringObjectDateTimeMin_LeavesMetaEmpty()
        {
            var t = new ValidTestObject() { Name = "StoreDateTimeMinTest", SomeDateTime = DateTime.MinValue };
            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);
            Assert.IsFalse( meta.ContainsKey("meta_SomeDateTime"));

        }
        [Test]
        public async Task WhenRetrievingObjectDateTimeMin_ObjectDateTimeMin()
        {
            var t = new ValidTestObject() { Name = "RetDateTimeMinTest", SomeDateTime = DateTime.MinValue };
            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var o = await helper.GetMetaItemAsync(t.Name);
            Assert.AreEqual(DateTime.MinValue, o.SomeDateTime);

        }
        [Test]
        public async Task WhenRetrievingObjectDateTimeNull_ObjectDateTimeNull()
        {
            var t = new ValidTestObject() { Name = "DateTimeNullTest", SomeNullableDateTime = null};
            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);
            Assert.IsFalse(meta.ContainsKey("meta_SomeNullableDateTime"));
            var o = await helper.GetMetaItemAsync(t.Name);
            Assert.AreEqual(null, o.SomeNullableDateTime);

        }


        [Test]
        public async Task WhenRetrievingObject_HasOriginalEnumValue()
        {
            var t = new ValidTestObject() {Name = "EnumMapTest", SomeEnum = TestEnum.SecondValue};
            var helper = new AzureFileStorage<ValidTestObject>(GetMetaContainer(t.Name).Object, emptyByteFunc);
            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Name);
            Assert.AreEqual(TestEnum.SecondValue, res.SomeEnum);
        }

        [Test]
        public async Task WhenRetrievingObjectGuidId_HasId()
        {
            var t = new ValidTestObjectGuidId() {Description = "GuidIdObject", Id = Guid.NewGuid()};
            var helper = new AzureFileStorage<ValidTestObjectGuidId>(GetMetaContainer(t.Id.ToString()).Object, id => new byte[0]);

            await helper.TrySaveItemAsync(t);

            var res = await helper.GetMetaItemAsync(t.Id.ToString());
            Assert.AreEqual(t.Id, res.Id);
        }
    }
}
