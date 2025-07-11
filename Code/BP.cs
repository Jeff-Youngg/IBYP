

namespace Vessel
{
	using ILOG.Concert;
	using ILOG.CPLEX;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics.Metrics;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Security.Authentication.ExtendedProtection;
	using System.Windows.Markup;
	using static ILOG.CPLEX.Cplex.Callback.Context;
	using static Vessel.Program;
	using static Vessel.Program.Vessel;

	internal class Program
	{
		public class inherit_info
		{
			int[] xposition;
			int indicator;
			public int[] Xposition { get => xposition; set => xposition = value; }
			public int Indicator { get => indicator; set => indicator = value; }
			public inherit_info(int[] xpos, int ind)
			{
				this.Xposition = xpos;
				this.Indicator = ind;
			}

			public inherit_info(inherit_info other)
			{
				this.Indicator = other.Indicator;

				this.Xposition = new int[other.Xposition.Length];

				for (int i = 0; i < other.Xposition.Length; i++)
				{
					this.Xposition[i] = other.xposition[i];
				}
			}
		}

		public class TreeNode
		{
			List<Vessel> pond;
			List<Vessel> cur_resultList;
			private double cost;
			List<inherit_info> constraints_add;
			public double Cost
			{
				get { return cost; }
				set { cost = value; }
			}

			public List<Vessel> Pond { get => pond; set => pond = value; }
			public List<Vessel> Cur_resultList { get => cur_resultList; set => cur_resultList = value; }
			public List<inherit_info> Constraints_add { get => constraints_add; set => constraints_add = value; }

			public TreeNode(List<Vessel> pool, List<Vessel> currentResultList, double cost, List<inherit_info> constraintsToAdd)
			{
				this.Pond = pool;
				this.Cur_resultList = currentResultList;
				this.cost = cost;
				this.Constraints_add = constraintsToAdd;
			}

			public TreeNode()
			{

			}

		}

		public class Costcomparer : IComparer<TreeNode>//
		{

			public int Compare(TreeNode x, TreeNode y)
			{
				return (x.Cost.CompareTo(y.Cost));
			}
		}

		public class Ccomparer : IComparable<Vessel>// 
		{
			public Vessel te;
			public int CompareTo(Vessel obj)
			{
				if (this.te.C == obj.C)
				{
					return 0;
				}
				else if (this.te.C > obj.C)
				{
					return 1;
				}
				else
				{
					return -1;
				}
			}

		}

		public class Node
		{
			public bool dummy;
			public int waitTime;
			public double[] tau;
			public int switchCost;
			public int id;
			public int serviceTime;
			public List<(int start, int end)> TimeWindow;
			private static int idCounter = 0;

			public Node(int id = -1)
			{
				if (id == -1)
				{
					this.id = idCounter++;
				}
				else
				{
					this.id = id;
				}
				this.tau = new double[R];
			}

			public Node(Node other)
			{
				this.switchCost = other.switchCost;
				this.id = other.id;
				this.serviceTime = other.serviceTime;
				this.tau = new double[other.tau.Count()];
				for (int i = 0; i < other.tau.Count(); i++)
				{
					tau[i] = other.tau[i];
				}

				this.TimeWindow = new List<(int start, int end)>(other.TimeWindow);
			}

			public Node(double[] tau, int switchCost, int id, int serviceTime, List<(int start, int end)> timeWindow)
			{
				this.tau = tau;
				this.switchCost = switchCost;
				this.id = id;
				this.serviceTime = serviceTime;
				TimeWindow = timeWindow;
			}
		}

		public class Vessel : IEquatable<Vessel>
		{
			public int No;
			public int id;
			public double pi;
			public double h;
			public double g;
			public int a;
			public int[] switchCost;
			public int d;
			public int K;
			public int[] Ki;
			public int[] pik;
			public int[,] e;
			public double[,,] x;
			public double[,,] alpha;
			public double[,] z;
			public double[] p;

			private static int NoCounter = 0;

			public int S;
			public int C;
			public int D;
			public bool assigned;

			public int wait;
			public int w = 0;
			public double cost;
			public double lambda;
			public double[,] beta;

			public Vessel(int No = -1)
			{
				if (No == -1)
				{
					this.No = NoCounter++;
				}
				else
				{
					this.No = No;
				}
				switchCost = new int[R];
				Array.Fill(switchCost, 0);
			}

			public Vessel(int id, double h, double g, int a, int d, int K, int[] Ki, int[] pik, int No = -1)
			{
				if (No == -1)
				{
					this.No = NoCounter++;
				}
				else
				{
					this.No = No;
				}

				this.id = id;
				switchCost = new int[R];
				Array.Fill(switchCost, 0);
				this.h = h;
				this.g = g;
				this.a = a;
				this.d = d;
				this.K = K;
				this.Ki = Ki;
				this.pik = pik;
			}


			public class CComparer : IComparer
			{
				public int Compare(object x, object y)
				{
					int ii;
					int a = (int)x;
					int b = (int)y;
					if (a > b)
					{
						ii = 1;
					}
					else
					{
						ii = -1;
					}
					return ii;

				}
			}

			public class DComparer : IComparer
			{
				public int Compare(object x, object y)
				{
					int ii;
					double a = (double)x;
					double b = (double)y;
					if (a > b)
					{
						ii = 1;
					}
					else
					{
						ii = -1;
					}
					return ii;

				}
			}

			public class VesselComparer : IComparer<Vessel>
			{
				public enum CompareType
				{
					a, b, id, S, C, cost
				}
				private CompareType type;

				public VesselComparer(CompareType type)
				{
					this.type = type;
				}
				public int Compare(Vessel x, Vessel y)
				{
					switch (this.type)
					{
						case CompareType.a:
							return x.a.CompareTo(y.a);
						case CompareType.S:
							return x.S.CompareTo(y.S);
						case CompareType.C:
							return x.C.CompareTo(y.C);
						case CompareType.cost:
							return x.cost.CompareTo(y.cost);

						default:
							return x.id.CompareTo(y.id);
					}
				}
			}

			public Vessel(Vessel other, bool flag = true)
			{
				if (flag == true)
				{
					this.No = NoCounter++;
				}
				else
				{
					this.No = other.No;
				}


				this.id = other.id;
				this.switchCost = new int[R];
				for (int i = 0; i < R; i++)
				{
					switchCost[i] = other.switchCost[i];
				}
				this.h = other.h;
				this.g = other.g;
				this.a = other.a;
				this.d = other.d;
				this.K = other.K;

				this.Ki = new int[other.Ki.Count()];
				for (int i = 0; i < other.Ki.Count(); i++)
				{
					Ki[i] = other.Ki[i];
				}

				this.pik = new int[other.pik.Count()];
				for (int i = 0; i < other.pik.Count(); i++)
				{
					pik[i] = other.pik[i];
				}

				int dim1e = other.e.GetLength(0);
				int dim2e = other.e.GetLength(1);
				this.e = new int[dim1e, dim2e];
				Array.Copy(other.e, this.e, other.e.Length);

				int dim1x = other.x.GetLength(0);
				int dim2x = other.x.GetLength(1);
				int dim3x = other.x.GetLength(2);
				this.x = new double[dim1x, dim2x, dim3x];
				Array.Copy(other.x, this.x, other.x.Length);

				int dim1alpha = other.alpha.GetLength(0);
				int dim2alpha = other.alpha.GetLength(1);
				int dim3alpha = other.alpha.GetLength(2);
				this.alpha = new double[dim1alpha, dim2alpha, dim3alpha];
				Array.Copy(other.alpha, this.alpha, other.alpha.Length);

				int dim1z = other.z.GetLength(0);
				int dim2z = other.z.GetLength(1);
				this.z = new double[dim1z, dim2z];
				Array.Copy(other.z, this.z, other.z.Length);


				this.S = other.S;
				this.C = other.C;
				this.D = other.D;

				this.p = new double[other.p.Count()];
				Array.Copy(other.p, this.p, other.p.Length);

				int dim1beta = other.beta.GetLength(0);
				int dim2beta = other.beta.GetLength(1);
				this.beta = new double[dim1beta, dim2beta];
				Array.Copy(other.beta, this.beta, other.beta.Length);

				this.assigned = other.assigned;

				this.wait = other.wait;
				this.w = other.w;
				this.cost = other.cost;
				this.lambda = other.lambda;
			}

			public bool Equals(Vessel other)
			{
				if (other == null) return false;

				
				if (this.id != other.id) return false;
				if (this.S != other.S) return false;
				if (this.C != other.C) return false;
				if (this.D != other.D) return false;
				if (this.cost != other.cost) return false;

				for (int b = 0; b < W; b++)
				{
					if (this.p[b] != other.p[b])
					{
						return false;
					}
				}
				
				if (!Are2DArraysEqual(this.beta, other.beta))
					return false;
				if (!Are2DArraysEqual(this.z, other.z))
					return false;
				if (!Are3DArraysEqual(this.alpha, other.alpha))
					return false;
				if (!Are3DArraysEqual(this.x, other.x))
					return false;
				return true;
			}

			
			public override bool Equals(object obj)
			{
				return Equals(obj as Vessel);
			}

			
			public override int GetHashCode()
			{
				unchecked
				{
					int hash = 17;

				
					hash = hash * 23 + this.id.GetHashCode();
					hash = hash * 23 + this.S.GetHashCode();  
					hash = hash * 23 + this.C.GetHashCode();
					hash = hash * 23 + this.D.GetHashCode();

					
					if (this.beta != null)
					{
						int rows = this.beta.GetLength(0);
						int cols = this.beta.GetLength(1);

						
						hash = hash * 23 + rows.GetHashCode();
						hash = hash * 23 + cols.GetHashCode();

						
						for (int i = 0; i < rows; i++)
						{
							for (int j = 0; j < cols; j++)
							{
								hash = hash * 23 + this.beta[i, j].GetHashCode();
							}
						}
					}

					
					if (this.z != null)
					{
						int rows = this.z.GetLength(0);
						int cols = this.z.GetLength(1);

						hash = hash * 23 + rows.GetHashCode();
						hash = hash * 23 + cols.GetHashCode();

						for (int i = 0; i < rows; i++)
						{
							for (int j = 0; j < cols; j++)
							{
								hash = hash * 23 + this.z[i, j].GetHashCode();
							}
						}
					}

					
					if (this.alpha != null)
					{
						int d0 = this.alpha.GetLength(0);
						int d1 = this.alpha.GetLength(1);
						int d2 = this.alpha.GetLength(2);

						hash = hash * 23 + d0.GetHashCode();
						hash = hash * 23 + d1.GetHashCode();
						hash = hash * 23 + d2.GetHashCode();

						for (int i = 0; i < d0; i++)
						{
							for (int j = 0; j < d1; j++)
							{
								for (int k = 0; k < d2; k++)
								{
									hash = hash * 23 + this.alpha[i, j, k].GetHashCode();
								}
							}
						}
					}

					if (this.p != null)
					{
						for (int b = 0; b < W; b++)
						{
							hash = hash * 23 + this.p[b].GetHashCode();
						}
					}

					return hash;
				}
			}


			private static bool Are3DArraysEqual(double[,,] arr1, double[,,] arr2)
			{
				if (arr1 == null && arr2 == null) return true;
				if (arr1 == null || arr2 == null) return false;

				
				if (arr1.GetLength(0) != arr2.GetLength(0)) return false;
				if (arr1.GetLength(1) != arr2.GetLength(1)) return false;
				if (arr1.GetLength(2) != arr2.GetLength(2)) return false;

				int d0 = arr1.GetLength(0);
				int d1 = arr1.GetLength(1);
				int d2 = arr1.GetLength(2);

				for (int i = 0; i < d0; i++)
				{
					for (int j = 0; j < d1; j++)
					{
						for (int k = 0; k < d2; k++)
						{
	
							if (arr1[i, j, k] != arr2[i, j, k])
							{
								
								return false;
							}
						}
					}
				}

				return true;
			}
			// 
			private static bool Are2DArraysEqual(double[,] arr1, double[,] arr2)
			{
				if (arr1 == null && arr2 == null) return true;
				if (arr1 == null || arr2 == null) return false;

				if (arr1.GetLength(0) != arr2.GetLength(0)) return false;
				if (arr1.GetLength(1) != arr2.GetLength(1)) return false;

				int dim0 = arr1.GetLength(0);
				int dim1 = arr1.GetLength(1);

				for (int i = 0; i < dim0; i++)
				{
					for (int j = 0; j < dim1; j++)
					{
						if (arr1[i, j] != arr2[i, j])
							return false;
					}
				}

				return true;
			}
		}

		public class Label : IComparable<Label>
		{
			public int id;
			public int finished;
			public int[] processingTime;
			public int t;
			public int s;//
			public double cost;
			public int switchTime;
			private static int idCounter = 0;
			public int currentN;// 
			public Label prev;
			public int delta;//

			public Label(int materialNum, int id = -1)
			{

				if (id == -1)
				{
					this.id = idCounter++;
				}
				else
				{
					this.id = id;
				}

				processingTime = new int[materialNum];
				Array.Fill(processingTime, 0);
			}

			public Label(Label other)
			{
				this.currentN = other.currentN;
				this.s = other.s;
				this.id = other.id;
				this.t = other.t;
				this.cost = other.cost;
				this.switchTime = other.switchTime;
				this.prev = other.prev;

				processingTime = new int[other.processingTime.Count()];
				for (int i = 0; i < other.processingTime.Count(); i++)
				{
					processingTime[i] = other.processingTime[i];
				}
			}

			public override bool Equals(object obj)
			{
			
				if (obj == null || obj.GetType() != this.GetType())
					return false;

				
				var label = obj as Label;
				return label != null &&
					   id == label.id;
			}

			
			public override int GetHashCode()
			{
				
				return id.GetHashCode();
			}

			public int CompareTo(Label other)
			{
				if (other == null)
					return 1; 

				int cmp = this.cost.CompareTo(other.cost);
				if (cmp == 0) cmp = this.t.CompareTo(other.t);
				if (cmp == 0) cmp = this.switchTime.CompareTo(other.switchTime);
				if (cmp == 0) cmp = this.id.CompareTo(other.id);

				return cmp;
			}
		}


		public class LabelComparer : IEqualityComparer<Label>
		{
			public bool Equals(Label x, Label y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (x == null || y == null) return false;
				
				return x.id == y.id;
			}

			public int GetHashCode(Label obj)
			{
				
				int hashId = obj.id.GetHashCode();

				return hashId; 
			}
		}



		public static List<List<(int start, int end)>> conflictInfo;
		public static int[] switchingCost;
		static double RC_EPS = 1.0e-5;
		static int N = 5;
		static int W = 3;
		static int R = 7;
		static int K = 10;
		static int T = 144;
		static int interruptTimes = 2;
		static int[][] Rk;

		static double Reducedcost = 0;
		static double initcost = 0;
		public static List<Vessel> ReadVesselData(string filename)
		{
			if (R == 7)
			{
				//Rk = new int[10][] { new int[]{0,1,2},
				//			   /*1*/ new int[]{3,4,5},
				//			   /*2*/ new int[]{0,3,6},
				//			   /*3*/ new int[]{1,4,6},
				//			   /*4*/ new int[]{1,2,5},
				//			   /*5*/ new int[]{0,2,4},
				//			   /*6*/ new int[]{1,3,5},
				//			   /*7*/ new int[]{2,3,6},
				//			   /*8*/ new int[]{4,6},
				//			   /*9*/ new int[]{0,5} };

				

				//Rk = new int[15][] { new int[]{ 1,  6 }, new int[] { 1, 3, 5 }, new int[] {  4, 6 }, new int[] { 3, 5 }, new int[] { 0, 4, 6 },
				//				new int[]{ 3, 5 }, new int[]{ 0, 2 }, new int[]{ 3, 6 }, new int[]{ 5, 1 }, new int[]{ 2, 6 }, new int[]{ 1, 6 },
				//				new int[]{ 6, 0 }, new int[]{ 3, 6, 5 }, new int[]{ 1, 5 }, new int[]{ 0, 3, 4 } };//7个料条 
				Rk = new int[15][] { new int[]{ 1, 5, 6 }, new int[] { 1, 3, 5 }, new int[] { 2, 4, 6 }, new int[] { 3, 5 }, new int[] { 0, 4, 6 },
									 new int[]{ 3, 5, 6 }, new int[]{ 0, 2 }, new int[]{ 3, 6 }, new int[]{ 5, 1 }, new int[]{ 2, 6 }, new int[]{ 1, 6 },
									 new int[]{ 6, 0 }, new int[]{ 3, 6, 5 }, new int[]{ 1, 5 }, new int[]{ 0, 3, 4 } };//7个料条
			}

			if (R == 9)
			{
				Rk = new int[15][] {
					new[]{1,5,6,7}, new[]{1,3,5,8}, new[]{2,4,6,7}, new[]{3,5,8},  new[]{0,4,6},
new[]{3,5,6}, new[]{0,2},   new[]{3,6},   new[]{5,1},  new[]{2,6},
new[]{1,6},   new[]{6,0},   new[]{3,6,5}, new[]{1,5},  new[]{0,3,4}
				};

				//Rk = new int[15][] { new int[]{ 1, 5, 6 }, new int[] { 1, 3, 5 }, new int[] { 2, 4, 6 }, new int[] { 3, 5, 7 }, new int[] { 0, 4, 6, 8 },
				//				new int[]{ 3, 5, 6, 7 }, new int[]{ 0, 2 ,8}, new int[]{ 3, 6, 7 }, new int[]{ 5, 1, 8 }, new int[]{ 2, 6 }, new int[]{ 1, 6 },
				//				new int[]{ 6, 0 }, new int[]{ 3, 6, 5 }, new int[]{ 1, 5 }, new int[]{ 0, 3, 4 } };//9个料条
				//Rk = new int[15][] { new int[] { 7, 5, 6 }, new int[] { 1, 3, 5 }, new int[] { 2, 4, 6 }, new int[] { 3, 5 }, new int[] { 0, 4, 8 }, new int[] { 3, 5, 7 }, new int[] { 0, 2 }, new int[] { 3, 6 }, new int[] { 5, 8 }, new int[] { 2, 7 }, new int[] { 1, 8 }, new int[] { 6, 7 }, new int[] { 3, 6, 5 }, new int[] { 4, 5 }, new int[] { 8, 3, 4 } };
			}
			List<Vessel> vessel_para = new List<Vessel>();
			Vessel v = new Vessel(0, 0, 0, 0, 0, 0, null, null);
			StreamReader sr = new StreamReader(filename);
			string nextline;

			while ((nextline = sr.ReadLine()) != null)
			{
				System.String[] data = nextline.Split(' ');
				int id = Convert.ToInt32(data[0]);//序号 从1开始
				double h = Convert.ToInt32(data[1]); //完工时间对应费用
				double g = Convert.ToInt32(data[2]);//超期惩罚
				int a = Convert.ToInt32(data[3]);//到达时间
												 //int b = Convert.ToInt32(data[4]);
				int d = Convert.ToInt32(data[4]);//预计离开时间
				int K = Convert.ToInt32(data[5]);//装载原料种数
				int[] Ki = new int[K];//装载的原料种类 序号
				for (int i = 0; i < K; i++)
				{
					Ki[i] = Convert.ToInt32(data[data.Length - 2 * K + i]) - 1;
				}
				int[] pk = new int[K];//每种原料所需卸载时间长度

				for (int i = 0; i < K; i++)
				{
					pk[i] = Convert.ToInt32(data[data.Length - K + i]);
				}
				v = new Vessel(id, h, g, a, d, K, Ki, pk);
				vessel_para.Add(v);

			}
			sr.Close();
			return vessel_para;
		}

