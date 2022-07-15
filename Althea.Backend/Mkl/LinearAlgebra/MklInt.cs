using System.Runtime.CompilerServices;


namespace Althea.Backend.Mkl.LinearAlgebra
{
	internal readonly struct MklInt
	{
#if MKL_I64
		public readonly long value;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private MklInt(long v) => this.value = v;

		public static readonly MklInt MaxValue = new(long.MaxValue);
#else
		public readonly int value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private MklInt(int v) => this.value = v;

		public static readonly MklInt MaxValue = new(int.MaxValue);
#endif
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator MklInt(long v) => new((int)v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator long(MklInt v) => v.value;
	}
}
