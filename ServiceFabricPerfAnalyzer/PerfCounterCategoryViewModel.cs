using ObservableDatabinding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace ServiceFabricPerfAnalyzer
{
	public class PerfCounterCategoryViewModel
	{
		public XamlSubject<string> Name { get; set; } = new XamlSubject<string>();
		public XamlSubject<bool> Expanded { get; set; } = new XamlSubject<bool>();
		public XamlSubject<bool> Selected { get; set; } = new XamlSubject<bool>();
		public XamlSubject<IEnumerable<PerfCounterViewModel>> Values { get; set; } = new XamlSubject<IEnumerable<PerfCounterViewModel>>();
		Dictionary<string, PerfCounterViewModel> valueDictionary = new Dictionary<string, PerfCounterViewModel>();

		public PerfCounterCategoryViewModel(PerformanceCounterCategory cat, IObservable<long> interval, MainViewModel parent)
		{
			Name.Value = cat.CategoryName;
			Values.Value = new PerfCounterViewModel[1];



			Expanded.Observable.CombineLatest(interval, (p, i) => p).Where(p => p).Subscribe(e =>
			{
				var vals = cat.ReadCategory();
				bool changed = false;

				foreach (var v in vals.Values.OfType<InstanceDataCollection>().OrderBy(v => v.CounterName))
				{
					if (!valueDictionary.ContainsKey(v.CounterName))
					{
						changed = true;
						var processors = parent.Processors.Where(x => Regex.IsMatch(cat.CategoryName, x.property.Category)).Select(x => (IPerfcounterProcessor)Activator.CreateInstance(x.type));
						var processor = processors.FirstOrDefault() ?? new DefaultperfcounterProcessor();
						processor.Category = cat.CategoryName;
						processor.Counter = v.CounterName;
						valueDictionary.Add(v.CounterName, new PerfCounterViewModel(parent, processor));
					}
					valueDictionary[v.CounterName].Update(v);
				}

				if (changed)
					Values.Value = valueDictionary.Values.ToList();
			});
		}
	}
}
