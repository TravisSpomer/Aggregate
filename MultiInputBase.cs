using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Aggregate
{
	public abstract class MultiInputBase<TItem, TValue> : ComponentBase where TItem : class
	{
		protected bool _isInitialized = false;
		protected EditContext? _editContext;

		private IReadOnlyList<TItem>? _items;
		private string? _fieldName;
		private Func<TItem, TValue>? _getter;
		private Action<TItem, TValue?>? _setter;
		private TValue? _value;

		private FieldIdentifier? _fieldIdentifier;
		private ValidationMessageStore? _errors;

		[CascadingParameter]
		private EditContext? CurrentEditContext
		{
			get
			{
				return _editContext;
			}
			set
			{
				if (_editContext == value) return;
				if (_editContext is not null)
				{
					_editContext.OnValidationRequested -= OnValidationRequested;
					_errors = null;
				}
				_editContext = value;
				if (_editContext is not null)
				{
					_editContext.OnValidationRequested += OnValidationRequested;
					_errors = new(_editContext);
				}
			}
		}

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

		[Parameter]
		public string? FieldName
		{
			get
			{
				return _fieldName;
			}
			set
			{
				if (_fieldName == value) return;
				_fieldName = value;
				OnSourceDataChanged();
			}
		}

		public TValue? Value
		{
			get
			{
				return _value;
			}
			protected set
			{
				if ((_value is null && value is null) || (value is not null && _value is not null && value.Equals(_value))) return;
				_value = value;
				HasChanged = true;
			}
		}

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
			// Multiple properties must be set before we can do anything.
			if (_items is null || _getter is null || _fieldName is null)
			{
				_isInitialized = false;
				_fieldIdentifier = null;
				IsInConflict = false;
				HasChanged = false;
				Value = default;
				return;
			}

			_fieldIdentifier = new FieldIdentifier(_items, _fieldName);

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

		private void OnValidationRequested(object? _sender, ValidationRequestedEventArgs _e)
		{
			// Validation doesn't work outside of an EditForm.
			if (!_isInitialized || _items is null || CurrentEditContext is null || _errors is null || _fieldIdentifier is null) return;
			if (FieldName is null) throw new InvalidOperationException("FieldName must not be null.");

			_errors.Clear();

			// Validation doesn't work when there are multiple items selected in conflict.
			// Otherwise, do normal validation.
			if (!IsInConflict)
			{
				Validate();
				// ValidationError("I'm always broken");
			}

			CurrentEditContext.NotifyValidationStateChanged();
		}

		protected virtual void Validate()
		{
			// Subclasses can use this to perform additional validation before the user-supplied validation.
			// (For example, a number field can verify that it's a valid number.)
		}

		protected void ValidationError(string message)
		{
			if (_errors is null || _fieldIdentifier is null) return;
			_errors.Add((FieldIdentifier)_fieldIdentifier, message);
		}

		protected IEnumerable<string> Errors
		{
			get
			{
				if (_errors is null || _fieldIdentifier is null) return Enumerable.Empty<string>();
				return _errors[(FieldIdentifier)_fieldIdentifier];
			}
		}

	}
}
