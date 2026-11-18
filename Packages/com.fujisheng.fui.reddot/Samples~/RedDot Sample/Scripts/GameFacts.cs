using System;
using System.Collections.Generic;

namespace FUI.Samples.RedDot
{
    /// <summary>中性的内存事实；不引用红点端口，不知道节点或刷新时机。</summary>
    public sealed class CountFact
    {
        /// <summary>事实变化事件；相同值也可显式重新采样。</summary>
        public event Action Changed;
        /// <summary>当前事实数量。</summary>
        public int Count { get; private set; }
        /// <summary>设备当前是否不可读，用于通讯自检。</summary>
        public bool Unavailable { get; private set; }
        /// <summary>设置事实并通知适配器。</summary>
        public void Set(int value)
        {
            if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
            Count = value;
            Changed?.Invoke();
        }
        /// <summary>重新采样但不修改事实。</summary>
        public void Resample() => Changed?.Invoke();
        /// <summary>切换设备可读性。</summary>
        public void SetUnavailable(bool value) { Unavailable = value; Changed?.Invoke(); }
    }

    /// <summary>一条独立物流链的物资记录；不与其他物流链共享写入身份。</summary>
    public sealed class CargoFacts
    {
        readonly Dictionary<string, int> items = new Dictionary<string, int>(StringComparer.Ordinal);
        /// <summary>变化的物资标识；null数量表示移除记录。</summary>
        public event Action<string, int?> Changed;
        /// <summary>只读枚举当前记录，供适配器首次连接时回放。</summary>
        public IEnumerable<KeyValuePair<string, int>> Items => items;
        /// <summary>查询本链路的物资。</summary>
        public int Get(string id) => items.TryGetValue(id, out var count) ? count : 0;
        /// <summary>更新物流事实。</summary>
        public void Set(string id, int count)
        {
            if (count < 0) { throw new ArgumentOutOfRangeException(nameof(count)); }
            items[id] = count;
            Changed?.Invoke(id, count);
        }
        /// <summary>撤销一笔物资记录。</summary>
        public void Remove(string id) { items.Remove(id); Changed?.Invoke(id, null); }
    }

    /// <summary>单位升级条件；是否可升级由事实推导，不保存重复布尔状态。</summary>
    public sealed class UnitFacts
    {
        /// <summary>单位状态变化。</summary>
        public event Action Changed;
        /// <summary>单位编号。</summary>
        public int Id { get; }
        /// <summary>训练是否完成。</summary>
        public bool Trained { get; private set; }
        /// <summary>材料是否齐备。</summary>
        public bool Supplied { get; private set; }
        /// <summary>是否具备升级条件。</summary>
        public bool CanUpgrade => Trained && Supplied;
        /// <summary>创建单位。</summary>
        public UnitFacts(int id) { Id = id; }
        /// <summary>完成训练。</summary>
        public void Train() { Trained = true; Changed?.Invoke(); }
        /// <summary>补齐材料。</summary>
        public void Supply() { Supplied = true; Changed?.Invoke(); }
        /// <summary>升级只消费满足条件的事实。</summary>
        public void Upgrade()
        {
            if (!CanUpgrade) { return; }
            Trained = false;
            Supplied = false;
            Changed?.Invoke();
        }
    }
}
