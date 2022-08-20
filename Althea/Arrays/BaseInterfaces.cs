using System.Text.Json;
using System.Text.Json.Serialization;

using Althea.Helpers;


namespace Althea.Array;

/// <summary>
/// The interface of abstract arrays.
/// </summary>
/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
public interface IArray<T> where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// When implemented by a derived class, get the size (in <typeparamref name="T"/>) of this array (the extent at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
	/// </summary>
	ReadOnlySpan<long> Size { get; }
}

/// <summary>
/// The interface of (column-major) dense arrays that may exist extra pitch at each dimension and thus the strides are not simply the accumulated product of its size.
/// </summary>
/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
public interface IPitchedArray<T> : IArray<T> where T : unmanaged, IBaseNumber<T>
{
	#region properties
	/// <summary>
	/// When implemented by a derived class, get the pitch (in <typeparamref name="T"/>) of this array (the outer size at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>. It must has length equals to <see cref="IArray{T}.Size"/> and consists numbers larger than or equals to <see cref="IArray{T}.Size"/> respectively.
	/// </summary>
	ReadOnlySpan<long> OuterSize { get; }

	/// <summary>
	/// When implemented by a derived class, check whether this array is actually pitched. The default implementation simply checks the point-wise equality of <see cref="IArray{T}.Size"/> and <see cref="OuterSize"/>.
	/// </summary>
	bool HasPitch => !this.OuterSize.SequenceEqual(this.Size);

	/// <summary>
	/// When implemented by a derived class, get the strides of this array at all dimensions as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
	/// </summary>
	/// <remarks>Usually, the first element is 1 and the last element shall be the product of <see cref="OuterSize"/>. The returned <see cref="ReadOnlySpan{T}.Length">size</see> == rank + 1</remarks>
	ReadOnlySpan<long> Strides { get; }
	#endregion
}

/// <summary>
/// The interface of (column-major) dense arrays with only one value storage that may has pitch.
/// </summary>
/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
/// <typeparam name="TS">The storage type used by the value array</typeparam>
public interface IDenseArray<T, TS> : IPitchedArray<T> where T : unmanaged, IBaseNumber<T> where TS : class, Storage.IStorage<T, TS>
{
	#region properties
	/// <summary>
	/// When implemented by a derived class, get the value array of this dense array.
	/// </summary>
	TS Storage { get; }

	/// <summary>
	/// The <see cref="JsonSerializerOptions"/> for <typeparamref name="TS"/>.
	/// </summary>
	protected static JsonSerializerOptions JsonSerializeOptions { get; } = new()
	{
		Converters = { TS.JsonConverter },
		WriteIndented = true,
	};
	#endregion
}

/// <summary>
/// The base interface for sparse arrays.
/// </summary>
/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
public interface ISparseArray<T> : IArray<T> where T : unmanaged, IBaseNumber<T>
{
	#region properties
	/// <summary>
	/// When implemented by a derived class, get the sparse format of this sparse array as a <see cref="SparseFormat"/>.
	/// </summary>
	SparseFormat Format { get; }

	/// <summary>
	/// When implemented by a derived class, get the default value of this sparse array
	/// </summary>
	T DefaultValue { get; }

	/// <summary>
	/// When implemented by a derived class, get number of elements actually stored this sparse vector.
	/// </summary>
	long NStored { get; }
	#endregion
}

