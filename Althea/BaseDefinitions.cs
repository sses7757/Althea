using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Memory;


namespace Althea
{
	/// <summary>
	/// The base interface for all runtime APIs defined in this assembly
	/// </summary>
	public abstract class AbstractRuntimeApi : IDisposable
	{
		#region basic
		/// <summary>
		/// Release all unmanaged resources held by this class
		/// </summary>
		public abstract void Dispose();
		#endregion

		#region support information
		/// <summary>
		/// Get the supported memory locations for all unary operations. Each flag in this value indicates a support of a certain location.
		/// </summary>
		public abstract StorageLocation SupportedUnaryLocations { get; }

		/// <summary>
		/// Get list of the supported memory locations for all binary operations. Each value must has exactly one or two flags to indicate a supported pair of certain locations.
		/// </summary>
		public abstract IReadOnlyList<StorageLocation> SupportedBinaryLocations { get; }

		/// <summary>
		/// Get list of the supported memory locations for all ternary operations. Each value must has exactly one to three flags to indicate a supported triple of certain locations.
		/// </summary>
		public abstract IReadOnlyList<StorageLocation> SupportedTernaryLocations { get; }

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported memory locations for all N-ary operations. Each value must has exactly one to three flags to indicate a supported triple of certain locations.
		/// </summary>
		/// <param name="N">the number of operands</param>
		/// <returns>The list of the supported memory locations for all N-ary operations.</returns>
		public virtual IReadOnlyList<StorageLocation> SupportedNaryLocations(int N)
		{
			return N switch
			{
				1 => new[] { this.SupportedUnaryLocations },
				2 => this.SupportedBinaryLocations,
				3 => this.SupportedTernaryLocations,
				_ => this.Direct_SupportedNaryLocations(N),
			};
		}

		/// <summary>
		/// Get list of the supported memory locations for all N-ary operations. This method will only be invoked internally with <paramref name="N"/> &gt; 3.
		/// </summary>
		/// <param name="N">the number of operands</param>
		/// <returns>The list of the supported memory locations for all N-ary operations.</returns>
		protected abstract IReadOnlyList<StorageLocation> Direct_SupportedNaryLocations(int N);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by unary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (can be combination of flags)</param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedUnitary(StorageLocation location) => location != StorageLocation.Uri && (location & this.SupportedUnaryLocations) == location;

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by binary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (must has exactly one or two flags)</param>
		/// <returns>Whether binary operations between <paramref name="location"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(StorageLocation location) => location.NumberOfFlags() <= 2 && this.SupportedBinaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="StorageLocation"/>s are supported by binary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <param name="location2">the second given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(StorageLocation location1, StorageLocation location2)
			=> location1.IsFlag() && location2.IsFlag() && this.SupportedTernaryLocations.Contains(location1 | location2);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by ternary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (must has exactly one to three flags)</param>
		/// <returns>Whether ternary operations between <paramref name="location"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedTernary(StorageLocation location) => location.NumberOfFlags() <= 3 && this.SupportedTernaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="StorageLocation"/>s are supported by ternary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <param name="location2">the second given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <param name="location3">the third given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <returns>Whether ternary operations between <paramref name="location1"/> and <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(StorageLocation location1, StorageLocation location2, StorageLocation location3)
			=> location1.IsFlag() && location2.IsFlag() && location3.IsFlag() && this.SupportedTernaryLocations.Contains(location1 | location2 | location3);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by N-ary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (must has exactly one to N flags)</param>
		/// <returns>Whether N-ary operations between <paramref name="location"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedNary(StorageLocation location) => this.SupportedNaryLocations(location.NumberOfFlags()).Contains(location);

		/// <summary>
		/// Check if the given <paramref name="locations"/> are supported by N-ary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="locations">the given <see cref="StorageLocation"/>s (must has exactly one or two flags)</param>
		/// <returns>Whether N-ary operations between <paramref name="locations"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedNary(params StorageLocation[] locations)
			=> locations.All(l => l.IsFlag()) && this.SupportedTernaryLocations.Contains(locations.Aggregate((l1, l2) => l1 | l2, (StorageLocation)0));
		#endregion
	}
}