		public static List<Node> allNodes;

		public static List<Node> dummyNodes;

		public static void InitialNodes()
		{
			allNodes = new List<Node>();
			for (int i = 0; i < R; i++)
			{
				Node nd = new Node(i);
				nd.TimeWindow = conflictInfo[i];
				nd.switchCost = switchingCost[i];
				allNodes.Add(nd);
			}

			Node dummy1 = new Node();
			dummy1.dummy = true;
			dummy1.waitTime = 1;

			Node dummy2 = new Node();
			dummy2.dummy = true;
			dummy2.waitTime = 2;

			dummyNodes = new List<Node>() { dummy1, dummy2 };

		}

		public static void ReadStockRowSwitchingData(string filename)
		{
			switchingCost = new int[R];
			StreamReader sr = new StreamReader(filename);
			string nextline;

			while ((nextline = sr.ReadLine()) != null)
			{
				System.String[] data = nextline.Split(' ');
				int stockRowNum = Convert.ToInt32(data[0]);

				for (int i = 0; i < stockRowNum; i++)
				{
					switchingCost[i] = Convert.ToInt32(data[1 + i]);
				}
			}
			sr.Close();
		}

		public static List<List<(int start, int end)>> ReadConflictData(string filename)
		{
			List<List<(int start, int end)>> conflictInfo = new List<List<(int start, int end)>>();
			StreamReader sr = new StreamReader(filename);
			string nextline;

			while ((nextline = sr.ReadLine()) != null)
			{
				System.String[] data = nextline.Split(' ');
				int rowID = Convert.ToInt32(data[0]);
				int pairNum = Convert.ToInt32(data[1]);

				List<(int start, int end)> tw = new List<(int start, int end)>();

				for (int i = 0; i < pairNum; i++)
				{
					int start = Convert.ToInt32(data[2 + i * 2]);
					int end = Convert.ToInt32(data[2 + i * 2 + 1]);
					tw.Add((start, end));
				}
				conflictInfo.Add(tw);
			}
			sr.Close();
			return conflictInfo;
		}

		public static void initialVessel(List<Vessel> v, string switchDataStr, string conflictDataStr)
		{
			ReadStockRowSwitchingData(switchDataStr);
			conflictInfo = ReadConflictData(conflictDataStr);

			foreach (Vessel temp in v)
			{
				temp.switchCost = switchingCost;
				temp.e = new int[R, T];
				temp.x = new double[K, R, T];
				temp.z = new double[K, R];
				temp.beta = new double[W, T];
				temp.p = new double[W];

				temp.alpha = new double[K, R, T];
				temp.assigned = false;
				temp.lambda = 1;
				for (int t = 0; t < T; t++)
				{
					for (int b = 0; b < W; b++)
					{
						temp.beta[b, t] = 0;
						temp.p[b] = 0;
					}
				}


				for (int k = 0; k < K; k++)
				{
					for (int r = 0; r < R; r++)
					{

						temp.z[k, r] = 0;
						for (int t = 0; t < T; t++)
						{
							temp.x[k, r, t] = 0;
							temp.alpha[k, r, t] = 0;
							//temp.e[r, t] = 0;
						}
					}
				}

				for (int r = 0; r < R; r++)
				{
					for (int t = 0; t < T; t++)
					{
						temp.e[r, t] = 0;
					}
				}

				for (int r = 0; r < conflictInfo.Count; r++)
				{
					foreach (var pair in conflictInfo[r])
					{
						for (int t = pair.start; t <= pair.end; t++)
						{
							temp.e[r, t] = 1;
						}
					}
				}

			}
		}

		static List<int> port_stayed = new List<int>();
	

		static void GenerateCombinations(int[][] Rk, int[] Ks, int depth, int[] current, List<int[]> combinations)
		{
			if (depth == Ks.Length)
			{
				combinations.Add((int[])current.Clone());
				return;
			}

			int index = Ks[depth]; //
			foreach (int value in Rk[index])
			{
				current[depth] = value;
				GenerateCombinations(Rk, Ks, depth + 1, current, combinations);
			}
		}

		public static void RemoveDominatedWithin(SortedSet<Label> pq, Vessel v)
		{
			var labelList = pq.ToList();

			
			for (int i = labelList.Count - 1; i >= 0; i--)
			{
				var current = labelList[i];
				bool dominated = false;
				
				for (int j = 0; j < labelList.Count; j++)
				{
					if (i == j) continue;
					if (Dominates(labelList[j], current, v))
					{
						dominated = true;
						break;
					}
				}
				
				if (dominated)
				{
					labelList.RemoveAt(i);
				}
			}

			// 
			pq.Clear();
			foreach (var label in labelList)
			{
				pq.Add(label);
			}
		}

		public static Dictionary<int, List<Label>> labelsAtNodes = new Dictionary<int, List<Label>>();

		static void InitialSolution(List<Vessel> vs)
		{
			int[] portAvailableTime = new int[W];
			vs = vs.OrderBy(x => x.a).ToList();
			Node[] assigned;


			for (int i = 0; i < vs.Count; i++)
			{
				List<int[]> combinations = new List<int[]>();
				GenerateCombinations(Rk, vs[i].Ki, 0, new int[vs[i].Ki.Length], combinations);

				int portWaitTime = 0;


				
				if (i >= W)// 
				{
					portWaitTime = Math.Max(0, portAvailableTime.Min() - vs[i].a);
				}

				int assignedPortID = Array.IndexOf(portAvailableTime, portAvailableTime.Min());

				List<Label> bestSolutions = new List<Label>();
				SortedDictionary<int, Label> allBestSolutions = new SortedDictionary<int, Label>();

				for (int c = 0; c < combinations.Count; c++)//
				{
					assigned = new Node[vs[i].K];
					for (int k = 0; k < vs[i].K; k++)
					{
						Node n = new Node(allNodes[combinations[c][k]]);
						//n = allNodes[combinations[c][k]];
						assigned[k] = n;
						n.serviceTime = vs[i].pik[k];

						
					}

					SortedSet<Label> pq = new SortedSet<Label>(Comparer<Label>.Create((a, b) =>
					{
						int cmp = a.cost.CompareTo(b.cost);
						if (cmp == 0) cmp = a.t.CompareTo(b.t);
						if (cmp == 0) cmp = a.switchTime.CompareTo(b.switchTime);
						if (cmp == 0) cmp = a.id.CompareTo(b.id);
						return cmp; // 
					}));


					Label init = new Label(vs[i].K);
					init.cost = 0 + vs[i].h * portWaitTime;
					init.switchTime = 0;
					init.t = vs[i].a + portWaitTime;
					init.prev = null;
					pq.Add(init);

					labelsAtNodes.Clear();

					while (pq.Count > 0)
					{
						RemoveDominatedWithin(pq, vs[i]);
						Label current = null;
						current = pq.Min();

						if (current.finished == Math.Pow(2, assigned.Count()) - 1)
						{
							allBestSolutions.Add(c, current);
							break;
						}

						pq.Remove(current);
						List<Label> extlbs = Extend(current, assigned, vs[i]);
						for (int idx = 0; idx < extlbs.Count; idx++)
						{
							pq.Add(extlbs[idx]);
							
						}
					}
				}

				Label minCostLabel = new Label(vs[i].K);
				double minCost = double.MaxValue;
				int minBerth = 0;
				int minCombIdx = 0;

				foreach (var pair in allBestSolutions)
				{
					if (pair.Value.cost < minCost)
					{
						minCost = pair.Value.cost;
						minCostLabel = pair.Value;
						minCombIdx = pair.Key;//comb的index
					}
				}

				Console.WriteLine("最小的cost为：" + minCost);
				Console.WriteLine("最小的label的berth为：" + minBerth);
				for (int idx = 0; idx < vs[i].K; idx++)
				{
					Console.WriteLine("第 " + idx + " 个物品的分配的料条为 " + combinations[minCombIdx][idx]);
				}

				Label iterlb = new Label(vs[i].K);
				if (minCostLabel.prev != null)
				{
					iterlb = minCostLabel;
				}
				else
				{
					Console.WriteLine("见鬼了");
				}

				int finishTimeTemp = -1;
				int startTimeTemp = T;
				while (iterlb.prev != null)
				{
					if (iterlb.finished != 0 || iterlb.processingTime.Any(x => x > 0))
					{
						if (iterlb.t > finishTimeTemp)
						{
							vs[i].C = iterlb.t;
							vs[i].D = Math.Max(0, iterlb.t - vs[i].d);
							portAvailableTime[assignedPortID] = vs[i].C;
							vs[i].w = assignedPortID;
							vs[i].p[assignedPortID] = 1;
							finishTimeTemp = iterlb.t;
						}

						if (iterlb.s < startTimeTemp)
						{
							vs[i].S = iterlb.s;
							startTimeTemp = iterlb.s;
						}

						Console.Write("开始时间：" + iterlb.s + "  ");
						Console.Write("结束时间：" + iterlb.t + "  ");
						Console.Write("工作节点：" + iterlb.currentN + "  ");

						int k = vs[i].Ki[iterlb.currentN];
						int r = allNodes[combinations[minCombIdx][iterlb.currentN]].id;

						vs[i].z[k, r] = 1;


						for (int t = iterlb.s; t <= iterlb.t; t++)
						{
							vs[i].alpha[k, r, t] = 1;
						}

						for (int t = iterlb.s; t < iterlb.t; t++)
						{
							vs[i].x[k, r, t] = 1;
						}

						Console.WriteLine();

						for (int k1 = 0; k1 < K; k1++)
						{
							for (int r1 = 0; r1 < R; r1++)
							{
								for (int t1 = 0; t1 < T; t1++)
								{
									if (vs[i].x[k1, r1, t1] == 1)
									{
										Console.WriteLine(k1 + "_" + r1 + "_" + t1);
									}
								}
							}
						}

						foreach (var tw in allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow)
						{
							if (tw.start <= iterlb.s && tw.end >= iterlb.t)//!!!
							{
								int newFrontStart = -1;
								int newFrontEnd = -1;
								int newBackStart = -1;
								int newBackEnd = -1;
								if (tw.start - iterlb.s != 0)
								{
									newFrontStart = tw.start;
									newFrontEnd = iterlb.s - 1;
								}
								if (tw.end - iterlb.t != 0)
								{
									newBackStart = iterlb.t + 1;
									newBackEnd = tw.end;
								}

								allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow.Remove(tw);

								if (newFrontStart != -1)
								{
									allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow.Add((newFrontStart, newFrontEnd));
								}

								if (newBackStart != -1)
								{
									allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow.Add((newBackStart, newBackEnd));
								}

								allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow =
									allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow.OrderBy(x => x.start).ToList();
								break;//
							}

							if (tw.start <= iterlb.s && tw.end + 1 >= iterlb.t)//
							{
								int newFrontStart = -1;
								int newFrontEnd = -1;
								int newBackStart = -1;
								int newBackEnd = -1;
								if (tw.start - iterlb.s != 0)
								{
									newFrontStart = tw.start;
									newFrontEnd = iterlb.s - 1;
								}
								if (tw.end + 1 - iterlb.t == 0)
								{
									newBackStart = -1;
									newBackEnd = -1;
								}

								allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow.Remove(tw);

								if (newFrontStart != -1)
								{
									allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow.Add((newFrontStart, newFrontEnd));
								}

								if (newBackStart != -1)
								{
									allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow.Add((newBackStart, newBackEnd));
								}

								allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow =
									allNodes[combinations[minCombIdx][iterlb.currentN]].TimeWindow.OrderBy(x => x.start).ToList();
								break;//
							}

						}

						iterlb = iterlb.prev;
						Console.WriteLine();
					}
					else
					{
						break;
					}
				}

				for (int t = startTimeTemp; t <= finishTimeTemp; t++)
				{
					vs[i].beta[assignedPortID, t] = 1;
				}
				vs[i].cost = minCost;
			}
		}

		public static List<Label> Extend(Label current, Node[] assigned, Vessel v, List<inherit_info> branching = null)
		{
			double arriveTime;
			List<Label> extendLabel = new List<Label>();

			arriveTime = current.t + 1;

			for (int j = 0; j < assigned.Count(); j++)//
			{
				if (assigned[j].serviceTime != current.processingTime[j])//
				{
					foreach (var tw in assigned[j].TimeWindow)
					{

						if (tw.end >= arriveTime)//
						{

							
							Label ext;

							int serviceStartTime = (int)Math.Max(arriveTime, tw.start);
							int serviceEndTime = 0;

							ext = new Label(v.K);
							serviceEndTime = Math.Min(tw.end, serviceStartTime + assigned[j].serviceTime - current.processingTime[j] - 1);

							ext.t = serviceEndTime + 1;//
							if (ext.t > T - 1)
							{
								break;//
							}
							ext.cost = current.cost + (serviceEndTime + 1 - arriveTime + 1) * v.h;

						
							ext.cost -= v.g * Math.Max(current.t - v.d, 0);//
							ext.cost += v.g * Math.Max(ext.t - v.d, 0);

							ext.cost += assigned[j].switchCost;
							///

							ext.prev = current;
							for (int i = 0; i < assigned.Count(); i++)
							{
								ext.processingTime[i] = current.processingTime[i];
							}
							ext.processingTime[j] += serviceEndTime - serviceStartTime + 1;
							ext.switchTime = current.switchTime + 1;
							ext.s = serviceStartTime;
							ext.currentN = j;

							
							if (ext.processingTime[j] == assigned[j].serviceTime)
							{
								ext.finished = current.finished | (1 << j);
							}
							else
							{
								ext.finished = current.finished;
							}

							if (ext.switchTime <= v.K + interruptTimes)
							{
								if (!Prune(ext, v, branching))//
								{
									//
									List<Label> lbs = new List<Label>();
									labelsAtNodes.TryGetValue(assigned[j].id, out lbs);//
									if (IsDominated(ext, lbs, v))
									{
										continue;
									}
									if (lbs != null)//
									{
										RemoveDominated(ext, lbs, v);//
										lbs.Add(ext);
										labelsAtNodes[assigned[j].id] = lbs;
									}
									else
									{
										labelsAtNodes.Add(assigned[j].id, new List<Label>() { ext });
									}

									extendLabel.Add(ext);
								}
								else
								{
									break;//
								}


							}

						}
					}
				}

			}

			return extendLabel;
		}

		public static double SumFromAToB(double[] array, int A, int B)
		{
			if (A > B || A < 0 || B >= array.Length)
			{
				throw new ArgumentException("Invalid index range");
			}

			double sum = 0;
			for (int i = A; i <= B; i++)
			{
				sum += array[i];
			}
			return sum;
		}

		public static List<Label> ExtendSP(Label current, Node[] assigned, Vessel v, int berthID, List<inherit_info> branching = null)
		{
			double arriveTime;
			List<Label> extendLabel = new List<Label>();

			arriveTime = current.t + 1;

			for (int j = 0; j < assigned.Count(); j++)//
			{
				if (assigned[j].serviceTime != current.processingTime[j])//
				{
					foreach (var tw in assigned[j].TimeWindow)
					{

						if (tw.end >= arriveTime)//
						{

						
							Label ext;

							int serviceStartTime = (int)Math.Max(arriveTime, tw.start);
							int serviceEndTime = 0;

							ext = new Label(v.K);
							serviceEndTime = Math.Min(tw.end, serviceStartTime + assigned[j].serviceTime - current.processingTime[j] - 1);

							ext.t = serviceEndTime + 1;
							if (ext.t > T - 1)
							{
								break;//
							}

							ext.cost = current.cost + (serviceEndTime + 1 - arriveTime + 1) * v.h;

							//ext.cost += (serviceEndTime + 1 - (current.processingTime.Any(x => x > 0)?arriveTime:a) + 1) * h;
							ext.cost -= v.g * Math.Max(current.t - v.d, 0);//
							ext.cost += v.g * Math.Max(ext.t - v.d, 0);

							ext.cost += assigned[j].switchCost;
							ext.s = serviceStartTime;
							ext.prev = current;
							ext.cost -= SumFromAToB(ExtractRow(alphadual, assigned[j].id), serviceStartTime, serviceEndTime + 1);//

							int initalStartT = ext.s;

							Label temp = ext.prev;

							while (temp != null && temp.processingTime.Sum() != 0)//
							{
								initalStartT = temp.s;

								temp = temp.prev;
							}

							if (ext.prev.processingTime.Sum() != 0)//
							{
								ext.cost += SumFromAToB(ExtractRow(wdual, berthID), initalStartT, current.t);
								ext.cost -= SumFromAToB(ExtractRow(wdual, berthID), initalStartT, serviceEndTime + 1);
							}
							else
							{
								ext.cost -= SumFromAToB(ExtractRow(wdual, berthID), serviceStartTime, serviceEndTime + 1);
							}

							for (int i = 0; i < assigned.Count(); i++)
							{
								ext.processingTime[i] = current.processingTime[i];
							}

							ext.processingTime[j] += serviceEndTime - serviceStartTime + 1;
							ext.switchTime = current.switchTime + 1;

							ext.currentN = j;

							
							if (ext.processingTime[j] == assigned[j].serviceTime)
							{
								ext.finished = current.finished | (1 << j);
							}
							else
							{
								ext.finished = current.finished;
							}

							if (ext.switchTime <= v.K + interruptTimes)
							{
								if (!PruneSP(ext, v, berthID, assigned, branching))//
								{
									//
									List<Label> lbs = new List<Label>();
									labelsAtNodes.TryGetValue(assigned[j].id, out lbs);//
									if (IsDominated(ext, lbs, v))
									{
										continue;
									}
									if (lbs != null)//
									{
										RemoveDominated(ext, lbs, v);//
										lbs.Add(ext);
										labelsAtNodes[assigned[j].id] = lbs;
									}
									else
									{
										labelsAtNodes.Add(assigned[j].id, new List<Label>() { ext });
									}

									extendLabel.Add(ext);
								}
								else
								{
									break;//
								}
							}

						}
					}
				}

			}

			return extendLabel;
		}

