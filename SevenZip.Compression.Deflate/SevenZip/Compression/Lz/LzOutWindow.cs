// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.Lz
{
    class LzOutWindow
    {
        private readonly Byte[] _buf;
        private Int32 _pos;
        private Int32 _limitPos;
        private Int32 _streamPos;
        private UInt64 _processedSize;
        private bool _overDict;

        public LzOutWindow(UInt32 bufSize)
        {
            _buf = new Byte[bufSize.Maximum(1U)];
            _pos = 0;
            _limitPos = _buf.Length;
            _streamPos = 0;
            _processedSize = 0;
            _overDict = false;
        }

        public UInt64 ProcessedSize
        {
            get
            {
                return
                    _processedSize
                    + (UInt32)(
                        _streamPos > _pos
                            ? _buf.Length - _streamPos + _pos
                            : _pos - _streamPos
                    );
            }
        }

        public bool CopyBlock(Span<Byte> buffer, ref Int32 bufferIndex, UInt32 distance, UInt32 len)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            Int32 pos;
            if (distance >= (UInt32)_pos)
            {
                if (!_overDict || distance >= _buf.Length)
                    return false;
                pos = _buf.Length - checked((Int32)distance) + _pos - 1;
            }
            else
                pos = _pos - (Int32)distance - 1;
            if (_limitPos - _pos > len && _buf.Length - pos > len)
            {
                _buf.CopyMemoryTo(pos, _buf, _pos, (Int32)len * sizeof(Byte));
                _pos += (Int32)len;
            }
            else
            {
                while (len > 0)
                {
                    if (pos >= _buf.Length)
                        pos = 0;
                    var rem = checked((Int32)len).Minimum(_buf.Length - pos).Minimum(_limitPos - _pos);
                    _buf.CopyMemoryTo(pos, _buf, _pos, rem);
                    pos += rem;
                    _pos += rem;
                    len -= (UInt32)rem;
                    if (_pos >= _limitPos)
                        Flush(buffer, ref bufferIndex);
                }
            }
            return true;
        }

        public void PutByte(Span<Byte> buffer, ref Int32 bufferIndex, Byte b)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            _buf[_pos++] = b;
            if (_pos >= _limitPos)
                Flush(buffer, ref bufferIndex);
        }

        public void PutBytes(Span<Byte> buffer, ref Int32 bufferIndex, ReadOnlySpan<Byte> data)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            if (data.Length <= 0)
                return;
            var dataOffset = 0;
            var size = data.Length;
            _buf[_pos++] = data[dataOffset++];
            --size;
            while (true)
            {
                if (_pos >= _limitPos)
                    Flush(buffer, ref bufferIndex);
                else if (size <= 0)
                    break;
                else
                {
                    var rem = (_limitPos - _pos).Minimum(size);
                    data.Slice(dataOffset, rem).CopyTo(_buf.AsSpan(_pos, rem));
                    _pos += rem;
                    size -= rem;
                    dataOffset += rem;
                }
            }
        }

        public Byte GetByte(UInt32 distance)
        {
            if (distance >= _pos)
                return _buf[_buf.Length - checked((Int32)distance) + _pos - 1];
            else
                return _buf[_pos - checked((Int32)distance) - 1];
        }

        public void Flush(Span<Byte> buffer, ref Int32 bufferIndex)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            while (_streamPos != _pos)
                FlushPart(buffer, ref bufferIndex);
        }

        private void FlushPart(Span<Byte> buffer, ref Int32 bufferIndex)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var size = (_streamPos >= _pos) ? (_buf.Length - _streamPos) : (_pos - _streamPos);
            _buf.AsSpan(_streamPos, size).CopyTo(buffer.Slice(bufferIndex, size));
            bufferIndex += size;
            _streamPos += size;
            if (_streamPos >= _buf.Length)
                _streamPos = 0;
            if (_pos >= _buf.Length)
            {
                _overDict = true;
                _pos = 0;
            }
            _limitPos = (_streamPos > _pos) ? _streamPos : _buf.Length;
            _processedSize += (UInt32)size;
        }
    }
}
