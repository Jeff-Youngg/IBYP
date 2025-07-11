using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
	
	const string YardFile = @"C:\Users\yang_\Desktop\新建文件夹\vesselData\12\UVA\Result\EXM1\11.txt";        
	const string ParamFile = @"C:\Users\yang_\Desktop\新建文件夹\vesselData\12\UVA\Data\vesselData1.txt"; 
	static readonly int[] SwitchCost = { 1200, 3000, 2500, 2100, 3200, 1300, 2100, 2500, 2700 };     

	
	record YardRec(int T, int R);
	record VParam(int Id, int H, int G, int A, int D);
	record VCost(int Id, int Ci, int Seg, double Hold, double Tard, double Sw, double Tot);

	
	static void Main()
	{
		
		ParseYardAndBerth(YardFile,
						  out var yardRaw,   
						  out var berthMap); 

		
		var yardMap = new Dictionary<int, List<YardRec>>();
		foreach (var (vid, recs) in yardRaw)
		{
			var dict = new Dictionary<int, int>();               
			foreach (var rec in recs)
				if (!dict.ContainsKey(rec.T)) dict[rec.T] = rec.R;
			yardMap[vid] = dict.Select(p => new YardRec(p.Key, p.Value))
							   .OrderBy(p => p.T).ToList();
		}

		
		var param = ParseParams(ParamFile);

		
		var res = new List<VCost>();

		foreach (int vid in yardMap.Keys.OrderBy(v => v))
		{
			if (!param.TryGetValue(vid, out var p) ||
				!berthMap.TryGetValue(vid, out var bl) || bl.Count == 0)
			{
				Console.WriteLine($"[Warn] vessel {vid} missing data — skipped");
				continue;
			}

			var list = yardMap[vid];
			CalcSegmentsAndSwitch(list, out int seg, out double sw);

			int Ci = bl.Max();                                
			double hold = p.H * Math.Max(0, Ci - p.A);         
			double tard = p.G * Math.Max(0, Ci - p.D);        
			res.Add(new VCost(vid, Ci, seg, hold, tard, sw, hold + tard + sw));
		}

		
		Console.WriteLine("Vessel |  C_i | Seg |  Hold |  Tard | Switch |  Total");
		Console.WriteLine("-------|-----:|----:|------:|------:|-------:|-------:");
		double tardCostSum = 0;
		double switchTimeSum = 0;
		double switchCostSum = 0;
		foreach (var r in res)
		{
			Console.WriteLine($"{r.Id,6} | {r.Ci,4} | {r.Seg,3} | {r.Hold,6:N0} |" +
							  $" {r.Tard,6:N0} | {r.Sw,7:N0} | {r.Tot,7:N0}");
			tardCostSum += r.Tard;
			switchTimeSum += r.Seg;
			switchCostSum += r.Sw;
		}
		Console.WriteLine($"Objective = {res.Sum(x => x.Tot):N0}");
		Console.WriteLine($"tardCostSum = "+ tardCostSum);
		Console.WriteLine($"switchTimeSum = "+ switchTimeSum);
		Console.WriteLine($"switchCostSum = " + switchCostSum);

		
		Console.WriteLine("\nVessel | tardTime | Switches");
		Console.WriteLine("-------|---------:|---------:");
		double tardTimeSum = 0;
		foreach (var r in res)
		{
			int tardTime = Math.Max(0, r.Ci - param[r.Id].D); 
			Console.WriteLine($"{r.Id,6} | {tardTime,8} | {r.Seg,8}");
			tardTimeSum += tardTime;
		}

		Console.WriteLine("tardTimeSum = "+ tardTimeSum);
	}

	
	static void ParseYardAndBerth(
		string file,
		out Dictionary<int, List<YardRec>> yardMap,
		out Dictionary<int, List<int>> berthMap)
	{
		yardMap = new();
		berthMap = new();
		if (!File.Exists(file)) throw new FileNotFoundException(file);

		var rxY = new Regex(@"vessel\s*(\d+)\s*'s\s*yard\s*assignment",
							 RegexOptions.IgnoreCase);
		var rxB = new Regex(@"vessel\s*(\d+)\s*'s\s*berth\s*allocation",
							 RegexOptions.IgnoreCase);

		int vid = -1; string mode = "";

		foreach (string raw in File.ReadLines(file))
		{
			string line = raw.Trim();
			if (line.Length == 0) continue;

			if (rxY.IsMatch(line)) { vid = int.Parse(rxY.Match(line).Groups[1].Value); mode = "yard"; continue; }
			if (rxB.IsMatch(line)) { vid = int.Parse(rxB.Match(line).Groups[1].Value); mode = "berth"; continue; }
			if (line.StartsWith("=")) continue;

			var tk = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

			if (mode == "yard" && tk.Length >= 3 &&
				int.TryParse(tk[1], out int r) && int.TryParse(tk[2], out int t))
			{
				yardMap.TryAdd(vid, new());
				yardMap[vid].Add(new YardRec(t, r));
			}
			else if (mode == "berth" && tk.Length >= 2 && int.TryParse(tk[1], out int t2))
			{
				berthMap.TryAdd(vid, new());
				berthMap[vid].Add(t2);
			}
		}
	}

	
	static Dictionary<int, VParam> ParseParams(string file)
	{
		var map = new Dictionary<int, VParam>();
		foreach (string raw in File.ReadLines(file))
		{
			var tk = raw.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			if (tk.Length < 5) continue;              
			int id = int.Parse(tk[0]);

			map[id] = new VParam(
				id,
				int.Parse(tk[1]),  
				int.Parse(tk[2]),  
				int.Parse(tk[3]),  
				int.Parse(tk[4])); 
		}
		return map;
	}

	
	static void CalcSegmentsAndSwitch(
		List<YardRec> lst, out int seg, out double cost)
	{
		seg = 1;
		cost = SwitchCost[lst[0].R];

		for (int i = 1; i < lst.Count; i++)
		{
			bool gap = lst[i].T != lst[i - 1].T + 1;
			bool rDiff = lst[i].R != lst[i - 1].R;
			if (gap || rDiff)
			{
				seg++;
				cost += SwitchCost[lst[i].R];
			}
		}
	}
}