		public static void RemoveDominated(Label newLabel, List<Label> labelsList, Vessel v)
		{
			if (labelsList == null)
			{
				return;
			}
			var dominated = new List<Label>();
			foreach (var lbl in labelsList)
			{
				if (Dominates(newLabel, lbl, v))
				{
					dominated.Add(lbl);
				}
			}
			foreach (var d in dominated)
			{
				labelsList.Remove(d);
			}
		}

		private static bool IsDominated(Label newLabel, List<Label> labelsList, Vessel v)
		{
			if (labelsList == null)
			{
				return false;
			}
			int count = 0;

			foreach (var lbl in labelsList)
			{
				count++;
				if (Dominates(lbl, newLabel, v))
				{
					
					return true;
				}
			}
			return false;
		}

		public static bool Dominates(Label A, Label B, Vessel v)
		{

			if (A.t == B.t && A.cost == B.cost && A.switchTime == B.switchTime)
			{
				if (A.processingTime.SequenceEqual(B.processingTime))
				{
					return true;
				}
			}

			int count = 0;
			int ccount = 0;

			for (int i = 0; i < A.processingTime.Length; i++)
			{
				if (A.processingTime[i] >= B.processingTime[i])
				{
					count++;
					if (A.processingTime[i] > B.processingTime[i])
					{
						ccount++;
					}
				}
			}

			if (count == A.processingTime.Count())//
			{
				int diffSum = 0;
				List<int> unfinishedR = new List<int>();//
				List<int> unfinishedTimeSlot = new List<int>();//

				int diffPosCount = 0;
				for (int i = 0; i < A.processingTime.Length; i++)
				{
					if (A.processingTime[i] - B.processingTime[i] != 0)
					{
						diffPosCount++;
					}
				}

				for (int i = 0; i < A.processingTime.Length; i++)
				{
					int diff = A.processingTime[i] - B.processingTime[i];

					if (A.processingTime[i] - B.processingTime[i] != 0)
					{
						unfinishedR.Add(i);
						unfinishedTimeSlot.Add(diff);
					}

					diffSum += diff;
				}

				diffSum += diffPosCount - 1;
				diffSum = Math.Max(0, diffSum);

				if (B.t + diffSum >= A.t)
				{
					 
					int switchCost = 0;
					for (int i = 0; i < unfinishedR.Count; i++)
					{
						switchCost += v.switchCost[unfinishedR[i]];
					}

					for (int i = 0; i < unfinishedR.Count; i++)
					{
						if (B.t + 1 > T - 1)
						{
							return true;
						}
					}

					double costLB = diffSum * v.h + v.g * (Math.Max(B.t + diffSum - v.d, 0)) - v.g * (Math.Max(B.t - v.d, 0)) + switchCost;

					if (B.cost + costLB > A.cost)
					{
						return true;
					}
				}
			}

			
			for (int i = 0; i < A.processingTime.Length; i++)
			{
				if (A.processingTime[i] <= B.processingTime[i])
					return false;
			}

			
			if (A.cost >= B.cost)
				return false;

			
			if (A.t >= B.t)
				return false;

			
			return true;
		}

