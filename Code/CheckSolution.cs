using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
	/* ======= 路径与常量 ======= */
	const string resultFile = @"C:\Users\yang_\Desktop\res\uva\5\dp.txt";        // yard+berth 混合文件
	const string vesselFile = @"C:\Users\yang_\Desktop\vesselData5.txt"; // id h g a d …
	static readonly int[] SwitchCost = { 1200, 3000, 2500, 2100, 3200, 1300, 2100 };     // r → 切换费率

	/* ======= 数据结构 ======= */
	record YardRec(int T, int R);
	record VParam(int Id, int H, int G, int A, int D);
	record VCost(int Id, int Ci, int Seg, double Hold, double Tard, double Sw, double Tot);

	/* ======= 入口 ======= */
	static void Main()
	{
		/* 1) 解析 yard-assignment 与 berth-allocation --------------------- */
		ParseYardAndBerth(resultFile,
						  out var yardRaw,   // vessel → List<YardRec>（含重复 t）
						  out var berthMap); // vessel → List<int>

		/* 2) 压缩同一 t 的重复 yard 行（取第一条 r） ----------------------- */
		var yardMap = new Dictionary<int, List<YardRec>>();
		foreach (var (vid, recs) in yardRaw)
		{
			var dict = new Dictionary<int, int>();               // t → r
			foreach (var rec in recs)
				if (!dict.ContainsKey(rec.T)) dict[rec.T] = rec.R;
			yardMap[vid] = dict.Select(p => new YardRec(p.Key, p.Value))
							   .OrderBy(p => p.T).ToList();
		}

		/* 3) 读参数文件 ---------------------------------------------------- */
		var param = ParseParams(vesselFile);

		/* 4) 逐船计算费用 -------------------------------------------------- */
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

			int Ci = bl.Max();                                 // 离泊时间
			double hold = p.H * Math.Max(0, Ci - p.A);         // h_i max(0, C_i − a_i)
			double tard = p.G * Math.Max(0, Ci - p.D);         // g_i max(0, C_i − d_i)
			res.Add(new VCost(vid, Ci, seg, hold, tard, sw, hold + tard + sw));
		}

		/* 5) 输出总表 ------------------------------------------------------ */
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

		/* 6) 额外小表：d_i 与切换次数 ------------------------------------ */
		Console.WriteLine("\nVessel | tardTime | Switches");
		Console.WriteLine("-------|---------:|---------:");
		double tardTimeSum = 0;
		foreach (var r in res)
		{
			int tardTime = Math.Max(0, r.Ci - param[r.Id].D); // 计算滞期时长
			Console.WriteLine($"{r.Id,6} | {tardTime,8} | {r.Seg,8}");
			tardTimeSum += tardTime;
		}

		Console.WriteLine("tardTimeSum = "+ tardTimeSum);
	}

	/* ======= 解析 yard / berth 区块 ===================================== */
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

	/* ======= 解析参数文件 (id h g a d …) ================================= */
	static Dictionary<int, VParam> ParseParams(string file)
	{
		var map = new Dictionary<int, VParam>();
		foreach (string raw in File.ReadLines(file))
		{
			var tk = raw.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			if (tk.Length < 5) continue;               // 至少 id h g a d
			int id = int.Parse(tk[0]);

			map[id] = new VParam(
				id,
				int.Parse(tk[1]),  // h_i
				int.Parse(tk[2]),  // g_i
				int.Parse(tk[3]),  // a_i
				int.Parse(tk[4])); // d_i
		}
		return map;
	}

	/* ======= 统计段数与切换费用 ========================================= */
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
