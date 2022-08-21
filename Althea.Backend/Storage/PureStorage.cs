namespace Althea.Backend.Storage;

// only used for internal creation by existing pointer
internal sealed class ActualPureStorage<T, TP> : PureStorage<T, TP> where T : unmanaged, IBaseNumber<T> where TP : notnull, IPointer<TP>
{
	internal ActualPureStorage(TP pointer) : base(pointer) { }

	~ActualPureStorage() => this.Dispose(false);
}