		public static bool Prune(Label lb, Vessel v, List<inherit_info> branching = null)//
		{
			int currentT = lb.t;
			int uncompleteT = 0;
			int currentInterruptedTimes = lb.switchTime;
			int exceptedInterruptedTimes = 0;
			double LB = lb.cost;

			for (int i = 0; i < v.K; i++)
			{
				if (lb.processingTime[i] != v.pik[i])
				{
					int tmp = v.pik[i] - lb.processingTime[i] + 1;
					uncompleteT += v.pik[i] - lb.processingTime[i];
					uncompleteT++;
					exceptedInterruptedTimes++;
				}
			}

			if (currentT + uncompleteT - 1 > T - 1 || currentInterruptedTimes + exceptedInterruptedTimes - 1 > v.K + interruptTimes /*|| LB > 0*/)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static double FindMaxConsecutiveSumSubarrayInRange(double[] arr, int k, int a, int b)
		{
			int n = b + 1; 
			if (k > n - a || k > b - a + 1) return double.MaxValue; 

			double maxSum = double.MinValue;
			double currentSum = 0;
			int start = a; 
			double[] result = new double[k];

			
			for (int i = a; i < a + k; i++)
			{
				currentSum += arr[i];
			}

			
			for (int i = a + k; i <= b; i++)
			{
				currentSum += arr[i] - arr[i - k]; 

				
				if (currentSum > maxSum)
				{
					maxSum = currentSum;
					start = i - k + 1;
				}
			}


			return maxSum;
		}


		public static bool PruneSP(Label lb, Vessel v, int BerthID, Node[] assignedR, List<inherit_info> branching)//
		{
			if (branching != null)
			{
				foreach (var branchInfo in branching)
				{
					if (branchInfo.Xposition[0] == v.id)//
					{
						if (branchInfo.Xposition.Length == 4)
						{
							if (branchInfo.Indicator == 1) //
							{
								if (branchInfo.Xposition[1] == v.Ki[lb.currentN])//
								{
									if (branchInfo.Xposition[2] != assignedR[lb.currentN].id)//
									{
										return true;
									}
									else
									{
										if (lb.s <= branchInfo.Xposition[3] && branchInfo.Xposition[3] <= lb.t)
										{

										}
										else
										{
											return true;
										}
									}
								}

								else//
								{
									if (lb.s <= branchInfo.Xposition[3] && branchInfo.Xposition[3] <= lb.t)
									{
										return true;
									}
								}
							}

							else //
							{
								if (branchInfo.Xposition[1] == v.Ki[lb.currentN])//
								{
									if (branchInfo.Xposition[2] == assignedR[lb.currentN].id)//
									{
										if (lb.s <= branchInfo.Xposition[3] && branchInfo.Xposition[3] <= lb.t)
										{
											return true;
										}
									}
								}
							}
						}
						if (branchInfo.Xposition.Length == 3)
						{
							if (branchInfo.Indicator == 1) //
							{
								if (branchInfo.Xposition[1] != BerthID)//
								{
									return true;
								}
							}

							else //
							{
								if (branchInfo.Xposition[1] == BerthID)
								{
									return true;
								}
							}

						}
					}

					else//
					{
						if (branchInfo.Xposition.Length == 4)
						{
							if (branchInfo.Xposition[2] == assignedR[lb.currentN].id)//
							{
								if ((lb.s <= branchInfo.Xposition[3] && branchInfo.Xposition[3] <= lb.t))
								{
									return true;
								}
							}
						}
					}
				}
			}

			int currentT = lb.t;
			int uncompleteT = 0;
			int currentInterruptedTimes = lb.switchTime;
			int exceptedInterruptedTimes = 0;
			double LB = lb.cost;

			for (int i = 0; i < v.K; i++)
			{
				double[] tau = ExtractRow(alphadual, assignedR[i].id);
				double[] gamma = ExtractRow(wdual, BerthID);

				if (lb.processingTime[i] != v.pik[i])
				{
					int tmp = v.pik[i] - lb.processingTime[i] + 1;
					uncompleteT += v.pik[i] - lb.processingTime[i];
					uncompleteT++;//
					exceptedInterruptedTimes++;
					LB += tmp * v.h + v.g * (Math.Max(lb.t + uncompleteT - v.d, 0)) - v.g * (Math.Max(lb.t - v.d, 0)) + assignedR[i].switchCost -
						(SumOfKLargestInRange(tau, lb.t, T - 1, tmp) - FindMaxConsecutiveSumSubarrayInRange(gamma, lb.t, T - 1, uncompleteT - tmp) + FindMaxConsecutiveSumSubarrayInRange(gamma, lb.t, T - 1, uncompleteT));
					//
				}
			}

			if (currentT + uncompleteT - 1 > T - 1 || currentInterruptedTimes + exceptedInterruptedTimes - 1 > v.K + interruptTimes || LB - pi[v.id - 1] >= 0)//不能完成的情况
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		static double[,] alphadual = new double[R, T];
		static double[,] wdual = new double[W, T];
		static double[] pi = new double[N];
		static double[] TestDual = new double[W - 1];
		static double UB = Double.MaxValue;

		static double rmpobj;
		static double LB = 0;

		static double MPcount = 0;

		

		static (List<Vessel>, double) MPsolver(List<Vessel> v)
		{
			List<Vessel> MPres = new List<Vessel>();
			Cplex MPsolver = new Cplex();
			ILinearNumExpr MpObj = MPsolver.LinearNumExpr();
			INumVar[] lambda = MPsolver.NumVarArray(v.Count, 0, int.MaxValue);

			for (int i = 0; i < v.Count(); i++)
			{
				MpObj.AddTerm(v[i].cost, lambda[i]);
			}
			MPsolver.AddMinimize(MpObj);
			IRange[,] rng1 = new IRange[R, T];
			for (int t = 0; t < T; t++)
			{
				for (int r = 0; r < R; r++)
				{
					ILinearNumExpr str19 = MPsolver.LinearNumExpr();
					for (int i = 0; i < v.Count; i++)
					{
						foreach (int k in v[i].Ki)
						{
							str19.AddTerm(lambda[i], v[i].alpha[k, r, t]);
						}

					}
					rng1[r, t] = MPsolver.AddLe(str19, 1, "alpha " + t + "_" + r);
				}

			}

			IRange[,] rng2 = new IRange[W, T];
			for (int t = 0; t < T; t++)
			{
				for (int b = 0; b < W; b++)
				{
					ILinearNumExpr str20 = MPsolver.LinearNumExpr();
					for (int i = 0; i < v.Count; i++)
					{
						str20.AddTerm(lambda[i], v[i].beta[b, t]);
					}

					rng2[b, t] = MPsolver.AddLe(str20, 1, "beta " + b + "_" + t);
				}
			}
			IRange[] rng3 = new IRange[N];
			for (int i = 0; i < N; i++)
			{
				ILinearNumExpr str21 = MPsolver.LinearNumExpr();
				for (int j = 0; j < v.Count; j++)
				{
					if (i == v[j].id - 1)
					{
						str21.AddTerm(lambda[j], 1);
					}
				}
				rng3[i] = MPsolver.AddEq(str21, 1);
			}

			IRange[] rng4 = new IRange[W];

			for (int b = 1; b < W; b++)
			{
				ILinearNumExpr str22 = MPsolver.LinearNumExpr();
				for (int i = 0; i < v.Count; i++)
				{
					str22.AddTerm(lambda[i], v[i].p[b]);
				}
				ILinearNumExpr str23 = MPsolver.LinearNumExpr();
				for (int i = 0; i < v.Count; i++)
				{
					str23.AddTerm(lambda[i], v[i].p[b - 1]);
				}
				rng4[b] = MPsolver.AddLe(MPsolver.Diff(str22, str23), 0);
			}

			MPsolver.ExportModel(@"C:\exportmodel\MP2.lp");

			if (MPsolver.Solve())
			{
				rmpobj = MPsolver.GetObjValue();

				for (int i = 0; i < v.Count; i++)
				{
					if (MPsolver.GetValue(lambda[i]) > 0)
					{
						v[i].lambda = MPsolver.GetValue(lambda[i]);
						MPres.Add(v[i]);
					}
				}

				MPres.Sort(new VesselComparer(VesselComparer.CompareType.id));


				if (MPres.Count == N)
				{
					if (rmpobj < UB)
					{
						UB = rmpobj;
						feasibleSol.Clear();
						feasibleSol.AddRange(MPres.Select(x => new Vessel(x)).ToList());
					}

				}
				for (int r = 0; r < R; r++)
				{
					for (int t = 0; t < T; t++)
					{
						alphadual[r, t] = MPsolver.GetDual(rng1[r, t]);
					}
				}

				for (int t = 0; t < T; t++)
				{
					for (int b = 0; b < W; b++)
					{
						wdual[b, t] = MPsolver.GetDual(rng2[b, t]);
					}
				}
				for (int i = 0; i < N; i++)
				{
					pi[i] = MPsolver.GetDual(rng3[i]);
				}
				for (int b = 1; b < W; b++)
				{
					TestDual[b - 1] = MPsolver.GetDual(rng4[b]);
				}

				MPsolver.End();
			}

			else
			{
				Console.WriteLine("mp has no solutions");
				return (null, int.MaxValue);
				throw new System.Exception("mp has no solutions");
			}

			MPcount++;
			return (MPres, rmpobj);
		}

		

		static double[] ExtractRow(double[,] array, int rowIndex)
		{
			int cols = array.GetLength(1);  
			double[] rowArray = new double[cols];  
			for (int j = 0; j < cols; j++)
			{
				rowArray[j] = array[rowIndex, j];  
			}
			return rowArray;  
		}


		public static double SumOfKLargestInRange(double[] arr, int start, int end, int k)
		{
			
			if (arr == null
				|| arr.Length == 0
				|| start < 0
				|| end >= arr.Length
				|| start > end
				|| k <= 0)
			{
				throw new ArgumentException("输入参数不合法。");
			}

			int length = end - start + 1;
			if (k > length)
			{
				return double.MinValue;
			}

			
			double[] subArray = new double[length];
			for (int i = 0; i < length; i++)
			{
				subArray[i] = arr[start + i];
			}

			
			Array.Sort(subArray);

			
			double sum = 0;
			for (int i = length - k; i < length; i++)
			{
				sum += subArray[i];
			}

			return sum;
		}


		public static List<Label> ExtendSPExact(Label current, Node[] assigned, Vessel v, int berthID, List<inherit_info> branching = null)
		{
			double arriveTime;
			List<Label> extendLabel = new List<Label>();




			arriveTime = current.t + 1;

			if (current.currentN < assigned.Count())//
			{
				for (int j = 0; j < assigned.Count() + dummyNodes.Count(); j++)
				{
					if (j < assigned.Count())
					{
						if (assigned[j].serviceTime != current.processingTime[j])//
						{
							foreach (var tw in assigned[j].TimeWindow)
							{
								if (tw.end >= arriveTime)//
								{
									
									Label ext;

									int serviceStartTime = (int)Math.Max(arriveTime, tw.start);
									int serviceEndTime = 0;

									for (int t = serviceStartTime; t <= Math.Min(tw.end, serviceStartTime + assigned[j].serviceTime - current.processingTime[j] - 1); t++)
									{
										ext = new Label(v.K);
										serviceEndTime = t;

										ext.t = serviceEndTime + 1;//

										if (ext.t > T - 1)
										{
											break;//
										}

										ext.cost = current.cost + (serviceEndTime + 1 - arriveTime + 1) * v.h;

										ext.cost -= v.g * Math.Max(current.t - v.d, 0);//
										ext.cost += v.g * Math.Max(ext.t - v.d, 0);

										ext.cost += assigned[j].switchCost;
										ext.s = serviceStartTime;
										ext.prev = current;

										ext.cost -= SumFromAToB(ExtractRow(alphadual, assigned[j].id), serviceStartTime, serviceEndTime + 1);//

										int initalStartT = ext.s;

										Label temp = ext.prev;

										while (temp != null && temp.processingTime.Sum() != 0)
										{
											initalStartT = temp.s;
											temp = temp.prev;
										}

										if (ext.prev.processingTime.Sum() != 0)//
										{
											ext.cost += SumFromAToB(ExtractRow(wdual, berthID), initalStartT, current.t);
											ext.cost -= SumFromAToB(ExtractRow(wdual, berthID), initalStartT, serviceEndTime + 1);
										}

										else
										{
											ext.cost -= SumFromAToB(ExtractRow(wdual, berthID), serviceStartTime, serviceEndTime + 1);
										}

										for (int i = 0; i < assigned.Count(); i++)
										{
											ext.processingTime[i] = current.processingTime[i];
										}
										ext.processingTime[j] += serviceEndTime - serviceStartTime + 1;
										ext.switchTime = current.switchTime + 1;

										ext.currentN = j;
										ext.delta = current.delta;
										//int newVisitedSet;
										if (ext.processingTime[j] == assigned[j].serviceTime)
										{
											ext.finished = current.finished | (1 << j);
										}
										else
										{
											ext.finished = current.finished;
										}


										if (ext.switchTime <= v.K + interruptTimes)
										{
											if (!PruneExact(ext, v, berthID, assigned, branching))//
											{
												//
												List<Label> lbs = new List<Label>();
												labelsAtNodes.TryGetValue(assigned[j].id, out lbs);//
												if (IsDominatedExact(ext, lbs, v))
												{
													continue;
												}
												if (lbs != null)//
												{
													RemoveDominatedExact(ext, lbs, v);//
													lbs.Add(ext);
													labelsAtNodes[assigned[j].id] = lbs;
												}
												else
												{
													labelsAtNodes.Add(assigned[j].id, new List<Label>() { ext });
												}
												extendLabel.Add(ext);
											}
											else
											{
												break;//
											}
										}

									}

								}
							}
						}
					}
					else//
					{
						if (current.processingTime.All(x => x == 0))
						{
							continue;
						}
						Label ext = new Label(v.K);

						int serviceStartTime = (int)arriveTime;
						int serviceEndTime = (int)arriveTime + dummyNodes[j - assigned.Count()].waitTime - 1;
						ext.t = serviceEndTime;//

						ext.cost += current.cost + (serviceEndTime - arriveTime + 1) * v.h;

						ext.cost -= v.g * Math.Max(current.t - v.d, 0);//
						ext.cost += v.g * Math.Max(ext.t - v.d, 0);


						ext.prev = current;
						ext.s = serviceStartTime;

						int initalStartT = ext.s;

						Label temp = ext.prev;

						while (temp != null && temp.processingTime.Sum() != 0)//
						{
							initalStartT = temp.s;

							temp = temp.prev;
						}

						if (ext.prev.processingTime.Sum() != 0)//
						{
							ext.cost += SumFromAToB(ExtractRow(wdual, berthID), initalStartT, current.t);
							ext.cost -= SumFromAToB(ExtractRow(wdual, berthID), initalStartT, serviceEndTime + 1);
						}
						else
						{
							ext.cost -= SumFromAToB(ExtractRow(wdual, berthID), serviceStartTime, serviceEndTime + 1);
						}

						for (int i = 0; i < assigned.Count(); i++)
						{
							ext.processingTime[i] = current.processingTime[i];
						}


						ext.currentN = j;
						ext.delta = current.delta;

						ext.finished = current.finished;

						if (ext.switchTime <= v.K + interruptTimes)
						{
							if (!PruneExact(ext, v, berthID, assigned, branching))//
							{
								extendLabel.Add(ext);
							}
							else
							{
								break;//
							}
						}

					}
				}


			}

			else
			{
				for (int j = 0; j < assigned.Count(); j++)
				{
					if (j < assigned.Count())
					{
						if (assigned[j].serviceTime != current.processingTime[j])//
						{
							foreach (var tw in assigned[j].TimeWindow)
							{
								if (tw.end >= arriveTime)//
								{

									Label ext;

									int serviceStartTime = (int)Math.Max(arriveTime, tw.start);
									int serviceEndTime = 0;

									for (int t = serviceStartTime; t <= Math.Min(tw.end, serviceStartTime + assigned[j].serviceTime - current.processingTime[j] - 1); t++)
									{
										ext = new Label(v.K);
										serviceEndTime = t;

										ext.t = serviceEndTime + 1;//
										if (ext.t > T - 1)
										{
											break;//
										}

										ext.cost = current.cost + (serviceEndTime + 1 - arriveTime + 1) * v.h;
										//ext.cost += (serviceEndTime + 1 - (current.processingTime.Any(x => x > 0)?arriveTime:a) + 1) * h;
										ext.cost -= v.g * Math.Max(current.t - v.d, 0);//
										ext.cost += v.g * Math.Max(ext.t - v.d, 0);

										ext.cost += assigned[j].switchCost;
										///
										ext.s = serviceStartTime;
										ext.prev = current;
										ext.cost -= SumFromAToB(ExtractRow(alphadual, assigned[j].id), serviceStartTime, serviceEndTime + 1);//

										int initalStartT = ext.s;

										Label temp = ext.prev;

										while (temp != null && temp.processingTime.Sum() != 0)//
										{
											initalStartT = temp.s;

											temp = temp.prev;
										}

										if (ext.prev.processingTime.Sum() != 0)//
										{
											ext.cost += SumFromAToB(ExtractRow(wdual, berthID), initalStartT, current.t);
											ext.cost -= SumFromAToB(ExtractRow(wdual, berthID), initalStartT, serviceEndTime + 1);
										}
										else
										{
											ext.cost -= SumFromAToB(ExtractRow(wdual, berthID), serviceStartTime, serviceEndTime + 1);
										}

										for (int i = 0; i < assigned.Count(); i++)
										{
											ext.processingTime[i] = current.processingTime[i];
										}
										ext.processingTime[j] += serviceEndTime - serviceStartTime + 1;
										ext.switchTime = current.switchTime + 1;

										ext.currentN = j;
										ext.delta = current.delta;
										//int newVisitedSet;
										if (ext.processingTime[j] == assigned[j].serviceTime)
										{
											ext.finished = current.finished | (1 << j);
										}
										else
										{
											ext.finished = current.finished;
										}


										if (ext.switchTime <= v.K + interruptTimes)
										{
											if (!PruneExact(ext, v, berthID, assigned, branching))//
											{
												//
												List<Label> lbs = new List<Label>();
												labelsAtNodes.TryGetValue(assigned[j].id, out lbs);
												if (IsDominatedExact(ext, lbs, v))
												{
													continue;
												}
												if (lbs != null)
												{
													RemoveDominatedExact(ext, lbs, v);
													lbs.Add(ext);
													labelsAtNodes[assigned[j].id] = lbs;
												}
												else
												{
													labelsAtNodes.Add(assigned[j].id, new List<Label>() { ext });
												}
												extendLabel.Add(ext);
											}
											else
											{
												break;
											}
										}

									}

								}
							}
						}
					}

				}
			}

			return extendLabel;
		}

		public static void RemoveDominatedExact(Label newLabel, List<Label> labelsList, Vessel v)
		{
			if (labelsList == null)
			{
				return;
			}
			var dominated = new List<Label>();
			foreach (var lbl in labelsList)
			{
				if (DominatesExact(newLabel, lbl, v))
				{
					dominated.Add(lbl);
				}
			}
			foreach (var d in dominated)
			{
				labelsList.Remove(d);
			}
		}

		private static bool DominatesExact(Label A, Label B, Vessel v)
		{
			int count = 0;
			int ccount = 0;

			Label current = B;

			if (A.t == B.t && A.currentN == B.currentN)
			{
				for (int i = 0; i < A.processingTime.Length; i++)
				{
					if (A.processingTime[i] <= B.processingTime[i])
						return false;
				}

				
				if (A.cost >= B.cost)
					return false;

				
				if (A.t >= B.t)
					return false;

				
				return true;
			}

			return false;

		}

		public static bool IsDominatedExact(Label newLabel, List<Label>? labelsList, Vessel v)
		{
			if (labelsList == null)
			{
				return false;
			}
			int count = 0;

			foreach (var lbl in labelsList)
			{
				count++;
				if (DominatesExact(lbl, newLabel, v))
				{
					return true;
				}
			}
			return false;
		}

		public static bool PruneExact(Label lb, Vessel v, int BerthID, Node[] assignedR, List<inherit_info> branching)
		{

			if (branching != null)
			{
				foreach (var branchInfo in branching)
				{
					if (branchInfo.Xposition[0] == v.id)
					{
						if (branchInfo.Indicator == 1) 
						{
							if (branchInfo.Xposition[1] == v.Ki[lb.currentN])
							{
								if (branchInfo.Xposition[2] != assignedR[lb.currentN].id)
								{
									return true;
								}
								else
								{
									if (lb.s <= branchInfo.Xposition[3] && branchInfo.Xposition[3] <= lb.t)
									{

									}
									else
									{
										return true;
									}
								}
							}

							else
							{
								if (lb.s <= branchInfo.Xposition[3] && branchInfo.Xposition[3] <= lb.t)
								{
									return true;
								}
							}
						}

						else //为0分支
						{
							if (branchInfo.Xposition[1] == v.Ki[lb.currentN])
							{
								if (branchInfo.Xposition[2] == assignedR[lb.currentN].id)
								{
									if (lb.s <= branchInfo.Xposition[3] && branchInfo.Xposition[3] <= lb.t)
									{
										return true;
									}
								}
							}
						}
					}

					else
					{
						if (branchInfo.Xposition[2] == assignedR[lb.currentN].id)
						{
							if (lb.s == branchInfo.Xposition[3] + 1 || (lb.s <= branchInfo.Xposition[3] && branchInfo.Xposition[3] <= lb.t))
							{
								return true;
							}
						}
					}
				}
			}
			int currentT = lb.t;
			int uncompleteT = 0;
			int currentInterruptedTimes = lb.switchTime;
			int exceptedInterruptedTimes = 0;
			double LB = lb.cost;

			if (lb.prev != null && lb.prev.processingTime.Any(x => x != 0))
			{
				if (lb.currentN == lb.prev.currentN)
				{
					return true;
				}
			}

			for (int i = 0; i < v.K; i++)
			{
				if (lb.processingTime[i] != assignedR[i].serviceTime)
				{
					int tmp = assignedR[i].serviceTime - lb.processingTime[i] + 1;
					uncompleteT += assignedR[i].serviceTime - lb.processingTime[i];
					uncompleteT++;
					exceptedInterruptedTimes++;

				}
			}

			if (currentT + uncompleteT - 1 > T - 1 || currentInterruptedTimes + exceptedInterruptedTimes - 1 > v.K + interruptTimes)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static T SumSubArray<T>(T[] array, int startIndex, int endIndex) where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable
		{
			dynamic sum = 0; // 使用dynamic以支持不同的数值类型
			for (int i = startIndex; i <= endIndex; i++)
			{
				sum += array[i];
			}
			return sum;
		}

		public static (Vessel pattern, double cost) SubproblemLabelSetting(Vessel v1, List<inherit_info> addconstraints, bool partialEnumeration)//最后一个参数是 是否部分枚举
		{
			Vessel v = new Vessel(v1);
			v.lambda = -1;

			//tempVessel.e = new int[R, T];
			v.x = new double[K, R, T];
			v.alpha = new double[K, R, T];
			v.z = new double[K, R];
			v.beta = new double[W, T];

			v.C = -1;
			v.S = -1;
			v.D = 0;

			List<Label> feasibleSolution = new List<Label>();
			List<Label> bestSolutions = new List<Label>();
			SortedDictionary<(int berth, int combIdx), List<Label>> allBestSolutions = new SortedDictionary<(int berth, int combIdx), List<Label>>();

			List<int[]> combinations = new List<int[]>();

			GenerateCombinations(Rk, v.Ki, 0, new int[v.Ki.Length], combinations);
			Node[] assigned;

			conflictInfo = ReadConflictData(conflictDataPath);
			InitialNodes();

			Label minCostLabel = new Label(v.K);
			double minCost = double.MaxValue;
			int minBerth = 0;
			int minCombIdx = 0;

			for (int k = 0; k < W; k++)
			{
				double[] gamma = ExtractRow(wdual, k);

				for (int i = 0; i < combinations.Count; i++)
				{
					assigned = new Node[v.K];
					for (int j = 0; j < v.K; j++)
					{
						Node n = new Node(allNodes[combinations[i][j]]);
						assigned[j] = n;
						n.serviceTime = v.pik[j];
					}

					SortedSet<Label> pq = new SortedSet<Label>(Comparer<Label>.Create((a, b) =>
					{
						int cmp = a.cost.CompareTo(b.cost);
						if (cmp == 0) cmp = a.t.CompareTo(b.t);
						if (cmp == 0) cmp = a.switchTime.CompareTo(b.switchTime);
						if (cmp == 0) cmp = a.id.CompareTo(b.id);
						return cmp; 
					}));

					Label init = new Label(v.K);

					if (k == 0)
					{
						init.cost = TestDual[k];
					}
					else if (k == W - 1)
					{
						init.cost = -TestDual[k - 1];
					}
					else
					{
						init.cost = -TestDual[k - 1] + TestDual[k];
					}

					//init.cost = 0;
					init.switchTime = 0;
					init.t = v.a;
					init.prev = null;
					init.delta = 0;

					labelsAtNodes.Clear();

					for (int delta = 0; delta < T; delta++)
					{
						Label dummyLabel = new Label(v.K);
						dummyLabel.t = init.t + delta;
						dummyLabel.cost = init.cost + v.h * delta + v.g * (Math.Max(delta - v.d, 0));
						dummyLabel.prev = init;
						dummyLabel.switchTime = 0;
						dummyLabel.delta = init.delta + delta;
						pq.Add(dummyLabel);

						while (pq.Count > 0)
						{
							
							RemoveDominatedWithin(pq, v);

							Label current = null;
							
							current = pq.Min;

							if (current.finished == Math.Pow(2, assigned.Count()) - 1)
							{
								
								if (current.delta == delta)
								{
									//bestSolutions.Add(current);
									if (!allBestSolutions.ContainsKey((k, i)))
									{
										allBestSolutions.Add((k, i), new List<Label>() { current });
									}
									else
									{
										allBestSolutions[(k, i)].Add(current);
									}

								}

								break;
							}

							pq.Remove(current);

							List<Label> extlbs = ExtendSP(current, assigned, v, k, addconstraints);

							for (int idx = 0; idx < extlbs.Count; idx++)
							{
								pq.Add(extlbs[idx]);
								if (extlbs[idx].finished == Math.Pow(2, assigned.Count()) - 1)
								{
									feasibleSolution.Add(extlbs[idx]);

									if (partialEnumeration)
									{
										if (extlbs[idx].cost - pi[v.id] < 0)
										{
											minCost = extlbs[idx].cost;
											minCostLabel = extlbs[idx];
											minBerth = k;
											minCombIdx = i;
										}
									}

								}
							}

						}
					}

				}
			}

			if (!partialEnumeration)
			{
				foreach (var pair in allBestSolutions)
				{
					for (int i = 0; i < pair.Value.Count; i++)
					{
						if (pair.Value[i].cost < minCost)
						{
							minCost = pair.Value[i].cost;
							minCostLabel = pair.Value[i];
							minBerth = pair.Key.berth;
							minCombIdx = pair.Key.combIdx;
						}
					}

				}

			}

			Console.WriteLine("最小的cost为：" + minCost);
			Console.WriteLine("最小的label的berth为：" + minBerth);
			for (int i = 0; i < v.K; i++)
			{
				Console.WriteLine("第 " + i + " 个物品的分配的料条为 " + combinations[minCombIdx][i]);
			}

			Label iterlb = new Label(v.K);

			if (minCostLabel.prev != null)
			{
				iterlb = minCostLabel;
			}

			else
			{
				Console.WriteLine("见鬼了");
			}

			int finishTimeTemp = -1;
			int startTimeTemp = T;


			if (minCost - pi[v.id - 1] < 0)
			{

				while (iterlb.prev != null)
				{
					if (iterlb.finished != 0 || iterlb.processingTime.Any(x => x > 0))
					{
						if (iterlb.t > finishTimeTemp)
						{
							v.C = iterlb.t;
							v.D = Math.Max(0, iterlb.t - v.d);
							v.w = minBerth;
							finishTimeTemp = iterlb.t;
						}

						if (iterlb.s < startTimeTemp)
						{
							v.S = iterlb.s;
							startTimeTemp = iterlb.s;
						}

						int k = v.Ki[iterlb.currentN];
						int r = allNodes[combinations[minCombIdx][iterlb.currentN]].id;

						v.z[k, r] = 1;

						for (int t = iterlb.s; t <= iterlb.t; t++)
						{
							v.alpha[k, r, t] = 1;
						}

						for (int t = iterlb.s; t < iterlb.t; t++)
						{
							v.x[k, r, t] = 1;
						}


						//========================debug 使用======================//
						Console.WriteLine("出发时间延后为：" + iterlb.delta);
						Console.Write("开始时间：" + iterlb.s + "  ");
						Console.Write("结束时间：" + iterlb.t + "  ");
						Console.Write("工作节点：" + iterlb.currentN + "  ");
						//========================================================//

						iterlb = iterlb.prev;
						Console.WriteLine();
					}
					else
					{
						v.p[minBerth] = 1;
						for (int t = startTimeTemp; t <= finishTimeTemp; t++)
						{
							v.beta[minBerth, t] = 1;
						}

						v.cost = CalCostVessel(v);

						return (v, minCost - pi[v.id - 1]);
						//break;
					}
				}
			}

			else//若reduced cost大于等于0
			{
				return (null, int.MaxValue);
			}

			return (null, int.MaxValue);
		}

		public static double CalCostVessel(Vessel v)
		{
			double cost = 0;

			cost += v.h * (v.C - v.a) + v.g * v.D;

			for (int k = 0; k < K; k++)
			{
				for (int r = 0; r < R; r++)
				{
					for (int t = 0; t < T; t++)
					{
						cost += (v.alpha[k, r, t] - v.x[k, r, t]) * allNodes[r].switchCost;
					}
				}
			}

			return cost;
		}

		public static (Vessel resSP, double rdCost) SubproblemCplex(Vessel v, List<inherit_info> addconstraints = null)
		{
			Vessel tempVessel = new Vessel(v);

			tempVessel.cost = 0;
			tempVessel.lambda = -1;


			//tempVessel.e = new int[R, T];
			tempVessel.x = new double[K, R, T];
			tempVessel.alpha = new double[K, R, T];
			tempVessel.z = new double[K, R];
			tempVessel.beta = new double[W, T];
			tempVessel.p = new double[W];
			for (int r = 0; r < R; r++)
			{
				for (int k = 0; k < K; k++)
				{
					tempVessel.z[k, r] = 0;
					for (int t = 0; t < T; t++)
					{
						for (int b = 0; b < W; b++)
						{
							tempVessel.x[k, r, t] = 0;
							tempVessel.alpha[k, r, t] = 0;
							tempVessel.beta[b, t] = 0;
							tempVessel.p[b] = 0;
						}
					}

				}
			}

			tempVessel.C = -1;
			tempVessel.S = -1;
			tempVessel.D = 0;

			Cplex subsolver = new Cplex();
			IIntVar C = subsolver.IntVar(0, T);
			IIntVar D = subsolver.IntVar(0, T);
			IIntVar S = subsolver.IntVar(0, T);
			IIntVar[,,] X = new IIntVar[K, R, T];
			IIntVar[,,] alpha = new IIntVar[K, R, T];

			for (int k = 0; k < K; k++)
			{
				for (int r = 0; r < R; r++)
				{
					for (int t = 0; t < T; t++)
					{
						X[k, r, t] = subsolver.BoolVar("X_" + k + "_" + r + "_" + t);
						alpha[k, r, t] = subsolver.BoolVar("Alpha_" + k + "_" + r + "_" + t);
					}
				}
			}

			IIntVar[,] Z = new IIntVar[K, R];

			for (int k = 0; k < K; k++)
			{
				for (int r = 0; r < R; r++)
				{
					Z[k, r] = subsolver.BoolVar("z " + k + " " + r);
				}
			}

			IIntVar[] P = subsolver.BoolVarArray(W);//p_b
			IIntVar[,] Beta = new IIntVar[W, T];//  
			for (int t = 0; t < T; t++)
			{
				for (int b = 0; b < W; b++)
				{
					Beta[b, t] = subsolver.BoolVar("beta " + b + " " + t);
				}
			}

			ILinearNumExpr costC = subsolver.LinearNumExpr();
			ILinearNumExpr costD = subsolver.LinearNumExpr();
			ILinearNumExpr reducedcostAlpha = subsolver.LinearNumExpr();
			ILinearNumExpr reducedcostBeta = subsolver.LinearNumExpr();
			ILinearNumExpr reducedcostU = subsolver.LinearNumExpr();

			for (int b = 1; b < W; b++)
			{
				reducedcostU.AddTerm(TestDual[b - 1], P[b]);
				reducedcostU.AddTerm(-TestDual[b - 1], P[b - 1]);
			}

			double M = 10e2;

			costC.AddTerm(tempVessel.h, C);

			costD.AddTerm(tempVessel.g, D);

			ILinearIntExpr str1 = subsolver.LinearIntExpr();
			ILinearIntExpr str2 = subsolver.LinearIntExpr();
			IIntExpr str3;

			foreach (int k in tempVessel.Ki)
			{
				foreach (int r in Rk[k])
				{
					for (int t = 0; t < T; t++)
					{
						str1.AddTerm(alpha[k, r, t], allNodes[r].switchCost);

						str2.AddTerm(X[k, r, t], -allNodes[r].switchCost);

						reducedcostAlpha.AddTerm(alphadual[r, t], alpha[k, r, t]);
					}

				}
			}

			str3 = subsolver.Sum(str2, str1);
			for (int t = 0; t < T; t++)
			{
				for (int b = 0; b < W; b++)
				{
					reducedcostBeta.AddTerm(wdual[b, t], Beta[b, t]);
				}
			}

			INumExpr spObj = subsolver.Diff(subsolver.Diff(subsolver.Sum(subsolver.Sum(costC, costD), str3), tempVessel.h * tempVessel.a), (subsolver.Sum(reducedcostAlpha, reducedcostBeta, reducedcostU)));


			//INumExpr spObj = subsolver.Diff(subsolver.Diff(subsolver.Sum(subsolver.Sum(costC, costD), str3), tempVessel.h * tempVessel.a), (subsolver.Sum(reducedcostAlpha, reducedcostBeta)));

			subsolver.AddMinimize(spObj);

			

			for (int t = 0; t < T; t++)
			{
				ILinearIntExpr str = subsolver.LinearIntExpr();
				foreach (var k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						str.AddTerm(1, alpha[k, r, t]);
					}
				}
				subsolver.AddGe(C, subsolver.Prod(t, str), "ctr2");//1b
			}

			subsolver.AddGe(D, subsolver.Diff(C, tempVessel.d), "ctr3");//1c

			for (int t = 0; t < T; t++)
			{
				foreach (var k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						subsolver.AddLe(S, subsolver.Sum(t, subsolver.Prod(M, subsolver.Diff(1, X[k, r, t]))), "ctr4");//1d
					}
				}
			}

			subsolver.AddGe(S, tempVessel.a + 1);//1e

			foreach (int k in tempVessel.Ki)
			{
				ILinearIntExpr str = subsolver.LinearIntExpr();
				foreach (int r in Rk[k])
				{
					str.AddTerm(1, Z[k, r]);
				}
				subsolver.AddEq(str, 1, "ctr5");//1j
			}

			for (int t = 0; t < T; t++)//1m
			{
				ILinearIntExpr str = subsolver.LinearIntExpr();
				foreach (int k in tempVessel.Ki)
				{
					//foreach (int r in Rk[k])
					//{
					for (int r = 0; r < R; r++)
					{
						str.AddTerm(1, alpha[k, r, t]);
					}
				}
				subsolver.AddLe(str, 1, "ctr13");//1m
			}

			for (int t = 0; t < T; t++)
			{
				foreach (int k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						subsolver.AddLe(X[k, r, t], tempVessel.e[r, t], "ctr17");//1n
					}
				}
			}

			for (int t = 0; t < T; t++)
			{
				foreach (int k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						subsolver.AddLe(alpha[k, r, t], Z[k, r], "ctr18");//1o
					}

				}
			}
			int kcount2 = 0;
			foreach (int k in tempVessel.Ki)//1k
			{
				ILinearIntExpr str = subsolver.LinearIntExpr();
				for (int t = 0; t < T; t++)
				{
					foreach (int r in Rk[k])
					{
						str.AddTerm(1, X[k, r, t]);
					}

				}
				subsolver.AddEq(str, tempVessel.pik[kcount2], "ctr11");//1k
				kcount2++;
			}

