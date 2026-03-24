using NUnit.Framework;
using System.Collections.Generic;

namespace MidiDriverOrderForKorg.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CheckSortWithoutAliases()
        {
            var inputData = new LinkedList<RegistryEntry>();
            inputData.AddLast(CreateEntry("DEV1", null));
            inputData.AddLast(CreateEntry("DEV2", null));
            inputData.AddLast(CreateEntry("DEV3", null));
            inputData.AddLast(CreateEntry("DEV4", null));

            var outData = new LinkedList<RegistryEntry>(Utils.SortEntries(inputData));
            Assert.AreEqual(inputData.Count, outData.Count);
            foreach (var entry in inputData)
            {
                var outEntry = outData.First.Value;
                Assert.IsTrue(entry == outEntry);
                outData.RemoveFirst();
            }
        }


        [Test]
        public void CheckSortWithAliases()
        {
            var inputData = new LinkedList<RegistryEntry>();
            inputData.AddLast(CreateEntry("DEV1", null));
            inputData.AddLast(CreateEntry("DEV2", "midi3"));
            inputData.AddLast(CreateEntry("DEV3", "midi1"));
            inputData.AddLast(CreateEntry("DEV4", null));
            inputData.AddLast(CreateEntry("DEV5", "midi2"));
            inputData.AddLast(CreateEntry("DEV6", null));
            inputData.AddLast(CreateEntry("DEV7", "midi5"));
            inputData.AddLast(CreateEntry("DEV8", null));

            var idx = 0;
            var outData = new List<RegistryEntry>(Utils.SortEntries(inputData));
            Assert.AreEqual(inputData.Count, outData.Count);
            Assert.AreEqual("DEV1", outData[idx++].DeviceName);
            Assert.AreEqual("DEV3", outData[idx++].DeviceName);
            Assert.AreEqual("DEV5", outData[idx++].DeviceName);
            Assert.AreEqual("DEV2", outData[idx++].DeviceName);
            Assert.AreEqual("DEV4", outData[idx++].DeviceName);
            Assert.AreEqual("DEV7", outData[idx++].DeviceName);
            Assert.AreEqual("DEV6", outData[idx++].DeviceName);
            Assert.AreEqual("DEV8", outData[idx++].DeviceName);
            Assert.AreEqual(outData.Count, idx);
        }

        [Test]
        public void CheckSortWithMidiAlias()
        {
            var inputData = new LinkedList<RegistryEntry>();
            inputData.AddLast(CreateEntry("DEV1", "midi"));
            inputData.AddLast(CreateEntry("DEV2", "midi1"));

            var outData = new List<RegistryEntry>(Utils.SortEntries(inputData));
            Assert.AreEqual(2, outData.Count);
            Assert.AreEqual("DEV1", outData[0].DeviceName);
            Assert.AreEqual("DEV2", outData[1].DeviceName);
        }

        [Test]
        public void CheckSortWithMidi0Alias()
        {
            var inputData = new LinkedList<RegistryEntry>();
            inputData.AddLast(CreateEntry("DEV1", "midi0"));
            inputData.AddLast(CreateEntry("DEV2", "midi1"));

            var outData = new List<RegistryEntry>(Utils.SortEntries(inputData));
            Assert.AreEqual(2, outData.Count);
            Assert.AreEqual("DEV1", outData[0].DeviceName);
            Assert.AreEqual("DEV2", outData[1].DeviceName);
        }

        [Test]
        public void CheckSortWithMidiAndMidi0Duplicate()
        {
            var inputData = new LinkedList<RegistryEntry>();   
            inputData.AddLast(CreateEntry("DEV1", "midi"));
            inputData.AddLast(CreateEntry("DEV2", "midi0"));
            inputData.AddLast(CreateEntry("DEV3", "midi1"));

            var outData = new List<RegistryEntry>(Utils.SortEntries(inputData));
            Assert.AreEqual(3, outData.Count);
            Assert.AreEqual("DEV1", outData[0].DeviceName);
            Assert.AreEqual("DEV3", outData[1].DeviceName);
            Assert.AreEqual("DEV2", outData[2].DeviceName);
        }

        private RegistryEntry CreateEntry(string name, string alias, string driver = null)
        {
            return new RegistryEntry(alias, name, driver, null, false, false);
        }
    }

}
