namespace Aggregate
{
	public sealed class Aggregate<TItem, TValue> where TItem : class
	{
		private readonly IList<TItem> _items;
		private readonly Func<TItem, TValue?> _getter;
		private readonly Action<TItem, TValue?> _setter;
		private readonly TValue? _fallback = default;

		public Aggregate(IList<TItem> items, Func<TItem, TValue?> getter, Action<TItem, TValue?> setter, TValue? fallback = default)
		{
			_items = items;
			_getter = getter;
			_setter = setter;
			_fallback = fallback;
		}

		private (bool, TValue?) CheckEquality()
		{
			if (_items.Count == 0)
			{
				return (true, _fallback);
			}

			TValue? first = _getter(_items.First());
			bool equal;
			if (first != null)
				equal = _items.Skip(1).All(value => first.Equals(_getter(value)));
			else
				equal = _items.Skip(1).All(value => _getter(value) == null);

			return (equal, equal ? first : _fallback);
		}

		public bool Equal
		{
			get
			{
				(bool equal, _) = CheckEquality();
				return equal;
			}
		}

		public TValue? Value
		{
			get
			{
				(_, TValue? value) = CheckEquality();
				return value;
			}
			set
			{
				foreach (TItem item in _items)
				{
					_setter(item, value);
				}
			}
		}
	}
}