			//14
			foreach (int k in tempVessel.Ki)//1q
			{
				foreach (int r in Rk[k])
				{
					for (int t = 0; t < T - 1; t++)
					{
						subsolver.AddLe(subsolver.Diff(subsolver.Sum(alpha[k, r, t], X[k, r, t]), alpha[k, r, t + 1]), 1, "ctr14");//测试																												 //subsolver.AddLe(alpha[k, r, t + 1], v1.e[r, t]);//测试
					}
				}
			}

			//19
			foreach (int k in tempVessel.Ki)//1p
			{
				foreach (int r in Rk[k])
				{
					for (int t = 0; t < T; t++)
					{
						subsolver.AddGe(alpha[k, r, t], X[k, r, t], "ctr19");
					}
				}
			}

			for (int t = 0; t < T; t++)//2a
			{
				ILinearIntExpr str25 = subsolver.LinearIntExpr();
				for (int b = 0; b < W; b++)
				{
					str25.AddTerm(1, Beta[b, t]);
				}
				subsolver.AddGe(C, subsolver.Prod(t, str25), "ctr22");
			}

			for (int t = 0; t < T; t++)//2b
			{
				ILinearIntExpr str26 = subsolver.LinearIntExpr();
				for (int b = 0; b < W; b++)
				{
					str26.AddTerm(1, Beta[b, t]);
				}
				subsolver.AddLe(S, subsolver.Sum(t, subsolver.Prod(M, subsolver.Diff(1, str26))), "ctr23");
			}

			ILinearIntExpr str27 = subsolver.LinearIntExpr();//2g
			for (int t = 0; t < T; t++)
			{
				for (int b = 0; b < W; b++)
				{
					str27.AddTerm(1, Beta[b, t]);
				}
			}
			subsolver.AddEq(str27, subsolver.Sum(subsolver.Diff(C, S), 1), "ctr2g");

			for (int t = 0; t < T; t++)//2c
			{
				foreach (int k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						ILinearIntExpr str28 = subsolver.LinearIntExpr();
						for (int b = 0; b < W; b++)
						{
							str28.AddTerm(1, Beta[b, t]);
						}
						subsolver.AddGe(str28, alpha[k, r, t], "ctr24");
					}
				}
			}


			for (int t = 0; t < T; t++)//2d
			{
				for (int b = 0; b < W; b++)
				{
					subsolver.AddLe(Beta[b, t], P[b], "ctr25");
				}
			}


			ILinearIntExpr str30 = subsolver.LinearIntExpr();
			for (int b = 0; b < W; b++)//2e
			{
				str30.AddTerm(1, P[b]);
			}
			subsolver.AddEq(str30, 1, "ctr26"); //AddEq(str30, 1)


			ILinearIntExpr str31 = subsolver.LinearIntExpr();
			ILinearIntExpr str32 = subsolver.LinearIntExpr();
			foreach (int k in tempVessel.Ki)
			{
				foreach (int r in Rk[k])
				{

					for (int t = 0; t < T; t++)
					{
						str31.AddTerm(1, alpha[k, r, t]);
						str32.AddTerm(1, X[k, r, t]);
					}

				}
			}

			subsolver.AddLe(str31, subsolver.Sum(str32, tempVessel.K + interruptTimes), "ctr16");//1r

			if (addconstraints != null)
			{
				for (int i = 0; i < addconstraints.Count; i++)
				{
					if (addconstraints[i].Indicator == 0)
					{
						if (addconstraints[i].Xposition.Count() == 4)
						{
							Alpha_Left(tempVessel, (int)addconstraints[i].Xposition[0], (int)addconstraints[i].Xposition[1], (int)addconstraints[i].Xposition[2], (int)addconstraints[i].Xposition[3], subsolver, alpha);
						}
						if (addconstraints[i].Xposition.Count() == 2)
						{
							Beta_Left(tempVessel, (int)addconstraints[i].Xposition[0], (int)addconstraints[i].Xposition[1], subsolver, Beta);
						}
						if (addconstraints[i].Xposition.Count() == 3)
						{
							P_Left(tempVessel, (int)addconstraints[i].Xposition[0], (int)addconstraints[i].Xposition[1], subsolver, P);
						}

					}
					if (addconstraints[i].Indicator == 1)
					{
						if (addconstraints[i].Xposition.Count() == 4)
						{
							Alpha_Right(tempVessel, (int)addconstraints[i].Xposition[0], (int)addconstraints[i].Xposition[1], (int)addconstraints[i].Xposition[2], (int)addconstraints[i].Xposition[3], subsolver, alpha);
						}
						if (addconstraints[i].Xposition.Count() == 2)
						{
							Beta_Right(tempVessel, (int)addconstraints[i].Xposition[0], (int)addconstraints[i].Xposition[1], subsolver, Beta);
						}

						if (addconstraints[i].Xposition.Count() == 3)
						{
							P_Right(tempVessel, (int)addconstraints[i].Xposition[0], (int)addconstraints[i].Xposition[1], subsolver, P);
						}

					}
				}
			}

			subsolver.ExportModel(@"C:\exportmodel\sp_check.lp");

			bool succ = subsolver.Solve();

			if (succ)
			{
				Console.WriteLine("Cplex Obj: " + subsolver.ObjValue);
				Console.WriteLine("C: " + subsolver.GetValue(C));
				Console.WriteLine("D: " + subsolver.GetValue(D));
				//Console.WriteLine("h cost " + (subsolver.GetValue(costC) - tempVessel.h * tempVessel.a));
				//Console.WriteLine("g cost " + subsolver.GetValue(costD));
				//Console.WriteLine("l cost " + subsolver.GetValue(str3));
				//Console.WriteLine("gamma cost " + subsolver.GetValue(reducedcostBeta));
				//Console.WriteLine("tau cost " + subsolver.GetValue(reducedcostAlpha));

				tempVessel.cost = subsolver.GetValue(subsolver.Diff(subsolver.Sum(subsolver.Sum(costC, costD), str3), tempVessel.h * tempVessel.a));
				tempVessel.lambda = 0;
				

				tempVessel.S = (int)Math.Round(subsolver.GetValue(S));
				tempVessel.C = (int)Math.Round(subsolver.GetValue(C));
				tempVessel.D = (int)Math.Round(subsolver.GetValue(D));

				tempVessel.cost = Math.Round(subsolver.GetValue(subsolver.Diff(subsolver.Sum(subsolver.Sum(costC, costD), str3), tempVessel.h * tempVessel.a)));//


				double spObjVal = subsolver.GetObjValue() - pi[tempVessel.id - 1];//

				//Console.WriteLine("obj " + subsolver.GetValue(spObj));
				//Console.WriteLine("Switching" + subsolver.GetValue(str3));
				//Console.WriteLine("hc " + tempVessel.C * tempVessel.h);
				//Console.WriteLine("fd " + tempVessel.D * tempVessel.g);


				double sumation = 0;
				Console.WriteLine("ha " + tempVessel.a * tempVessel.h);


				for (int b = 0; b < W; b++)
				{
					if (subsolver.GetValue(P[b]) > 0.9)
					{
						tempVessel.w = b;
					}

					tempVessel.p[b] = (int)Math.Round(subsolver.GetValue(P[b]));
				}


				foreach (int k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						for (int t = 0; t < T; t++)
						{
							tempVessel.x[k, r, t] = Math.Round(subsolver.GetValue(X[k, r, t]));
							tempVessel.alpha[k, r, t] = Math.Round(subsolver.GetValue(alpha[k, r, t]));
							#region
							//=============== debug =============//
							//if (tempVessel.x[k, r, t] == 1)
							//{
							//	Console.Write("x  ");
							//	Console.WriteLine(k + " " + r + " " + t);
							//}

							//if (tempVessel.alpha[k, r, t] == 1)
							//{
							//	sumation += alphadual[r, t];
							//	Console.Write("alpha  ");
							//	Console.WriteLine(k + " " + r + " " + t);
							//	Console.WriteLine(alphadual[r, t]);
							//}
							//=============== debug =============//
							#endregion
						}
					}
				}

				for (int t = 0; t < T; t++)
				{
					for (int b = 0; b < W; b++)
					{
						tempVessel.beta[b, t] = Math.Round(subsolver.GetValue(Beta[b, t]));
						//sum2 += wdual[t, b];
					}
				}

