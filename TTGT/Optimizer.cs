using System;
using System.Collections.Generic;
using System.Threading;

using CudaCSharp.Linq;
using CudaCSharp.Tensor;


namespace TTGT.Optimizer
{
	/// <summary>
	/// The optimizer for <see cref="ContractionPlan"/>
	/// </summary>
	internal static class Optimizer
	{
		private static readonly Random rand = new Random();

		internal static ContractionPlan Optimize(in ContractionInput input)
		{
			if (Math.Max(input.LeftSize.Length, input.RightSize.Length) <= BruteMax)
				return BruteForceOptimize(input);
			// use multi-thread MA
			ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);
			List<ContractionPlan> bestPlans = new List<ContractionPlan>();
			using var countDown = new CountdownEvent(Environment.ProcessorCount);
			void Runner(in ContractionInput input)
			{
				var p = MAOptimize(in input);
				if (p.HasValue)
					bestPlans.Add(p.Value);
				countDown.Signal();
			}
			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				ThreadPool.QueueUserWorkItem(x => Runner((ContractionInput)x), input);
			}
			// wait for complete
			countDown.Wait();
			return bestPlans.MinBy(p => p.EstimationTime ?? double.MaxValue);
		}

		#region MA
		#region struct
		private readonly struct SimplePlan
		{
			internal readonly int[] leftPerm, rightPerm;
			internal readonly bool swap;
			internal readonly double fitness, breachScore;

			internal SimplePlan(int[] leftPerm, int[] rightPerm, bool swap, in ContractionInput input)
			{
				this.leftPerm = leftPerm; this.rightPerm = rightPerm;
				this.swap = swap;
				var (plan, violationValue) = ContractionPlan.CreateAllowViolation(leftPerm, rightPerm, swap, input);
				if (plan.HasValue)
				{
					this.fitness = plan.Value.EstimationTime.Value;
					this.breachScore = 0;
				}
				else
				{
					this.fitness = violationValue.Value.cost + violationValue.Value.breach;
					this.breachScore = violationValue.Value.breach;
				}
			}

			internal double CalcFitness(in ContractionInput input, bool? swap = null)
			{
				var (plan, violationValue) = ContractionPlan.CreateAllowViolation(this.leftPerm, this.rightPerm, swap ?? this.swap, input);
				if (plan.HasValue)
					return plan.Value.EstimationTime.Value;
				else
					return violationValue.Value.cost + violationValue.Value.breach;
			}

			public static bool operator ==(SimplePlan lhs, SimplePlan rhs)
			{
				if ((lhs.leftPerm is null) != (rhs.leftPerm is null) || (lhs.rightPerm is null) != (rhs.rightPerm is null))
					return false;
				return (lhs.leftPerm == rhs.leftPerm || lhs.leftPerm.SequenceEqual(rhs.leftPerm)) &&
						(lhs.rightPerm == rhs.rightPerm || lhs.rightPerm.SequenceEqual(rhs.rightPerm)) &&
						lhs.swap == rhs.swap;
			}
			public static bool operator !=(SimplePlan lhs, SimplePlan rhs) => !(lhs == rhs);

			public override bool Equals(object obj) => obj is SimplePlan p && this == p;
			public override int GetHashCode() => HashCode.Combine(this.leftPerm.HashCodeOfArray(), this.rightPerm.HashCodeOfArray(), this.swap);
		}
		#endregion

		#region main
		private const double ProbLocalSearch = 0.7;
		private const int ChildEachGeneration = 100, MaxNoImproveBeforeRestart = 4;

		/// <summary>
		/// The memetic algorithm (MA) optimizer for <see cref="ContractionPlan"/>
		/// </summary>
		/// <remarks>In computer science and operations research, a memetic algorithm (MA) is an extension of the traditional genetic algorithm. It uses a local search technique to reduce the likelihood of the premature convergence.</remarks>
		internal static ContractionPlan? MAOptimize(in ContractionInput input, int maxIter = 100)
		{
			var pop = InitPopulation(in input);
			SimplePlan? nonBreachBest = null;
			int noImprove = 0;
			for (int iter = 0; iter < maxIter; iter++)
			{
				pop.RemoveRange(PopulationSize, pop.Count - PopulationSize);
				if (noImprove >= MaxNoImproveBeforeRestart)
				{ // restart preserving only half of the size
					pop.RemoveRange(PopulationSize / 2, pop.Count - PopulationSize / 2);
					pop.AddRange(InitPopulation(in input));
					noImprove = 0;
				}
				for (int i = 0; i < ChildEachGeneration; i++)
				{
					var parents = SelectParentsByFitness(pop);
					var child = CrossoverGenerateChild(parents, in input);
					if (rand.NextDouble() < ProbLocalSearch)
						child = child.LocalSearch(in input);
					if (!pop.Contains(child))
						pop.Add(child);
				}
				pop.StochasticSort();
				var currentBest = pop.Where(p => p.breachScore == 0).MinBy(p => p.fitness);
				if (currentBest != default && (!nonBreachBest.HasValue || nonBreachBest.Value.fitness > currentBest.fitness))
					nonBreachBest = currentBest;
				if (currentBest != default && nonBreachBest.HasValue && currentBest.fitness >= nonBreachBest.Value.fitness)
					noImprove++;
			}
			var best = pop.Where(p => p.breachScore == 0).MinBy(p => p.fitness);
			if (best == default || (nonBreachBest.HasValue && best.fitness > nonBreachBest.Value.fitness))
				best = nonBreachBest.Value;
			if (best == default) // still no non-breach
				return null;
			else
				return ContractionPlan.CreateAllowViolation(best.leftPerm, best.rightPerm, best.swap, in input).plan.Value;
		}
		#endregion

		#region sort
		private const double ProbSort = 0.45, SortRatio = 0.8;

		private static void StochasticSort(this List<SimplePlan> pop)
		{
			int nSwap = 1;
			for (int iter = 0; iter < pop.Count * SortRatio && nSwap > 0; iter++)
			{
				nSwap = 0;
				for (int i = 0; i < pop.Count - 1; i++)
				{
					double b1 = pop[i].breachScore, b2 = pop[i + 1].breachScore;
					double f1 = pop[i].fitness - b1, f2 = pop[i + 1].fitness - b2;
					if (((b1 == 0 && b2 == 0) || rand.NextDouble() < ProbSort) && f1 > f2)
					{
						nSwap++; (pop[i], pop[i + 1]) = (pop[i + 1], pop[i]);
					}
					else if (b1 > b2)
					{
						nSwap++; (pop[i], pop[i + 1]) = (pop[i + 1], pop[i]);
					}
				}
			}
		}
		#endregion

		#region local search
		private static SimplePlan LocalSearch(this SimplePlan plan, in ContractionInput input)
		{
			// greedy search
			double bestImprove = 1;
			while (bestImprove > 0)
			{
				double newFitness, oldFitness = plan.fitness;
				bestImprove = 0;
				bool bestLeft = true;
				int bestI = 0, bestJ = 1;
				// check left first
				for (int i = 0; i < plan.leftPerm.Length; i++)
				{
					for (int j = 0; j < plan.leftPerm.Length; j++)
					{
						if (i == j) continue;
						// temp swap and calculate fitness
						(plan.leftPerm[i], plan.leftPerm[j]) = (plan.leftPerm[j], plan.leftPerm[i]);
						newFitness = plan.CalcFitness(in input);
						(plan.leftPerm[i], plan.leftPerm[j]) = (plan.leftPerm[j], plan.leftPerm[i]);
						if (oldFitness - newFitness > bestImprove)
						{
							bestImprove = oldFitness - newFitness;
							bestI = i; bestJ = j;
						}
					}
				}
				// check right
				for (int i = 0; i < plan.rightPerm.Length; i++)
				{
					for (int j = 0; j < plan.rightPerm.Length; j++)
					{
						if (i == j) continue;
						// temp swap and calculate fitness
						(plan.rightPerm[i], plan.rightPerm[j]) = (plan.rightPerm[j], plan.rightPerm[i]);
						newFitness = plan.CalcFitness(in input);
						(plan.rightPerm[i], plan.rightPerm[j]) = (plan.rightPerm[j], plan.rightPerm[i]);
						if (oldFitness - newFitness > bestImprove)
						{
							bestImprove = oldFitness - newFitness;
							bestI = i; bestJ = j; bestLeft = false;
						}
					}
				}
				// check swap and continue
				newFitness = plan.CalcFitness(in input, !plan.swap);
				if (oldFitness - newFitness > bestImprove)
				{
					bestImprove = oldFitness - newFitness;
					plan = new SimplePlan(plan.leftPerm, plan.rightPerm, !plan.swap, in input);
				}
				else
				{
					int[] perm = (bestLeft ? plan.leftPerm : plan.rightPerm).Clone() as int[];
					(perm[bestI], perm[bestJ]) = (perm[bestJ], perm[bestI]);
					plan = new SimplePlan(bestLeft ? perm : plan.leftPerm, bestLeft ? plan.rightPerm : perm, plan.swap, in input);
				}
			}
			return plan;
		}
		#endregion

		#region crossover
		private static SimplePlan CrossoverGenerateChild((SimplePlan, SimplePlan) parents, in ContractionInput input)
		{
			List<int> child1L = new List<int>(parents.Item1.leftPerm), child1R = new List<int>(parents.Item1.rightPerm);
			List<int> child2L = new List<int>(parents.Item2.leftPerm), child2R = new List<int>(parents.Item2.rightPerm);

			int point1L = rand.Next(parents.Item1.leftPerm.Length), point1R = rand.Next(parents.Item1.rightPerm.Length);
			int point2L = rand.Next(parents.Item1.leftPerm.Length), point2R = rand.Next(parents.Item1.rightPerm.Length);

			int lengthL = point2L - point1L, lengthR = point2R - point1R;
			if (lengthL < 0)
				lengthL += parents.Item1.leftPerm.Length;
			if (lengthR < 0)
				lengthR += parents.Item1.rightPerm.Length;

			// crossover left
			Dictionary<int, int> mapping1 = new Dictionary<int, int>(lengthL * 2);
			Dictionary<int, int> mapping2 = new Dictionary<int, int>(lengthL * 2);
			for (int i = 0; i < lengthL; i++)
			{
				int index = (i + point1L) % parents.Item1.leftPerm.Length;
				int item1 = child1L[index];
				int item2 = child2L[index];
				child1L[index] = item2;
				child2L[index] = item1;
				mapping1[item1] = item2;
				mapping2[item2] = item1;
			}
			CheckUnmappedElements(child1L, mapping2, point1L, point2L);
			CheckUnmappedElements(child2L, mapping1, point1L, point2L);

			// crossover right
			mapping1 = new Dictionary<int, int>(lengthR * 2);
			mapping2 = new Dictionary<int, int>(lengthR * 2);
			for (int i = 0; i < lengthR; i++)
			{
				int index = (i + point1R) % parents.Item1.rightPerm.Length;
				int item1 = child1R[index];
				int item2 = child2R[index];
				child1R[index] = item2;
				child2R[index] = item1;
				mapping1[item1] = item2;
				mapping2[item2] = item1;
			}
			CheckUnmappedElements(child1R, mapping2, point1R, point2R);
			CheckUnmappedElements(child2R, mapping1, point1R, point2R);

			// return
			bool childSwap = parents.Item1.swap == parents.Item2.swap ? parents.Item1.swap : rand.Next(0, 2) == 0;
			return new SimplePlan(child1L.ToArray(), child1R.ToArray(), childSwap, in input);
		}
		private static void CheckUnmappedElements(List<int> offspring, IDictionary<int, int> mapping, int mappingStart, int mappingEnd)
		{
			for (int i = 0; i < offspring.Count; i++)
			{
				if (!InsideMappedRegion(i, mappingStart, mappingEnd))
				{
					int mapped = offspring[i];
					while (mapping.ContainsKey(mapped))
					{
						mapped = mapping[mapped];
					}
					offspring[i] = mapped;
				}
			}
		}
		private static bool InsideMappedRegion(int position, int startPoint, int endPoint)
		{
			bool enclosed = position < endPoint && position >= startPoint;
			bool wrapAround = startPoint > endPoint && (position >= startPoint || position < endPoint);
			return enclosed || wrapAround;
		}

		private static (SimplePlan, SimplePlan) SelectParentsByFitness(List<SimplePlan> pop)
		{
			var fitnessAccu = pop.Select(p => 1 / p.fitness).AccumulateSum().Skip(1);
			var r = rand.NextDouble() * fitnessAccu[^1];
			int firstInd = 0;
			for (int i = 0; i < pop.Count; i++)
			{
				if (r < fitnessAccu[i])
				{
					firstInd = i; break;
				}
			}
			int secondInd = firstInd;
			while (secondInd == firstInd)
			{
				r = rand.NextDouble() * fitnessAccu[^1];
				for (int i = 0; i < pop.Count; i++)
				{
					if (r < fitnessAccu[i])
					{
						secondInd = i; break;
					}
				}
			}
			return (pop[firstInd], pop[secondInd]);
		}
		#endregion

		#region init
		private const int PopulationSize = 30;

		private static List<SimplePlan> InitPopulation(in ContractionInput input)
		{
			var leftIdentity = ArrayLinq.Range(0, input.LeftSize.Length);
			var rightIdentity = ArrayLinq.Range(0, input.RightSize.Length);
			var initLeftPerm = ArrayLinq.Repeat(leftIdentity, PopulationSize).Select(a => a.Shuffle());
			var initRightPerm = ArrayLinq.Repeat(rightIdentity, PopulationSize).Select(a => a.Shuffle());
			var initSwap = ArrayLinq.Repeat(false, PopulationSize).Select(a => rand.Next(0, 2) == 0);
			var population = new List<SimplePlan>(PopulationSize);
			for (int i = 0; i < initLeftPerm.Count; i++)
			{
				population.Add(new SimplePlan(initLeftPerm[i].ToArray(), initRightPerm[i].ToArray(), initSwap[i], in input));
			}
			return population;
		}
		#endregion
		#endregion

		#region brute force
		private const int BruteMax = 6;

		internal static ContractionPlan BruteForceOptimize(ContractionInput input)
		{
			var contractPerms = input.LeftContractIndex.Zip(input.RightContractIndex, (l, r) => (l, r)).ToArray().GeneratePermutations();
			var leftFreePerms = input.LeftFreeIndex.ToArray().GeneratePermutations();
			var rightFreePerms = input.RightFreeIndex.ToArray().GeneratePermutations();
			IReadOnlyList<bool> transL = new[] { false, true }, transR = new[] { false, true }, swap = new[] { false, true };
			ContractionPlan bestPlan = default;
			for (int ic = 0; ic < contractPerms.Count; ic++)
				for (int il = 0; il < leftFreePerms.Count; il++)
					for (int ir = 0; ir < rightFreePerms.Count; ir++)
						for (int itl = 0; itl < transL.Count; itl++)
							for (int itr = 0; itr < transR.Count; itr++) 
								for (int iis = 0; iis < swap.Count; iis++)
								{
									var c = contractPerms[ic];
									int[] l = leftFreePerms[il], r = rightFreePerms[ir];
									bool tl = transL[itl], tr = transR[itr], s = swap[iis];
									var plan = ContractionPlan.CreateAllowViolation(
												leftPerm: (tl ? c.Select(cc => cc.l).Concat(l) : l.Concat(c.Select(cc => cc.l))).ToArray(),
												rightPerm: (tr ? r.Concat(c.Select(cc => cc.r)) : c.Select(cc => cc.r).Concat(r)).ToArray(),
												swap: s, input: input);
									var actualPlan = plan.plan.Value; // must be a valid plan
									if (!bestPlan.EstimationTime.HasValue || actualPlan.EstimationTime.Value < bestPlan.EstimationTime.Value)
										bestPlan = actualPlan;
								}
			return bestPlan;
		}

		internal static List<T[]> GeneratePermutations<T>(this T[] set)
		{
			var list = new List<T[]>(ArrayLinq.Range(1, set.Length).Prod());
			GeneratePermutations(list, set, 0);
			return list;
		}

		private static void GeneratePermutations<T>(List<T[]> list, T[] set, int l)
		{
			if (l == set.Length - 1)
				list.Add(set.Clone() as T[]);
			else
			{
				for (int i = l; i < set.Length; i++)
				{
					(set[l], set[i]) = (set[i], set[l]);
					GeneratePermutations(list, set, l + 1); // In-order Traversal
					(set[l], set[i]) = (set[i], set[l]);
				}
			}
		}
		#endregion
	}
}
