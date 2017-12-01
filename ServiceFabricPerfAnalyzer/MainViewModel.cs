using ObservableDatabinding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ServiceFabricPerfAnalyzer
{
	public class MainViewModel
	{
		public XamlSubject<PerfCounterCategoryViewModel[]> Categories { get; set; } = new XamlSubject<PerfCounterCategoryViewModel[]>();
		public XamlSubject<int> Interval { get; set; } = new XamlSubject<int>();
		public XamlSubject<PerfCounterViewModel> SelectedCounter { get; set; } = new XamlSubject<PerfCounterViewModel>();
		public List<(Type type , PerfCounterProcessorAttribute property)> Processors { get; private set; }

		public MainViewModel()
		{
			Processors = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
						  from type in assembly.GetTypes()
						  let property = type.GetCustomAttributes(true).OfType<PerfCounterProcessorAttribute>().FirstOrDefault()
						  where property != null
						  select (type, property)).ToList();

			var interval = Observable.Switch(Interval.Observable.StartWith(500).Select(i => Observable.Interval(TimeSpan.FromMilliseconds(i))));
			Categories.Value = PerformanceCounterCategory.GetCategories().OrderBy(c => c.CategoryName).Select(c => new PerfCounterCategoryViewModel(c, interval, this)).ToArray();
		}
	}
}
