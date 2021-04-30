using Althea.Storage;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Mkl.Storage
{
	/// <summary>
	/// The MKL back-end of <see cref="AbstractApi"/> that supports storage locations of CPU and file.
	/// </summary>
	public class StorageApi : CSharp.Storage.StorageApi
	{
		public override (int major, int minor) DriverVersion(StorageLocation location)
		{
			if (location.Type == LocationType.CpuRam)
				return MklRuntime.GetDriverVersion();
			else
				return default;
		}
	}
}
