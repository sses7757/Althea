using System;
using System.Runtime.InteropServices;

using Althea.Linq;
using Althea.Storage;


namespace Althea.Tensor
{
	/// <summary>
	/// The Tensor routine interface
	/// </summary>
	public interface ITensor : IDisposable
	{
		/// <summary>
		/// Permute (general transpose) and scale this tensor to form a new tensor: $B_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.
		/// </summary>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="op">the <see cref="UnitaryOperation"/> <c>Ψ</c> to apply on each element before scaling</param>
		/// <param name="A">the source tensor</param>
		/// <param name="sizeA">size/extent of <paramref name="A"/></param>
		/// <param name="B">the output tensor</param>
		/// <param name="sizeB">size/extent of <paramref name="B"/></param>
		/// <param name="permAToB">the permutation order from <paramref name="A"/> to <paramref name="B"/></param>
		void Permute<T>(Storage<T> A, long[] sizeA, T α, UnitaryOperation op, Storage<T> B, long[] sizeB, ReadOnlySpan<int> permAToB) where T : struct, IComparable<T>;

		/// <summary>
		/// Permute (general transpose) and scale this tensor to form a new tensor: $B_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.
		/// </summary>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="op">the <see cref="UnitaryOperation"/> <c>Ψ</c> to apply on each element before scaling</param>
		/// <param name="A">the source tensor</param>
		/// <param name="sizeA">size/extent of <paramref name="A"/></param>
		/// <param name="B">the output tensor</param>
		/// <param name="sizeB">size/extent of <paramref name="B"/></param>
		/// <param name="permAToB">the permutation order from <paramref name="A"/> to <paramref name="B"/></param>
		public delegate void DelegatePermute<T>(Storage<T> A, long[] sizeA, T α, UnitaryOperation op, Storage<T> B, long[] sizeB, ReadOnlySpan<int> permAToB) where T : struct, IComparable<T>;

		// Ignore Spelling: ijkl mjl kmi li
		/// <summary>
		/// Partial reduction of tensor <paramref name="A"/>: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The indices not in <paramref name="permAToC"/> of <paramref name="A"/> will be aggregated according to <paramref name="reduction"/>.
		/// </summary>
		/// <param name="reduction">the reduce <see cref="BinaryOperation"/> <c>Φ</c></param>
		/// <param name="α">scalar α</param>
		/// <param name="opA"><see cref="UnitaryOperation"/> <c>Ψ<sub>A</sub></c></param>
		/// <param name="A">tensor A</param>
		/// <param name="sizeA">size/extent of <paramref name="A"/></param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="opC"><see cref="UnitaryOperation"/> <c>Ψ<sub>C</sub></c></param>
		/// <param name="C">tensor C</param>
		/// <param name="sizeC">size/extent of <paramref name="C"/></param>
		/// <param name="D">output tensor D</param>
		/// <param name="permAToC">the permutation order from <paramref name="A"/> to <paramref name="C"/></param>
		/// <remarks>For example, if you want to reduce a 4-tensor <c><paramref name="A"/><sub>ijkl</sub></c> to a 2-tensor <c><paramref name="D"/><sub>li</sub></c>, then <paramref name="permAToC"/> can be <c>{3,0}</c>.</remarks>
		void Reduce<T>(BinaryOperation reduction, T α, UnitaryOperation opA, Storage<T> A, long[] sizeA, T β, UnitaryOperation opC, Storage<T> C, long[] sizeC, Storage<T> D, ReadOnlySpan<int> permAToC) where T : struct, IComparable<T>;

