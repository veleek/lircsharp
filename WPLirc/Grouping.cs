using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Ben.Data
{
    /// <summary>
    /// Implementation of the generic grouping interface that can be used for binding
    /// grouped elements to a LongListSelector in a meaningful way.
    /// </summary>
    /// <typeparam name="TKey">The type of the key for each group</typeparam>
    /// <typeparam name="TElement">The type of the values in each group</typeparam>
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        public Grouping(TKey key, IEnumerable<TElement> elements)
        {
            this.Key = key;
            this.Elements = elements;
        }

        public TKey Key { get; private set; }
        public IEnumerable<TElement> Elements { get; private set; }

        public override bool Equals(object obj)
        {
            var otherGroup = obj as Grouping<TKey, TElement>;

            return (otherGroup != null) && (otherGroup.Key.Equals(this.Key));
        }

        public override int GetHashCode()
        {
            return this.Key.GetHashCode();
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return Elements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    /// Implementation of the generic grouping interface that can be used for binding
    /// grouped elements to a LongListSelector in a meaningful way.
    /// </summary>
    /// <remarks>
    /// This grouping derives from ObservableCollection which allows elemnts to be added
    /// and removed to allow dynamic filtering in a relatively quick way even with larger
    /// lists (200-300 items)
    /// </remarks>
    /// <typeparam name="TKey">The type of the key for each group</typeparam>
    /// <typeparam name="TElement">The type of the values in each group</typeparam>
    public class ObservableGrouping<TKey, TElement> : ObservableCollection<TElement>, IGrouping<TKey, TElement>
    {
        public ObservableGrouping(TKey key, IEnumerable<TElement> elements)
            : base(elements)
        {
            this.Key = key;
        }

        public TKey Key { get; private set; }
        public IEnumerable<TElement> Elements
        {
            get { return this; }
        }

        public override bool Equals(object obj)
        {
            var otherGroup = obj as ObservableGrouping<TKey, TElement>;

            return (otherGroup != null) && (otherGroup.Key.Equals(this.Key));
        }

        public override int GetHashCode()
        {
            return this.Key.GetHashCode();
        }
    }
}
