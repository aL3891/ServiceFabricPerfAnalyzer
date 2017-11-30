using ObservableDatabinding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
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

		public MainViewModel()
		{
			//var cats = PerformanceCounterCategory.GetCategories();
			//var sfActors = cats.FirstOrDefault(c => c.CategoryName == "Service Fabric Actor");
			//var sfActorMethods = cats.FirstOrDefault(c => c.CategoryName == "Service Fabric Actor Method");
			//var sfServices = cats.FirstOrDefault(c => c.CategoryName == "Service Fabric Service");
			//var sfServiceMethod = cats.FirstOrDefault(c => c.CategoryName == "Service Fabric Service Method");

			var interval = Observable.Switch(Interval.Observable.StartWith(1).Select(i => Observable.Interval(TimeSpan.FromSeconds(i))));
			Categories.Value = PerformanceCounterCategory.GetCategories().OrderBy(c => c.CategoryName).Select(c => new PerfCounterCategoryViewModel(c, interval, this)).ToArray();
		}
	}




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
						valueDictionary.Add(v.CounterName, new PerfCounterViewModel(parent));
					}
					valueDictionary[v.CounterName].Update(cat.CategoryName, v);
				}

				if (changed)
					Values.Value = valueDictionary.Values.ToList();
			});
		}
	}


	public static class NameResolver
	{
		static Lazy<Dictionary<String, (Application app, Service service, Partition partition)>> apa = new Lazy<Dictionary<String, (Application app, Service service, Partition partition)>>(() =>
		{
			FabricClient client = new FabricClient();
			var apps = client.QueryManager.GetApplicationListAsync().Result;
			var nodes = client.QueryManager.GetNodeListAsync();
			var apa2 = from app in apps
					   from s in client.QueryManager.GetServiceListAsync(app.ApplicationName).Result
					   from p in client.QueryManager.GetPartitionListAsync(s.ServiceName).Result
					   select (app, s, p);
			return apa2.ToDictionary(k => k.p.PartitionInformation.Id.ToString(), v => v);
		});

		public static async Task<string> Resolve(string categoryName, string perfcounter, string instance)
		{
			//should probably be more pluggable
			var res = instance;

			switch (categoryName)
			{
				case "Service Fabric Actor":
					res = apa.Value[instance.Split('_')[0]].service.ServiceName.AbsolutePath.Substring(1);
					break;
				case "Service Fabric Actor Method":
					res = apa.Value[instance.Split('_')[2]].service.ServiceName.AbsolutePath.Substring(1);
					res = res + "." + instance.Split('_')[0];
					break;
				case "Service Fabric Service":
					res = apa.Value[instance.Split('_')[0]].service.ServiceName.AbsolutePath.Substring(1);
					break;
				case "Service Fabric Service Method":
					res = apa.Value[instance.Split('_')[2]].service.ServiceName.AbsolutePath.Substring(1);
					res = res + "." + instance.Split('_')[0];
					break;
				//case "Service Fabric Actor":
				//	break;
				//case "Service Fabric Actor":
				//	break;
				//case "Service Fabric Actor":
				//	break;
				//case "Service Fabric Actor":
				//	break;
				//case "Service Fabric Actor":
				//	break;
				//case "Service Fabric Actor":
				//	break;
				//case "Service Fabric Actor":
				//	break;

				default:
					break;
			}
			return res;
		}
	}

	public class PerfCounterViewModel
	{
		public XamlSubject<string> Name { get; set; } = new XamlSubject<string>();
		public XamlSubject<bool> Selected { get; set; } = new XamlSubject<bool>();
		public XamlSubject<IEnumerable<InstanceValueViewModel>> Values { get; set; } = new XamlSubject<IEnumerable<InstanceValueViewModel>>();
		Dictionary<string, InstanceValueViewModel> valueDictionary = new Dictionary<string, InstanceValueViewModel>();

		public PerfCounterViewModel(MainViewModel parent)
		{
			Selected.Observable.Subscribe(s =>
			{
				parent.SelectedCounter.Value = this;
			});
		}

		public void Update(string categoryName, InstanceDataCollection id)
		{
			Name.Value = id.CounterName;
			if (Selected.Value)
			{
				foreach (var v in id.Values.OfType<InstanceData>())
				{
					if (!valueDictionary.ContainsKey(v.InstanceName))
						valueDictionary.Add(v.InstanceName, new InstanceValueViewModel(NameResolver.Resolve(categoryName, id.CounterName, v.InstanceName).Result));
					valueDictionary[v.InstanceName].Update(v);
				}

				Values.Value = valueDictionary.Values.OrderByDescending(v => v.Value.Value).ToList();
				Name.Value += " (" + Values.Value.FirstOrDefault()?.Value.Value.ToString("#######0.##") + ")";
			}
			else
			{
				var max = id.Values.OfType<InstanceData>().OrderByDescending(i => i.RawValue).FirstOrDefault();
				if (max != null)
				{
					if (!valueDictionary.ContainsKey(max.InstanceName))
						valueDictionary.Add(max.InstanceName, new InstanceValueViewModel(NameResolver.Resolve(categoryName, id.CounterName, max.InstanceName).Result));
					valueDictionary[max.InstanceName].Update(max);
					Name.Value += " (" + valueDictionary[max.InstanceName].Value.Value.ToString("#######0.##") + ")";
				}
			}
		}
	}

	public class InstanceValueViewModel
	{
		public XamlSubject<string> Instance { get; set; } = new XamlSubject<string>();
		public XamlSubject<string> Name { get; set; } = new XamlSubject<string>();
		public XamlSubject<float> Value { get; set; } = new XamlSubject<float>();
		public CounterSample lastSample = CounterSample.Empty;

		public InstanceValueViewModel(string name)
		{
			Name.Value = name;
		}

		internal void Update(InstanceData i)
		{
			Value.Value = CounterSample.Calculate(lastSample, i.Sample);
			lastSample = i.Sample;

			if (Instance.Value == null)
				Instance.Value = i.InstanceName;
		}
	}
}
