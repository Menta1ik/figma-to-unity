using NUnit.Framework;
using FigmaImporter.V2.Core.Services;

namespace FigmaImporter.V2.Tests
{
    public class CacheTests
    {
        private FigmaResponseCache _cache;
        private string _testFileId;

        [SetUp]
        public void Setup()
        {
            _cache = new FigmaResponseCache();
            _testFileId = "__test_cache_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        [TearDown]
        public void Teardown()
        {
            _cache.ClearCache(_testFileId);
        }

        [Test]
        public void Cache_SaveThenLoad_RoundTrip()
        {
            string json = "{\"test\":true}";
            _cache.SaveToCache(_testFileId, null, "v1", json);

            string loaded = _cache.TryLoadCached(_testFileId, null, "v1");
            Assert.AreEqual(json, loaded);
        }

        [Test]
        public void Cache_VersionMismatch_ReturnsNull()
        {
            _cache.SaveToCache(_testFileId, null, "v1", "{\"data\":1}");

            string loaded = _cache.TryLoadCached(_testFileId, null, "v2");
            Assert.IsNull(loaded, "Different version should return null");
        }

        [Test]
        public void Cache_SaveWithNodeId_RoundTrip()
        {
            string json = "{\"node\":true}";
            _cache.SaveToCache(_testFileId, "123:456", "v1", json);

            string loaded = _cache.TryLoadCached(_testFileId, "123:456", "v1");
            Assert.AreEqual(json, loaded);
        }

        [Test]
        public void Cache_ClearSpecificFileId_RemovesOnlyThatFile()
        {
            string otherId = _testFileId + "_other";
            _cache.SaveToCache(_testFileId, null, "v1", "data1");
            _cache.SaveToCache(otherId, null, "v1", "data2");

            _cache.ClearCache(_testFileId);

            Assert.IsNull(_cache.TryLoadCached(_testFileId, null, "v1"), "Cleared file should be gone");
            Assert.AreEqual("data2", _cache.TryLoadCached(otherId, null, "v1"), "Other file should remain");

            _cache.ClearCache(otherId);
        }

        [Test]
        [Category("Destructive")]
        public void Cache_ClearAll_RemovesCacheDirectory()
        {
            _cache.SaveToCache(_testFileId, null, "v1", "data");

            _cache.ClearCache();

            Assert.IsNull(_cache.TryLoadCached(_testFileId, null, "v1"), "Nothing should remain after full clear");
        }

        [Test]
        public void Cache_LoadFromEmpty_ReturnsNull()
        {
            string loaded = _cache.TryLoadCached(_testFileId, null, "v1");
            Assert.IsNull(loaded, "No saved data should return null");
        }

        [Test]
        public void Cache_SaveEmptyContent_DoesNotWrite()
        {
            _cache.SaveToCache(_testFileId, null, "v1", "");
            Assert.IsNull(_cache.TryLoadCached(_testFileId, null, "v1"), "Empty content should not be cached");

            _cache.SaveToCache(_testFileId, null, "v1", null);
            Assert.IsNull(_cache.TryLoadCached(_testFileId, null, "v1"), "Null content should not be cached");
        }
    }
}