		/// <summary>
		/// Partial reduction of tensor <paramref name="A"/>: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The indices not in <paramref name="permAToC"/> of <paramref name="A"/> will be aggregated according to <paramref name="reduction"/>.
		/// </summary>
		/// <param name="reduction">the reduce <see cref="BinaryOperation"/> <c>Φ</c></param>
		/// <param name="α">scalar α</param>
		/// <param name="opA"><see cref="UnitaryOperation"/> <c>Ψ<sub>A</sub></c></param>
		/// <param name="A">tensor A</param>
		/// <param name="sizeA">size/extent of <paramref name="A"/></param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="opC"><see cref="UnitaryOperation"/> <c>Ψ<sub>C</sub></c></param>
		/// <param name="C">tensor C</param>
		/// <param name="sizeC">size/extent of <paramref name="C"/></param>
		/// <param name="D">output tensor D</param>
		/// <param name="permAToC">the permutation order from <paramref name="A"/> to <paramref name="C"/></param>
		/// <remarks>For example, if you want to reduce a 4-tensor <c><paramref name="A"/><sub>ijkl</sub></c> to a 2-tensor <c><paramref name="D"/><sub>li</sub></c>, then <paramref name="permAToC"/> can be <c>{3,0}</c>.</remarks>
		public delegate void DelegateReduce<T>(BinaryOperation reduction, T α, UnitaryOperation opA, Storage<T> A, long[] sizeA, T β, UnitaryOperation opC, Storage<T> C, long[] sizeC, Storage<T> D, ReadOnlySpan<int> permAToC) where T : struct, IComparable<T>;

		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $D_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A">tensor A</param>
		/// <param name="β">scalar β</param>
		/// <param name="B">tensor B</param>
		/// <param name="C">tensor C</param>
		/// <param name="D">output tensor D</param>
		/// <param name="sizeA">left tensor's size/extent</param>
		/// <param name="sizeB">right tensor's size/extent</param>
		/// <param name="sizeC">output tensor's size/extent</param>
		/// <param name="concA">sorted left tensor's contract indices</param>
		/// <param name="concB">right tensor's contract indices sorted by <paramref name="concA"/></param>
		/// <param name="freeA">left tensor's free indices sorted by <paramref name="freeCA"/></param>
		/// <param name="freeCA">output tensor's indices corresponding to left tensor's</param>
		/// <param name="freeB">right tensor's free indices sorted by <paramref name="freeCB"/></param>
		/// <param name="freeCB">output tensor's indices corresponding to right tensor's</param>
		void Contract<T>(T α, Storage<T> A, Storage<T> B, T β, Storage<T> C, Storage<T> D, long[] sizeA, long[] sizeB, long[] sizeC, ReadOnlySpan<int> concA, ReadOnlySpan<int> concB, ReadOnlySpan<int> freeA, ReadOnlySpan<int> freeCA, ReadOnlySpan<int> freeB, ReadOnlySpan<int> freeCB) where T : struct, IComparable<T>;

		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $D_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A">tensor A</param>
		/// <param name="β">scalar β</param>
		/// <param name="B">tensor B</param>
		/// <param name="C">tensor C</param>
		/// <param name="D">output tensor D</param>
		/// <param name="sizeA">left tensor's size/extent</param>
		/// <param name="sizeB">right tensor's size/extent</param>
		/// <param name="sizeC">output tensor's size/extent</param>
		/// <param name="concA">sorted left tensor's contract indices</param>
		/// <param name="concB">right tensor's contract indices sorted by <paramref name="concA"/></param>
		/// <param name="freeA">left tensor's free indices sorted by <paramref name="freeCA"/></param>
		/// <param name="freeCA">output tensor's indices corresponding to left tensor's</param>
		/// <param name="freeB">right tensor's free indices sorted by <paramref name="freeCB"/></param>
		/// <param name="freeCB">output tensor's indices corresponding to right tensor's</param>
		public delegate void DelegateContract<T>(T α, Storage<T> A, Storage<T> B, T β, Storage<T> C, Storage<T> D, long[] sizeA, long[] sizeB, long[] sizeC, ReadOnlySpan<int> concA, ReadOnlySpan<int> concB, ReadOnlySpan<int> freeA, ReadOnlySpan<int> freeCA, ReadOnlySpan<int> freeB, ReadOnlySpan<int> freeCB) where T : struct, IComparable<T>;
	}
}

namespace Althea.Tensor.Cuda
{
	internal sealed class CudaTensor : ITensor
	{
		#region base
		private Handle handle;

		public CudaTensor()
		{
			this.handle = new Handle();
			NativeMethods.cutensorInit(ref this.handle).Check();
		}

		public void Dispose()
		{
			// do nothing
		}

		private static bool? _canUseCudaTensor = null;

		private static bool CanUseCudaTensor {
			get {
				if (_canUseCudaTensor.HasValue)
					return _canUseCudaTensor.Value;
				var (major, _) = Runtime.API.CUDAComputeCapability;
				_canUseCudaTensor = major >= 7 && (!CudaCSharpHelpers.IsWindows || Runtime.API.DeviceVersion.major >= 11);
				return _canUseCudaTensor.Value;
			}
		}
		#endregion

