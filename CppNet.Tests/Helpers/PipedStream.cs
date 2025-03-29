namespace CppNet.Tests.Helpers;

public sealed class PipedStream : Stream
{

    private readonly MemoryStream _stream = new MemoryStream();

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => throw new NotSupportedException("Cannot set Position on PipedStream, as it's the same as seeking");
    }

    public override void Flush() => _stream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Cannot seek PipedStream");

    public override void SetLength(long value) => _stream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
        _stream.Flush();
        _stream.Seek(offset, SeekOrigin.Begin);
    }
}
