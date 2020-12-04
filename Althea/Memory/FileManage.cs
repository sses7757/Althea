using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

using Althea.Arrays;
using Althea.Linq;


namespace Althea.Memory
{
	/// <summary>
	/// The file manager static class that provides conversion between <see cref="PureArray{T}"/> and file
	/// </summary>
	public static class ArrayFile
	{
		internal sealed class ArrayHeadInfo
		{
			public readonly Type type;

			public readonly long[] size;

			public readonly IReadOnlyDictionary<string, object> otherInfo;

			public readonly bool onHost;

			public readonly IReadOnlyDictionary<string, byte[]> check;

			[Newtonsoft.Json.JsonConstructor]
			public ArrayHeadInfo(string type, long[] size, IReadOnlyDictionary<string, object> otherInfo, bool onHost, IReadOnlyDictionary<string, byte[]> check)
				: this(Type.GetType(type), size, otherInfo, onHost, check) { }

			internal ArrayHeadInfo(Type type, IReadOnlyList<long> size, IReadOnlyDictionary<string, object> otherInfo, bool onHost, IReadOnlyDictionary<string, byte[]> check)
			{
				this.type = type;
				this.size = size.ToArray();
				this.otherInfo = otherInfo;
				this.onHost = onHost;
				this.check = check;
			}
		}

		private const string PointerExtension = ".ptr", HeadFileName = "head.json";

		private static void WriteFile(string folder, string key, IPointer value)
		{
			NativeMethods.hostToFile(value.Ptr, value.LengthInBytes, Path.Join(folder, key + PointerExtension)).Check();
		}

		private unsafe static IReadOnlyList<KeyValuePair<string, byte[]>> CheckCode(IReadOnlyDictionary<string, IPointer> pointers)
		{
			var hash = new List<KeyValuePair<string, byte[]>>();
			using var sha = System.Security.Cryptography.SHA256.Create();
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="array">the array to save</param>
		/// <param name="folder">the folder to save to, default is a random hash code as folder name. It must be a non-existing or empty folder</param>
		/// <param name="overrideCheck">override the global setting of calculate check sum or not, default null means do not override</param>
		/// <param name="compress">compress the result folder as a .zip file or remains it a folder</param>
		/// <returns>the folder / file name</returns>
		/// <exception cref="IOException">if the target folder already contains file(s)</exception>
		public static string ToFile<T>(this PureArray<T> array, string folder = null, bool? overrideCheck = null, bool compress = true) where T : struct, IComparable<T>
		{
			return ToFileAsync(array, folder, overrideCheck, compress).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Check if the <paramref name="folder"/> to save to is licit
		/// </summary>
		/// <param name="folder">the folder to save arrays in</param>
		public static void CheckSaveFolder(string folder)
		{
			if (File.Exists(folder))
				throw new DirectoryNotFoundException();
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
			else if (Directory.GetDirectories(folder).Length + Directory.GetFiles(folder).Length > 0)
				throw new DirectoryNotFoundException(Resource.FolderNonEmpty);
		}

		/// <summary>
		/// Check if the <paramref name="folder"/> to load from is licit
		/// </summary>
		/// <param name="folder">the folder to load arrays from</param>
		public static void CheckLoadFolder(string folder)
		{
			if ((File.GetAttributes(folder) & FileAttributes.Directory) == FileAttributes.Directory)
			{
				if (!Directory.Exists(folder))
					throw new DirectoryNotFoundException();
				else if (Directory.GetDirectories(folder).Length + Directory.GetFiles(folder).Length == 0)
					throw new DirectoryNotFoundException(Resource.FolderIllegal);
			}
			else if (Path.GetExtension(folder) != ".zip")
			{
				throw new FileNotFoundException(Resource.FolderIllegal);
			}
		}

		/// <summary>
		/// Save the target <paramref name="array"/> to file with relative or absolute folder path <paramref name="folder"/> asynchronously.
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="array">the array to save</param>
		/// <param name="folder">the folder to save to, default is a random hash code as folder name. It must be a non-existing or empty folder</param>
		/// <param name="overrideCheck">override the global setting of calculate check sum or not, default null means do not override</param>
		/// <param name="compress">compress the result folder as a .zip file or remains it a folder</param>
		/// <returns>the folder / file name</returns>
		/// <exception cref="IOException">if the target folder already contains file(s)</exception>
		public async static Task<string> ToFileAsync<T>(this PureArray<T> array, string folder = null, bool? overrideCheck = null, bool compress = true) where T : struct, IComparable<T>
		{
			if (array is null || array == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(array), Resource.ArrayCannotNull);
			overrideCheck ??= GlobalSettings.FileCheck;
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

		private static IPointer ReadFile(string file)
		{
			long size = 0;
			NativeMethods.hostFromFileGetSize(ref size, file).Check();
			var pointer = Storage<byte>.Create(size, onHost: true);
			NativeMethods.hostFromFile(pointer, size, file).Check();
			return pointer;
		}

		/// <summary>
		/// Read the saved folder / file back to a <see cref="PureArray{T}"/>
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="folder">the folder or file name saved</param>
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="folder">the folder or file name saved</param>
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
				var dict = new Dictionary<string, IPointer>();
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
				var hostArray = Arrays.PureArrayFactory.Reconstruct<T>(head.type, head.size, dict, otherInfo: head.otherInfo);
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
