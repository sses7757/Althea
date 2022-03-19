using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

using Althea.Linq;
using Althea.Arrays;
using Althea.NativeTypes;

using MEM = Althea.Storage.IAbstractApi;
namespace Althea.Storage
{
	#region URI related
	/// <summary>
	/// The enum representing the URI schemes which can be used as a storage location detail <see cref="StorageLocation.Detail"/>.
	/// </summary>
	/// <remarks>See <see cref="Uri.UriSchemeFile"/>, etc.</remarks>
	public enum UriScheme : short
	{
		/// <summary>
		/// Specifies that the URI scheme is unknown
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Specifies that the URI is a pointer to a file
		/// </summary>
		File = 1,
		/// <summary>
		/// Specifies that the URI is accessed through the TCP/IP directly.
		/// </summary>
		TCP = 2,
		/// <summary>
		/// Specifies that the URI is accessed through the File Transfer Protocol (FTP).
		/// </summary>
		FTP = 3,
		/// <summary>
		/// Specifies that the URI is accessed through the Hypertext Transfer Protocol (HTTP).
		/// </summary>
		HTTP = 4,
		/// <summary>
		/// Specifies that the URI is accessed through the Secure Hypertext Transfer Protocol (HTTPS).
		/// </summary>
		HTTPS = 5,
	}

	/// <summary>
	/// The static class for extension methods of <see cref="UriScheme"/>
	/// </summary>
	public static class UriSchemeExtension
	{
		/// <summary>
		/// Get the <see cref="UriScheme"/> from a <see cref="Uri"/>
		/// </summary>
		/// <param name="uri">The absolute <see cref="Uri"/></param>
		/// <returns>the <see cref="UriScheme"/> of <paramref name="uri"/>, or <see cref="UriScheme.Unknown"/> if <paramref name="uri"/>'s scheme is not in <see cref="UriScheme"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="uri"/> is not an absolute URI</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UriScheme GetScheme(this Uri uri)
		{
			if (!uri.IsAbsoluteUri)
				throw new ArgumentOutOfRangeException(nameof(uri), uri, Parameter.InvalidValue);
			if (uri.Scheme == Uri.UriSchemeFile)
				return UriScheme.File;
			if (uri.Scheme == @"tcp" || uri.Scheme == Uri.UriSchemeNetTcp)
				return UriScheme.TCP;
			if (uri.Scheme == Uri.UriSchemeFtp)
				return UriScheme.FTP;
			if (uri.Scheme == Uri.UriSchemeHttp)
				return UriScheme.HTTP;
			if (uri.Scheme == Uri.UriSchemeHttps)
				return UriScheme.HTTPS;
			if (EnumHelper.TryParse(uri.Scheme, out UriScheme s))
				return s;
			return UriScheme.Unknown;
		}
	}
	#endregion

	#region custom JSON converters
	internal sealed class TypeConverter : JsonConverter<Type>
	{
		public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException();
			try
			{
				return Type.GetType(reader.GetString() ?? string.Empty);
			}
			catch (Exception e)
			{
				throw new JsonException(Resources.ParameterError.UnexpectedType, e);
			}
		}