				foreach (int k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						tempVessel.z[k, r] = Math.Round(subsolver.GetValue(Z[k, r]));
					}
				}
				return (tempVessel, spObjVal);
			}

			else
			{
				Console.WriteLine("子问题有问题");
				subsolver.ExportModel(@"C:\exportmodel\Error.lp");
				return (null, double.MaxValue);
			}
		}

		public static (double vesselCost, double rdCost) SubproblemCplexCheck(Vessel v, Vessel check, List<inherit_info> addconstraints = null)
		{
			Vessel tempVessel = new Vessel(v);
			tempVessel.lambda = -1;

			tempVessel.cost = 0;
			//tempVessel.e = new int[R, T];
			tempVessel.x = new double[K, R, T];
			tempVessel.alpha = new double[K, R, T];
			tempVessel.z = new double[K, R];
			tempVessel.beta = new double[W, T];

			for (int r = 0; r < R; r++)
			{
				for (int k = 0; k < K; k++)
				{
					tempVessel.z[k, r] = 0;
					for (int t = 0; t < T; t++)
					{
						for (int b = 0; b < W; b++)
						{
							tempVessel.x[k, r, t] = 0;
							tempVessel.alpha[k, r, t] = 0;
							tempVessel.beta[b, t] = 0;
						}
					}

				}
			}

			tempVessel.C = -1;
			tempVessel.S = -1;
			tempVessel.D = 0;

			Cplex subsolver = new Cplex();
			IIntVar C = subsolver.IntVar(0, T, "C");
			IIntVar D = subsolver.IntVar(0, T, "D");
			IIntVar S = subsolver.IntVar(0, T, "S");
			IIntVar[,,] X = new IIntVar[K, R, T];
			IIntVar[,,] alpha = new IIntVar[K, R, T];

			for (int k = 0; k < K; k++)
			{
				for (int r = 0; r < R; r++)
				{
					for (int t = 0; t < T; t++)
					{
						X[k, r, t] = subsolver.BoolVar("X_" + k + "_" + r + "_" + t);
						alpha[k, r, t] = subsolver.BoolVar("Alpha_" + k + "_" + r + "_" + t);
					}
				}
			}

			IIntVar[,] Z = new IIntVar[K, R];

			for (int k = 0; k < K; k++)
			{
				for (int r = 0; r < R; r++)
				{
					Z[k, r] = subsolver.BoolVar("z " + k + " " + r);
				}
			}

			IIntVar[] P = new IIntVar[W];//p_b
			for (int w = 0; w < W; w++)
			{
				P[w] = subsolver.BoolVar("P_" + w);
			}
			



			IIntVar[,] Beta = new IIntVar[W, T];
			for (int t = 0; t < T; t++)
			{
				for (int b = 0; b < W; b++)
				{
					Beta[b, t] = subsolver.BoolVar("beta " + b + " " + t);
				}
			}

			ILinearNumExpr costC = subsolver.LinearNumExpr();
			ILinearNumExpr costD = subsolver.LinearNumExpr();
			ILinearNumExpr reducedcostAlpha = subsolver.LinearNumExpr();
			ILinearNumExpr reducedcostBeta = subsolver.LinearNumExpr();
			ILinearNumExpr reducedcostU = subsolver.LinearNumExpr();

			for (int b = 1; b < W; b++)
			{
				reducedcostU.AddTerm(TestDual[b - 1], P[b]);
				reducedcostU.AddTerm(-TestDual[b - 1], P[b - 1]);
			}

			double M = 10e2;

			costC.AddTerm(tempVessel.h, C);

			costD.AddTerm(tempVessel.g, D);

			ILinearIntExpr str1 = subsolver.LinearIntExpr();
			ILinearIntExpr str2 = subsolver.LinearIntExpr();
			IIntExpr str3;

			foreach (int k in tempVessel.Ki)
			{
				foreach (int r in Rk[k])
				{
					for (int t = 0; t < T; t++)
					{
						str1.AddTerm(alpha[k, r, t], allNodes[r].switchCost);

						str2.AddTerm(X[k, r, t], -allNodes[r].switchCost);

						reducedcostAlpha.AddTerm(alphadual[r, t], alpha[k, r, t]);
					}

				}
			}

			str3 = subsolver.Sum(str2, str1);
			for (int t = 0; t < T; t++)
			{
				for (int b = 0; b < W; b++)
				{
					reducedcostBeta.AddTerm(wdual[b, t], Beta[b, t]);
				}
			}

			INumExpr spObj = subsolver.Diff(subsolver.Diff(subsolver.Sum(subsolver.Sum(costC, costD), str3), tempVessel.h * tempVessel.a), (subsolver.Sum(reducedcostAlpha, reducedcostBeta, reducedcostU)));


			//INumExpr spObj = subsolver.Diff(subsolver.Diff(subsolver.Sum(subsolver.Sum(costC, costD), str3), tempVessel.h * tempVessel.a), (subsolver.Sum(reducedcostAlpha, reducedcostBeta)));

			subsolver.AddMinimize(spObj);


			subsolver.AddEq(C, check.C, "Eq C");
			subsolver.AddEq(S, check.S, "Eq S");
			subsolver.AddEq(D, check.D, "Eq D");

			for (int k = 0; k < K; k++)
			{
				for (int r = 0; r < R; r++)
				{
					for (int t = 0; t < T; t++)
					{
						subsolver.AddEq(X[k, r, t], check.x[k, r, t], "Eq x");
						subsolver.AddEq(alpha[k, r, t], check.alpha[k, r, t], "Eq alpha");
						subsolver.AddEq(Z[k, r], check.z[k, r], "Eq z");


					}
				}
			}


			for (int w = 0; w < W; w++)
			{
				for (int t = 0; t < T; t++)
				{
					subsolver.AddEq(Beta[w, t], check.beta[w, t], "Eq beta");
					subsolver.AddEq(P[w], check.p[w], "Eq p");
				}
			}


			for (int t = 0; t < T; t++)
			{
				ILinearIntExpr str = subsolver.LinearIntExpr();
				foreach (var k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						str.AddTerm(1, alpha[k, r, t]);
					}
				}
				subsolver.AddGe(C, subsolver.Prod(t, str), "ctr2");//1b
			}

			subsolver.AddGe(D, subsolver.Diff(C, tempVessel.d), "ctr3");//1c

			for (int t = 0; t < T; t++)
			{
				foreach (var k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						subsolver.AddLe(S, subsolver.Sum(t, subsolver.Prod(M, subsolver.Diff(1, X[k, r, t]))), "ctr4");//1d
					}
				}
			}

			subsolver.AddGe(S, tempVessel.a + 1);//1e

			foreach (int k in tempVessel.Ki)
			{
				ILinearIntExpr str = subsolver.LinearIntExpr();
				foreach (int r in Rk[k])
				{
					str.AddTerm(1, Z[k, r]);
				}
				subsolver.AddEq(str, 1, "ctr5");//1j
			}

			for (int t = 0; t < T; t++)//1m
			{
				ILinearIntExpr str = subsolver.LinearIntExpr();
				foreach (int k in tempVessel.Ki)
				{
					//foreach (int r in Rk[k])
					//{
					for (int r = 0; r < R; r++)
					{
						str.AddTerm(1, alpha[k, r, t]);
					}
				}
				subsolver.AddLe(str, 1, "ctr13");//1m
			}

			for (int t = 0; t < T; t++)
			{
				foreach (int k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						subsolver.AddLe(X[k, r, t], tempVessel.e[r, t], "ctr17");//1n
					}
				}
			}

			for (int t = 0; t < T; t++)
			{
				foreach (int k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						subsolver.AddLe(alpha[k, r, t], Z[k, r], "ctr18");//1o
					}

				}
			}
			int kcount2 = 0;
			foreach (int k in tempVessel.Ki)//1k
			{
				ILinearIntExpr str = subsolver.LinearIntExpr();
				for (int t = 0; t < T; t++)
				{
					foreach (int r in Rk[k])
					{
						str.AddTerm(1, X[k, r, t]);
					}

				}
				subsolver.AddEq(str, tempVessel.pik[kcount2], "ctr11");//1k
				kcount2++;
			}

			//14
			foreach (int k in tempVessel.Ki)//1q
			{
				foreach (int r in Rk[k])
				{
					for (int t = 0; t < T - 1; t++)
					{
						subsolver.AddLe(subsolver.Diff(subsolver.Sum(alpha[k, r, t], X[k, r, t]), alpha[k, r, t + 1]), 1, "ctr14");//测试																												 //subsolver.AddLe(alpha[k, r, t + 1], v1.e[r, t]);//测试
					}
				}
			}

			//19
			foreach (int k in tempVessel.Ki)//1p
			{
				foreach (int r in Rk[k])
				{
					for (int t = 0; t < T; t++)
					{
						subsolver.AddGe(alpha[k, r, t], X[k, r, t], "ctr19");
					}
				}
			}

			for (int t = 0; t < T; t++)//2a
			{
				ILinearIntExpr str25 = subsolver.LinearIntExpr();
				for (int b = 0; b < W; b++)
				{
					str25.AddTerm(1, Beta[b, t]);
				}
				subsolver.AddGe(C, subsolver.Prod(t, str25), "ctr22");
			}

			for (int t = 0; t < T; t++)//2b
			{
				ILinearIntExpr str26 = subsolver.LinearIntExpr();
				for (int b = 0; b < W; b++)
				{
					str26.AddTerm(1, Beta[b, t]);
				}
				subsolver.AddLe(S, subsolver.Sum(t, subsolver.Prod(M, subsolver.Diff(1, str26))), "ctr23");
			}

			ILinearIntExpr str27 = subsolver.LinearIntExpr();//2g
			for (int t = 0; t < T; t++)
			{
				for (int b = 0; b < W; b++)
				{
					str27.AddTerm(1, Beta[b, t]);
				}
			}
			subsolver.AddEq(str27, subsolver.Sum(subsolver.Diff(C, S), 1), "ctr2g");

			for (int t = 0; t < T; t++)//2c
			{
				foreach (int k in tempVessel.Ki)
				{
					foreach (int r in Rk[k])
					{
						ILinearIntExpr str28 = subsolver.LinearIntExpr();
						for (int b = 0; b < W; b++)
						{
							str28.AddTerm(1, Beta[b, t]);
						}
						subsolver.AddGe(str28, alpha[k, r, t], "ctr24");
					}
				}
			}


			for (int t = 0; t < T; t++)//2d
			{
				for (int b = 0; b < W; b++)
				{
					subsolver.AddLe(Beta[b, t], P[b], "ctr25");
				}
			}


			ILinearIntExpr str30 = subsolver.LinearIntExpr();
			for (int b = 0; b < W; b++)//2e
			{
				str30.AddTerm(1, P[b]);
			}
			subsolver.AddEq(str30, 1, "ctr26"); 


			ILinearIntExpr str31 = subsolver.LinearIntExpr();
			ILinearIntExpr str32 = subsolver.LinearIntExpr();
			foreach (int k in tempVessel.Ki)
			{
				foreach (int r in Rk[k])
				{

					for (int t = 0; t < T; t++)
					{
						str31.AddTerm(1, alpha[k, r, t]);
						str32.AddTerm(1, X[k, r, t]);
					}

				}
			}

			subsolver.AddLe(str31, subsolver.Sum(str32, tempVessel.K + interruptTimes), "ctr16");//1r

			if (addconstraints != null)
			{
				for (int i = 0; i < addconstraints.Count; i++)
				{
					if (addconstraints[i].Indicator == 0)
					{
						if (addconstraints[i].Xposition.Count() == 4)
						{
							Alpha_Left(tempVessel, (int)addconstraints[i].Xposition[0], (int)addconstraints[i].Xposition[1], (int)addconstraints[i].Xposition[2], (int)addconstraints[i].Xposition[3], subsolver, alpha);
						}

					}
					if (addconstraints[i].Indicator == 1)
					{
						if (addconstraints[i].Xposition.Count() == 4)
						{
							Alpha_Right(tempVessel, (int)addconstraints[i].Xposition[0], (int)addconstraints[i].Xposition[1], (int)addconstraints[i].Xposition[2], (int)addconstraints[i].Xposition[3], subsolver, alpha);
						}

					}
				}
			}

			subsolver.ExportModel(@"C:\exportmodel\sp_check.lp");

			bool succ = subsolver.Solve();

			if (succ)
			{
				Console.WriteLine("Cplex Obj: " + subsolver.ObjValue);
				Console.WriteLine("C: " + subsolver.GetValue(C));
				Console.WriteLine("D: " + subsolver.GetValue(D));
				Console.WriteLine("h cost " + (subsolver.GetValue(costC) - tempVessel.h * tempVessel.a));
				Console.WriteLine("g cost " + subsolver.GetValue(costD));
				Console.WriteLine("l cost " + subsolver.GetValue(str3));
				Console.WriteLine("gamma cost " + subsolver.GetValue(reducedcostBeta));
				Console.WriteLine("tau cost " + subsolver.GetValue(reducedcostAlpha));

				tempVessel.cost = subsolver.GetValue(subsolver.Diff(subsolver.Sum(subsolver.Sum(costC, costD), str3), tempVessel.h * tempVessel.a));
				for (int t = 0; t < T; t++)
				{
					for (int b = 0; b < W; b++)
					{
						if (subsolver.GetValue(Beta[b, t]) > 0.9)
						{
							Console.WriteLine("时刻" + t + "在泊位" + b);
						}
					}
				}

				foreach (var k in tempVessel.Ki)
				{
					Console.WriteLine("原料" + k + " ");
					foreach (var r in Rk[k])
					{
						for (int t = 0; t < T; t++)
						{
							if (subsolver.GetValue(alpha[k, r, t]) > 0.9)
							{
								Console.WriteLine("alpha在料条 " + r + "在时刻 " + t);
							}
						}
					}

					Console.WriteLine();
				}


				for (int t = 0; t < T; t++)
				{
					ILinearIntExpr str = subsolver.LinearIntExpr();
					foreach (var k in tempVessel.Ki)
					{
						foreach (int r in Rk[k])
						{
							if (subsolver.GetValue(X[k, r, t]) > 0.9)
							{
								Console.Write("x在料条 " + r + "在时刻 " + t);
							}
						}
					}

					Console.WriteLine();
				}



				double spObjVal = subsolver.GetObjValue() - pi[tempVessel.id - 1];//

				Console.WriteLine("obj " + subsolver.GetValue(spObj));
				Console.WriteLine("Switching" + subsolver.GetValue(str3));
				Console.WriteLine("hc " + tempVessel.C * tempVessel.h);
				Console.WriteLine("fd " + tempVessel.D * tempVessel.g);

				return (tempVessel.cost, spObjVal);

				
			}

			else
			{
				Console.WriteLine("子问题有问题");
				subsolver.ExportModel(@"C:\exportmodel\Error.lp");
				return (double.MaxValue, double.MaxValue);
			}
		}

		static void Alpha_Left(Vessel v, int id, int k, int r, int t, Cplex sub, IIntVar[,,] alpha)
		{
			if (v.id == id)
			{
				sub.AddLe(alpha[k, r, t], 0, "test");
			}
		}

		static void Alpha_Right(Vessel v, int id, int k, int r, int t, Cplex sub, IIntVar[,,] alpha)
		{
			if (v.id == id)
			{
				sub.AddGe(alpha[k, r, t], 1);
			}
		}

		static void Beta_Left(Vessel v, int b, int t, Cplex sub, IIntVar[,] beta)
		{

			sub.AddLe(beta[b, t], 0, "test");

		}

		static void P_Left(Vessel v, int id, int b, Cplex sub, IIntVar[] p)
		{
			if (v.id == id)
				sub.AddLe(p[b], 0, "test");

		}

		static void Beta_Right(Vessel v, int b, int t, Cplex sub, IIntVar[,] beta)
		{

			sub.AddGe(beta[b, t], 1);

		}

		static void P_Right(Vessel v, int id, int b, Cplex sub, IIntVar[] p)
		{
			if (v.id == id)
				sub.AddGe(p[b], 1);

		}

		public static int TestExactCount = 0;
		public static List<Vessel> feasibleSol = new List<Vessel>();
		public static int spCount = 0;
		public static (List<Vessel> ptns, double val) ColumnGeneration(List<Vessel> templateVessel, HashSet<Vessel> A, List<inherit_info> branchInfos)
		{
			List<Vessel> mpRes = new List<Vessel>();

			double mpObj = -1;

			while (true)
			{
				DateTime timerCheck = DateTime.Now;
				if ((timerCheck - timer).TotalSeconds > 7200)
				{
					Console.WriteLine("stop");
					while (true)
					{

						Console.WriteLine("Feas val " + UB);
						Console.WriteLine("overall runtime " + 7200);
						Console.WriteLine("root LS time: " + rootLSTime);
						Console.WriteLine("root Cplex time " + rootCplexTime);
						Console.WriteLine("root UB " + rootUB);
						Console.WriteLine("root LB " + rootLB);
						Console.WriteLine("IterNum " + IterNum);
						Console.WriteLine("root ColumnNum " + rootColumnNum);
						Console.WriteLine("root Time " + rootNodeTime);

						Console.WriteLine("Node Num total " + NodeNum);

						foreach (var vessel in feasibleSol)
						{
							Console.WriteLine("==========================" + vessel.id + "===========================");
							Console.WriteLine("vessel" + vessel.id + "'s yard assignment");
							for (int k = 0; k < K; k++)
							{
								for (int r = 0; r < R; r++)
								{
									for (int t = 0; t < T; t++)
									{
										if (vessel.x[k, r, t] == 1)
										{
											Console.WriteLine(k + " " + r + " " + t);
										}
									}
								}
							}

							Console.WriteLine("vessel" + vessel.id + "'s berth allocation");
							for (int b = 0; b < W; b++)
							{
								for (int t = 0; t < T; t++)
								{
									if (vessel.beta[b, t] == 1)
									{
										Console.WriteLine(b + " " + t);
									}
								}
							}
						}

						return (null, int.MaxValue);
					}
				}

				int iterCount = 0;
				int spSolvedCount = 0;
				(mpRes, mpObj) = MPsolver(A.ToList());
				if (Math.Abs(mpObj-768050)<1)
				{
					Console.WriteLine(	);
				}
				
				DateTime initialSolTime = DateTime.Now;
				Console.WriteLine((initialSolTime - initialSolBegin).TotalSeconds);
				if (mpRes == null)
				{
					return (null, int.MaxValue);
				}
				if (mpRes.Count == N)
				{
					
					if (UB > mpObj)
					{
						UB = mpObj;
						feasibleSol = mpRes.Select(x => new Vessel(x)).ToList();
					}

					if (UB - LB <= 1 - RC_EPS)
					{
						Console.WriteLine("求解到最优");
						return (mpRes, mpObj);
					}
					//}
				}
				bool hasDuplicates = A.Count != A.Distinct().Count();
				if (hasDuplicates)
				{
					Console.WriteLine("有重复");

					var duplicateIndices = A.Select((vessel, index) => new { Vessel = vessel, Index = index })
								.GroupBy(x => x.Vessel)
								.Where(g => g.Count() > 1)
								.SelectMany(g => g.Select(v => v.Index))
								.OrderByDescending(x => x) 
								.ToList();

					
					foreach (var index in duplicateIndices)
					{
						Console.WriteLine("Duplicate index: " + index);
					}

					//foreach (var index in duplicateIndices)
					//{
					//	A.RemoveAt(index);
					//}
					Console.WriteLine("+++++++++++++++++++++++++++");

					var duplicateIndices2 = A.Select((vessel, index) => new { Vessel = vessel, Index = index })
								.GroupBy(x => x.Vessel)
								.Where(g => g.Count() > 1)
								.SelectMany(g => g.Select(v => v.Index))
								.OrderByDescending(x => x) 
								.ToList();

					
					foreach (var index in duplicateIndices2)
					{
						Console.WriteLine("Duplicate index: " + index);
					}
				}

				#region
				//Console.WriteLine("=================6==================");
				//for (int k = 0; k < K; k++)
				//{
				//	for (int r = 0; r < R; r++)
				//	{
				//		for (int t = 0; t < T; t++)
				//		{
				//			if (A[6].alpha[k, r, t] > 0.9)
				//			{
				//				Console.WriteLine(k + "_" + r + "_" + t);
				//			}
				//		}
				//	}
				//}
				//for (int b = 0; b < W; b++)
				//{
				//	for (int t = 0; t < T; t++)
				//	{
				//		if (A[6].beta[b, t] > 0.9)
				//		{
				//			Console.WriteLine(b + "_" + t);
				//		}
				//	}
				//}

				//Console.WriteLine("=================22==================");
				//for (int k = 0; k < K; k++)
				//{
				//	for (int r = 0; r < R; r++)
				//	{
				//		for (int t = 0; t < T; t++)
				//		{
				//			if (A[22].alpha[k, r, t] > 0.9)
				//			{
				//				Console.WriteLine(k + "_" + r + "_" + t);
				//			}
				//		}
				//	}
				//}
				//for (int b = 0; b < W; b++)
				//{
				//	for (int t = 0; t < T; t++)
				//	{
				//		if (A[22].beta[b, t] > 0.9)
				//		{
				//			Console.WriteLine(b + "_" + t);
				//		}
				//	}
				//}
				#endregion

				for (int i = 0; i < N; i++)
				{
					DateTime timerCheck2 = DateTime.Now;
					if ((timerCheck2 - timer).TotalSeconds > 7200)
					{
						Console.WriteLine("stop");
						while (true)
						{
							//System.Threading.Thread.Sleep(1000);
							Console.WriteLine("Feas val " + UB);
							Console.WriteLine("overall runtime " + 7200);
							Console.WriteLine("root LS time: " + rootLSTime);
							Console.WriteLine("root Cplex time " + rootCplexTime);
							Console.WriteLine("root UB " + rootUB);
							Console.WriteLine("root LB " + rootLB);
							Console.WriteLine("IterNum " + IterNum);
							Console.WriteLine("root ColumnNum " + rootColumnNum);
							Console.WriteLine("root Time " + rootNodeTime);

							Console.WriteLine("Node Num total " + NodeNum);

							foreach (var vessel in feasibleSol)
							{
								Console.WriteLine("==========================" + vessel.id + "===========================");
								Console.WriteLine("vessel" + vessel.id + "'s yard assignment");
								for (int k = 0; k < K; k++)
								{
									for (int r = 0; r < R; r++)
									{
										for (int t = 0; t < T; t++)
										{
											if (vessel.x[k, r, t] == 1)
											{
												Console.WriteLine(k + " " + r + " " + t);
											}
										}
									}
								}

								Console.WriteLine("vessel" + vessel.id + "'s berth allocation");
								for (int bb = 0; bb < W; bb++)
								{
									for (int t = 0; t < T; t++)
									{
										if (vessel.beta[bb, t] == 1)
										{
											Console.WriteLine(bb + " " + t);
										}
									}
								}
							}
							return (null, int.MaxValue);
						}
					}

					spCount++;


					(Vessel resSP, double rdCost) spRes = new(new Vessel(), 0.0);
					//DateTime a = DateTime.Now;
					//var spResLS = SubproblemLabelSetting(templateVessel[i], branchInfos, false);
					//DateTime b = DateTime.Now;

					//LSTime += (b - a).TotalSeconds;
					#region
					//if (spResLS.pattern != null)
					//{
					//	var checkLS = SubproblemCplexCheck(templateVessel[i], spResLS.pattern, branchInfos);

					//	if (checkLS.rdCost > RC_EPS)
					//	{

					//		Console.WriteLine("!!!!!!!!!!!!!!");
					//	}
					//	if (spResLS.pattern.cost != checkLS.vesselCost)
					//	{
					//		Console.WriteLine("00000000000000000000000");

					//		SubproblemCplexCheck(templateVessel[i], spResLS.pattern, branchInfos);
					//	}

					//	if (Math.Abs(spResLS.cost - checkLS.rdCost) > RC_EPS)
					//	{
					//		Console.WriteLine("000000000000000000");
					//	}

					//	if (spResLS.cost > -1)
					//	{
					//		Console.WriteLine();
					//	}
					//}

					//var cplexTest = SubproblemCplex(templateVessel[i], branchInfos);

					//if (cplexTest.rdCost != spResLS.cost)
					//{
					//	Console.WriteLine(	);
					//}

					//if (spResLS.pattern != null)
					//{
					//	if (cplexTest.resSP.cost != spResLS.pattern.cost)
					//	{
					//		Console.WriteLine();
					//	}

					//}

					//if (spResLS.cost == cplexTest.rdCost)
					//{
					//	Console.WriteLine("========================LS=========================");
					//	for (int k = 0; k < K; k++)
					//	{
					//		for (int r = 0; r < R; r++)
					//		{
					//			for (int t = 0; t < T; t++)
					//			{
					//				if (spResLS.pattern.x[k, r, t] == 1)
					//				{
					//					Console.WriteLine(k + " " + r + " " + t);
					//				}
					//			}
					//		}
					//	}


					//	Console.WriteLine("========================CPLEX=========================");
					//	for (int k = 0; k < K; k++)
					//	{
					//		for (int r = 0; r < R; r++)
					//		{
					//			for (int t = 0; t < T; t++)
					//			{
					//				if (cplexTest.resSP.x[k, r, t] == 1)
					//				{
					//					Console.WriteLine(k + " " + r + " " + t);
					//				}
					//			}
					//		}
					//	}
					//}


					//=======================================================//
					//if (spResLS.cost != cplexTest.rdCost && spResLS.cost != int.MaxValue)
					//{
					//	if (spResLS.cost >= 0 && cplexTest.rdCost < 0)
					//		Console.WriteLine();
					//}

					#endregion

					//===========================================================//
					//if (spResLS.pattern != null && spResLS.cost < -RC_EPS)
					//{
					//	spRes = (new Vessel(spResLS.pattern), spResLS.cost);
					//}

					//else
					//{
					//	DateTime c = DateTime.Now;
					//	spRes = SubproblemCplex(templateVessel[i], branchInfos);
					//	DateTime d = DateTime.Now;
					//	CplexTime += (d - c).TotalSeconds;
					//}
					//=========================================================//
					DateTime c = DateTime.Now;
					spRes = SubproblemCplex(templateVessel[i], branchInfos);
					DateTime d = DateTime.Now;

					CplexTime += (d - c).TotalSeconds;

					#region
					//Console.WriteLine("=======SP" + i + "=======");
					//for (int k = 0; k < K; k++)
					//{
					//	for (int r = 0; r < R; r++)
					//	{
					//		for (int t = 0; t < T; t++)
					//		{
					//			if (spRes.resSP.x[k, r, t] == 1)
					//			{
					//				Console.WriteLine(k + " " + r + " " + t);
					//			}
					//		}
					//	}
					//}
					#endregion


					int countPrev = A.Count;
					//spRes = SubproblemCplex(templateVessel[i], branchInfos);


					if (spRes.rdCost > -RC_EPS)
					{
						
							spSolvedCount++;
							Console.WriteLine("求解到最优 or 不可行");
							Console.WriteLine(i);
						
					}


					else if (spRes.resSP != null && spRes.rdCost < RC_EPS)
					{
						if (A.Contains(spRes.resSP))
						{
							Vessel dup = null;
							A.TryGetValue(spRes.resSP, out dup);
							Console.WriteLine("重复");

							Console.WriteLine("==================");
							for (int k = 0; k < K; k++)
							{
								for (int r = 0; r < R; r++)
								{
									for (int t = 0; t < T; t++)
									{
										if (spRes.resSP.x[k, r, t] == 1)
										{
											Console.WriteLine(k + " " + r + " " + t);
										}
									}
								}
							}
							Console.WriteLine("============================");
							for (int k = 0; k < K; k++)
							{
								for (int r = 0; r < R; r++)
								{
									for (int t = 0; t < T; t++)
									{
										if (dup.x[k, r, t] == 1)
										{
											Console.WriteLine(k + " " + r + " " + t);
										}
									}
								}
							}
							Console.WriteLine("============================================");
						}

						A.Add(new Vessel(spRes.resSP, false));
						if (A.Count == countPrev)
						{
							Console.WriteLine();
						}
					}
				}

				if (spSolvedCount == N)
				{
					Console.WriteLine("每个子问题都不能减小了");
					(mpRes, mpObj) = MPsolver(A.ToList());
					break;
				}
			}

			return (mpRes, mpObj);
		}

		static double sumcost(List<Vessel> list)
		{
			double cost = 0;
			foreach (Vessel vi in list)
			{
				cost += vi.cost * vi.lambda;
			}
			return cost;

		}

		public static List<Vessel> FinalRes = new List<Vessel>();

		public static bool exactLSFlag = false;

		public static (List<Vessel>, double) MPsolverInteger(List<Vessel> v)
		{
			List<Vessel> MPres = new List<Vessel>();
			Cplex MPsolver = new Cplex();
			ILinearNumExpr MpObj = MPsolver.LinearNumExpr();
			INumVar[] lambda = MPsolver.BoolVarArray(v.Count);

			for (int i = 0; i < v.Count(); i++)
			{
				MpObj.AddTerm(v[i].cost, lambda[i]);
			}
			MPsolver.AddMinimize(MpObj);
			IRange[,] rng1 = new IRange[R, T];
			for (int t = 0; t < T; t++)
			{
				for (int r = 0; r < R; r++)
				{
					ILinearNumExpr str19 = MPsolver.LinearNumExpr();
					for (int i = 0; i < v.Count; i++)
					{
						foreach (int k in v[i].Ki)
						{
							str19.AddTerm(lambda[i], v[i].alpha[k, r, t]);
						}

					}
					rng1[r, t] = MPsolver.AddLe(str19, 1, "alpha " + t + "_" + r);
				}

			}

			IRange[,] rng2 = new IRange[W, T];
			for (int t = 0; t < T; t++)
			{
				for (int b = 0; b < W; b++)
				{
					ILinearNumExpr str20 = MPsolver.LinearNumExpr();
					for (int i = 0; i < v.Count; i++)
					{
						str20.AddTerm(lambda[i], v[i].beta[b, t]);
					}

					rng2[b, t] = MPsolver.AddLe(str20, 1, "beta " + b + "_" + t);
				}
			}
			IRange[] rng3 = new IRange[N];
			for (int i = 0; i < N; i++)
			{
				ILinearNumExpr str21 = MPsolver.LinearNumExpr();
				for (int j = 0; j < v.Count; j++)
				{
					if (i == v[j].id - 1)
					{
						str21.AddTerm(lambda[j], 1);
					}
				}
				rng3[i] = MPsolver.AddEq(str21, 1);
			}

			IRange[] rng4 = new IRange[W];

			for (int b = 1; b < W; b++)
			{
				ILinearNumExpr str22 = MPsolver.LinearNumExpr();
				for (int i = 0; i < v.Count; i++)
				{
					str22.AddTerm(lambda[i], v[i].p[b]);
				}
				ILinearNumExpr str23 = MPsolver.LinearNumExpr();
				for (int i = 0; i < v.Count; i++)
				{
					str23.AddTerm(lambda[i], v[i].p[b - 1]);
				}
				rng4[b] = MPsolver.AddLe(MPsolver.Diff(str22, str23), 0);
			}

			MPsolver.ExportModel(@"C:\exportmodel\MP2.lp");

			if (MPsolver.Solve())
			{
				rmpobj = MPsolver.GetObjValue();

				for (int i = 0; i < v.Count; i++)
				{
					if (MPsolver.GetValue(lambda[i]) > 0)
					{
						v[i].lambda = MPsolver.GetValue(lambda[i]);
						MPres.Add(v[i]);
					}
				}

				MPres.Sort(new VesselComparer(VesselComparer.CompareType.id));

				MPsolver.End();
			}

			else
			{
				Console.WriteLine("mp has no solutions");
				return (null, int.MaxValue);
				throw new System.Exception("mp has no solutions");
			}

			MPcount++;
			return (MPres, rmpobj);
		}

		public static bool strongFirstFlag = true;

		static (List<Vessel>, double obj) BranchPrice(List<Vessel> templateVessel, List<Vessel> pond)
		{
			int[] branchXIdx = new int[5];
			double[,,,] xTemp = new double[N, K, R, T];
			HashSet<Vessel> uniVessel = new HashSet<Vessel>(pond);
			DateTime now = DateTime.Now;
			var tempRes = ColumnGeneration(templateVessel, uniVessel, null);
			NodeNum++;
			DateTime end = DateTime.Now;

			rootNodeTime = (end - now).TotalSeconds;
			IterNum = spCount;

			rootColumnNum = uniVessel.Count();
			rootLB = tempRes.val;

			rootCplexTime = CplexTime;
			rootLSTime = LSTime;

			LB = tempRes.val;

			if (tempRes.ptns != null)
			{
				if (tempRes.ptns.Count() == N)
				{
					if (tempRes.Item2 < UB)
					{
						UB = tempRes.Item2;
						rootUB = UB;
					}
				}
			}



			var rootInteger = MPsolverInteger(uniVessel.ToList());

			if (rootInteger.Item1 != null)
			{
				rootUB = rootInteger.Item2;
				if (rootInteger.Item2 < UB)
				{
					UB = rootInteger.Item2;
					feasibleSol.Clear();
					feasibleSol.AddRange(rootInteger.Item1.Select(x => new Vessel(x)).ToList());
					if ((UB - LB) / UB <= 0.001)
					{
						return (rootInteger.Item1.Select(x => new Vessel(x)).ToList(), UB);
					}
				}

				else
				{
					if ((UB - LB) / UB <= 0.001)
					{
						if (tempRes.ptns != null)
						{
							if (tempRes.ptns.Count() == N)
							{
								rootUB = UB;
								return (tempRes.Item1.Select(x => new Vessel(x)).ToList(), tempRes.val);
							}
							else
							{
								rootUB = UB;
								return (feasibleSol.Select(x => new Vessel(x)).ToList(), UB);
							}
						}
					}

				}
			}


			if (tempRes.ptns == null && (end - now).TotalSeconds >= 7199)
			{
				Console.WriteLine("Feas val " + UB);
				Console.WriteLine("overall runtime " + 7200);
				Console.WriteLine("root LS time: " + rootLSTime);
				Console.WriteLine("root Cplex time " + rootCplexTime);
				Console.WriteLine("root UB " + rootUB);
				Console.WriteLine("root LB " + rootLB);
				Console.WriteLine("IterNum " + IterNum);
				Console.WriteLine("root ColumnNum " + rootColumnNum);
				Console.WriteLine("root Time " + rootNodeTime);

				Console.WriteLine("Node Num total " + NodeNum);

				foreach (var vessel in feasibleSol)
				{
					Console.WriteLine("==========================" + vessel.id + "===========================");
					Console.WriteLine("vessel" + vessel.id + "'s yard assignment");
					for (int k = 0; k < K; k++)
					{
						for (int r = 0; r < R; r++)
						{
							for (int t = 0; t < T; t++)
							{
								if (vessel.x[k, r, t] == 1)
								{
									Console.WriteLine(k + " " + r + " " + t);
								}
							}
						}
					}

					Console.WriteLine("vessel" + vessel.id + "'s berth allocation");
					for (int b = 0; b < W; b++)
					{
						for (int t = 0; t < T; t++)
						{
							if (vessel.beta[b, t] == 1)
							{
								Console.WriteLine(b + " " + t);
							}
						}
					}
				}
				return (null, int.MaxValue);
			}



			Console.WriteLine("   ");

			tempRes = MPsolver(uniVessel.ToList());

			//DivingHeuristic(uniVessel.ToList(), tempRes.ptns, tempRes.val);

			Console.WriteLine();
			Stack<TreeNode> pq = new Stack<TreeNode>();
			pq.Push(new TreeNode(uniVessel.ToList(), tempRes.ptns, sumcost(tempRes.ptns), null));

			while (pq.Count > 0)
			{
				TreeNode currentParent = pq.Pop();
				List<Vessel> currentRes = currentParent.Cur_resultList;
				List<Vessel> currentPond = currentParent.Pond;
				List<inherit_info> currentConstraints = currentParent.Constraints_add;

				if (currentParent.Cost - UB > RC_EPS || currentRes.Count == 0 || currentParent.Cost <= 0) 
				{
					continue;
				}

				if (currentRes.Count == N)
				{
					if (currentParent.Cost < UB)
					{
						UB = currentParent.Cost;

						if (UB - LB < 1 - RC_EPS)
						{
							Console.WriteLine("找到最优解");
							FinalRes = currentRes.Select(x => new Vessel(x)).ToList();
							return (FinalRes, currentParent.Cost);
						}

						continue;
					}
				}

				Array.Clear(xTemp, 0, xTemp.Length);

				Dictionary<int[], double> rfDic = new Dictionary<int[], double>(new IntArrayComparer());

				for (int p = 0; p < currentRes.Count; p++)
				{
					for (int i = 1; i <= N; i++)
					{
						for (int k = 0; k < K; k++)
						{
							for (int r = 0; r < R; r++)
							{
								for (int t = 0; t < T; t++)
								{
									if (currentRes[p].id == i)
									{
										if (rfDic.ContainsKey(new int[] { i, k, r, t }))
										{
											rfDic[new int[] { i, k, r, t }] += currentRes[p].lambda * (currentRes[p].alpha[k, r, t]);
										}

										else
										{
											rfDic.Add(new int[] { i, k, r, t }, currentRes[p].lambda * currentRes[p].alpha[k, r, t]);
										}
									}
								}
							}

						}
					}
				}

				
				List<KeyValuePair<int[], double>> sortedList = rfDic.ToList();

				
				sortedList.Sort((pair1, pair2) =>
				{
					double diff1 = Math.Abs(pair1.Value - 0.5);
					double diff2 = Math.Abs(pair2.Value - 0.5);
					return diff1.CompareTo(diff2);
				});
				Console.WriteLine();
				//branchXIdx = sortedList.First().Key;
				//double val = sortedList.First().Value;

				List<int[]> bracnchTemp = sortedList
	.Where(pair => pair.Value != 0)
	.Select(pair => pair.Key)
	.ToList();
				branchXIdx = bracnchTemp.First();

				if (strongFirstFlag || sortedList.First().Value == 0)
				{
					rfDic = new Dictionary<int[], double>(new IntArrayComparer());
					for (int p = 0; p < currentRes.Count; p++)
					{
						for (int i = 1; i <= N; i++)
						{
							for (int b = 0; b < W; b++)
							{
								if (currentRes[p].id == i)
								{
									if (rfDic.ContainsKey(new int[] { i, b, b }))
									{
										rfDic[new int[] { i, b, b }] += currentRes[p].lambda * (currentRes[p].p[b]);
									}

									else
									{
										rfDic.Add(new int[] { i, b, b }, currentRes[p].lambda * currentRes[p].p[b]);
									}
								}

							}
						}
					}

					sortedList = rfDic.ToList();

					
					sortedList.Sort((pair1, pair2) =>
					{
						double diff1 = Math.Abs(pair1.Value - 0.5);
						double diff2 = Math.Abs(pair2.Value - 0.5);
						return diff1.CompareTo(diff2);
					});

					//branchXIdx = sortedList.First().Key;
					//val = sortedList.First().Value;

					bracnchTemp.AddRange(sortedList.Where(pair => pair.Value != 0).Select(pair => pair.Key).ToList());
					branchXIdx = bracnchTemp.First();
				}

				if (strongFirstFlag = true)
				{
					branchXIdx = strongBranch(bracnchTemp, currentPond, currentParent.Cost);
					//strongFirstFlag = false;
				}

				//List<inherit_info> leftBranch = currentConstraints.Select(p => new inherit_info(p)).ToList();
				List<inherit_info> leftBranch = currentConstraints?.Select(p => new inherit_info(p)).ToList() ?? new List<inherit_info>();
				//List<inherit_info> rightBranch = currentConstraints.Select(p => new inherit_info(p)).ToList();
				List<inherit_info> rightBranch = currentConstraints?.Select(p => new inherit_info(p)).ToList() ?? new List<inherit_info>();


				leftBranch.Add(new inherit_info(branchXIdx, 0));
				rightBranch.Add(new inherit_info(branchXIdx, 1));



				List<Vessel> leftPool = new List<Vessel>();
				List<Vessel> rightPool = new List<Vessel>();

				foreach (Vessel l in currentPond)
				{
					if (l.id == branchXIdx[0])
					{
						if (branchXIdx.Length == 4)
						{
							if (l.alpha[branchXIdx[1], branchXIdx[2], branchXIdx[3]] == 0)
							{
								Console.WriteLine(l.alpha[branchXIdx[1], branchXIdx[2], branchXIdx[3]]);
								leftPool.Add(new Vessel(l));
							}
						}
						else if (branchXIdx.Length == 3)
						{
							if (l.p[branchXIdx[1]] == 0)
							{
								leftPool.Add(new Vessel(l));
							}
						}
					}
					if (l.id != branchXIdx[0])
					{
						leftPool.Add(new Vessel(l));
					}
				}

				Console.WriteLine("=====================================================");

				foreach (Vessel l in currentPond)
				{
					if (l.id == branchXIdx[0])
					{
						if (branchXIdx.Length == 4)
						{
							if (l.alpha[branchXIdx[1], branchXIdx[2], branchXIdx[3]] == 1)
							{
								//Console.WriteLine(l.No);
								rightPool.Add(new Vessel(l));
							}
						}
						else if (branchXIdx.Length == 3)
						{
							if (l.p[branchXIdx[1]] == 1)
							{
								rightPool.Add(new Vessel(l));
							}
						}
					}

					if (l.id != branchXIdx[0])
					{
						rightPool.Add(new Vessel(l));
					}

				}

				HashSet<Vessel> leftPoolSet = new HashSet<Vessel>(leftPool);

				int testt = 0;
				foreach (var item in currentPond)
				{
					if (item.id == 1)
					{
						if (item.p[W - 1] == 0)
						{
							testt++;
						}

					}
				}
				Console.WriteLine();
				int testtt = 0;
				foreach (var item in currentPond)
				{
					if (item.id != 1)
					{

						testtt++;
					}
				}
				Console.WriteLine();




				var leftRes = ColumnGeneration(templateVessel, leftPoolSet, leftBranch);
				//DivingHeuristic(leftPoolSet.ToList(), leftRes.ptns, leftRes.val);


				TreeNode leftChild = new TreeNode();

				if (leftRes.ptns == null)
				{
					goto CalRightBranchLabel;
				}

				var leftUB = MPsolverInteger(leftPoolSet.ToList());
				if (leftUB.Item2 < UB)
				{
					UB = leftUB.Item2;
					feasibleSol.Clear();
					feasibleSol.AddRange(leftUB.Item1.Select(x => new Vessel(x)).ToList());
				}

				leftRes = MPsolver(leftPoolSet.ToList());

				if (leftRes.ptns.Count == N)
				{
					if (UB >= leftRes.val - RC_EPS)
					{
						UB = leftRes.val;
						feasibleSol.Clear();

						feasibleSol.AddRange(leftRes.ptns.Select(x => new Vessel(x)).ToList());

						if ((UB - LB) / UB <= 0.001)//
						{
							return (leftRes.ptns.Select(x => new Vessel(x)).ToList(), leftRes.val);
						}
					}
				}

				leftChild = new TreeNode(leftPool, leftRes.ptns, sumcost(leftRes.ptns), leftBranch);
				NodeNum++;

			CalRightBranchLabel:

				HashSet<Vessel> rightPoolSet = new HashSet<Vessel>(rightPool);
				var rightRes = ColumnGeneration(templateVessel, rightPoolSet, rightBranch);
				//DivingHeuristic(rightPoolSet.ToList(), rightRes.ptns, rightRes.val);

				if (rightRes.ptns == null || rightRes.ptns.Count == 0)
				{
					if (leftChild.Cur_resultList != null || leftChild.Cur_resultList != null)
					{
						if (leftChild.Cost <= UB - 1 + RC_EPS)
						{
							pq.Push(leftChild);
						}

						continue;
					}
					else
					{
						Console.WriteLine("左右分支都无解");
						continue;
					}
				}

				TreeNode rightChild = new TreeNode();

				if (rightRes.ptns.Count == N)
				{
					if (UB >= rightRes.val - RC_EPS)
					{
						UB = rightRes.val;

						feasibleSol.Clear();

						feasibleSol.AddRange(rightRes.ptns.Select(x => new Vessel(x)).ToList());

						if ((UB - LB) / LB <= 0.001)
						{
							return (rightRes.ptns.Select(x => new Vessel(x)).ToList(), rightRes.val);
						}
					}
				}

				var rightUB = MPsolverInteger(rightPoolSet.ToList());
				if (rightUB.Item2 < UB)
				{
					UB = rightUB.Item2;
					feasibleSol.Clear();
					feasibleSol.AddRange(rightUB.Item1.Select(x => new Vessel(x)).ToList());
				}

				rightRes = MPsolver(rightPoolSet.ToList());

				rightChild = new TreeNode(rightPool, rightRes.ptns, sumcost(rightRes.ptns), rightBranch);

				NodeNum++;

				if (leftRes.ptns != null)
				{
					if (leftChild.Cost > rightChild.Cost)
					{
						if (leftChild.Cost <= UB - 1 + RC_EPS)
						{
							pq.Push(leftChild);
						}
						else
						{
							Console.WriteLine();
						}

						if (rightChild.Cur_resultList != null)
						{
							if (rightChild.Cost <= UB - 1 + RC_EPS)
								pq.Push(rightChild);
							else
							{
								Console.WriteLine();
							}
						}
						else
						{
							Console.WriteLine();
						}
					}
					else
					{
						if (rightChild.Cur_resultList != null)
						{
							if (rightChild.Cost <= UB + RC_EPS)
								pq.Push(rightChild);
							else
							{
								Console.WriteLine();
							}
						}
						else
						{
							Console.WriteLine();
						}

						if (leftChild.Cur_resultList != null)
						{
							if (leftChild.Cost <= UB + RC_EPS)
								pq.Push(leftChild);
							else
							{
								Console.WriteLine();
							}
						}
						else
						{
							Console.WriteLine();
						}
					}
				}
				else
				{
					if (rightChild.Cur_resultList != null)
					{
						if (rightChild.Cost <= UB - 1 + RC_EPS)
							pq.Push(rightChild);
						else
						{
							Console.WriteLine();
						}
					}
					else
					{
						Console.WriteLine();
					}
				}

				if (pq.Count > 0)
					LB = pq.Min(x => x.Cost);
			}

			Console.WriteLine("遍历了所有节点");

			return (null, UB);
		}


		static int[] strongBranch(List<int[]> idx, List<Vessel> pond, double parentCost)
		{

			double bestImprovement = double.MinValue;
			List<int> bestPos = new List<int>();

			for (int i = 0; i < idx.Count; i++)
			{
				var tempPond = pond.Select(x => new Vessel(x)).ToList();

				List<Vessel> strongLeft = new List<Vessel>();
				List<Vessel> strongRight = new List<Vessel>();

				strongLeft = getLeftPond(tempPond, idx[i]);
				strongRight = getRightPond(tempPond, idx[i]);
				double prevObj = parentCost;

				var leftStrongRes = MPsolver(strongLeft);
				double currentObjLeft = leftStrongRes.Item2;

				var rightStrongRes = MPsolver(strongRight);
				double currentObjRight = rightStrongRes.Item2;

				double improvement = Math.Min(currentObjLeft, currentObjRight) - prevObj;

				if (improvement > bestImprovement)
				{
					bestImprovement = improvement;
					bestPos = idx[i].ToList();
				}

			}

			return bestPos.ToArray();
		}

		static List<Vessel> getLeftPond(List<Vessel> Pond, int[] position)
		{
			List<Vessel> leftxpond = new List<Vessel>();
			foreach (Vessel l in Pond)
			{
				if (l.id == position[0])
				{
					if (position.Length == 4)
					{
						if (l.alpha[position[1], position[2], position[3]] == 0)
						{
							leftxpond.Add(new Vessel(l));
						}
					}
					else if (position.Length == 3)
					{
						if (l.p[position[1]] == 0)
						{
							leftxpond.Add(new Vessel(l));
						}
					}
				}

				if (l.id != position[0])
				{
					leftxpond.Add(new Vessel(l));
				}
			}
			return leftxpond;
		}

		static List<Vessel> getRightPond(List<Vessel> Pond, int[] position)//
		{
			List<Vessel> leftxpond = new List<Vessel>();
			foreach (Vessel l in Pond)
			{
				if (l.id == position[0])
				{
					if (position.Length == 4)
					{
						if (l.alpha[position[1], position[2], position[3]] == 1)
						{
							leftxpond.Add(new Vessel(l));
						}
					}
					else if (position.Length == 3)
					{
						if (l.p[position[1]] == 1)
						{
							leftxpond.Add(new Vessel(l));
						}
					}
				}

				if (l.id != position[0])
				{
					leftxpond.Add(new Vessel(l));
				}
			}
			return leftxpond;
		}

		static (List<Vessel>, List<int[]>) checkInitialSolution(List<Vessel> vs)
		{
			List<Vessel> conflictVessels = new List<Vessel>();
			HashSet<int> conflictVesselIdx = new HashSet<int>();
			List<int[]> conflictInfo = new List<int[]>();

			for (int r = 0; r < R; r++)
			{
				for (int t = 0; t < T; t++)
				{
					bool exist = false;

					for (int k = 0; k < K; k++)
					{
						for (int i = 0; i < N; i++)
						{
							if (vs[i].alpha[k, r, t] == 1)
							{
								if (exist == false)
								{
									exist = true;
									continue;
								}

								if (exist == true)
								{
									conflictVesselIdx.Add(i);
									conflictInfo.Add(new int[] { vs[i].id, k, r, t });
								}
							}
						}

					}
				}
			}

			for (int i = 0; i < conflictVesselIdx.Count; i++)
			{
				conflictVessels.Add(vs[conflictVesselIdx.ElementAt(i)]);
			}
			return (conflictVessels, conflictInfo);
		}

		static int[][] berthInterval = new int[W][];


		static List<Tuple<int, int>> checkBerthAvailableInterval(List<Vessel> vs)
		{
			List<Tuple<int, int>> availableTimeIntervals = new List<Tuple<int, int>>();

			for (int b = 0; b < W; b++)
			{
				berthInterval[b] = new int[T];
			}

			for (int b = 0; b < W; b++)
			{
				for (int t = 0; t < T; t++) 
				{
					for (int i = 0; i < vs.Count; i++) 
					{
						if (vs[i].beta[b, t] == 1) 
						{
							berthInterval[b][t] = 1;  
						}
					}
				}
			}

			
			int start = -1;
			for (int t = 0; t < T; t++) 
			{
				int berthUsedNum = 0;
				for (int b = 0; b < W; b++) 
				{
					if (berthInterval[b][t] == 1) 
					{
						berthUsedNum++;
					}
				}

				
				if (berthUsedNum <= W - 1)
				{
					if (start == -1)  
					{
						start = t;  
					}
				}
				else
				{
					if (start != -1)  
					{
						availableTimeIntervals.Add(new Tuple<int, int>(start, t - 1)); 
						start = -1;  
					}
				}
			}

			if (start != -1)
			{
				availableTimeIntervals.Add(new Tuple<int, int>(start, T - 1));
			}

			return availableTimeIntervals; 
		}
		

		public static void AssignByFinishTime(List<Vessel> vessels)
		{
			for (int i = 0; i < vessels.Count; i++)
			{
				vessels[i].w = -1;
				for (int b = 0; b < W; b++)
				{
					vessels[i].p[b] = 0;
					for (int t = 0; t < T; t++)
					{
						vessels[i].beta[b, t] = 0;
					}
				}
			}
			
			vessels.Sort((v1, v2) =>
			{
				int primary = v1.C.CompareTo(v2.C);
				if (primary == 0)
					return v1.S.CompareTo(v2.S);
				return primary;
			});

			
			var unassigned = new List<Vessel>(vessels);

			
			for (int berthNum = 0; berthNum < W; berthNum++)
			{
				
				if (unassigned.Count == 0) break;

				
				double lastFinish = double.MinValue; 

				for (int i = 0; i < unassigned.Count; i++)
				{
					var v = unassigned[i];
					
					if (v.S > lastFinish)
					{
						v.w = berthNum;
						lastFinish = v.C;
						v.p[berthNum] = 1;
						for (int t = v.S; t <= v.C; t++)
						{
							v.beta[berthNum, t] = 1;
						}
					}
				}

				unassigned.RemoveAll(v => v.w == berthNum);
			}
		}


		static List<inherit_info> getForbidAlphaList(List<Vessel> vs, int conflictVesselID, Vessel conflictVessel)
		{
			List<inherit_info> forbid = new List<inherit_info>();
			for (int i = 0; i < vs.Count; i++)
			{
				foreach (var k in vs[i].Ki)//
				{
					foreach (var r in Rk[k])
					{
						for (int t = vs[i].S; t <= vs[i].C; t++)
						{
							if (vs[i].alpha[k, r, t] == 1)
							{
								foreach (var kk in conflictVessel.Ki)
								{
									inherit_info info = new inherit_info(new int[] { conflictVesselID, kk, r, t }, 0);
									forbid.Add(info);
								}
							}
						}

					}
				}
			}

			return forbid;
		}

		static List<inherit_info> getForbidBetaList(List<Vessel> vs)
		{
			List<inherit_info> forbid = new List<inherit_info>();
			for (int i = 0; i < vs.Count; i++)
			{
				for (int w = 0; w < W; w++)
				{
					if (vs[i].beta[w, vs[i].S] == 1)
					{
						for (int t = vs[i].S; t <= vs[i].C; t++)
						{
							inherit_info info = new inherit_info(new int[] { w, t }, 0);
							forbid.Add(info);
						}
					}
				}
			}

			return forbid;
		}

		public static string conflictDataPath;

		public static double rootLB = 0;
		public static double rootUB = 0;
		public static double rootNodeTime = 0;
		public static int rootColumnNum = 0;
		public static double rootLSTime = 0;
		public static double LSTime = 0;
		public static double rootCplexTime = 0;
		public static double CplexTime = 0;
		public static int IterNum = 0;
		public static int NodeNum = 0;
		public static DateTime timer;
		public static DateTime initialSolBegin;
		static void Main(string[] args)
		{
			timer = DateTime.Now;
			conflictDataPath = "C:\\Users\\yang_\\Desktop\\conflictData144.txt";

			List<Vessel> initialVessels = ReadVesselData("C:\\Users\\yang_\\Desktop\\Data\\vesselData\\5\\hard\\vesselData3" +
				".txt");
			initialVessel(initialVessels, "C:\\Users\\yang_\\Desktop\\switchingCostData.txt", conflictDataPath);
			InitialNodes();

			List<Vessel> vessels = initialVessels.Select(x => new Vessel(x)).ToList();

			DateTime now = DateTime.Now;
			initialSolBegin = DateTime.Now;
			InitialSolution(vessels);
			vessels = vessels.OrderByDescending(v => v.a).ToList();
			var conflictVessels = checkInitialSolution(vessels);
			Console.WriteLine("=========================================");


			for (int i = 0; i < conflictVessels.Item1.Count; i++)
			{
				vessels.Remove(conflictVessels.Item1[i]);
			}

			while (conflictVessels.Item1.Count != 0)
			{
				var timeInterval = checkBerthAvailableInterval(vessels);

				var forbid = getForbidAlphaList(vessels, conflictVessels.Item1[0].id, conflictVessels.Item1[0]);

				forbid.AddRange(getForbidBetaList(vessels));

				Vessel temp = new Vessel(conflictVessels.Item1[0]);

				#region
				//int prevStartT = FindFirstIndexOfOne(temp.beta);

				//int newEndT = FindEndTimeAfterStart(timeInterval, prevStartT);

				//if (newEndT != -1)
				//{
				//	temp.a = prevStartT - 1;
				//	//temp.d = conflictVessels.Item1[0].d;
				//	//temp.g = conflictVessels.Item1[0].g;
				//	if (conflictVessels.Item1[0].d < newEndT)
				//	{
				//		temp.d = conflictVessels.Item1[0].d;
				//		temp.g = conflictVessels.Item1[0].g;
				//	}
				//	else
				//	{
				//		temp.d = newEndT;
				//		temp.g = int.MaxValue;
				//	}
				#endregion

				var tempRes = SubproblemCplex(temp, forbid);


				if (tempRes.resSP != null)
				{
					for (int k = 0; k < K; k++)
					{
						for (int r = 0; r < R; r++)
						{
							for (int t = 0; t < T; t++)
							{
								if (tempRes.resSP.alpha[k, r, t] == 1)
								{
									Console.WriteLine(k + " " + r + " " + t);
								}
							}
						}
					}
					
					//tempRes.resSP.a = conflictVessels.Item1[0].a;
					//tempRes.resSP.d = conflictVessels.Item1[0].d;
					//tempRes.resSP.g = conflictVessels.Item1[0].g;

					vessels.Add(tempRes.resSP);
					//AssignByFinishTime(vessels);

					conflictVessels.Item1.RemoveAt(0);
				}
			}

			ReAssignBerth(vessels);

			//如果对应的船

			vessels.Sort((x, y) => x.id.CompareTo(y.id));
			foreach (var item in vessels)
			{
				Console.WriteLine("=========" + item.id + "=========");
				for (int b = 0; b < W; b++)
				{
					for (int t = 0; t < T; t++)
					{
						if (item.beta[b, t] == 1)
						{
							Console.WriteLine(b + " " + t);
						}
					}
				}
				Console.WriteLine("========================");
			}

			Console.WriteLine();
			for (int i = 0; i < N; i++)
			{
				Console.WriteLine("==========================" + i + "===========================");
				for (int k = 0; k < K; k++)
				{
					for (int r = 0; r < R; r++)
					{
						for (int t = 0; t < T; t++)
						{
							if (vessels[i].alpha[k, r, t] == 1)
							{
								Console.WriteLine(k + " " + r + " " + t);
							}
						}
					}
				}
			}

			var conflictVessel = checkInitialSolution(vessels);

			if (conflictVessel.Item1.Count == 0)
			{
				(List<Vessel> bpRes, double bpObjVal) = BranchPrice(initialVessels, vessels);

				DateTime finish = DateTime.Now;
				Console.WriteLine("Opt val " + bpObjVal);
				Console.WriteLine("overall runtime " + (finish - now).TotalSeconds);
				Console.WriteLine("root LS time: " + rootLSTime);
				Console.WriteLine("root Cplex time " + rootCplexTime);
				Console.WriteLine("root UB " + rootUB);
				Console.WriteLine("root LB " + rootLB);
				Console.WriteLine("IterNum " + IterNum);
				Console.WriteLine("root ColumnNum " + rootColumnNum);
				Console.WriteLine("root Time " + rootNodeTime);

				Console.WriteLine("Node Num total " + NodeNum);

				if (bpRes == null)
				{
					if (Math.Abs(feasibleSol.Sum(x => x.cost) - UB) < RC_EPS)
					{
						foreach (var vessel in feasibleSol)
						{
							Console.WriteLine("==========================" + vessel.id + "===========================");
							Console.WriteLine("vessel" + vessel.id + "'s yard assignment");
							for (int k = 0; k < K; k++)
							{
								for (int r = 0; r < R; r++)
								{
									for (int t = 0; t < T; t++)
									{
										if (vessel.x[k, r, t] == 1)
										{
											Console.WriteLine(k + " " + r + " " + t);
										}
									}
								}
							}

							Console.WriteLine("vessel" + vessel.id + "'s berth allocation");
							for (int b = 0; b < W; b++)
							{
								for (int t = 0; t < T; t++)
								{
									if (vessel.beta[b, t] == 1)
									{
										Console.WriteLine(b + " " + t);
									}
								}
							}
						}
					}
				}

				foreach (var vessel in bpRes ?? feasibleSol)
				{
					Console.WriteLine("==========================" + vessel.id + "===========================");
					Console.WriteLine("vessel" + vessel.id + "'s yard assignment");
					for (int k = 0; k < K; k++)
					{
						for (int r = 0; r < R; r++)
						{
							for (int t = 0; t < T; t++)
							{
								if (vessel.x[k, r, t] == 1)
								{
									Console.WriteLine(k + " " + r + " " + t);
								}
							}
						}
					}

					Console.WriteLine("vessel" + vessel.id + "'s berth allocation");
					for (int b = 0; b < W; b++)
					{
						for (int t = 0; t < T; t++)
						{
							if (vessel.beta[b, t] == 1)
							{
								Console.WriteLine(b + " " + t);
							}
						}
					}
				}
				Console.WriteLine("Hello, World!");
			}
		}

		public static void ReAssignBerth(List<Vessel> vessels)
		{
			int[] berthTimes = new int[W];
			for (int i = 0; i < N; i++)
			{
				for (int w = 0; w < W; w++)
				{
					if (vessels[i].p[w] == 1)
					{
						berthTimes[w] += 1;
					}
				}
			}

			var mapping = berthTimes
		   .Select((value, index) => new { value, index })
		   .OrderByDescending(x => x.value)
		   .Select((x, newIndex) => new { OriginalIndex = x.index, NewIndex = newIndex })
		   .ToDictionary(x => x.OriginalIndex, x => x.NewIndex);


			for (int i = 0; i < N; i++)
			{
				for (int w = 0; w < W; w++)
				{
					if (vessels[i].p[w] == 1)
					{
						vessels[i].w = mapping[w];

						vessels[i].p[w] = 0;
						vessels[i].p[mapping[w]] = 1;

						for (int t = vessels[i].S; t <= vessels[i].C; t++)
						{
							vessels[i].beta[w, t] = 0;
							vessels[i].beta[mapping[w], t] = 1;
						}
						break;
					}
				}
			}
		}


		class IntArrayComparer : IEqualityComparer<int[]>
		{
			public bool Equals(int[] x, int[] y)
			{
				
				if (x == null || y == null)
					return x == y;

				if (x.Length != y.Length)
					return false;

				for (int i = 0; i < x.Length; i++)
				{
					if (x[i] != y[i])
						return false;
				}

				return true;
			}

			public int GetHashCode(int[] obj)
			{
				if (obj == null) return 0;
				int hash = 17;
				foreach (int val in obj)
				{
					hash = hash * 31 + val;
				}
				return hash;
			}
		}


		class ListComparer : IEqualityComparer<List<int>>
		{
			public bool Equals(List<int> x, List<int> y)
			{
				
				if (x == null && y == null)
					return true;
				if (x == null || y == null)
					return false;

				
				if (x.Count != y.Count)
					return false;

				
				var xSorted = x.OrderBy(i => i).ToList();
				var ySorted = y.OrderBy(i => i).ToList();
				return xSorted.SequenceEqual(ySorted);
			}

			public int GetHashCode(List<int> obj)
			{
				if (obj == null)
					return 0;

				
				int hash = 17;
				foreach (int item in obj.OrderBy(i => i))
				{
					hash = hash * 31 + item.GetHashCode();
				}
				return hash;
			}
		}
	}
}