// Ignore Spelling: readonly
/// <summary>
/// The interface for polymorphism subtype JSON convertible type <typeparamref name="TSelf"/>.
/// </summary>
/// <typeparam name="TSelf">The actual type whose subtypes will be serialized and deserialized polymorphically which must be abstract or an interface.</typeparam>
/// <remarks>For this interface to work, the <see cref="JsonSerializerOptions.Converters"/> shall contains a <see cref="JsonConverter"/>.<br/>
/// And any subtype of <typeparamref name="TSelf"/> shall be able to correctly deserialized by <see cref="JsonSerializer.Deserialize{TSelf}(string, JsonSerializerOptions?)"/> (e.g. via a constructor with <see cref="JsonConstructorAttribute"/>).</remarks>
/// <example><code>
/// public abstract class BaseClass : <see cref="ISubtypeJsonConvertible{TSelf}"/>
/// {
///		// functional codes...
///		
///		private static readonly JsonSerializerOptions options = new() { <see cref="JsonSerializerOptions.Converters"/> = { <see cref="ISubtypeJsonConvertible{TSelf}"/>&lt;BaseClass&gt; };
/// 
///		public string Serialize()
///		{
///			return <see cref="JsonSerializer.Serialize{TValue}(TValue, JsonSerializerOptions?)">JsonSerializer.Serialize</see>(this, options);
///		}
///		
///		public static BaseClass Deserialize(string json)
///		{
///			return <see cref="JsonSerializer.Deserialize{TValue}(string, JsonSerializerOptions?)">JsonSerializer.Deserialize</see>(this, options);
///		}
/// }
/// </code></example>
public interface ISubtypeJsonConvertible<TSelf> where TSelf : ISubtypeJsonConvertible<TSelf>
{
	#region JSON serialization
	/// <summary>
	/// The polymorphism JSON converter for <typeparamref name="TSelf"/>'s sub-types.
	/// </summary>
	protected sealed class JsonConverter : JsonConverter<TSelf>
	{
		static JsonConverter()
		{
			if (!typeof(TSelf).IsAbstract && !typeof(TSelf).IsInterface)
				throw new InvalidOperationException(Resources.ParameterError.UnexpectedType);
		}

		/// <summary>
		/// The default constructor for <see cref="JsonConverter"/>
		/// </summary>
		public JsonConverter()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes().Where(static t => t.IsAssignableTo(typeof(TSelf))))
				{
					if (type == typeof(TSelf) || type.IsAbstract)
						continue;
					if (!type.GetConstructors()
							 .Where(static c => c.CustomAttributes
									.Select(static a => a.AttributeType)
									.Contains(typeof(JsonConstructorAttribute)))
							 .Any())
						continue;
					try
					{
						var func = typeof(JsonSerializer).GetMethod(nameof(JsonSerializer.Deserialize), 0, new[] { typeof(Utf8JsonReader).MakeByRefType(), typeof(JsonSerializerOptions) })?.MakeGenericMethod(type)?.CreateDelegate<ReadDelegate>();
						if (func is null)
							continue;
						constructors.Add(type, func);
					}
					catch (Exception)
					{
						continue;
					}
				}
			}
		}

		private delegate TSelf? ReadDelegate(ref Utf8JsonReader reader, JsonSerializerOptions options);

		private readonly Dictionary<Type, ReadDelegate> constructors = new();

		private const string TYPE_NAME = "$type", PROP_NAME = "$value";

		/// <inheritdoc/>
		public override TSelf? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			// read type
			if (!reader.Read())
				throw new JsonException();
			if (reader.GetString() != TYPE_NAME)
				throw new JsonException(Resources.ParameterError.UnexpectedValue);
			if (!reader.Read())
				throw new JsonException();
			Type? type;
			try
			{
				type = Type.GetType(reader.GetString() ?? "");
			}
			catch (Exception e)
			{
				throw new JsonException(Resources.ParameterError.UnexpectedType, e);
			}
			if (type is null || !type.IsAssignableTo(typeof(TSelf)))
				throw new JsonException(Resources.ParameterError.InvalidValue);
			// read properties
			if (!reader.Read())
				throw new JsonException();
			if (reader.GetString() != PROP_NAME)
				throw new JsonException(Resources.ParameterError.UnexpectedValue);
			if (!reader.Read())
				throw new JsonException();
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();
			// deserialize
			var result = constructors[type].Invoke(ref reader, options);
			// end
			if (!reader.Read())
				throw new JsonException();
			if (reader.TokenType != JsonTokenType.EndObject)
				throw new JsonException();
			return result;
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, TSelf value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString(TYPE_NAME, value.GetType().AssemblyQualifiedName);
			writer.WritePropertyName(PROP_NAME);
			{
				// "object" will be changed to actual type during serialization
				JsonSerializer.Serialize(writer, (object)value, options);
			}
			writer.WriteEndObject();
		}
	}
	#endregion
}

