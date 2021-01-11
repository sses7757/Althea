using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Array;


namespace Althea.Tensor
{
	/// <summary>
	/// The Tensor API library wrapper and some static method for 
	/// </summary>
	public static class API
	{
		#region base
		/// <summary>
		/// Static class initializer
		/// </summary>
		static API()
		{
			if (GlobalSettings.TensorGPU != null)
				GPUconstructor = GlobalSettings.TensorGPU.GetConstructor(Array.Empty<Type>());
			else
				GPUconstructor = typeof(Cuda.CudaTensor).GetConstructor(Array.Empty<Type>());
			if (GlobalSettings.TensorCPU != null)
				CPUconstructor = GlobalSettings.TensorCPU.GetConstructor(Array.Empty<Type>());
			else
				CPUconstructor = typeof(Mkl.MklTensor).GetConstructor(Array.Empty<Type>());
			Initialize();
		}

		/// <summary>
		/// Reset the Tensor libraries
		/// </summary>
		public static void Reset()
		{
			try
			{
				GPU.Dispose();
				CPU.Dispose();
			}
			catch (StatusException e)
			{
				Log.Write($"Error at reseting Tensor library \"{e.Message}\":" + Environment.NewLine + e.StackTrace, level: LogLevel.Error);
			}
			finally
			{
				Initialize();
			}
		}

		/// <summary>
		/// Singleton Tensor API of GPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static ITensor GPU => _GPUInit.Value;

		/// <summary>
		/// Singleton Tensor API of CPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static ITensor CPU => _CPUInit.Value;

		private static readonly System.Reflection.ConstructorInfo GPUconstructor, CPUconstructor;

		private static Lazy<ITensor> _GPUInit, _CPUInit;

		private static void Initialize()
		{
			_GPUInit = new Lazy<ITensor>(() => GPUconstructor.Invoke(Array.Empty<object>()) as ITensor, true);
			_CPUInit = new Lazy<ITensor>(() => CPUconstructor.Invoke(Array.Empty<object>()) as ITensor, true);
		}
		#endregion


		#region permute check
		/// <summary>
		/// Check sizes and labels of permutation $B_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.
		/// </summary>
		/// <typeparam name="TTen">tensor type</typeparam>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="A">the tensor A</param>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="newOrder">the permute order of <paramref name="A"/> as <see cref="TensorOrder"/></param>
		/// <param name="B">the tensor B</param>
		/// <param name="order">the permute order from <paramref name="A"/> to <paramref name="B"/></param>
		public static void PermuteCheck<TTen, T>(TTen A, T α, TensorOrder newOrder, TTen B, Span<int> order)
			where TTen : PureArray<T>, Althea.ITensor
			where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (α.IsZero())
				throw new ArgumentOutOfRangeException(nameof(α), Resource.ParaCannotZero);

			newOrder.GetIntSpanOrder(A, order, out _, allowPartial: false);
			Span<long> sizeB = stackalloc long[order.Length];
			A.Size.ReOrderTo(sizeB, order);
			if (!sizeB.SequenceEqual(B.Size))
				throw new ArgumentException(Resource.TensorWrongSize, nameof(B));
		}
		#endregion

