using System.Diagnostics;


namespace Althea.Helpers
{
	#region interface
	/// <summary>
	/// The interface for fixed buffer structures
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	public interface IFixedBuffer<T> : IReadOnlyList<T> where T : unmanaged, IEquatable<T>
	{
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		int NonDefaults { get; }

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index as a <see cref="int"/></param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		new T this[int index] { get; }

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		T[] ToArray();

		internal T[] Data => this.ToArray();

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		IFixedBuffer<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>;

		/// <summary>
		/// Copy the data from this fixed buffer to the given <paramref name="span"/>
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> to copy to</param>
		/// <param name="offset">The offset to start copying in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="span"/> is too large</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> is out of boundary</exception>
		void CopyToSpan(Span<T> span, int offset = 0);

		/// <summary>
		/// Convert this fixed buffer to a new <typeparamref name="TStruct"/> by copying the values from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="copyStart">The start position to copy in bytes</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		TStruct ToStruct<TStruct>(int copyStart = 0) where TStruct : struct;

		/// <summary>
		/// Check whether the this fixed buffer contains the given <paramref name="value"/> 
		/// </summary>
		/// <param name="value">The value to find</param>
		/// <returns>Whether the this fixed buffer contains the given <paramref name="value"/> </returns>
		bool Contains(T value);
	}
	#endregion

	#region debug view
	internal interface IAsSpan<T>
	{
		Span<T> AsSpan(int size = 0);
	}

	internal sealed class FixedBufferDebugView<T>
	{
		private readonly T[] m_array;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items => this.m_array;

		public FixedBufferDebugView(IAsSpan<T> s)
		{
			this.m_array = s.AsSpan().ToArray();
		}
	}
	#endregion
}