/// <summary>
/// The complicated interface for sparse arrays.
/// </summary>
/// <typeparam name="T">Any unmanaged number as the value data type</typeparam>
/// <typeparam name="TS">The storage type used by the value array(s)</typeparam>
/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
public interface ISparseArray<T, TInd, TS, TSInd> : ISparseArray<T>
	where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
	where TS : class, Storage.IStorage<T, TS> where TSInd : class, Storage.IStorage<TInd, TSInd>
{
	#region properties
	/// <summary>
	/// When implemented by a derived class, get the value array(s) of this sparse array
	/// </summary>
	ReadOnlySpan<TS> ValueStorages { get; }

	/// <summary>
	/// When implemented by a derived class, get the index array(s) of this sparse array
	/// </summary>
	ReadOnlySpan<TSInd> IndexStorages { get; }

	/// <summary>
	/// When implemented by a derived class, get the constant block size of this sparse array, can be empty if it is not a <see cref="SparseFormat.Blocking.Simple"/>
	/// </summary>
	ReadOnlySpan<long> BlockSize { get; }

	/// <summary>
	/// The <see cref="JsonSerializerOptions"/> for <typeparamref name="TS"/>.
	/// </summary>
	protected static JsonSerializerOptions JsonSerializeOptions { get; } = new()
	{
		Converters = { TS.JsonConverter, TSInd.JsonConverter },
		WriteIndented = true,
	};
	#endregion
}

/// <summary>
/// The interface for tensor that contains basic members (size and labels).
/// </summary>
/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
public interface ILabeledTensor<T> where T : unmanaged, IBaseNumber<T>
{
	#region properties
	/// <summary>
	/// When implemented by a derived class, get the rank of this tensor.
	/// </summary>
	int Rank { get; }

	/// <summary>
	/// When implemented by a derived class, get the size of this array (the extent at all dimensions) in <typeparamref name="T"/> as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
	/// </summary>
	ReadOnlySpan<long> Size { get; }

	/// <summary>
	/// When implemented by a derived class, get the presenting length (in <typeparamref name="T"/>) of this tensor.
	/// </summary>
	long Length { get; }

	/// <summary>
	/// When implemented by a derived class, get or set the label array as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> used to mark each index of this tensor.
	/// </summary>
	/// <exception cref="ArgumentException">If the setting value's length is not the same as the <see cref="Rank"/></exception>
	ReadOnlySpan<char> Labels { get; set; }
	#endregion

	#region method
	/// <summary>
	/// When implemented by a derived class, get the label at rank <paramref name="index"/>
	/// </summary>
	/// <param name="index">The index of the rank whose label will be obtained</param>
	/// <returns>The <see cref="char"/> label at <paramref name="index"/></returns>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range of <see cref="Rank"/></exception>
	char GetLabel(int index);

	/// <summary>
	/// When implemented by a derived class, set the label at rank <paramref name="index"/> to <paramref name="value"/>
	/// </summary>
	/// <param name="index">The index of the rank whose label will be set</param>
	/// <param name="value">The <see cref="char"/> label at <paramref name="index"/> to set</param>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range of <see cref="Rank"/></exception>
	void SetLabel(int index, char value);

	/// <summary>
	/// When implemented by a derived class, set the label(s) used to mark each index of this tensor
	/// </summary>
	/// <param name="labels">The label(s) to set as an array of <see cref="char"/></param>
	/// <exception cref="ArgumentNullException">If <paramref name="labels"/> is null or empty</exception>
	/// <exception cref="ArgumentException">If the length of <paramref name="labels"/> is not the same as the <see cref="Rank"/></exception>
	void SetLabels(params char[] labels);
	#endregion
}

/// <summary>
/// The interface for printable arrays.
/// </summary>
/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
public interface IPrintable<T> where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// When implemented by a derived class, print this array to <see cref="string"/> under given print <paramref name="settings"/>.
	/// </summary>
	/// <param name="settings">The <see cref="PrintSettings"/> to use, default null means <see cref="Settings.PrintSetting"/></param>
	/// <returns>The printed array as a <see cref="string"/>.</returns>
	string Print(PrintSettings? settings = null);
}
