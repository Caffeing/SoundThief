using System;
using System.Collections.Generic;
using UnityEngine;
namespace SoundThief
{
    [Serializable]
    public struct CollectedItem
    {
        public string id, label;
        public CollectedItem(string id, string label) { this.id = id; this.label = label; }
    }

    public sealed class StageInventory : MonoBehaviour
    {
        readonly List<CollectedItem> items = new List<CollectedItem>();
        public IReadOnlyList<CollectedItem> Items => items;
        public event Action Changed;
        public bool Contains(string id) => !string.IsNullOrWhiteSpace(id) && items.Exists(item => item.id == id);
        public bool Add(string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || Contains(id)) return false;
            items.Add(new CollectedItem(id, string.IsNullOrWhiteSpace(label) ? id : label)); Changed?.Invoke(); return true;
        }
        public void Remove(string id) { if (items.RemoveAll(item => item.id == id) > 0) Changed?.Invoke(); }
        public void Clear() { items.Clear(); Changed?.Invoke(); }
    }
}
