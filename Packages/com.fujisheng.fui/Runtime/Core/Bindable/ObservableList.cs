using System;
using System.Collections;
using System.Collections.Generic;

namespace FUI.Bindable
{
    public interface IReadOnlyObservableList<out T> : IReadOnlyList<T>, INotifyCollectionChanged { }

    /// <summary>
    /// 可观察列表
    /// </summary>
    /// <typeparam name="T">item类型</typeparam>
    public class ObservableList<T> : IReadOnlyObservableList<T>, IList<T>, IList where T : ObservableObject
    {
        readonly List<T> items;

        public ObservableList()
        {
            items = new List<T>();
        }

        public ObservableList(int capacity)
        {
            items = new List<T>(capacity);
        }

        public ObservableList(IEnumerable<T> collection)
        {
            items = new List<T>(collection);
        }

        public event CollectionAddHandler CollectionAdd;
        public event CollectionRemoveHandler CollectionRemove;
        public event CollectionReplaceHandler CollectionReplace;
        public event CollectionResetHandler CollectionReset;
        public event CollectionMoveHandler CollectionMove;
        

        public T this[int index]
        {
            get { return items[index]; }
            set { items[index] = value; }
        }
        public int Count { get { return items.Count; } }
        public int Capacity { get { return items.Capacity; } set { items.Capacity = value; } }

        public void Add(T item)
        {
            items.Add(item);
            CollectionAdd?.Invoke(this, items.Count - 1, item);
        }
        public void AddRange(IEnumerable<T> collection)
        {
            items.AddRange(collection);
            CollectionAdd?.Invoke(this, null, null);
        }
        public void Clear()
        {
            CollectionReset?.Invoke(this);
            items.Clear();
        }
        public bool Contains(T item) => items.Contains(item);
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            var result = items.ConvertAll<TOutput>(converter);
            CollectionReplace?.Invoke(this, null, null, null);
            return result;
        }
        public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public void CopyTo(int index, T[] array, int arrayIndex, int count) => items.CopyTo(index, array, arrayIndex, count);
        public void CopyTo(T[] array) => items.CopyTo(array);
        public bool Exists(Predicate<T> match) => items.Exists(match);
        public T Find(Predicate<T> match) => items.Find(match);
        public List<T> FindAll(Predicate<T> match) => items.FindAll(match);
        public int FindIndex(Predicate<T> match) => items.FindIndex(match);
        public int FindIndex(int startIndex, Predicate<T> match) => items.FindIndex(startIndex, match);
        public int FindIndex(int startIndex, int count, Predicate<T> match) => items.FindIndex(startIndex, count, match);
        public T FindLast(Predicate<T> match) => items.FindLast(match);
        public int FindLastIndex(Predicate<T> match) => items.FindLastIndex(match);
        public int FindLastIndex(int startIndex, Predicate<T> match) => items.FindLastIndex(startIndex, match);
        public int FindLastIndex(int startIndex, int count, Predicate<T> match) => items.FindLastIndex(startIndex, count, match);
        public void ForEach(Action<T> action) => items.ForEach(action);
        public List<T>.Enumerator GetEnumerator() => items.GetEnumerator();
        public List<T> GetRange(int index, int count) => items.GetRange(index, count);
        public int IndexOf(T item, int index, int count) => items.IndexOf(item, index, count);
        public int IndexOf(T item, int index) => items.IndexOf(item, index);
        public int IndexOf(T item) => items.IndexOf(item);
        public void Insert(int index, T item)
        {
            items.Insert(index, item);
            CollectionAdd?.Invoke(this, index, item);
        }
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            items.InsertRange(index, collection);
            CollectionAdd?.Invoke(this, null, null);
        }
        public int LastIndexOf(T item) => items.LastIndexOf(item);
        public int LastIndexOf(T item, int index) => items.LastIndexOf(item, index);
        public int LastIndexOf(T item, int index, int count) => items.LastIndexOf(item, index, count);
        public bool Remove(T item)
        {
            var index = items.IndexOf(item);
            if(index < 0)
            {
                return false;
            }
          
            items.RemoveAt(index);
            CollectionRemove?.Invoke(this, index, item);
            return true;
        }
        public int RemoveAll(Predicate<T> match)
        {
            var result = items.RemoveAll(match);
            CollectionRemove?.Invoke(this, null, null);
            return result;
        }
        public void RemoveAt(int index)
        {
            var removed = items[index];
            items.RemoveAt(index);
            CollectionRemove?.Invoke(this, index, removed);
        }
        public void RemoveRange(int index, int count)
        {
            items.RemoveRange(index, count);
            CollectionRemove?.Invoke(this, null, null);
        }
        public void Reverse(int index, int count)
        {
            items.Reverse(index, count);
            CollectionMove?.Invoke(this);
        }
        public void Reverse()
        {
            items.Reverse();
            CollectionMove?.Invoke(this);
        }
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            items.Sort(index, count, comparer);
            CollectionMove?.Invoke(this);
        }
        public void Sort(Comparison<T> comparison)
        {
            items.Sort(comparison);
            CollectionMove?.Invoke(this);
        }
        public void Sort()
        {
            items.Sort();
            CollectionMove?.Invoke(this);
        }
        public void Sort(IComparer<T> comparer)
        {
            items.Sort(comparer);
            CollectionMove?.Invoke(this);
        }
        public T[] ToArray()
        {
            return items.ToArray();
        }
        public void TrimExcess()
        {
            items.TrimExcess();
        }
        public bool TrueForAll(Predicate<T> match)
        {
            return items.TrueForAll(match);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)items).CopyTo(array, index);
        }

        public int Add(object value)
        {
            return ((IList)items).Add(value);
        }

        public bool Contains(object value)
        {
            return ((IList)items).Contains(value);
        }

        public int IndexOf(object value)
        {
            return ((IList)items).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            ((IList)items).Insert(index, value);
        }

        public void Remove(object value)
        {
            ((IList)items).Remove(value);
        }

        bool ICollection<T>.IsReadOnly => false;

        public object SyncRoot => ((ICollection)items).SyncRoot;

        public bool IsSynchronized => ((ICollection)items).IsSynchronized;

        public bool IsReadOnly => ((IList)items).IsReadOnly;

        public bool IsFixedSize => ((IList)items).IsFixedSize;

        object IList.this[int index] { get => ((IList)items)[index]; set => ((IList)items)[index] = value; }
    }
}