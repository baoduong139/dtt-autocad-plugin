using System;
using System.Collections.Generic;

namespace DTools.Core
{
    /// <summary>
    /// Bộ nhớ đệm giới hạn dung lượng LRU (Least Recently Used) Thread-safe
    /// Tối ưu hóa đặc biệt cho máy tính RAM 8 GB, tự động thu hồi tài nguyên cũ nhất khi vượt ngưỡng
    /// </summary>
    public class BoundedLruCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _map;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lock = new object();

        private class CacheItem
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
        }

        public BoundedLruCache(int capacity = 20)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Dung lượng cache phải > 0");
            _capacity = capacity;
            _map = new Dictionary<TKey, LinkedListNode<CacheItem>>(_capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        public int Capacity => _capacity;

        public int Count
        {
            get
            {
                lock (_lock) { return _map.Count; }
            }
        }

        public bool TryGet(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    // Đưa lên đầu danh sách vừa sử dụng
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default;
                return false;
            }
        }

        public void Set(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_map.TryGetValue(key, out var existingNode))
                {
                    existingNode.Value.Value = value;
                    _lruList.Remove(existingNode);
                    _lruList.AddFirst(existingNode);
                    return;
                }

                // Nếu đạt dung lượng tối đa, thu hồi phần tử ở đuôi (ít dùng nhất)
                if (_map.Count >= _capacity)
                {
                    var last = _lruList.Last;
                    if (last != null)
                    {
                        _map.Remove(last.Value.Key);
                        _lruList.RemoveLast();

                        // Nếu giá trị là IDisposable (ví dụ Bitmap/Image), tự động Dispose
                        if (last.Value.Value is IDisposable disposable)
                        {
                            try { disposable.Dispose(); } catch { }
                        }
                    }
                }

                var newItem = new CacheItem { Key = key, Value = value };
                var newNode = new LinkedListNode<CacheItem>(newItem);
                _lruList.AddFirst(newNode);
                _map[key] = newNode;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                foreach (var node in _lruList)
                {
                    if (node.Value is IDisposable disposable)
                    {
                        try { disposable.Dispose(); } catch { }
                    }
                }
                _map.Clear();
                _lruList.Clear();
            }
        }
    }
}
