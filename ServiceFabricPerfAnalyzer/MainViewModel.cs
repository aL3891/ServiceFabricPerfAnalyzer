using ObservableDatabinding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFabricPerfAnalyzer
{
	public class MainViewModel
	{
		public XamlSubject<PerformanceCounterCategory[]> Categories { get; set; } = new XamlSubject<PerformanceCounterCategory[]>();

		public XamlSubject<PerformanceCounterCategory> SelectedCategory { get; set; } = new XamlSubject<PerformanceCounterCategory>();



		public XamlObservable<PerfViewModel[]> PerfValues { get; set; } = new XamlObservable<PerfViewModel[]>();

		public MainViewModel()
		{
			var cats = PerformanceCounterCategory.GetCategories();
			var sfActors = cats.FirstOrDefault(c => c.CategoryName == "Service Fabric Actor");
			var sfActorMethods = cats.FirstOrDefault(c => c.CategoryName == "Service Fabric Actor Method");
			var sfServices = cats.FirstOrDefault(c => c.CategoryName == "Service Fabric Service");
			var sfServiceMethod = cats.FirstOrDefault(c => c.CategoryName == "Service Fabric Service Method");

			Categories.Value = cats;

			SelectedCategory.Observable.CombineLatest(Observable.Interval(TimeSpan.FromSeconds(5)), (p, i) => p);


			PerfValues.Observable = SelectedCategory.Observable.Select(prop =>
			{
				var values = prop.ReadCategory();
				


				return new PerfViewModel[0];

			});
		}
	}

	public class PerfViewModel
	{

	}
}