		#region operation
		public void Permute<T>(Storage<T> A, long[] sizeA, T α, UnitaryOperation op, Storage<T> B, long[] sizeB, ReadOnlySpan<int> permAToB) where T : struct, IComparable<T>
		{
			if (CanUseCudaTensor)
			{
				var descrA = TensorDescription.Create<T>(this.handle, sizeA, op);
				var descrB = TensorDescription.Create<T>(this.handle, sizeB);
				DataType type = default(T).ToDataType();
				NativeMethods.Permutation<T> func = type switch
				{
					DataType.RealSingle => new NativeMethods.Permutation<float>(NativeMethods.cutensorPermutationS) as NativeMethods.Permutation<T>,
					DataType.RealDouble => new NativeMethods.Permutation<double>(NativeMethods.cutensorPermutationD) as NativeMethods.Permutation<T>,
					DataType.ComplexSingle => new NativeMethods.Permutation<FloatComplex>(NativeMethods.cutensorPermutationC) as NativeMethods.Permutation<T>,
					DataType.ComplexDouble => new NativeMethods.Permutation<DoubleComplex>(NativeMethods.cutensorPermutationZ) as NativeMethods.Permutation<T>,
					_ => null,
				};
				if (func is null && α.IsOne())
				{	// support other data types such as int and long
					func = type.Bytes() switch
					{
						4 => new NativeMethods.Permutation<float>(NativeMethods.cutensorPermutationS) as NativeMethods.Permutation<T>,
						8 => new NativeMethods.Permutation<double>(NativeMethods.cutensorPermutationD) as NativeMethods.Permutation<T>,
						16 => new NativeMethods.Permutation<DoubleComplex>(NativeMethods.cutensorPermutationZ) as NativeMethods.Permutation<T>,
						_ => null,
					};
				}
				if (func is null)
					throw new NotSupportedException(Resource.DataTypeNotSupport);

				Span<int> modeA = stackalloc int[sizeA.Length], modeB = stackalloc int[sizeB.Length];
				modeA.FillWithRange(1);
				modeA.ReOrderTo(modeB, permAToB);
				func(ref this.handle, ref α, A, ref descrA, MemoryMarshal.GetReference(modeA), B, ref descrB, MemoryMarshal.GetReference(modeB), type.ToCudaDataType(), IntPtr.Zero).Check();
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public void Reduce<T>(BinaryOperation reduction, T α, UnitaryOperation opA, Storage<T> A, long[] sizeA, T β, UnitaryOperation opC, Storage<T> C, long[] sizeC, Storage<T> D, ReadOnlySpan<int> permAToC) where T : struct, IComparable<T>
		{
			if (CanUseCudaTensor)
			{
				Span<int> modeA = stackalloc int[sizeA.Length], modeC = stackalloc int[sizeC.Length];
				modeA.FillWithRange(1);
				modeA.ReOrderTo(modeC, permAToC);
				var refModeA = MemoryMarshal.GetReference(modeA);
				var refModeC = MemoryMarshal.GetReference(modeC);

				var descrA = TensorDescription.Create<T>(this.handle, sizeA, opA);
				var descrC = TensorDescription.Create<T>(this.handle, sizeC, opC);
				long workSize = 0;
				NativeMethods.cutensorReductionGetWorkspace(ref this.handle, A, ref descrA, in refModeA, C, ref descrC, in refModeC, D, ref descrC, in refModeC, reduction, default(T).ToDataType().ToComputeType(), ref workSize).Check();
				using var workBuf = Storage<byte>.Create(workSize, onHost: false);
				NativeMethods.Reduction<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.Reduction<float>(NativeMethods.cutensorReductionS) as NativeMethods.Reduction<T>,
					DataType.RealDouble => new NativeMethods.Reduction<double>(NativeMethods.cutensorReductionD) as NativeMethods.Reduction<T>,
					DataType.ComplexSingle => new NativeMethods.Reduction<FloatComplex>(NativeMethods.cutensorReductionC) as NativeMethods.Reduction<T>,
					DataType.ComplexDouble => new NativeMethods.Reduction<DoubleComplex>(NativeMethods.cutensorReductionZ) as NativeMethods.Reduction<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(ref this.handle, ref α, A, ref descrA, in refModeA, ref β, C, ref descrC, in refModeC, D, ref descrC, in refModeC, reduction, default(T).ToDataType().ToComputeType(), workBuf, workSize, IntPtr.Zero).Check();
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public void Contract<T>(T α, Storage<T> A, Storage<T> B, T β, Storage<T> C, Storage<T> D, long[] sizeA, long[] sizeB, long[] sizeC, ReadOnlySpan<int> concA, ReadOnlySpan<int> concB, ReadOnlySpan<int> freeA, ReadOnlySpan<int> freeCA, ReadOnlySpan<int> freeB, ReadOnlySpan<int> freeCB) where T : struct, IComparable<T>
		{
			if (CanUseCudaTensor)
			{
				// get modes
				Span<int> modeA = stackalloc int[sizeA.Length], modeB = stackalloc int[sizeC.Length], modeC = stackalloc int[sizeC.Length];
				modeA.FillWithRange(1);
				{
					Span<int> temp = stackalloc int[concA.Length];
					modeA.ReOrderTo(temp, concA);
					temp.InverseOrderTo(modeB, concB);
					int maxB = modeB.Max();
					for (int i = 0; i < modeB.Length; i++)
					{
						if (modeB[i] == 0)
							modeB[i] = ++maxB;
					}
					temp = stackalloc int[freeA.Length];
					modeA.ReOrderTo(temp, freeA);
					temp.InverseOrderTo(modeC, freeCA);
					modeB.ReOrderTo(temp, freeB);
					temp.InverseOrderTo(modeC, freeCB);
				}
				var refModeA = MemoryMarshal.GetReference(modeA);
				var refModeB = MemoryMarshal.GetReference(modeB);
				var refModeC = MemoryMarshal.GetReference(modeC);
				// get tensor descriptions
				var type = default(T).ToDataType().ToComputeType();
				var descrA = TensorDescription.Create<T>(this.handle, sizeA);
				var descrB = TensorDescription.Create<T>(this.handle, sizeB);
				var descrC = TensorDescription.Create<T>(this.handle, sizeC);
				// get alignments
				int alignA = 0, alignB = 0, alignC = 0, alignD = 0;
				NativeMethods.cutensorGetAlignmentRequirement(ref this.handle, A, ref descrA, ref alignA).Check();
				NativeMethods.cutensorGetAlignmentRequirement(ref this.handle, B, ref descrB, ref alignB).Check();
				NativeMethods.cutensorGetAlignmentRequirement(ref this.handle, C, ref descrC, ref alignC).Check();
				NativeMethods.cutensorGetAlignmentRequirement(ref this.handle, D, ref descrC, ref alignD).Check();
				// initialize descriptor
				var contractDescr = new ContractDescription();
				NativeMethods.cutensorInitContractionDescriptor(ref this.handle, ref contractDescr, ref descrA, in refModeA, alignA, ref descrB, in refModeB, alignB, ref descrC, in refModeC, alignC, ref descrC, in refModeC, alignD, type).Check();

				// check cache
				ContractionCache<ContractPlan>.TryGet(sizeA, sizeB, sizeC, concA, concB, freeA, freeCA, freeB, freeCB, out var plan, out var input);
				ContractPlan contractPlan; // final contract plan
				if (plan.HasValue)
				{
					contractPlan = plan.Value;
				}
				else
				{
					// initialize candidate
					long workSize = 0;
					var contractFind = new ContractFind();
					NativeMethods.cutensorInitContractionFind(ref this.handle, ref contractFind, ContractionAlgorithm.Default).Check();
					NativeMethods.cutensorContractionGetWorkspace(ref this.handle, ref contractDescr, ref contractFind, WorkSpacePreference.Recommended, ref workSize).Check();
					contractPlan = new ContractPlan(workSize); // final contract plan
					NativeMethods.cutensorInitContractionPlan(ref this.handle, ref contractPlan, ref contractDescr, ref contractFind, workSize).Check();
					// add to cache
					ContractionCache<ContractPlan>.Add(input, contractPlan);
				}
				// create work space and do contraction
				using var work = Storage<byte>.Create(contractPlan.workSize, onHost: false);
				NativeMethods.Contraction<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.Contraction<float>(NativeMethods.cutensorContractionS) as NativeMethods.Contraction<T>,
					DataType.RealDouble => new NativeMethods.Contraction<double>(NativeMethods.cutensorContractionD) as NativeMethods.Contraction<T>,
					DataType.ComplexSingle => new NativeMethods.Contraction<FloatComplex>(NativeMethods.cutensorContractionC) as NativeMethods.Contraction<T>,
					DataType.ComplexDouble => new NativeMethods.Contraction<DoubleComplex>(NativeMethods.cutensorContractionZ) as NativeMethods.Contraction<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(ref this.handle, ref contractPlan, ref α, A, B, ref β, C, D, work, contractPlan.workSize, IntPtr.Zero).Check();
			}
			else
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}
}

namespace Althea.Tensor.Mkl
{
	internal sealed class MklTensor : ITensor
	{
		// TODO: MKL tensor
		public MklTensor() { }

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public void Permute<T>(Storage<T> A, long[] sizeA, T α, UnitaryOperation op, Storage<T> B, long[] sizeB, ReadOnlySpan<int> permAToB) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void Reduce<T>(BinaryOperation reduction, T α, UnitaryOperation opA, Storage<T> A, long[] sizeA, T β, UnitaryOperation opC, Storage<T> C, long[] sizeC, Storage<T> D, ReadOnlySpan<int> permAToC) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void Contract<T>(T α, Storage<T> A, Storage<T> B, T β, Storage<T> C, Storage<T> D, long[] sizeA, long[] sizeB, long[] sizeC, ReadOnlySpan<int> concA, ReadOnlySpan<int> concB, ReadOnlySpan<int> freeA, ReadOnlySpan<int> freeCA, ReadOnlySpan<int> freeB, ReadOnlySpan<int> freeCB) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}
	}
}