		#region reduce check
		/// <summary>
		/// Check sizes and labels of reduction $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$.
		/// </summary>
		/// <typeparam name="TTen">tensor type</typeparam>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="α">the scalar to multiply <paramref name="A"/></param>
		/// <param name="A">the input tensor <typeparamref name="TTen"/> to be reduced</param>
		/// <param name="β">the scalar to multiply <paramref name="C"/></param>
		/// <param name="C">the tensor <typeparamref name="TTen"/> to be added at last</param>
		/// <param name="opC">the <see cref="UnitaryOperation"/> on <paramref name="C"/> before addition</param>
		/// <param name="D">the tensor <typeparamref name="TTen"/> to be overwritten</param>
		/// <param name="permA2C">the permutation order from <paramref name="A"/> to <paramref name="C"/></param>
		public static void ReduceCheck<TTen, T>(T α, TTen A, T β, ref TTen C, ref UnitaryOperation opC, ref TTen D, Span<int> permA2C)
			where TTen : PureArray<T>, Althea.ITensor
			where T : struct, IComparable<T>
		{
			if (α.IsZero())
				throw new ArgumentOutOfRangeException(nameof(α), Resource.ParaCannotZero);
			if (D is null)
				D = C;
			if (β.IsZero() || C is null || C == PureArray<T>.EmptyDnTen)
			{
				C = D;
				opC = UnitaryOperation.Identity;
			}
			if (A is null || A == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (C is null || C == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);
			if (D is null || D == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(D), Resource.ArrayCannotNull);
			if (!D.Size.SequenceEqual(C.Size))
				throw new ArgumentException(Resource.TensorWrongSize, nameof(D));
			if (!D.Label.SequenceEqual(C.Label))
				throw new ArgumentException(Resource.TensorWrongIndex, nameof(D));

			// check unique
			Span<int> findC = stackalloc int[C.Rank];
			for (int i = 0; i < C.Rank; i++)
			{
				var ind = A.Label.IndexOf(C.Label[i]);
				if (ind < 0)
					throw new ArgumentException(Resource.TensorWrongIndex, nameof(C));
				findC[i] = ind;
			}
			// find permutation
			A.Label.FindPermutationTo(C.Label, permA2C);
		}
		#endregion

		#region contract checks
		private static void GetContractionOutputSize(IReadOnlyList<char> leftLabel, IReadOnlyList<long> leftSize, IReadOnlyList<char> rightLabel, IReadOnlyList<long> rightSize, char[] outputLabel, long[] outputSize)
		{
			int now = 0;
			for (int i = 0; i < leftLabel.Count; i++)
			{
				var ind = rightLabel.IndexOf(leftLabel[i]);
				if (ind >= 0)
				{
					if (rightSize[ind] != leftSize[i])
						throw new ArgumentOutOfRangeException(nameof(rightSize));
				}
				else
				{
					outputLabel[now] = leftLabel[i]; outputSize[now++] = leftSize[i];
				}
			}
			for (int i = 0; i < rightLabel.Count; i++)
			{
				var ind = leftLabel.IndexOf(rightLabel[i]);
				if (ind >= 0)
				{
					if (leftSize[ind] != rightSize[i])
						throw new ArgumentOutOfRangeException(nameof(leftSize));
				}
				else
				{
					outputLabel[now] = rightLabel[i]; outputSize[now++] = rightSize[i];
				}
			}
		}

		/// <summary>
		/// Check the sizes and labels of two tensors about to contract
		/// </summary>
		/// <typeparam name="TTen">tensor type</typeparam>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="left">left tensor</param>
		/// <param name="right">right tensor</param>
		/// <param name="commonRank">output, number of common indices</param>
		/// <returns>if cannot contract, throws errors, otherwise, returns the label and size of output tensor</returns>
		public static (char[] label, long[] size) OutOfPlaceContractCheck<TTen, T>(T α, TTen left, TTen right, out int commonRank)
			where TTen : PureArray<T>, Althea.ITensor
			where T : struct, IComparable<T>
		{
			// check
			if (α.IsZero())
				throw new ArgumentOutOfRangeException(nameof(α), Resource.ParaCannotZero);
			if (left is null || left == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (right is null || right == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (left.OnHost != right.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);

			// get common length
			commonRank = GetContractRank(left, right);

			// get label and size
			int newRank = left.Rank + right.Rank - commonRank * 2;
			char[] newLabel = new char[newRank];
			long[] newSize = new long[newRank];
			try
			{
				// check if all the common legs have same size
				GetContractionOutputSize(left.Label, left.Size, right.Label, right.Size, newLabel, newSize);
			}
			catch (ArgumentOutOfRangeException e)
			{
				throw new ArgumentException(Resource.TensorWrongSize, e);
			}
			// if result is a scalar
			if (newLabel.Length == 0)
			{
				newSize = new[] { 1L };
				newLabel = new[] { (char)(Math.Max(left.Label.Max(), right.Label.Max()) + 1) };
			}

			// return
			return (newLabel, newSize);
		}
		private static int GetContractRank(Althea.ITensor A, Althea.ITensor B)
		{
			int commonRank = 0;
			for (int i = 0; i < A.Rank; i++)
			{
				if (B.Label.IndexOf(A.Label[i]) >= 0)
					commonRank++;
			}
			return commonRank;
		}

		/// <summary>
		/// Check sizes and labels of contraction :$D_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <typeparam name="TTen">tensor type</typeparam>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="α">scalar to multiply <paramref name="A"/> * <paramref name="B"/></param>
		/// <param name="A">tensor A, must not be null</param>
		/// <param name="B">tensor B, must not be null</param>
		/// <param name="β">scalar to multiply tensor <paramref name="C"/></param>
		/// <param name="C">tensor C, if null, replaced to <paramref name="D"/></param>
		/// <param name="D">tensor D, if null, replaced to <paramref name="C"/></param>
		/// <param name="commonRank">output, number of common indices</param>
		public static void InPlaceContractCheck<TTen, T>(T α, TTen A, TTen B, T β, ref TTen C, ref TTen D, out int commonRank)
			where TTen : PureArray<T>, Althea.ITensor
			where T : struct, IComparable<T>
		{
			if (α.IsZero())
				throw new ArgumentOutOfRangeException(nameof(α), Resource.ParaCannotZero);
			if (D is null)
				D = C;
			if (β.IsZero() || C is null || C == PureArray<T>.EmptyDnTen)
			{
				C = D;
			}
			if (A is null || A == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (C is null || C == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);
			if (D is null || D == PureArray<T>.EmptyDnTen)
				throw new ArgumentNullException(nameof(D), Resource.ArrayCannotNull);
			if (!D.Size.SequenceEqual(C.Size))
				throw new ArgumentException(Resource.TensorWrongSize, nameof(D));
			if (!D.Label.SequenceEqual(C.Label))
				throw new ArgumentException(Resource.TensorWrongIndex, nameof(D));

			commonRank = GetContractRank(A, B);
		}

		// Ignore Spelling: stackalloc
		/// <summary>
		/// Check contraction <c>A * B + C</c> and return permutations.
		/// </summary>
		/// <param name="sizeA">size of tensor A</param>
		/// <param name="labelA">label of tensor A</param>
		/// <param name="sizeB">size of tensor B</param>
		/// <param name="labelB">label of tensor B</param>
		/// <param name="sizeC">size of tensor C</param>
		/// <param name="labelC">label of tensor C</param>
		/// <param name="concA">sorted left tensor's contract indices</param>
		/// <param name="concB">right tensor's contract indices sorted by <paramref name="concA"/></param>
		/// <param name="freeA">left tensor's free indices sorted by <paramref name="freeCA"/></param>
		/// <param name="freeCA">output tensor's indices corresponding to left tensor's</param>
		/// <param name="freeB">right tensor's free indices sorted by <paramref name="freeCB"/></param>
		/// <param name="freeCB">output tensor's indices corresponding to right tensor's</param>
		/// <remarks>must be invoked after <see cref="OutOfPlaceContractCheck{TTen, T}"/> or <see cref="InPlaceContractCheck{TTen, T}"/></remarks>
		/// <example><code>
		/// <see cref="InPlaceContractCheck{TTen, T}"/>(α, A, B, β, ref C, ref D, out int commonRank);
		/// Span&lt;int&gt; concA = stackalloc int[commonRank], concB = stackalloc int[commonRank];
		/// Span&lt;int&gt; freeA = stackalloc int[A.Rank - commonRank], freeCA = stackalloc int[C.Rank - freeA.Length];
		/// Span&lt;int&gt; freeB = stackalloc int[B.Rank - commonRank], freeCB = stackalloc int[C.Rank - freeB.Length];
		/// <see cref="ContractCheck"/>(A.Size, A.Label, B.Size, B.Label, C.Size, C.Label, concA, concB, freeA, freeCA, freeB, freeCB);
		/// var onHost = CudaCSharpHelpers.CheckOnHost(A, B, C, D);
		/// var function = onHost ? new ITensor.DelegateContract&lt;T&gt;(CPU.Contract) : GPU.Contract;
		/// function(α, A.Pointer, B.Pointer, β, C.Pointer, D.Pointer, A.Size.ToArray(), B.Size.ToArray(), C.Size.ToArray(), concA, concB, freeA, freeCA, freeB, freeCB);
		/// </code>
		/// Or
		/// <code>
		/// var (sizeC, labelC) = <see cref="OutOfPlaceContractCheck{TTen, T}"/>(α, A, B, out int commonRank);
		/// Span&lt;int&gt; concA = stackalloc int[commonRank], concB = stackalloc int[commonRank];
		/// Span&lt;int&gt; freeA = stackalloc int[A.Rank - commonRank], freeCA = stackalloc int[sizeC.Length - freeA.Length];
		/// Span&lt;int&gt; freeB = stackalloc int[B.Rank - commonRank], freeCB = stackalloc int[sizeC.Length - freeB.Length];
		/// <see cref="ContractCheck"/>(A.Size, A.Label, B.Size, B.Label, sizeC, labelC, concA, concB, freeA, freeCA, freeB, freeCB);
		/// var onHost = CudaCSharpHelpers.CheckOnHost(A, B, C, D);
		/// var function = onHost ? new ITensor.DelegateContract&lt;T&gt;(CPU.Contract) : GPU.Contract;
		/// function(α, A.Pointer, B.Pointer, β, C.Pointer, D.Pointer, A.Size.ToArray(), B.Size.ToArray(), C.Size.ToArray(), concA, concB, freeA, freeCA, freeB, freeCB);
		/// </code></example>
		public static void ContractCheck(IReadOnlyList<long> sizeA, IReadOnlyList<char> labelA, IReadOnlyList<long> sizeB, IReadOnlyList<char> labelB, IReadOnlyList<long> sizeC, IReadOnlyList<char> labelC, Span<int> concA, Span<int> concB, Span<int> freeA, Span<int> freeCA, Span<int> freeB, Span<int> freeCB)
		{
			// get common mode and size & left and right contraction indices
			int commonRank = concA.Length;
			Span<char> commonMode = stackalloc char[commonRank];
			Span<long> commonSize = stackalloc long[commonRank];
			int now = 0;
			for (int i = 0; i < sizeA.Count; i++)
			{
				var ind = labelB.IndexOf(labelA[i]);
				if (ind >= 0)
				{
					if (sizeB[ind] != sizeA[i])
						throw new ArgumentOutOfRangeException(nameof(sizeA));
					commonMode[now] = labelA[i]; commonSize[now] = sizeA[i];
					concA[now] = i; concB[now++] = ind;
				}
			}
			// get free mode and size
			Span<char> freeMode = stackalloc char[sizeC.Count];
			Span<long> freeSize = stackalloc long[sizeC.Count];
			now = 0;
			for (int i = 0; i < sizeA.Count; i++)
			{
				if (!commonMode.Contains(labelA[i]))
				{
					freeMode[now] = labelA[i]; freeSize[now++] = sizeA[i];
				}
			}
			for (int i = 0; i < sizeB.Count; i++)
			{
				if (!commonMode.Contains(labelB[i]))
				{
					freeMode[now] = labelB[i]; freeSize[now++] = sizeB[i];
				}
			}
			// check free mode and size with C
			for (int i = 0; i < freeMode.Length; i++)
			{
				int ind = labelC.IndexOf(freeMode[i]);
				if (ind < 0 || sizeC[ind] != freeSize[i])
					throw new ArgumentException(Resource.TensorWrongSize, nameof(sizeC));
			}
			// get left and right free indices
			int nowA = 0, nowB = 0;
			for (int i = 0; i < sizeC.Count; i++)
			{
				var ind = labelA.IndexOf(labelC[i]);
				if (ind >= 0)
				{
					freeA[nowA] = ind; freeCA[nowA++] = i;
				}
				else
				{
					ind = labelB.IndexOf(labelC[i]);
					freeB[nowB] = ind; freeCB[nowB++] = i;
				}
			}
		}
		#endregion

		#region main
		/// <summary>
		/// Permute (general transpose) and scale this tensor to form a new tensor: $B_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="newOrder">the new permutation order in <see cref="TensorOrder"/></param>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="op">the <see cref="UnitaryOperation"/> <c>Ψ</c> to apply on each element before scaling</param>
		/// <param name="A">the source tensor</param>
		/// <param name="B">the output tensor</param>
		public static void Permute<T>(DenseTensor<T> A, T α, UnitaryOperation op, TensorOrder newOrder, DenseTensor<T> B) where T : struct, IComparable<T>
		{
			Span<int> permA2B = stackalloc int[A.Rank];
			PermuteCheck(A, α, newOrder, B, permA2B);
			var onHost = CudaCSharpHelpers.CheckOnHost(A, B);
			var func = onHost ? new ITensor.DelegatePermute<T>(CPU.Permute) : GPU.Permute;
			func(A.Pointer, A.Size.ToArray(), α, op, B.Pointer, B.Size.ToArray(), permA2B);
		}

		/// <summary>
		/// Partial reduction of tensor <paramref name="A"/>: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The missing indices of <paramref name="A"/> compared to <paramref name="C"/> will be aggregated according to <paramref name="reduction"/>.
		/// </summary>
		/// <param name="reduction">the reduce <see cref="BinaryOperation"/> <c>Φ</c></param>
		/// <param name="α">scalar α</param>
		/// <param name="opA"><see cref="UnitaryOperation"/> <c>Ψ<sub>A</sub></c></param>
		/// <param name="A"><see cref="DenseTensor{T}"/> A</param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="opC"><see cref="UnitaryOperation"/> <c>Ψ<sub>C</sub></c>, default identity</param>
		/// <param name="C"><see cref="DenseTensor{T}"/> C, default null</param>
		/// <param name="D">output tensor D</param>
		/// <remarks>If <paramref name="C"/> is null, or <paramref name="β"/> is zero, this tensor itself will be used instead of <paramref name="C"/>.</remarks>>
		public static void Reduce<T>(BinaryOperation reduction, T α, UnitaryOperation opA, DenseTensor<T> A, T β, UnitaryOperation opC, DenseTensor<T> C, DenseTensor<T> D) where T : struct, IComparable<T>
		{
			Span<int> permA2C = stackalloc int[(C ?? D).Rank];
			ReduceCheck(α, A, β, ref C, ref opC, ref D, permA2C);
			var onHost = CudaCSharpHelpers.CheckOnHost(A, C, D);
			var func = onHost ? new ITensor.DelegateReduce<T>(CPU.Reduce) : GPU.Reduce;
			func(reduction, α, opA, A.Pointer, A.Size.ToArray(), β, opC, C.Pointer, C.Size.ToArray(), D.Pointer, permA2C);
		}

		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $D_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A">tensor A</param>
		/// <param name="β">scalar β</param>
		/// <param name="B">tensor B</param>
		/// <param name="C">tensor C</param>
		/// <param name="D">output tensor D</param>
		public static void Contract<T>(T α, DenseTensor<T> A, DenseTensor<T> B, T β, DenseTensor<T> C, DenseTensor<T> D) where T : struct, IComparable<T>
		{
			InPlaceContractCheck(α, A, B, β, ref C, ref D, out int commonRank);
			if (commonRank == 0 || commonRank == A.Rank)
				throw new ArgumentException(Resource.TensorWrongSize);
			Span<int> concA = stackalloc int[commonRank], concB = stackalloc int[commonRank];
			Span<int> freeA = stackalloc int[A.Rank - commonRank], freeCA = stackalloc int[freeA.Length];
			Span<int> freeB = stackalloc int[B.Rank - commonRank], freeCB = stackalloc int[freeB.Length];
			ContractCheck(A.Size, A.Label, B.Size, B.Label, C.Size, C.Label, concA, concB, freeA, freeCA, freeB, freeCB);
			var onHost = CudaCSharpHelpers.CheckOnHost(A, B, C, D);
			var func = onHost ? new ITensor.DelegateContract<T>(CPU.Contract) : GPU.Contract;
			func(α, A.Pointer, B.Pointer, β, C.Pointer, D.Pointer, A.Size.ToArray(), B.Size.ToArray(), C.Size.ToArray(), concA, concB, freeA, freeCA, freeB, freeCB);
		}
		#endregion
	}
}
