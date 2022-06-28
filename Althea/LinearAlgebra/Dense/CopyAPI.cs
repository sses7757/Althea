using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Numerics;
using Althea.Resources;
using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract interface for dense linear algebra copy API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface ICopyAbstractApi : IAbstractRuntimeApi<ICopyAbstractApi>
	{
		#region storage operations
		/// <summary>
		/// When implemented by a derived class, copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP1">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <typeparam name="TP2">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer</param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destination">The destination pointer</param>
		/// <param name="destinationLD">The destination array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in the real type, if it is 0, 2D block as large as possible shall be copied</param>
		/// <param name="copyWidth">Output the width that actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are ignored</remarks>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <c><paramref name="sourceLD"/> and <paramref name="width"/> indicate size larger than <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see></c>, 
		/// or <c><paramref name="destinationLD"/> and <paramref name="width"/> indicate size larger than <paramref name="destination"/>.<see cref="IStorage.LengthInBytes">Length</see></c>
		/// </exception>
		[AbstractApiMethod]
		public abstract bool MemoryCopy2D<T, TP1, TP2>(PointerSegment<TP1> source, long sourceLD, PointerSegment<TP2> destination, long destinationLD, long height, long width, out long copyWidth) where T : unmanaged, INumber<T> where TP1 : IPointer<TP1> where TP2 : IPointer<TP2>;

		/// <summary>
		/// When implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP1">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <typeparam name="TP2">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination pointer to copy to</param>
		/// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool StridedCopy<T, TP1, TP2>(PointerSegment<TP1> source, long strideSource, PointerSegment<TP2> destination, long strideDestination, out long actualCopied) where T : unmanaged, INumber<T> where TP1 : IPointer<TP1> where TP2 : IPointer<TP2>;
		#endregion
	}


	/// <summary>
	/// The static class for extension methods for <see cref="IStorage"/> and <see cref="IStorage{T, TSelf}"/>
	/// </summary>
	public static class StorageExtension
	{
		#region method generators
#pragma warning disable CS8601
		internal static readonly MethodInfo SizeOfPointerMethod = typeof(IStorage).GetMethod(nameof(IStorage.SizeOfPointer), 0, BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null);
#pragma warning restore CS8601
		private static Action<TS1, TS2, long, long, long, long> GetCopy2DMethod<T, TS1, TS2>() where T : unmanaged, INumber<T> where TS1 : class, IStorage where TS2 : class, IStorage
		{
			Type type1 = typeof(TS1), type2 = typeof(TS2);
			MethodInfo[] pointerGetters1 = TS2.PointerGetters, pointerGetters2 = TS2.PointerGetters;
			MethodInfo[] pointerLen1 = new MethodInfo[pointerGetters1.Length], pointerMove1 = new MethodInfo[pointerGetters1.Length];
			MethodInfo[] pointerLen2 = new MethodInfo[pointerGetters2.Length], pointerMove2 = new MethodInfo[pointerGetters2.Length];
			MethodInfo[,] pointerCopy2D = new MethodInfo[pointerGetters1.Length, pointerGetters2.Length];
			MethodInfo[,] pointerCopy = new MethodInfo[pointerGetters1.Length, pointerGetters2.Length];
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				var pg = pointerGetters1[i];
				bool first = true;
			SET_METHOD:
				if (pg is null || pg.ReturnType.GenericTypeArguments.Length != 1)
					throw new InvalidOperationException(StorageError.InvalidPointerGetter);
				if (pg.GetParameters().Length != 0 &&
					!(pg.GetParameters().Length == 2 && pg.GetParameters()
														  .Select(static p => p.ParameterType)
														  .SequenceEqual(new[] { typeof(long), typeof(bool) })))
					throw new InvalidOperationException(StorageError.InvalidPointerGetter);
				var ptrLen = pg.ReturnType.GetProperty(nameof(PointerSegment<ManagedPointer>.LengthInBytes), BindingFlags.Public | BindingFlags.Instance)?.GetAccessors(false)?.FirstOrDefault();
				if (ptrLen is null || ptrLen.ReturnType != typeof(long))
					throw new InvalidOperationException(StorageError.InvalidPointerGetter);
				var ptrMove = pg.ReturnType.GetMethod(nameof(PointerSegment<ManagedPointer>.MoveBy), 0, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(long), typeof(long) }, null);
				if (ptrMove is null)
					throw new InvalidOperationException(StorageError.InvalidPointerGetter);
				if (first)
				{
					pointerLen1[i] = ptrLen; pointerMove1[i] = ptrMove;
					pg = pointerGetters2[i];
					first = false;
					goto SET_METHOD;
				}
				pointerLen2[i] = ptrLen; pointerMove2[i] = ptrMove;
			}
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				for (int j = 0; j < pointerGetters2.Length; j++)
				{
					var copyMethod = typeof(ApiSelector).GetMethod(nameof(CopyApiSelector.MemoryCopy2D), 3, BindingFlags.Public | BindingFlags.Static, null, new[] { pointerGetters1[i].ReturnType, typeof(long), pointerGetters2[j].ReturnType, typeof(long), typeof(long), typeof(long) }, null)?.MakeGenericMethod(typeof(T), pointerGetters1[i].ReturnType.GenericTypeArguments[0], pointerGetters2[j].ReturnType.GenericTypeArguments[0]);
					if (copyMethod is null)
						throw new InvalidOperationException(StorageError.InvalidPointerGetter);
					pointerCopy2D[i, j] = copyMethod;
					copyMethod = typeof(ApiSelector).GetMethod(nameof(ApiSelector.MemoryCopy), 2, BindingFlags.Public | BindingFlags.Static, null, new[] { pointerGetters1[i].ReturnType, pointerGetters2[j].ReturnType }, null)?.MakeGenericMethod(pointerGetters1[i].ReturnType.GenericTypeArguments[0], pointerGetters2[j].ReturnType.GenericTypeArguments[0]);
					if (copyMethod is null)
						throw new InvalidOperationException(StorageError.InvalidPointerGetter);
					pointerCopy[i, j] = copyMethod;
				}
			}

			DynamicMethod method = new($"2D Copier from {type1.GetGenericString()} to {type2.GetGenericString()}", null, new[] { type1, type2, typeof(long), typeof(long), typeof(long), typeof(long) });
			var IL = method.GetILGenerator();
			Label ret = IL.DefineLabel(), thr = IL.DefineLabel();
			Label[,] branches = new Label[pointerGetters1.Length + 1, pointerGetters2.Length + 1];
			for (int i = 0; i <= pointerGetters1.Length; i++)
				for (int j = 0; j <= pointerGetters2.Length; j++)
					branches[i, j] = i == pointerGetters1.Length || j == pointerGetters2.Length ? ret : IL.DefineLabel();
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldc_I4, i);
				IL.Emit(OpCodes.Callvirt, SizeOfPointerMethod);
				IL.DeclareLocal(typeof(long));
				IL.Emit(OpCodes.Stloc_S, i); // long sizeSrcPointerI = src.SizeOfPointer(i);
			}
			for (int j = 0; j < pointerGetters2.Length; j++)
			{
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Ldc_I4, j);
				IL.Emit(OpCodes.Callvirt, SizeOfPointerMethod);
				IL.DeclareLocal(typeof(long));
				IL.Emit(OpCodes.Stloc_S, j + pointerGetters1.Length); // long sizeDstPointerJ = dst.SizeOfPointer(i);
			}
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				IL.DeclareLocal(pointerGetters1[i].ReturnType);
			}
			for (int j = 0; j < pointerGetters2.Length; j++)
			{
				IL.DeclareLocal(pointerGetters2[j].ReturnType);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int sizeSrcPointer(int i) => i;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int sizeDstPointer(int j) => j + pointerGetters1.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int srcPointer(int i) => i + pointerGetters1.Length + pointerGetters2.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int dstPointer(int j) => j + pointerGetters1.Length * 2 + pointerGetters2.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int copiedSrc() => (pointerGetters1.Length + pointerGetters2.Length) * 2;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int copiedDst() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 1;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int copyWidth() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 2;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int offsetSrc() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 3;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int offsetDst() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 4;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int loopI() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 5;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int loopJ() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 6;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalIInc(bool save = true)
			{
				IL.Emit(OpCodes.Ldloc_S, loopI());
				IL.Emit(OpCodes.Ldc_I8, 1L);
				IL.Emit(OpCodes.Add);
				if (save)
					IL.Emit(OpCodes.Dup);
				IL.Emit(OpCodes.Stloc_S, loopI());
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalJInc(bool save = true)
			{
				IL.Emit(OpCodes.Ldloc_S, loopJ());
				IL.Emit(OpCodes.Ldc_I8, 1L);
				IL.Emit(OpCodes.Add);
				if (save)
					IL.Emit(OpCodes.Dup);
				IL.Emit(OpCodes.Stloc_S, loopJ());
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalSrcNext(int i)
			{
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldloc_S, loopI());
				IL.Emit(OpCodes.Ldc_I4_0);
				IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = src.PointerI(i, false);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalDstNext(int j)
			{
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Ldloc_S, loopJ());
				IL.Emit(OpCodes.Ldc_I4_1);
				IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
				IL.Emit(OpCodes.Stloc_S, dstPointer(j)); // dstPtrI = dst.PointerI(j, true);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalSrcMove(int i)
			{
				IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
				IL.Emit(OpCodes.Ldloc_S, copiedSrc());
				IL.Emit(OpCodes.Ldc_I8, 0L);
				IL.Emit(OpCodes.Calli, pointerMove1[i]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = srcPtrI.Move(copiedSrc);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalDstMove(int j)
			{
				IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
				IL.Emit(OpCodes.Ldloc_S, copiedDst());
				IL.Emit(OpCodes.Ldc_I8, 0L);
				IL.Emit(OpCodes.Calli, pointerMove2[j]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(j)); // dstPtrJ = dstPtrJ.Move(copiedDst);
			}

			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, copiedSrc()); // long copiedSrc = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, copiedDst()); // long copiedDst = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, copyWidth()); // long copyWidth = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, offsetSrc()); // long offsetSrc = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, offsetDst()); // long offsetDst = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, loopI()); // long i = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, loopJ()); // long j = 0;

			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				for (int j = 0; j < pointerGetters2.Length; j++)
				{
					IL.MarkLabel(branches[i, j]);
					int type = (pointerGetters1[i].GetParameters().Length != 0 ? 0.SetBit(0) : 0) + (pointerGetters2[j].GetParameters().Length != 0 ? 0.SetBit(1) : 0);
					IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
					IL.Emit(OpCodes.Brfalse_S, branches[i + 1, j]); // if (sizeSrcPointerI == 0) goto P[I+1, J];
					if (type.IsBitNotSet(0))
					{   // if (sizeSrcPointerI != 1) throw;
						IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
						IL.Emit(OpCodes.Ldc_I8, 1L);
						IL.Emit(OpCodes.Bne_Un_S, thr);
					}
					IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
					IL.Emit(OpCodes.Brfalse_S, branches[i, j + 1]); // if (sizeSrcPointerJ == 0) goto P[I, J+1];
					if (type.IsBitNotSet(1))
					{   // if (sizeSrcPointerJ != 1) throw;
						IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
						IL.Emit(OpCodes.Ldc_I8, 1L);
						IL.Emit(OpCodes.Bne_Un_S, thr);
					}

					IL.Emit(OpCodes.Ldarg_0);
					if (type.IsBitNotSet(0))
					{
						IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
					}
					else
					{
						IL.Emit(OpCodes.Ldloc_S, loopI());
						IL.Emit(OpCodes.Ldc_I4_0);
						IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
					}
					IL.Emit(OpCodes.Ldloc_S, offsetSrc());
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Calli, pointerMove1[i]);
					IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = src.PointerI(i, false).Move(offsetSrc);
					IL.Emit(OpCodes.Ldarg_1);
					if (type.IsBitNotSet(1))
					{
						IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
					}
					else
					{
						IL.Emit(OpCodes.Ldloc_S, loopJ());
						IL.Emit(OpCodes.Ldc_I4_1);
						IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
					}
					IL.Emit(OpCodes.Ldloc_S, offsetDst());
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Calli, pointerMove2[j]);
					IL.Emit(OpCodes.Stloc_S, dstPointer(j)); // dstPtrJ = dst.PointerJ(j, true).Move(offsetDst);

					Label noMove = IL.DefineLabel(), srcMove = IL.DefineLabel(), dstMove = IL.DefineLabel();
					Label loopStart = default, c1next = default, c2next = default, c3next = default, c1next1 = default, c1next2 = default;
					if (type != 0)
					{
						loopStart = IL.DefineLabel(); c1next = IL.DefineLabel(); c2next = IL.DefineLabel(); c3next = IL.DefineLabel();
						IL.MarkLabel(loopStart);
					}

					// while (true) {
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Ldarg_2);
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Ldarg_3);
					IL.Emit(OpCodes.Ldarg_S, 4);
					IL.Emit(OpCodes.Ldind_I8, 0L);
					IL.Emit(OpCodes.Call, pointerCopy2D[i, j]); // copyWidth = CopyApiSelector.MemoryCopy2D(srcPtrI, srcLD, dstPtrJ, dstLD, height, 0);
					IL.Emit(OpCodes.Dup);
					IL.Emit(OpCodes.Dup);
					IL.Emit(OpCodes.Stloc_S, copyWidth());
					IL.Emit(OpCodes.Ldarg_2);
					IL.Emit(OpCodes.Mul);
					IL.Emit(OpCodes.Stloc_S, copiedSrc()); // copiedSrc = copyWidth * srcLD;
					IL.Emit(OpCodes.Ldarg_3);
					IL.Emit(OpCodes.Mul);
					IL.Emit(OpCodes.Stloc_S, copiedDst()); // copiedDst = copyWidth * dstLD;
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Calli, pointerLen1[i]);
					IL.Emit(OpCodes.Blt_S, noMove);
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Blt_S, noMove);
					// if (copiedSrc >= srcPtrI.LengthInBytes && copiedDst >= dstPtrJ.LengthInBytes) {
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen1[j]);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_S, offsetSrc()); // offsetSrc = copiedSrc - srcPtrI.LengthInBytes;
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetDst = copiedDst - dstPtrJ.LengthInBytes;
					switch (type)
					{
						case 0b00:
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1];
							break;
						case 0b01:
							LocalIInc();
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Blt_S, c1next); // if (++i >= sizeSrcPointerI) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); i = 0; // goto P[I+1, J+1]; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1];
							break;
						case 0b10:
							LocalJInc();
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Blt_S, c1next); // if (++j >= sizeDstPointerJ) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1]; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J];
							break;
						case 0b11:
							c1next1 = IL.DefineLabel(); c1next2 = IL.DefineLabel();
							LocalIInc(false);
							LocalJInc(false);
							IL.Emit(OpCodes.Ldloc_S, loopI());
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Bge_S, c1next);
							IL.Emit(OpCodes.Ldloc_S, loopJ());
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Bge_S, c1next); // if (++i < sizeSrcPointerI & ++j < sizeDstPointerJ) {
							LocalSrcNext(i);
							LocalDstNext(j);
							IL.Emit(OpCodes.Br_S, loopStart); // continue; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Ldloc_S, loopI());
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Bge_S, c1next1); // else if (i < sizeSrcPointerI) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
							IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1]; }
							IL.MarkLabel(c1next1);
							IL.Emit(OpCodes.Ldloc_S, loopJ());
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Blt_S, c1next2); // else if (j < sizeDstPointerJ) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J]; }
							IL.MarkLabel(c1next2); // else {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI());
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // i = j = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1]; }
							break;
					}
					// }
					// else if (copiedSrc < srcPtrI.LengthInBytes && copiedDst < dstPtrJ.LengthInBytes) {
					IL.MarkLabel(noMove);
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Calli, pointerLen1[i]);
					IL.Emit(OpCodes.Bge_S, dstMove);
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Bge_S, srcMove);
					LocalSrcMove(i); LocalDstMove(j);
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Call, pointerCopy[i, j]);
					IL.Emit(OpCodes.Dup); // copied = ApiSelector.MemoryCopy(srcPtrI, dstPtrJ);
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, copiedDst()); // copiedSrc += copied; copiedDst += copied;
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Calli, pointerLen1[i]);
					IL.Emit(OpCodes.Bge_S, dstMove);
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Bge_S, srcMove);
					// }
					// if (copiedSrc < srcPtrI.LengthInBytes) {
					IL.MarkLabel(srcMove);
					IL.Emit(OpCodes.Ldloc_S, offsetSrc());
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, offsetSrc()); // offsetSrc += copiedSrc;
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetDst = copiedDst - dstPtrJ.LengthInBytes;
					if (type.IsBitNotSet(1))
					{
						IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1];
					}
					else
					{
						LocalSrcMove(i);
						LocalJInc();
						IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
						IL.Emit(OpCodes.Blt_S, c2next); // if (++j >= sizeDstPointerJ) {
						IL.Emit(OpCodes.Ldc_I8, 0L);
						IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
						IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1]; }
						IL.MarkLabel(c2next);
						LocalDstNext(j);
						IL.Emit(OpCodes.Br_S, loopStart); // continue;
					}
					// }
					// else if (copied < dstPtrJ.LengthInBytes) {
					IL.MarkLabel(dstMove);
					IL.Emit(OpCodes.Ldloc_S, offsetDst());
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetDst += copiedDst;
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen1[j]);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_S, offsetSrc()); // offsetSrc = copiedSrc - srcPtrI.LengthInBytes;
					if (type.IsBitNotSet(0))
					{
						IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J];
					}
					else
					{
						LocalDstMove(j);
						LocalIInc();
						IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
						IL.Emit(OpCodes.Blt_S, c3next); // if (++i >= sizeSrcPointerI) {
						IL.Emit(OpCodes.Ldc_I8, 0L);
						IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
						IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J]; }
						IL.MarkLabel(c3next);
						LocalSrcNext(i);
						IL.Emit(OpCodes.Br_S, loopStart); // continue;
					}
					// }
					// } end while
				}
			}

			IL.MarkLabel(ret);
			IL.Emit(OpCodes.Ret);
			IL.MarkLabel(thr);
			IL.ThrowException(typeof(InvalidOperationException));

			method.DefineParameter(1, ParameterAttributes.In, "source");
			method.DefineParameter(2, ParameterAttributes.In, "destination");
			method.DefineParameter(3, ParameterAttributes.In, "sourceLeadDimBytes");
			method.DefineParameter(4, ParameterAttributes.In, "destinationLeadDimBytes");
			method.DefineParameter(5, ParameterAttributes.In, "height");
			method.DefineParameter(6, ParameterAttributes.In, "width");
			return method.CreateDelegate<Action<TS1, TS2, long, long, long, long>>();
		}


		private static Action<TS1, TS2, long, long> GetCopyStridedMethod<T, TS1, TS2>() where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			Type type1 = typeof(TS1), type2 = typeof(TS2);
			MethodInfo[] pointerGetters1 = TS2.PointerGetters, pointerGetters2 = TS2.PointerGetters;
			MethodInfo[] pointerLen1 = new MethodInfo[pointerGetters1.Length], pointerMove1 = new MethodInfo[pointerGetters1.Length];
			MethodInfo[] pointerLen2 = new MethodInfo[pointerGetters2.Length], pointerMove2 = new MethodInfo[pointerGetters2.Length];
			MethodInfo[,] pointerCopy2D = new MethodInfo[pointerGetters1.Length, pointerGetters2.Length];
			MethodInfo[,] pointerCopy = new MethodInfo[pointerGetters1.Length, pointerGetters2.Length];
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				var pg = pointerGetters1[i];
				bool first = true;
			SET_METHOD:
				if (pg is null || pg.ReturnType.GenericTypeArguments.Length != 1)
					throw new InvalidOperationException(StorageError.InvalidPointerGetter);
				if (pg.GetParameters().Length != 0 &&
					!(pg.GetParameters().Length == 2 && pg.GetParameters()
														  .Select(static p => p.ParameterType)
														  .SequenceEqual(new[] { typeof(long), typeof(bool) })))
					throw new InvalidOperationException(StorageError.InvalidPointerGetter);
				var ptrLen = pg.ReturnType.GetProperty(nameof(PointerSegment<ManagedPointer>.LengthInBytes), BindingFlags.Public | BindingFlags.Instance)?.GetAccessors(false)?.FirstOrDefault();
				if (ptrLen is null || ptrLen.ReturnType != typeof(long))
					throw new InvalidOperationException(StorageError.InvalidPointerGetter);
				var ptrMove = pg.ReturnType.GetMethod(nameof(PointerSegment<ManagedPointer>.MoveBy), 0, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(long), typeof(long) }, null);
				if (ptrMove is null)
					throw new InvalidOperationException(StorageError.InvalidPointerGetter);
				if (first)
				{
					pointerLen1[i] = ptrLen; pointerMove1[i] = ptrMove;
					pg = pointerGetters2[i];
					first = false;
					goto SET_METHOD;
				}
				pointerLen2[i] = ptrLen; pointerMove2[i] = ptrMove;
			}
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				for (int j = 0; j < pointerGetters2.Length; j++)
				{
					var copyMethod = typeof(ApiSelector).GetMethod(nameof(CopyApiSelector.StridedCopy), 2, BindingFlags.Public | BindingFlags.Static, null, new[] { pointerGetters1[i].ReturnType, typeof(long), pointerGetters2[j].ReturnType, typeof(long), typeof(long), typeof(long) }, null)?.MakeGenericMethod(pointerGetters1[i].ReturnType.GenericTypeArguments[0], pointerGetters2[j].ReturnType.GenericTypeArguments[0]);
					if (copyMethod is null)
						throw new InvalidOperationException(StorageError.InvalidPointerGetter);
					pointerCopy2D[i, j] = copyMethod;
					copyMethod = typeof(ApiSelector).GetMethod(nameof(ApiSelector.MemoryCopy), 2, BindingFlags.Public | BindingFlags.Static, null, new[] { pointerGetters1[i].ReturnType, pointerGetters2[j].ReturnType }, null)?.MakeGenericMethod(pointerGetters1[i].ReturnType.GenericTypeArguments[0], pointerGetters2[j].ReturnType.GenericTypeArguments[0]);
					if (copyMethod is null)
						throw new InvalidOperationException(StorageError.InvalidPointerGetter);
					pointerCopy[i, j] = copyMethod;
				}
			}

			DynamicMethod method = new($"Stride Copier from {type1.GetGenericString()} to {type2.GetGenericString()}", null, new[] { type1, type2, typeof(long), typeof(long), typeof(long), typeof(long) });
			var IL = method.GetILGenerator();
			Label ret = IL.DefineLabel(), thr = IL.DefineLabel();
			Label[,] branches = new Label[pointerGetters1.Length + 1, pointerGetters2.Length + 1];
			for (int i = 0; i <= pointerGetters1.Length; i++)
				for (int j = 0; j <= pointerGetters2.Length; j++)
					branches[i, j] = i == pointerGetters1.Length || j == pointerGetters2.Length ? ret : IL.DefineLabel();
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldc_I4, i);
				IL.Emit(OpCodes.Callvirt, SizeOfPointerMethod);
				IL.DeclareLocal(typeof(long));
				IL.Emit(OpCodes.Stloc_S, i); // long sizeSrcPointerI = src.SizeOfPointer(i);
			}
			for (int j = 0; j < pointerGetters2.Length; j++)
			{
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Ldc_I4, j);
				IL.Emit(OpCodes.Callvirt, SizeOfPointerMethod);
				IL.DeclareLocal(typeof(long));
				IL.Emit(OpCodes.Stloc_S, j + pointerGetters1.Length); // long sizeDstPointerJ = dst.SizeOfPointer(i);
			}
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				IL.DeclareLocal(pointerGetters1[i].ReturnType);
			}
			for (int j = 0; j < pointerGetters2.Length; j++)
			{
				IL.DeclareLocal(pointerGetters2[j].ReturnType);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int sizeSrcPointer(int i) => i;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int sizeDstPointer(int j) => j + pointerGetters1.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int srcPointer(int i) => i + pointerGetters1.Length + pointerGetters2.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int dstPointer(int j) => j + pointerGetters1.Length * 2 + pointerGetters2.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int copiedSrc() => (pointerGetters1.Length + pointerGetters2.Length) * 2;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int copiedDst() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 1;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int copied() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 2;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int offsetSrc() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 3;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int offsetDst() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 4;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int loopI() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 5;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int loopJ() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 6;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalIInc(bool save = true)
			{
				IL.Emit(OpCodes.Ldloc_S, loopI());
				IL.Emit(OpCodes.Ldc_I8, 1L);
				IL.Emit(OpCodes.Add);
				if (save)
					IL.Emit(OpCodes.Dup);
				IL.Emit(OpCodes.Stloc_S, loopI());
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalJInc(bool save = true)
			{
				IL.Emit(OpCodes.Ldloc_S, loopJ());
				IL.Emit(OpCodes.Ldc_I8, 1L);
				IL.Emit(OpCodes.Add);
				if (save)
					IL.Emit(OpCodes.Dup);
				IL.Emit(OpCodes.Stloc_S, loopJ());
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalSrcNext(int i)
			{
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldloc_S, loopI());
				IL.Emit(OpCodes.Ldc_I4_0);
				IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = src.PointerI(i, false);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalDstNext(int j)
			{
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Ldloc_S, loopJ());
				IL.Emit(OpCodes.Ldc_I4_1);
				IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
				IL.Emit(OpCodes.Stloc_S, dstPointer(j)); // dstPtrI = dst.PointerI(j, true);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalSrcMove(int i)
			{
				IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
				IL.Emit(OpCodes.Ldloc_S, copiedSrc());
				IL.Emit(OpCodes.Ldc_I8, 0L);
				IL.Emit(OpCodes.Calli, pointerMove1[i]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = srcPtrI.Move(copiedSrc);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalDstMove(int j)
			{
				IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
				IL.Emit(OpCodes.Ldloc_S, copiedDst());
				IL.Emit(OpCodes.Ldc_I8, 0L);
				IL.Emit(OpCodes.Calli, pointerMove2[j]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(j)); // dstPtrJ = dstPtrJ.Move(copiedDst);
			}

			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, copiedSrc()); // long copiedSrc = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, copiedDst()); // long copiedDst = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, copied()); // long copied = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, offsetSrc()); // long offsetSrc = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, offsetDst()); // long offsetDst = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, loopI()); // long i = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, loopJ()); // long j = 0;

			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				for (int j = 0; j < pointerGetters2.Length; j++)
				{
					IL.MarkLabel(branches[i, j]);
					int type = (pointerGetters1[i].GetParameters().Length != 0 ? 0.SetBit(0) : 0) + (pointerGetters2[j].GetParameters().Length != 0 ? 0.SetBit(1) : 0);
					IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
					IL.Emit(OpCodes.Brfalse_S, branches[i + 1, j]); // if (sizeSrcPointerI == 0) goto P[I+1, J];
					if (type.IsBitNotSet(0))
					{   // if (sizeSrcPointerI != 1) throw;
						IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
						IL.Emit(OpCodes.Ldc_I8, 1L);
						IL.Emit(OpCodes.Bne_Un_S, thr);
					}
					IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
					IL.Emit(OpCodes.Brfalse_S, branches[i, j + 1]); // if (sizeSrcPointerJ == 0) goto P[I, J+1];
					if (type.IsBitNotSet(1))
					{   // if (sizeSrcPointerJ != 1) throw;
						IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
						IL.Emit(OpCodes.Ldc_I8, 1L);
						IL.Emit(OpCodes.Bne_Un_S, thr);
					}

					IL.Emit(OpCodes.Ldarg_0);
					if (type.IsBitNotSet(0))
					{
						IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
					}
					else
					{
						IL.Emit(OpCodes.Ldloc_S, loopI());
						IL.Emit(OpCodes.Ldc_I4_0);
						IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
					}
					IL.Emit(OpCodes.Ldloc_S, offsetSrc());
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Calli, pointerMove1[i]);
					IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = src.PointerI(i, false).Move(offsetSrc);
					IL.Emit(OpCodes.Ldarg_1);
					if (type.IsBitNotSet(1))
					{
						IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
					}
					else
					{
						IL.Emit(OpCodes.Ldloc_S, loopJ());
						IL.Emit(OpCodes.Ldc_I4_1);
						IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
					}
					IL.Emit(OpCodes.Ldloc_S, offsetDst());
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Calli, pointerMove2[j]);
					IL.Emit(OpCodes.Stloc_S, dstPointer(j)); // dstPtrJ = dst.PointerJ(j, true).Move(offsetDst);

					Label noMove = IL.DefineLabel(), srcMove = IL.DefineLabel(), dstMove = IL.DefineLabel();
					Label loopStart = default, c1next = default, c2next = default, c3next = default, c1next1 = default, c1next2 = default;
					if (type != 0)
					{
						loopStart = IL.DefineLabel(); c1next = IL.DefineLabel(); c2next = IL.DefineLabel(); c3next = IL.DefineLabel();
						IL.MarkLabel(loopStart);
					}

					// while (true) {
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Ldarg_2);
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Ldarg_3);
					IL.Emit(OpCodes.Ldarg_S, 4);
					IL.Emit(OpCodes.Ldind_I8, 0L);
					IL.Emit(OpCodes.Call, pointerCopy2D[i, j]); // copied = CopyApiSelector.StridedCopy(srcPtrI, srcInc, dstPtrJ, dstInc);
					IL.Emit(OpCodes.Dup);
					IL.Emit(OpCodes.Dup);
					IL.Emit(OpCodes.Stloc_S, copied());
					IL.Emit(OpCodes.Ldarg_2);
					IL.Emit(OpCodes.Mul);
					IL.Emit(OpCodes.Stloc_S, copiedSrc()); // copiedSrc = copied * srcInc;
					IL.Emit(OpCodes.Ldarg_3);
					IL.Emit(OpCodes.Mul);
					IL.Emit(OpCodes.Stloc_S, copiedDst()); // copiedDst = copied * dstInc;
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Calli, pointerLen1[i]);
					IL.Emit(OpCodes.Blt_S, noMove);
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Blt_S, noMove);
					// if (copiedSrc >= srcPtrI.LengthInBytes && copiedDst >= dstPtrJ.LengthInBytes) {
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen1[j]);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_S, offsetSrc()); // offsetSrc = copiedSrc - srcPtrI.LengthInBytes;
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetDst = copiedDst - dstPtrJ.LengthInBytes;
					switch (type)
					{
						case 0b00:
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1];
							break;
						case 0b01:
							LocalIInc();
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Blt_S, c1next); // if (++i >= sizeSrcPointerI) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); i = 0; // goto P[I+1, J+1]; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1];
							break;
						case 0b10:
							LocalJInc();
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Blt_S, c1next); // if (++j >= sizeDstPointerJ) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1]; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J];
							break;
						case 0b11:
							c1next1 = IL.DefineLabel(); c1next2 = IL.DefineLabel();
							LocalIInc(false);
							LocalJInc(false);
							IL.Emit(OpCodes.Ldloc_S, loopI());
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Bge_S, c1next);
							IL.Emit(OpCodes.Ldloc_S, loopJ());
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Bge_S, c1next); // if (++i < sizeSrcPointerI & ++j < sizeDstPointerJ) {
							LocalSrcNext(i);
							LocalDstNext(j);
							IL.Emit(OpCodes.Br_S, loopStart); // continue; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Ldloc_S, loopI());
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Bge_S, c1next1); // else if (i < sizeSrcPointerI) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
							IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1]; }
							IL.MarkLabel(c1next1);
							IL.Emit(OpCodes.Ldloc_S, loopJ());
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Blt_S, c1next2); // else if (j < sizeDstPointerJ) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J]; }
							IL.MarkLabel(c1next2); // else {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI());
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // i = j = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1]; }
							break;
					}
					// }
					// else if (copiedSrc < srcPtrI.LengthInBytes && copiedDst < dstPtrJ.LengthInBytes) {
					IL.MarkLabel(noMove);
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Calli, pointerLen1[i]);
					IL.Emit(OpCodes.Bge_S, dstMove);
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Bge_S, srcMove);
					// do nothing
					// }
					// if (copiedSrc < srcPtrI.LengthInBytes) {
					IL.MarkLabel(srcMove);
					IL.Emit(OpCodes.Ldloc_S, offsetSrc());
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, offsetSrc()); // offsetSrc += copiedSrc;
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetDst = copiedDst - dstPtrJ.LengthInBytes;
					if (type.IsBitNotSet(1))
					{
						IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1];
					}
					else
					{
						LocalSrcMove(i);
						LocalJInc();
						IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
						IL.Emit(OpCodes.Blt_S, c2next); // if (++j >= sizeDstPointerJ) {
						IL.Emit(OpCodes.Ldc_I8, 0L);
						IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
						IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1]; }
						IL.MarkLabel(c2next);
						LocalDstNext(j);
						IL.Emit(OpCodes.Br_S, loopStart); // continue;
					}
					// }
					// else if (copied < dstPtrJ.LengthInBytes) {
					IL.MarkLabel(dstMove);
					IL.Emit(OpCodes.Ldloc_S, offsetDst());
					IL.Emit(OpCodes.Ldloc_S, copiedDst());
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetDst += copiedDst;
					IL.Emit(OpCodes.Ldloc_S, copiedSrc());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen1[j]);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_S, offsetSrc()); // offsetSrc = copiedSrc - srcPtrI.LengthInBytes;
					if (type.IsBitNotSet(0))
					{
						IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J];
					}
					else
					{
						LocalDstMove(j);
						LocalIInc();
						IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
						IL.Emit(OpCodes.Blt_S, c3next); // if (++i >= sizeSrcPointerI) {
						IL.Emit(OpCodes.Ldc_I8, 0L);
						IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
						IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J]; }
						IL.MarkLabel(c3next);
						LocalSrcNext(i);
						IL.Emit(OpCodes.Br_S, loopStart); // continue;
					}
					// }
					// } end while
				}
			}

			IL.MarkLabel(ret);
			IL.Emit(OpCodes.Ret);
			IL.MarkLabel(thr);
			IL.ThrowException(typeof(InvalidOperationException));

			method.DefineParameter(1, ParameterAttributes.In, "source");
			method.DefineParameter(2, ParameterAttributes.In, "destination");
			method.DefineParameter(3, ParameterAttributes.In, "sourceStride");
			method.DefineParameter(4, ParameterAttributes.In, "destinationStride");
			return method.CreateDelegate<Action<TS1, TS2, long, long>>();
		}
		#endregion

		#region extension methods
		private static readonly Dictionary<(RuntimeTypeHandle, RuntimeTypeHandle), Delegate> copy2DFunc = new();

		/// <summary>
		/// Copy the data from <paramref name="source"/> storage to <paramref name="destination"/> storage in 2D/matrix form with <paramref name="height"/> and <paramref name="width"/>.
		/// </summary>
		/// <typeparam name="T">The data type of <typeparamref name="TS1"/> and <typeparamref name="TS2"/></typeparam>
		/// <typeparam name="TS1">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The destination storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage</param>
		/// <param name="destination">The destination storage</param>
		/// <param name="sourceLD">The source array's actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destinationLD">The destination array's actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <c><paramref name="sourceLD"/> and <paramref name="width"/> indicate size larger than <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see></c>, 
		/// or <c><paramref name="destinationLD"/> and <paramref name="width"/> indicate size larger than <paramref name="destination"/>.<see cref="IStorage.LengthInBytes">Length</see></c>
		/// </exception>
		/// <exception cref="InvalidOperationException">If <paramref name="source"/> overlaps with <paramref name="destination"/> or the <see cref="IStorage.PointerGetters"/> of <typeparamref name="TS1"/> or <typeparamref name="TS2"/> are not correct pointer property names</exception>
		public static void Copy2DTo<T, TS1, TS2>(this TS1 source, long sourceLD, TS2 destination, long destinationLD, long height, long width) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (source == destination)
				return;
			if (source.OverlapWith(destination))
				throw new InvalidOperationException(StorageError.CannotCopyOverlap);
			if (sourceLD == destinationLD && sourceLD == height)
			{
				source.CopyTo<T, TS1, TS2>(destination.MakeReference(0, height * width));
				return;
			}
			long sourceLen = sourceLD * width - (sourceLD - height);
			if (sourceLD > source.Length)
				throw new ArgumentOutOfRangeException(nameof(sourceLD), sourceLD, Resources.ParameterError.InvalidValue);
			long destLen = destinationLD * width - (destinationLD - height);
			if (destLen > destination.Length)
				throw new ArgumentOutOfRangeException(nameof(destinationLD), destinationLD, Resources.ParameterError.InvalidValue);
			source.MakeReference(0, sourceLen);
			destination.MakeReference(0, destLen);
			
			RuntimeTypeHandle handle1 = typeof(TS1).TypeHandle, handle2 = typeof(TS2).TypeHandle;
			if (copy2DFunc.TryGetValue((handle1, handle2), out var copier))
				goto FINAL;
			copier = GetCopy2DMethod<T, TS1, TS2>();
			copy2DFunc[(handle1, handle2)] = copier;
		FINAL:
			((Action<TS1, TS2, long, long, long, long>)copier).Invoke(source, destination, sourceLD * Unmanaged<T>.Size, destinationLD * Unmanaged<T>.Size, height, width);
		}

		/// <summary>
		/// Copy the data from <paramref name="source"/> storage to <paramref name="destination"/> span in 2D/matrix form with <paramref name="height"/> and <paramref name="width"/>.
		/// </summary>
		/// <typeparam name="T">The data type of <typeparamref name="TS"/> and <paramref name="destination"/></typeparam>
		/// <typeparam name="TS">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage</param>
		/// <param name="destination">The destination span</param>
		/// <param name="sourceLD">The source array's actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destinationLD">The destination array's actual height (actual leading dimension) in <typeparamref name="T"/>, default 0 means the same as <paramref name="height"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/></param>
		public static unsafe void ToManaged2D<T, TS>(this TS source, long sourceLD, Span<T> destination, long height, long width, long destinationLD = 0) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (destinationLD == 0)
				destinationLD = height;
			fixed (T* dst = destination)
			{
				ManagedPointer mp = new(new(dst), destination.Length * sizeof(T));
				ManagedPureStorage<T> ms = new(mp);
				Copy2DTo<T, TS, ManagedPureStorage<T>>(source, sourceLD, ms, destinationLD, height, width);
			}
		}

		/// <summary>
		/// Copy the data from <paramref name="source"/> span to <paramref name="destination"/> storage in 2D/matrix form with <paramref name="height"/> and <paramref name="width"/>.
		/// </summary>
		/// <typeparam name="T">The data type of <typeparamref name="TS"/> and <paramref name="destination"/></typeparam>
		/// <typeparam name="TS">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source span</param>
		/// <param name="destination">The destination storage</param>
		/// <param name="sourceLD">The source array's actual height (actual leading dimension) in <typeparamref name="T"/>, default 0 means the same as <paramref name="height"/></param>
		/// <param name="destinationLD">The destination array's actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/></param>
		public static unsafe void FromManaged2D<T, TS>(this Span<T> source, long destinationLD, TS destination, long height, long width, long sourceLD = 0) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (sourceLD == 0)
				sourceLD = height;
			fixed (T* src = source)
			{
				ManagedPointer mp = new(new(src), source.Length * sizeof(T));
				ManagedPureStorage<T> ms = new(mp);
				Copy2DTo<T, ManagedPureStorage<T>, TS>(ms, sourceLD, destination, destinationLD, height, width);
			}
		}

		private static readonly Dictionary<(RuntimeTypeHandle, RuntimeTypeHandle), Delegate> copyStridedFunc = new();

		/// <summary>
		/// Copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The destination storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage to copy from</param>
		/// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination storage to copy to</param>
		/// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		public static long StridedCopyTo<T, TS1, TS2>(this TS1 source, long strideSource, TS2 destination, long strideDestination) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (source.OverlapWith(destination))
				throw new InvalidOperationException(StorageError.CannotCopyOverlap);
			if (strideSource < 1)
				throw new ArgumentOutOfRangeException(nameof(strideSource));
			if (strideDestination < 1)
				throw new ArgumentOutOfRangeException(nameof(strideDestination));
			if (source == destination)
				return 0;
			if (strideSource == 1 && strideDestination == 1)
			{
				return source.CopyTo<T, TS1, TS2>(destination);
			}

			RuntimeTypeHandle handle1 = typeof(TS1).TypeHandle, handle2 = typeof(TS2).TypeHandle;
			if (copyStridedFunc.TryGetValue((handle1, handle2), out var copier))
				goto FINAL;
			copier = GetCopyStridedMethod<T, TS1, TS2>();
			copyStridedFunc[(handle1, handle2)] = copier;
		FINAL:
			((Action<TS1, TS2, long, long>)copier).Invoke(source, destination, strideSource, strideDestination);
			return Math.Max((source.Length - 1) / strideSource + 1, (destination.Length - 1) / strideDestination + 1);
		}

		/// <summary>
		/// Copy the <paramref name="source"/> storage to <paramref name="destination"/> span with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage to copy from</param>
		/// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination span to copy to</param>
		/// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		public static unsafe long ToManagedStride<T, TS>(this TS source, long strideSource, Span<T> destination, long strideDestination = 1) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			fixed (T* dst = destination)
			{
				ManagedPointer mp = new(new(dst), destination.Length * sizeof(T));
				ManagedPureStorage<T> ms = new(mp);
				return StridedCopyTo<T, TS, ManagedPureStorage<T>>(source, strideSource, ms, strideDestination);
			}
		}

		/// <summary>
		/// Copy the <paramref name="source"/> span to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source span to copy from</param>
		/// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination storage to copy to</param>
		/// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		public static unsafe long FromManagedStride<T, TS>(this Span<T> source, long strideSource, TS destination, long strideDestination) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			fixed (T* src = source)
			{
				ManagedPointer mp = new(new(src), source.Length * sizeof(T));
				ManagedPureStorage<T> ms = new(mp);
				return StridedCopyTo<T, ManagedPureStorage<T>, TS>(ms, strideSource, destination, strideDestination);
			}
		}
		#endregion
	}
}
