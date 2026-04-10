using NUnit.Framework;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    [TestFixture]
    public class LongGuidTests
    {
        [Test]
        public void NewGuid_ReturnsValidGuid()
        {
            LongGuid guid = LongGuid.NewGuid();
            Assert.IsTrue(guid.IsValid());
        }

        [Test]
        public void DefaultGuid_IsInvalid()
        {
            LongGuid guid = default;
            Assert.IsFalse(guid.IsValid());
        }

        [Test]
        public void TwoNewGuids_AreNotEqual()
        {
            LongGuid a = LongGuid.NewGuid();
            LongGuid b = LongGuid.NewGuid();
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void SameValues_AreEqual()
        {
            LongGuid a = new LongGuid(123L, 456L);
            LongGuid b = new LongGuid(123L, 456L);
            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);
        }

        [Test]
        public void DifferentValues_AreNotEqual()
        {
            LongGuid a = new LongGuid(123L, 456L);
            LongGuid b = new LongGuid(123L, 789L);
            Assert.AreNotEqual(a, b);
            Assert.IsTrue(a != b);
        }

        [Test]
        public void GetRawValues_ReturnsConstructorValues()
        {
            LongGuid guid = new LongGuid(111L, 222L);
            (long v1, long v2) = guid.GetRawValues();
            Assert.AreEqual(111L, v1);
            Assert.AreEqual(222L, v2);
        }

        [Test]
        public void ToBase64String_Roundtrips()
        {
            LongGuid original = LongGuid.NewGuid();
            string base64 = original.ToBase64String();
            LongGuid restored = LongGuid.FromBase64String(base64);
            Assert.AreEqual(original, restored);
        }

        [Test]
        public void ToByteArray_Roundtrips()
        {
            LongGuid original = LongGuid.NewGuid();
            byte[] bytes = original.ToByteArray();
            Assert.AreEqual(16, bytes.Length);
            LongGuid restored = LongGuid.FromByteArray(bytes);
            Assert.AreEqual(original, restored);
        }

        [Test]
        public void GetHashCode_ConsistentForEqualGuids()
        {
            LongGuid a = new LongGuid(42L, 99L);
            LongGuid b = new LongGuid(42L, 99L);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
