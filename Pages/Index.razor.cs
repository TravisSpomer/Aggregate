namespace Aggregate.Pages
{
	public partial class Index
	{
		private readonly Aggregate<Person, string> AggregatedName;
		private readonly Aggregate<Person, string> AggregatedCountry;

		public Index()
		{
			// These have to be initialized here because they depend on "people", and the order that initializers happen in is not guaranteed.
			AggregatedName = new(people, person => person.Name, (person, value) => person.Name = value ?? string.Empty, "(Multiple names)");
			AggregatedCountry = new(people, person => person.Country, (person, value) => person.Country = value ?? string.Empty, string.Empty);
		}
	}
}
