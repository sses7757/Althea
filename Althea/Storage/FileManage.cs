using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

using Althea.Linq;
using Althea.Arrays;
using Althea.NativeTypes;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Storage
{
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
				throw new JsonException(Resources.Parameter.UnexpectedType, e);
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
					if (reader.TokenType != JsonTokenType.PropertyName)
						throw new JsonException();
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						if (type == default || locations is null || locations.Length == 0)
							throw new JsonException(Resources.Parameter.WrongSize);
						return new CombinationOfLocations(type, locations);
					}
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
					throw new JsonException(Resources.Parameter.UnexpectedValue, e);
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

			public IReadOnlyDictionary<string, string> StringProperties => throw new NotImplementedException();

			public bool Equals(IPointer? other) => throw new NotImplementedException();

			public bool IsValid() => true;
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
					if (reader.TokenType != JsonTokenType.PropertyName)
						throw new JsonException();
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						if (lengthInBytes == 0 || location == default)
							throw new JsonException(Resources.Parameter.WrongSize);
						return new PointerSegment(new TempPointer { LengthInBytes = lengthInBytes, Location = location });
					}
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
					throw new JsonException(Resources.Parameter.UnexpectedValue, e);
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
					if (reader.TokenType != JsonTokenType.PropertyName)
						throw new JsonException();
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						success = true;
						break;
					}
					switch (reader.GetString())
					{
						case DataType:
							reader.Read();
							dataType = TypeConverter.Read(ref reader, typeToConvert, options) ?? throw new JsonException(Resources.Parameter.UnexpectedType);
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
					throw new JsonException(Resources.Parameter.WrongSize);
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
					throw new JsonException(Resources.Parameter.UnexpectedValue, e);
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
				throw new JsonException(Resources.Parameter.InvalidValue, e);
			}
		}

		public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(Encoding.ASCII.GetString(value));
		}
	}
	#endregion

	#region array head
	internal record ArrayInfo
	{
		[JsonConstructor]
		public ArrayInfo(Type type, long[] size, Dictionary<string, object> metaData, Dictionary<string, IStorage> storages, Dictionary<string, byte[]>? checkSums)
		{
			try
			{
				if (type is null)
					throw new ArgumentNullException(nameof(type));
				if (size is null || size.Length == 0)
					throw new ArgumentNullException(nameof(size));
				if (metaData is null)
					throw new ArgumentNullException(nameof(metaData));
				if (storages is null || storages.Count == 0)
					throw new ArgumentNullException(nameof(storages));
				if (checkSums is not null && checkSums.Count != storages.Count)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(checkSums));
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
		public Dictionary<string, object> MetaData { get; }

		// string-keyed dictionary can be automatically serialized and deserialized by System.Text.Json
		public Dictionary<string, IStorage> Storages { get; }

		public Dictionary<string, byte[]>? CheckSums { get; }
	}
	#endregion

	/// <summary>
	/// The static class as a file manager that provides conversion between arrays and files
	/// </summary>
	public static class ArrayFile
	{
		private const string PointerExtension = ".ptr", HeadFileName = "head.json";

		private static void WriteFile(string folder, string name, IStorage storage)
		{
			long length = storage.LengthInBytes;
			Storage<byte> byteStorage = storage.AsByteStorage();
			string path = Path.Join(folder, name + PointerExtension);
			var storageAPI = MEM.GetFirstImplCanFileTransfer(storage.LocationDescription);
			if (storageAPI is null)
			{
				Directory.CreateDirectory(folder);
				using var file = File.Create(path, 1 << 16);
				byte[] buffer = new byte[1 << 16];
				long offset = 0;
				while (offset < length)
				{
					long copied = MEM.ToManaged(byteStorage + offset, buffer);
					file.Write(buffer, 0, (int)copied);
					offset += copied;
				}
			}
			else
			{
				var pointer = storageAPI.AllocateFileAt(path, length);
				try
				{
					MEM.MemoryCopy(byteStorage, new PureReferenceStorage<byte>(pointer));
				}
				catch (Exception)
				{
					MEM.Free(pointer);
					throw;
				}
			}
		}

		private unsafe static IReadOnlyList<KeyValuePair<string, byte[]>> CheckCode(IReadOnlyDictionary<string, IStorage> pointers)
		{
			var hash = new List<KeyValuePair<string, byte[]>>();
			using var sha = System.Security.Cryptography.SHA512.Create();
			//sha.Initialize();
			foreach (var item in pointers)
			{
				using var stream = new UnmanagedMemoryStream((byte*)item.Value.Ptr, item.Value.LengthInBytes);
				hash.Add(new KeyValuePair<string, byte[]>(item.Key, sha.ComputeHash(stream)));
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
			return ToFileAsync(array, folder, check, compress).GetAwaiter().GetResult();
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
		public static string ToFileAsync<T>(this ValueArray<T> array, string? folder = null, bool check = false, bool compress = false) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (array is null || array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			if (string.IsNullOrWhiteSpace(folder))
				folder = Path.GetRandomFileName();
			CheckSaveFolder(folder);

			// need on host for the following
			var host = array.OnHost ? array : array.ToTheOtherMemory();
			try
			{
				// calculate check code
				IReadOnlyDictionary<string, byte[]> check = null;
				if (overrideCheck.Value)
				{
					var checkList = await Task.Run(() => CheckCode(host.GetPointers()));
					check = new Dictionary<string, byte[]>(checkList);
				}
				// create head info
				var info = new ArrayHeadInfo(array.GetType(), array.Size, array.GetOtherInfo(), array.OnHost, check);
				// write head as JSON
				var head = Newtonsoft.Json.JsonConvert.SerializeObject(info, Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(Path.Join(folder, HeadFileName), head);
				// write pointers
				foreach (var item in host.GetPointers())
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
			finally
			{
				if (!array.OnHost) host.Dispose();
			}
		}


		/// <summary>
		/// Read the saved folder / file back to a <see cref="PureArray{T}"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="folder">The folder or file name saved</param>
		/// <param name="forceOnHost">default null means using the header file to identify the storage location (on host / on device); if a <see cref="bool"/> value is indicated, it will override the header file's info</param>
		/// <returns>the <see cref="PureArray{T}"/> read from disk</returns>
		/// <exception cref="IOException">if the check code and the file do not match</exception>
		public static PureArray<T> FromFile<T>(this string folder, bool? forceOnHost = null) where T : struct, IComparable<T>
		{
			return FromFileAsync<T>(folder, forceOnHost).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Read the saved folder / file back to a <see cref="PureArray{T}"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="folder">The folder or file name saved</param>
		/// <param name="forceOnHost">default null means using the header file to identify the storage location (on host / on device); if a <see cref="bool"/> value is indicated, it will override the header file's info</param>
		/// <returns>the <see cref="PureArray{T}"/> read from disk</returns>
		/// <exception cref="IOException">if the check code and the file do not match</exception>
		public async static Task<PureArray<T>> FromFileAsync<T>(this string folder, bool? forceOnHost = null) where T : struct, IComparable<T>
		{
			if (string.IsNullOrWhiteSpace(folder))
				throw new ArgumentNullException(nameof(folder));
			CheckLoadFolder(folder);

			string orgFolder = folder;
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
				var head = Newtonsoft.Json.JsonConvert.DeserializeObject(json, typeof(ArrayHeadInfo)) as ArrayHeadInfo;
				// read pointers
				var dict = new Dictionary<string, IStorage>();
				foreach (var ptr in Directory.GetFiles(folder, "*" + PointerExtension))
				{
					var key = Path.GetFileNameWithoutExtension(ptr);
					var pointer = await Task.Run(() => ReadFile(ptr));
					dict.Add(key, pointer);
				}
				// check pointers
				if (!(head.check is null))
				{
					var check = CheckCode(dict);
					var fileCheck = new List<KeyValuePair<string, byte[]>>(head.check);
					if (check.Count != head.check.Count || check.Except(fileCheck, new StringBytesComparer()).Count != 0)
						throw new IOException("Corrupted");
				}
				// to host array first
				var hostArray = Array.PureArrayFactory.Reconstruct<T>(head.type, head.size, dict, otherInfo: head.otherInfo);
				forceOnHost ??= head.onHost;
				if (forceOnHost.Value)
					return hostArray;
				// else to device
				using (hostArray)
					return await Task.Run(() => hostArray.ToTheOtherMemory());
			}
			finally
			{
				// delete the unzipped folder
				if (folder != orgFolder && Directory.Exists(folder))
					Directory.Delete(folder, true);
			}
		}

		internal struct StringBytesComparer : IEqualityComparer<KeyValuePair<string, byte[]>>
		{
			public bool Equals([AllowNull] KeyValuePair<string, byte[]> x, [AllowNull] KeyValuePair<string, byte[]> y)
			{
				return x.Key == y.Key && x.Value.Length == y.Value.Length && x.Value.SequenceEqual(y.Value);
			}

			public int GetHashCode([DisallowNull] KeyValuePair<string, byte[]> obj)
			{
				return HashCode.Combine(obj.Key, Encoding.ASCII.GetString(obj.Value).GetHashCode(CudaCSharpConverters.StrCmp));
			}
		}
	}
}
