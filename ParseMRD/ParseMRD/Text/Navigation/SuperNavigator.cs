using System.Diagnostics.Contracts;

namespace MaxReaderModel.Text.Navigation
{
	/// <summary>
	/// As other unit navigators looks for delimiters, 
	/// this one is intended to look for text unit itself.
	/// 
	/// Поскольку остальные навигаторы ищут разделители,
	/// супер навигатор предназначен для поиска самих частей текста.
	/// </summary>
    public class SuperNavigator : IUnitNavigator
    {
        IUnitNavigator _baseNavigator;

		/// <summary>
		/// Creates SuperNavigator bases on top of the given unit navigator
		/// </summary>
		/// <param name="baseNavigator">base unit navigator</param>
		public SuperNavigator(IUnitNavigator baseNavigator)
		{
			Contract.Requires(baseNavigator != null);
			_baseNavigator = baseNavigator;
		}
        
        public ISegment Next(IText text, int offset)
        {
            int textLength = text.TextLength;
            if (offset >= textLength - 1) return SimpleSegment.Invalid;

            var sep1 = _baseNavigator.Next(text, offset);

            int nextOffset = sep1.EndOffset;
            if (SimpleSegment.IsInvalid(sep1)) return sep1;

            var sep2 = _baseNavigator.Next(text, nextOffset);
            
            int endOffset = sep2.Offset;
            if (_baseNavigator is SentenceNavigator) endOffset = sep2.EndOffset;
            if (sep2.Offset == -1 || sep2.Length == -1) endOffset = textLength;
            int len = endOffset - nextOffset;
            if (len <= 0) return SimpleSegment.Invalid;
            return new SimpleSegment(nextOffset, len);
        }

        public ISegment Prev(IText text, int offset)
        {
            if (offset <= 0) return SimpleSegment.Invalid;

            var sep1 = _baseNavigator.Prev(text, offset);
            if (sep1.Offset == -1 || sep1.Length == -1) return SimpleSegment.Invalid;
            var sep2 = _baseNavigator.Prev(text, sep1.Offset);
            int beginOffset = (sep2.Offset == -1 || sep2.Length == -1) ? 0 : sep2.EndOffset;
            int len = sep1.Offset - beginOffset;
	        if (_baseNavigator is SentenceNavigator) len = sep1.EndOffset - beginOffset;
            return len > 0 ? new SimpleSegment(beginOffset, len) : SimpleSegment.Invalid;
        }
    }
}