using System;
using System.IO;
using System.Runtime.Serialization;

namespace MaxReaderModel.Text
{
    /// <summary>
    /// Implements the IText interface using a string.
    /// </summary>
    public class StringText : IText
    {
        /// <summary>
        /// Gets a text source containing the empty string.
        /// </summary>
        public static readonly StringText Empty = new StringText(string.Empty);

        readonly string text;
        
        /// <summary>
        /// Creates a new StringTextSource with the given text.
        /// </summary>
        public StringText(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            this.text = text;
        }

        /// <inheritdoc/>
        public int TextLength
        {
            get { return text.Length; }
        }

        /// <inheritdoc/>
        public char GetCharAt(int offset)
        {
            return text[offset];
        }

        /// <inheritdoc/>
        public string GetText(int offset, int length)
        {
            return text.Substring(offset, length);
        }

        /// <inheritdoc/>
        public string GetText(ISegment segment)
        {
            if (segment == null) throw new ArgumentNullException("segment");
            return text.Substring(segment.Offset, segment.Length);
        }

        #region Search
        /// <inheritdoc/>
        public int IndexOf(char c, int startIndex, int count)
        {
            return text.IndexOf(c, startIndex, count);
        }

        /// <inheritdoc/>
        public int IndexOfAny(char[] anyOf, int startIndex, int count)
        {
            return text.IndexOfAny(anyOf, startIndex, count);
        }

        /// <inheritdoc/>
        public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
        {
            return text.IndexOf(searchText, startIndex, count, comparisonType);
        }

        /// <inheritdoc/>
        public int LastIndexOf(char c, int startIndex, int count)
        {
            return text.LastIndexOf(c, startIndex + count, count);
        }


        public int LastIndexOfAny(char[] anyOf, int startIndex, int count)
        {
            return text.LastIndexOfAny(anyOf, startIndex, count);
        }

        /// <inheritdoc/>
        public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
        {
            return text.LastIndexOf(searchText, startIndex, count, comparisonType);
        }
        #endregion

        #region #region get TextReader / to TextWriter
        /// <inheritdoc/>
        public TextReader CreateReader()
        {
            return new StringReader(text);
        }        

        #endregion
    }
}