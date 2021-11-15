using System;

namespace TigerBeetle
{
	public interface IData
	{
		ReadOnlySpan<byte> AsReadOnlySpan();
	}
}
