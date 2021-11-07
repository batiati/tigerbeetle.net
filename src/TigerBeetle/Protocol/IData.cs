using System;

namespace TigerBeetle.Protocol
{
	public interface IData
	{
		ReadOnlySpan<byte> AsReadOnlySpan();
	}
}