		public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.AssemblyQualifiedName);
		}
	}

	internal sealed class LocationDescriptionConverter : JsonConverter<CombinationOfLocations>
	{
		public override CombinationOfLocations Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();
			try
			{
				CombinationType type = default;
				StorageLocation[]? locations = null;
				while (reader.Read())
				{
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						if (type == default || locations is null || locations.Length == 0)
							throw new JsonException(Resources.ParameterError.WrongSize);
						return new CombinationOfLocations(type, locations);
					}
					if (reader.TokenType != JsonTokenType.PropertyName)
						throw new JsonException();
					switch (reader.GetString())
					{
						case Type:
							reader.Read();
							string? a = reader.GetString();
							type = a is null ? default : Enum.Parse<CombinationType>(a);
							break;
						case Locations:
							reader.Read();
							var loc = JsonSerializer.Deserialize<int[]>(ref reader, options);
							locations = loc?.Select(static l => new StorageLocation(l))?.ToArray();
							break;
						default:
							throw new JsonException();
					}
				}
				// read to end while not ended
				throw new JsonException();
			}
			catch (Exception e)
			{
				if (e is JsonException)
					throw;
				else
					throw new JsonException(Resources.ParameterError.UnexpectedValue, e);
			}
		}

		private const string Type = nameof(CombinationOfLocations.Type), Locations = "Locations";

		public override void Write(Utf8JsonWriter writer, CombinationOfLocations value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			{
				writer.WriteString(Type, value.Type.ToString());
				////writer.WriteNumber(nameof(value.Count), value.Count);
				writer.WriteStartArray(Locations);
				foreach (var l in value)
				{
					writer.WriteNumberValue(l.AsInt());
				}
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}
	}

	internal sealed class PointerSegmentConverter : JsonConverter<PointerSegment>
	{
		private sealed class TempPointer : IPointer
		{
			public StorageLocation Location { get; set; }

			public long LengthInBytes { get; set; }

			public string StringMain => throw new NotImplementedException();

			public IEnumerable<KeyValuePair<string, object?>> StringProperties => throw new NotImplementedException();

			public bool Equals(IPointer? other) => throw new NotImplementedException();

			public bool IsValid() => true;

			public override string ToString() => throw new NotImplementedException();
		}

		public override PointerSegment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();
			try
			{
				StorageLocation location = default;
				long lengthInBytes = 0;
				while (reader.Read())
				{
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						if (lengthInBytes == 0 || location == default)
							throw new JsonException(Resources.ParameterError.WrongSize);
						return new PointerSegment(new TempPointer { LengthInBytes = lengthInBytes, Location = location });
					}
					if (reader.TokenType != JsonTokenType.PropertyName)
						throw new JsonException();
					switch (reader.GetString())
					{
						case Location:
							reader.Read();
							location = new(JsonSerializer.Deserialize<int>(ref reader, options));
							break;
						case Length:
							reader.Read();
							lengthInBytes = JsonSerializer.Deserialize<long>(ref reader, options);
							break;
						default:
							throw new JsonException();
					}
				}
				// read to end while not ended
				throw new JsonException();
			}
			catch (Exception e)
			{
				if (e is JsonException)
					throw;
				else
					throw new JsonException(Resources.ParameterError.UnexpectedValue, e);
			}
		}

		private const string Location = nameof(PointerSegment.Location), Length = nameof(PointerSegment.LengthInBytes);

		public override void Write(Utf8JsonWriter writer, PointerSegment value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			{
				writer.WriteNumber(Location, value.Location.AsInt());
				writer.WriteNumber(Length, value.LengthInBytes);
			}
			writer.WriteEndObject();
		}
	}

	internal sealed class IStorageConverter : JsonConverter<IStorage>
	{
		private static readonly JsonConverter<CombinationOfLocations> LocationConverter = new LocationDescriptionConverter();

		private static readonly JsonConverter<PointerSegment> PointerConverter = new PointerSegmentConverter();

		private static readonly JsonConverter<Type> TypeConverter = new TypeConverter();

		public override IStorage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();

			IStorage? result = null;
			try
			{
				Type? dataType = null;
				CombinationOfLocations locations = default;
				List<PointerSegment> pointers = new();
				bool success = false;
				while (reader.Read())
				{
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						success = true;
						break;
					}
					if (reader.TokenType != JsonTokenType.PropertyName)
						throw new JsonException();
					switch (reader.GetString())
					{
						case DataType:
							reader.Read();
							dataType = TypeConverter.Read(ref reader, typeToConvert, options) ?? throw new JsonException(Resources.ParameterError.UnexpectedType);
							break;
						case Locations:
							reader.Read();
							locations = LocationConverter.Read(ref reader, typeToConvert, options);
							break;
						case Pointers:
							reader.Read();
							if (reader.TokenType != JsonTokenType.StartArray)
								throw new JsonException();
							while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
							{
								pointers.Add(PointerConverter.Read(ref reader, typeToConvert, options));
							}
							break;
						default:
							throw new JsonException();
					}
				}
				if (!success)
				{
					// read to end while not ended
					throw new JsonException();
				}
				// allocate and create empty storage
				if (dataType is null || locations == default || pointers.Count == 0)
					throw new JsonException(Resources.ParameterError.WrongSize);
				var type = dataType.ToDataType();
				int size = type.Bytes();
				if (pointers.Any(p => p.LengthInBytes % size != 0))
					throw new JsonException(Resources.Other.CannotDivide);
				var createFunc = typeof(StorageFactory<>).MakeGenericType(dataType)
														 .GetMethod(nameof(StorageFactory<int>.Create))?
														 .CreateDelegate<CreateDelegate>();
				if (createFunc is null)
					throw new JsonException(Resources.Other.CannotFindMethod);
				var spanLoc = locations.CopyLocationsToSpan(stackalloc StorageLocation[locations.Count]);
				Span<long> spanLen = stackalloc long[pointers.Count];
				pointers.CopyTo(spanLen, p => p.LengthInBytes / size);
				return createFunc.Invoke(locations.Type, spanLoc, spanLen);
			}
			catch (Exception e)
			{
				result?.Dispose();
				if (e is JsonException)
					throw;
				else
					throw new JsonException(Resources.ParameterError.UnexpectedValue, e);
			}
		}

		private const string DataType = "DataType", Locations = nameof(IStorage.LocationDescription), Pointers = "Pointers";

		public override void Write(Utf8JsonWriter writer, IStorage value, JsonSerializerOptions options)
		{
			Type storageType = value.GetType();
			if (!storageType.IsGenericType || storageType.GenericTypeArguments.Length != 1)
				throw new NotSupportedException(Resources.Other.InvalidGeneric);
			Type type = storageType.GenericTypeArguments[0];
			writer.WriteStartObject();
			{
				// data type
				writer.WriteString(DataType, type.AssemblyQualifiedName);
				// LocationDescription
				writer.WritePropertyName(Locations);
				LocationConverter.Write(writer, value.LocationDescription, options);
				// PointerSegments
				writer.WriteStartArray(Pointers);
				foreach (var p in value)
				{
					PointerConverter.Write(writer, p, options);
				}
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}
	}

	internal sealed class ByteArrayConverter : JsonConverter<byte[]>
	{
		public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			try
			{
				string? s = reader.GetString();
				if (s is null)
					throw new ArgumentNullException(nameof(reader));
				return Encoding.ASCII.GetBytes(s);
			}
			catch (Exception e)
			{
				throw new JsonException(Resources.ParameterError.InvalidValue, e);
			}
		}

		public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(Encoding.ASCII.GetString(value));
		}
	}
	#endregion

	#region array head
	internal record ArrayInfo : IDisposable
	{
		[JsonConstructor]
		public ArrayInfo(Type type, long[] size, IReadOnlyDictionary<string, object>? metaData, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, byte[]>? checkSums)
		{
			try
			{
				if (type is null)
					throw new ArgumentNullException(nameof(type));
				if (size is null || size.Length == 0)
					throw new ArgumentNullException(nameof(size));
				if (storages is null || storages.Count == 0)
					throw new ArgumentNullException(nameof(storages));
				if (checkSums is not null && checkSums.Count != storages.Count)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(checkSums));
			}
			catch (Exception)
			{
				if (storages is not null)
				{
					foreach (var kv in storages)
					{
						kv.Value?.Dispose();
					}
				}
				throw;
			}
			this.Type = type; this.Size = size; this.MetaData = metaData; this.Storages = storages; this.CheckSums = checkSums;
		}

		public Type Type { get; }

		public long[] Size { get; }

		// string-keyed dictionary can be automatically serialized and deserialized by System.Text.Json
		// the objects must has [JsonConverter(...)] or can be converted by internal converters
		public IReadOnlyDictionary<string, object>? MetaData { get; }

		// string-keyed dictionary can be automatically serialized and deserialized by System.Text.Json
		public IReadOnlyDictionary<string, IStorage> Storages { get; }

		public IReadOnlyDictionary<string, byte[]>? CheckSums { get; }

		public void Dispose()
		{
			if (this.Storages is null)
				return;
			foreach (var kv in this.Storages)
			{
				kv.Value?.Dispose();
			}
		}
	}
	#endregion

	#region storage stream
	internal sealed class StorageStream : Stream
	{
		private readonly Storage<byte> storage;

		private long offset = 0;

		public StorageStream(IStorage storage) => this.storage = storage.AsByteStorage();

		public override bool CanRead => this.storage.IsOffsetValid(offset);

		public override bool CanSeek => true;

		public override bool CanWrite => this.storage.IsOffsetValid(offset);

		public override long Length => this.storage.Length;

		public override long Position {
			get => this.offset;
			set => this.offset = Math.Min(value, this.storage.Length);
		}

		public override long Seek(long offset, SeekOrigin origin) => this.Position = offset;

		public override void SetLength(long value) => throw new InvalidOperationException();

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count) => (int)MEM.ToManaged(this.storage + offset, new Span<byte>(buffer, offset, count));

		public override void Write(byte[] buffer, int offset, int count) => MEM.FromManaged(this.storage + offset, new Span<byte>(buffer, offset, count));
	}
	#endregion

	/// <summary>
	/// The static class as a file manager that provides conversion between arrays and files
	/// </summary>
	public static class ArrayFile
	{
		private const string PointerExtension = ".ptr", HeadFileName = "head.json";

		#region write to file
		private static void WriteFile(string folder, string name, IStorage storage)
		{
			long length = storage.LengthInBytes;
			Storage<byte> byteStorage = storage.AsByteStorage();
			string path = Path.Join(folder, name + PointerExtension);
			var storageAPI = MEM.GetFirstImplCanFileTransfer(storage.LocationDescription);
			if (storageAPI is null)
			{
				Directory.CreateDirectory(folder);
				using var file = File.Create(path);
				using var storageStream = new StorageStream(storage);
				storageStream.CopyTo(file);
			}
			else
			{
				string tempPath = path + ".temp";
				var pointer = storageAPI.AllocateFileAt(tempPath, length);
				try
				{
					MEM.MemoryCopy(byteStorage, new PureReferenceStorage<byte>(pointer));
					File.Copy(tempPath, path);
				}
				finally
				{
					MEM.Free(pointer, true);
				}
			}
		}

		private unsafe static IReadOnlyDictionary<string, byte[]> CheckCode(IReadOnlyDictionary<string, IStorage> pointers)
		{
			var hash = new Dictionary<string, byte[]>(pointers.Count);
			using var sha = System.Security.Cryptography.SHA512.Create();
			//sha.Initialize();
			foreach (var item in pointers)
			{
				using var stream = new StorageStream(item.Value);
				hash.Add(item.Key, sha.ComputeHash(stream));
			}
			return hash;
		}

		/// <summary>
		/// Save the target <paramref name="array"/> to file with relative or absolute folder path <paramref name="folder"/> synchronously.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="array">The array to save</param>
		/// <param name="folder">The folder to save to, default is a random hash code as folder name. It must be a non-existing or empty folder</param>
		/// <param name="check">Whether to calculate check sum or not, default null means do not override</param>
		/// <param name="compress">Whether to compress the result folder as a .zip file or remains it a folder</param>
		/// <returns>the folder / file name</returns>
		/// <exception cref="IOException">if the target folder already contains file(s)</exception>
		public static string ToFile<T>(this ValueArray<T> array, string? folder = null, bool check = false, bool compress = false) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return ToFileAsync(array, folder, check, compress).Result;
		}

		/// <summary>
		/// Check if the <paramref name="folder"/> to save to is legal
		/// </summary>
		/// <param name="folder">The folder to save arrays in</param>
		/// <exception cref="DirectoryNotFoundException">If the target <paramref name="folder"/> is illegal</exception>
		public static void CheckSaveFolder(string folder)
		{
			if (File.Exists(folder))
				throw new DirectoryNotFoundException();
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
			else if (Directory.GetDirectories(folder).Length + Directory.GetFiles(folder).Length > 0)
				throw new DirectoryNotFoundException();
		}

		/// <summary>
		/// Check if the <paramref name="folder"/> to load from is legal
		/// </summary>
		/// <param name="folder">The folder to load arrays from</param>
		/// <exception cref="DirectoryNotFoundException">If the target <paramref name="folder"/> is illegal</exception>
		public static void CheckLoadFolder(string folder)
		{
			if ((File.GetAttributes(folder) & FileAttributes.Directory) == FileAttributes.Directory)
			{
				if (!Directory.Exists(folder))
					throw new DirectoryNotFoundException();
				else if (Directory.GetDirectories(folder).Length + Directory.GetFiles(folder).Length == 0)
					throw new DirectoryNotFoundException();
			}
			else if (Path.GetExtension(folder) != ".zip")
			{
				throw new FileNotFoundException();
			}
		}

		private static readonly JsonSerializerOptions Options = new()
		{
			WriteIndented = true,
			Converters =
			{
				new TypeConverter(),
				new LocationDescriptionConverter(),
				new PointerSegmentConverter(),
				new IStorageConverter()
			}
		};

		/// <summary>
		/// Save the target <paramref name="array"/> to file with relative or absolute folder path <paramref name="folder"/> asynchronously.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="array">The array to save</param>
		/// <param name="folder">The folder to save to, default is a random hash code as folder name. It must be a non-existing or empty folder</param>
		/// <param name="check">Whether to calculate check sum or not, default null means do not override</param>
		/// <param name="compress">Whether to compress the result folder as a .zip file or remains it a folder</param>
		/// <returns>the folder / file name</returns>
		/// <exception cref="IOException">if the target folder already contains file(s)</exception>
		public static async Task<string> ToFileAsync<T>(this ValueArray<T> array, string? folder = null, bool check = false, bool compress = false) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (array is null || array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			if (string.IsNullOrWhiteSpace(folder))
				folder = Path.GetRandomFileName();
			CheckSaveFolder(folder);

			// calculate checksum
			Dictionary<string, byte[]>? checksums = null;
			if (check)
			{
				var checkList = await Task.Run(() => CheckCode(array.GetStorages()));
				checksums = new Dictionary<string, byte[]>(checkList);
			}
			// create head info
			var info = new ArrayInfo(array.GetType(), array.Size.ToArray(), array.GetMetaData(), array.GetStorages(), checksums);
			// write head as JSON
			var head = JsonSerializer.Serialize(info, Options);
			File.WriteAllText(Path.Join(folder, HeadFileName), head);
			// write pointers
			foreach (var item in info.Storages)
			{
				await Task.Run(() => WriteFile(folder, item.Key, item.Value));
			}
			// compress if needed
			if (compress)
			{
				var file = Path.TrimEndingDirectorySeparator(folder) + ".zip";
				await Task.Run(() => ZipFile.CreateFromDirectory(folder, file, CompressionLevel.Optimal, false));
				Directory.Delete(folder, true);
				return file;
			}
			// return
			return folder;
		}
		#endregion


		#region read from file
		/// <summary>
		/// Read the saved folder / file back to a <see cref="ValueArray{T}"/> synchronously
		/// </summary>
		/// <typeparam name="T">An unmanaged number as data type</typeparam>
		/// <param name="folder">The folder or file name previously saved</param>
		/// <returns>The <see cref="ValueArray{T}"/> read from the file(s) in <paramref name="folder"/></returns>
		/// <exception cref="IOException">if the check code and the file do not match</exception>
		public static ValueArray<T> FromFile<T>(this string folder) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return FromFileAsync<T>(folder).Result;
		}

		/// <summary>
		/// Read the saved folder / file back to a <see cref="ValueArray{T}"/> asynchronously
		/// </summary>
		/// <typeparam name="T">An unmanaged number as data type</typeparam>
		/// <param name="folder">The folder or file name previously saved</param>
		/// <returns>The <see cref="ValueArray{T}"/> read from the file(s) in <paramref name="folder"/></returns>
		/// <exception cref="IOException">if the check code and the file do not match</exception>
		public async static Task<ValueArray<T>> FromFileAsync<T>(this string folder) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (string.IsNullOrWhiteSpace(folder))
				throw new ArgumentNullException(nameof(folder));
			CheckLoadFolder(folder);

			string orgFolder = folder;
			ArrayInfo? head = null;
			try
			{
				// decompress if needed
				if (!File.GetAttributes(folder).HasFlag(FileAttributes.Directory))
				{
					var newFolder = Path.Join(Path.GetDirectoryName(folder), Path.GetFileNameWithoutExtension(folder));
					await Task.Run(() => ZipFile.ExtractToDirectory(folder, newFolder));
					folder = newFolder;
				}
				// read head info
				string json = File.ReadAllText(Path.Join(folder, HeadFileName));
				head = JsonSerializer.Deserialize<ArrayInfo>(json, Options);
				if (head is null)
					throw new IOException(Resources.Other.CannotDeserialize);
				// read pointers
				foreach (var kv in head.Storages)
				{
					string fileName = Path.Combine(folder, kv.Key + PointerExtension);
					if (!File.Exists(fileName))
						throw new FileNotFoundException(fileName);
					await ReadFile(fileName, kv.Value);
				}
				// check pointers
				if (head.CheckSums is not null)
				{
					var check = CheckCode(head.Storages);
					if (check.Count != head.CheckSums.Count)
						throw new IOException(Resources.Exception.FileCorrupted);
					var temp = System.Linq.Enumerable.Except(check, head.CheckSums, new StringBytesComparer());
					if (System.Linq.Enumerable.Any(temp))
						throw new IOException(Resources.Exception.FileCorrupted);
				}
				// to array
				return ValueArrayFactory<T>.Create(head.Type, head.Size, head.Storages, head.MetaData);
			}
			catch (Exception)
			{
				head?.Dispose();
				throw;
			}
			finally
			{
				// delete the unzipped folder
				if (folder != orgFolder && Directory.Exists(folder))
					Directory.Delete(folder, true);
			}
		}

		private static async Task ReadFile(string file, IStorage storage)
		{
			using var fileStream = File.OpenRead(file);
			using var storageStream = new StorageStream(storage);
			if (fileStream.Length != storageStream.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(file));
			await fileStream.CopyToAsync(storageStream);
		}

		internal struct StringBytesComparer : IEqualityComparer<KeyValuePair<string, byte[]>>
		{
			public bool Equals([AllowNull] KeyValuePair<string, byte[]> x, [AllowNull] KeyValuePair<string, byte[]> y)
			{
				return x.Key == y.Key && x.Value.Length == y.Value.Length && new ReadOnlySpan<byte>(x.Value).SequenceEqual(y.Value);
			}

			public int GetHashCode([DisallowNull] KeyValuePair<string, byte[]> obj)
			{
				return HashCode.Combine(obj.Key, Encoding.ASCII.GetString(obj.Value).GetHashCode());
			}
		}
		#endregion
	}
}
