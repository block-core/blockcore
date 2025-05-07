using Blockcore.Utilities;
using Xunit;

namespace Blockcore.Tests.Utilities
{
    public class LruHashSetTests
    {
        [Fact]
        public void AddOrUpdate_AddsItemToSet()
        {
            // Arrange
            var set = new LruHashSet<int>(maxSize: 2);

            // Act
            set.AddOrUpdate(1);

            // Assert
            Assert.True(set.Contains(1));
        }

        [Fact]
        public void AddOrUpdate_UpdatesExistingItem()
        {
            // Arrange
            var set = new LruHashSet<int>(maxSize: 2);
            set.AddOrUpdate(1);

            // Act
            set.AddOrUpdate(1);

            // Assert
            Assert.True(set.Contains(1));
        }

        [Fact]
        public void AddOrUpdate_RemovesOldestItemWhenMaxSizeExceeded()
        {
            // Arrange
            var set = new LruHashSet<int>(maxSize: 2);
            set.AddOrUpdate(1);
            set.AddOrUpdate(2);

            // Act
            set.AddOrUpdate(3);

            // Assert
            Assert.False(set.Contains(1));
            Assert.True(set.Contains(2));
            Assert.True(set.Contains(3));
        }

        [Fact]
        public void Clear_RemovesAllItemsFromSet()
        {
            // Arrange
            var set = new LruHashSet<int>(maxSize: 2);
            set.AddOrUpdate(1);
            set.AddOrUpdate(2);

            // Act
            set.Clear();

            // Assert
            Assert.False(set.Contains(1));
            Assert.False(set.Contains(2));
        }

        [Fact]
        public void Contains_ReturnsTrueIfItemInSet()
        {
            // Arrange
            var set = new LruHashSet<int>(maxSize: 2);
            set.AddOrUpdate(1);

            // Act
            var contains = set.Contains(1);

            // Assert
            Assert.True(contains);
        }

        [Fact]
        public void Contains_ReturnsFalseIfItemNotInSet()
        {
            // Arrange
            var set = new LruHashSet<int>(maxSize: 2);
            set.AddOrUpdate(1);

            // Act
            var contains = set.Contains(2);

            // Assert
            Assert.False(contains);
        }

        [Fact]
        public void Remove_RemovesItemFromSet()
        {
            // Arrange
            var set = new LruHashSet<int>(maxSize: 2);
            set.AddOrUpdate(1);

            // Act
            set.Remove(1);

            // Assert
            Assert.False(set.Contains(1));
        }
    }
}
