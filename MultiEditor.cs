using Microsoft.AspNetCore.Components;

namespace Aggregate
{
	public abstract class MultiEditor<TItem, TValue> : ComponentBase where TItem : class
	{
		protected bool _isInitialized = false;
		private IReadOnlyList<TItem>? _items;
		private Func<TItem, TValue>? _getter;
		private Action<TItem, TValue?>? _setter;

		[Parameter]
		public IReadOnlyList<TItem>? Items
		{
			get
			{
				return _items;
			}
			set
			{
				if (_items == value) return;
				_items = value;
				OnSourceDataChanged();
			}
		}

		public virtual TValue? Value { get; protected set; }

		[Parameter]
		public Func<TItem, TValue>? GetValue
		{
			get
			{
				return _getter;
			}
			set
			{
				if (_getter == value) return;
				_getter = value;
				OnSourceDataChanged();
			}
		}

		[Parameter]
		public Action<TItem, TValue?>? SetValue
		{
			get
			{
				return _setter;
			}
			set
			{
				_setter = value;
			}
		}

		public bool IsInConflict { get; private set; } = false;

		public bool HasChanged { get; protected set; }

		public bool CommitChanges()
		{
			if (!_isInitialized || _items is null || _setter is null)
				throw new InvalidOperationException("The Items, GetValue, and SetValue parameters must be set!");

			// If there are no changes to commit, just return immediately.
			if (!HasChanged) return false;

			// Now commit the changes to the data model.
			TValue? value = Value;
			IsInConflict = false;
			foreach (TItem item in _items)
				_setter(item, value);
			HasChanged = false;
			return true;
		}

		[Parameter] public TValue? ConflictValue { get; set; } = default;

		[Parameter] public bool IsEditableInConflict { get; set; } = true;

		private void OnSourceDataChanged()
		{
			// Both Items and GetValue must be set before we can do anything.
			if (_items is null || _getter is null)
			{
				_isInitialized = false;
				IsInConflict = false;
				HasChanged = false;
				Value = default;
				return;
			}

			// If there are no items, we don't need to do anything.
			if (_items.Count == 0)
			{
				_isInitialized = true;
				IsInConflict = false;
				HasChanged = false;
				Value = default;
				return;
			}

			// Otherwise, analyze the data and determine if each item has the same value or not.
			TValue? first = _getter(_items[0]);
			bool equal;
			if (first != null)
				equal = _items.Skip(1).All(value => first.Equals(_getter(value)));
			else
				equal = _items.Skip(1).All(value => _getter(value) == null);

			// Prepare the class for use!
			if (equal)
			{
				IsInConflict = false;
				Value = first;
			}
			else
			{
				IsInConflict = true;
				Value = ConflictValue;
			}
			HasChanged = false;
			_isInitialized = true;
		}
	}
}
