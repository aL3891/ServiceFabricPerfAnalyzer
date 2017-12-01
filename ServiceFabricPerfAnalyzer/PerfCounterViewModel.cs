using ObservableDatabinding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace ServiceFabricPerfAnalyzer
{
	public class PerfCounterViewModel
	{
		public XamlSubject<string> Name { get; set; } = new XamlSubject<string>();
		public XamlSubject<bool> Selected { get; set; } = new XamlSubject<bool>();
		public XamlSubject<IEnumerable<InstanceValueViewModel>> Values { get; set; } = new XamlSubject<IEnumerable<InstanceValueViewModel>>();
		public IPerfcounterProcessor Processor { get; }

		Dictionary<string, InstanceValueViewModel> valueDictionary = new Dictionary<string, InstanceValueViewModel>();

		public PerfCounterViewModel(MainViewModel parent, IPerfcounterProcessor proecssor)
		{
			Selected.Observable.Subscribe(s => { parent.SelectedCounter.Value = this; });
			Processor = proecssor;
		}

		public void Update(InstanceDataCollection id)
		{
			var data = new InstanceData[id.Count];
			id.CopyTo(data, 0);
			Name.Value = id.CounterName;

			foreach (var v in data)
			{
				if (!valueDictionary.ContainsKey(v.InstanceName))
					valueDictionary.Add(v.InstanceName, new InstanceValueViewModel(v.InstanceName, Processor));
				valueDictionary[v.InstanceName].Update(v);
			}

			var ordered = valueDictionary.Values.OrderByDescending(v => v.Value.Value).ToList();
			Name.Value += " (" + ordered.FirstOrDefault()?.Value.Value.ToString("#######0.##") + ")";

			if (Selected.Value)
				Values.Value = ordered;
		}
	}
}
