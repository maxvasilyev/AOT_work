using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MaxReaderModel.Text
{
    /// <summary>
    /// Implements the IText interface using as virtual string.
    /// ������������������ ������, ������ ����� �� ������ ���������� ��������� �������, ��������� ��������� ������� <see cref="string"/> (������, ������� ������������)
    /// </summary>
    public class VirtualText : IText, IDisposable
    {        
        //[DataMember]
        private readonly StreamReader _reader;
        private int _charWidth = -1; // -1 - UTF8, 1 - ansi/asci or 2 - UTF16
        private int _position; // Current number of chars read from StreamReader
        /// <summary>
        /// ������ ������, ������ ����� ����� ������ ������ �������� ���� ����� (�������� ��� UTF8 ������ ��������� �������)
        /// TODO ����� �������� ������������?
        /// � ����� ����� ������������ � ������� � ����������� �����
        /// </summary>
        private long _textLength = -1;

        private class TextPage
        {
            public ISegment Segment;
            public string Text;
            public override string ToString()
            {
                return string.Format("{0} '{1}'", Segment, Text);
            }
        }
        private List<TextPage> _pages = new List<TextPage>();
        private int _pageSize;
        private int _maxConcurrentPages;
        
        private TextPage _getPage(int offset)
        {
            var page = _pages.FirstOrDefault(p => p.Segment.Contains(offset, 1));
            if (page == null)
            {
                page = _loadPage(offset);
            }
            return page;
        }

        private TextPage _loadPage(int offset)
        {
            int from = (offset / _pageSize) * _pageSize;
            char[] buffer = new char[_pageSize];
            if (from < _position)
            {
                // rewind
                _reader.BaseStream.Seek(0, SeekOrigin.Begin);
                _reader.DiscardBufferedData();
                _position = 0;
            }
            int skip = from - _position;
            while (skip > 0)
            {
                // skip
                int skipPart = Math.Min(skip, _pageSize);
                int skipCount = _reader.Read(buffer, 0, skipPart);
                _position += skipCount;
                skip -= skipCount;
            }
            if (_reader.EndOfStream) throw new InvalidOperationException("End of stream");
            int count = _reader.Read(buffer, 0, _pageSize);
            if (count == 0) throw new InvalidOperationException("count 0");
            _position += count;
            var page = new TextPage()
            {
                Text = new string(buffer, 0, count),
                Segment = new SimpleSegment(from, count)
            };
            _pages.Add(page);
            if (_pages.Count > _maxConcurrentPages)
            {
                _pages.RemoveAt(0);
            }
            return page;
        }

        /// <summary>
        /// Creates a new StringTextSource with the given text.
        /// </summary>
        public VirtualText(StreamReader reader, int charWidth = -1, int pageSize = 100, int maxPages = 2)
        {
            Contract.Requires(reader != null);
            Contract.Assume(charWidth == -1 || charWidth == 1 || charWidth == 2);
            Contract.Assume(pageSize >= 1);
            Contract.Assume(maxPages >= 2);
            _reader = reader;
            _charWidth = charWidth;
            _pageSize = pageSize;
            _maxConcurrentPages = maxPages;
            _position = 0;
            if (_charWidth > 0)
            {
                _textLength = _reader.BaseStream.Length / _charWidth;
            }
            else
            {
                // start scan
                _textLength = _reader.ReadToEnd().Length;
                _reader.BaseStream.Position = 0;
                _reader.DiscardBufferedData();
                // ����� � ����� ������ �������������� � ������� ������ �����
            }
        }

        /// <inheritdoc/>
        public int TextLength
        {
            get { return (int)_textLength; }
        }

        /// <inheritdoc/>
        public char GetCharAt(int offset)
        {
            var page = _getPage(offset);
            // Find page by offset
            return page.Text[offset - page.Segment.Offset];
        }        

        /// <inheritdoc/>
        public string GetText(int offset, int length)
        {
            StringBuilder sb = new StringBuilder(length);
            LookForward(offset, length,
                (pageText, localStartIndex, localCount) =>
                {
                    sb.Append(pageText.Substring(localStartIndex, localCount));
                    return -1;
                });
            return sb.ToString();
        }

        /// <inheritdoc/>
        public string GetText(ISegment segment)
        {
            if (segment == null) throw new ArgumentNullException("segment");
            return GetText(segment.Offset, segment.Length);
        }

        #region Search

        /// <inheritdoc/>
        public int IndexOf(char c, int startIndex, int count)
        {
            return LookForward(startIndex, count,
                (pageText, localStartIndex, localCount) =>
                {
                    return pageText.IndexOf(c, localStartIndex, localCount);
                });
        }
        
        /// <inheritdoc/>
        public int IndexOfAny(char[] anyOf, int startIndex, int count)
        {
            return LookForward(startIndex, count,
                (pageText, localStartIndex, localCount) =>
                {
                    return pageText.IndexOfAny(anyOf, localStartIndex, localCount);
                });

        }

        /// <summary>
        /// ���� ��������� ���������
        /// </summary>
        /// <param name="searchText">��� ������, ����� ������ ���� ������ ������� ��������</param>
        /// <param name="startIndex">���������� �������, ������ �������� ��������</param>
        /// <param name="count">������� �������������</param>
        /// <param name="comparisonType"></param>
        /// <returns>-1 or index</returns>
        public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
        {
            if (searchText.Length > _pageSize) throw new InvalidOperationException("searchText length is greater than page size");
            return LookForward(startIndex, count,
                (pageText, localStartIndex, localCount) =>
                {
                    return pageText.IndexOf(searchText, localStartIndex, localCount, comparisonType);
                });
        }

        /// <summary>
        /// ������������ �������� ������ � ���������� ������� � ��������� ���������
        /// </summary>
        /// <param name="startIndex">������ ��������� - ����� �������, ��������� ������</param>
        /// <param name="count">����� ���������</param>
        /// <param name="func">f(pageText, startIndex, count) ret index, -1 means continue</param>
        /// <returns>���������� ��������� ������ ��� -1, ���� ������ �� �������</returns>
        protected int LookForward(int startIndex, int count, Func<string, int, int, int> func)
        {
            if (TextLength != -1 &&
                startIndex + count > TextLength) throw new ArgumentOutOfRangeException("count", count, "startIndex and count should be inside range");

            int index = -1;
            TextPage page = null;
            while (index == -1 && count > 0)
            {
                page = _getPage(startIndex);
                // ������������� �������� �������, ������ �������� ��������
                int localStartIndex = startIndex - page.Segment.Offset;
                // ������������� �������� �����, ������� �������������
                int localCount = Math.Min(page.Segment.Length - localStartIndex, count);
                if (localCount == 0) throw new ArgumentException("count");

                // ����� �������
                index = func(page.Text, localStartIndex, localCount);

                startIndex += localCount;
                count -= localCount;                
            }
            // ������� ���������� ������ ��� -1
            if (index != -1)
            {
                index += page.Segment.Offset;
            }
            return  index;
        }

        /// <summary>
        /// ������������ �������� ������ � �������� ����������� (� ��������� �������� � ������) � ���������� ������� � ��������� ���������
        /// </summary>
        /// <param name="startIndex">������ ��������� - ������ �������, ��������� �����</param>
        /// <param name="count">����� ���������</param>
        /// <param name="func">f(pageText, startIndex, count) ret index, -1 means continue</param>
        /// <returns>���������� ��������� ������ ��� -1, ���� ������ �� �������</returns>
        protected int LookBackward(int startIndex, int count, Func<string, int, int, int> func)
        {
            if (startIndex - count < -1) throw new ArgumentOutOfRangeException("count", count, "startIndex and count should be inside range");

            int index = -1;
            TextPage page = null;
            while (index == -1 && count > 0)
            {
                page = _getPage(startIndex);
                // ������������� �������� �������, ������ �������� �������� (������ ������� ������ ��������)
                int localStartIndex = startIndex - page.Segment.Offset;
                // ������������� �������� �����, ������� �������������
                int localCount = Math.Min(localStartIndex + 1, count);
                
                // ����� �������
                index = func(page.Text, localStartIndex, localCount);

                startIndex -= localCount;
                count -= localCount;
            }
            // ������� ���������� ������ ��� -1
            if (index != -1)
            {
                index += page.Segment.Offset;
            }
            return index;
        }

        /// <inheritdoc/>
        public int LastIndexOf(char c, int startIndex, int count)
        {
            return LookBackward(startIndex, count,
                (pageText, localStartIndex, localCount) =>
                {
                    return pageText.LastIndexOf(c, localStartIndex, localCount);
                });
        }

        public int LastIndexOfAny(char[] anyOf, int startIndex, int count)
        {
            return LookBackward(startIndex, count,
                (pageText, localStartIndex, localCount) =>
                {
                    return pageText.LastIndexOfAny(anyOf, localStartIndex, localCount);
                });
        }


        /// <inheritdoc/>
        public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
        {
            if (searchText.Length > _pageSize) throw new InvalidOperationException("searchText length is greater than page size");
            return LookBackward(startIndex, count,
                (pageText, localStartIndex, localCount) =>
                {
                    return pageText.LastIndexOf(searchText, localStartIndex, localCount, comparisonType);
                });
        }
        #endregion

        public TextReader CreateReader()
        {
            _reader.DiscardBufferedData();
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);
            return _reader;
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}