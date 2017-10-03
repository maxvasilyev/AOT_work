namespace MaxReaderModel.Text.Navigation
{
	/// <summary>
	/// Scans text forward or backward from the given location
	/// and returns segment for the next (or prev) Word delimiter
	/// </summary>
    public class WordNavigator : IUnitNavigator
    {
	    private bool _isWordChar(char c)
	    {
		    return char.IsLetterOrDigit(c);//? || c == '-';

	    }

	    /// <summary>
        /// Finds next word delimiter
        /// </summary>
        public ISegment Next(IText text, int offset)
        {
            int textLength = text.TextLength;
	        if (offset == -1) return new SimpleSegment(-1, 1);
			if (offset >= textLength - 1) return SimpleSegment.Invalid;

			// слово_1    слово2 ..
			// ^      ^  ^
			// offset begin end 
			int begin = offset;
	        // пропустить слово
	        while (begin < textLength && _isWordChar(text.GetCharAt(begin))) begin++;
            int end = begin + 1;
			// пропустить пробелы/другие разделители
            while (end < textLength && !_isWordChar(text.GetCharAt(end))) end++;

            if (begin >= textLength) return SimpleSegment.Invalid;
            return new SimpleSegment(begin, end - begin);
        }

		/// <summary>
		/// Finds prev word delimiter
		/// </summary>
		public ISegment Prev(IText text, int offset)
        {
            if (offset <= 0) return SimpleSegment.Invalid;
            int textLength = text.TextLength;
            if (offset > textLength) offset = textLength; // for this method cursor can be a one char out of string
            
            // слово1    слово2
            //       ^  ^  ^
            // begin end offset
            // пропустить слово
            // пропстить пробелы
            int end = offset - 1;
            while (end >= 0 && !char.IsWhiteSpace(text.GetCharAt(end))) end--;
            int begin = end - 1;
            while (begin >= 0 && char.IsWhiteSpace(text.GetCharAt(begin))) begin--;
            begin++;
            if (begin == -1 || end == -1) return SimpleSegment.Invalid;
            return new SimpleSegment(begin, end - begin + 1);
        }
    }

    // возвращает слова, а не разделители
    //public class WordNavigator : IUnitNavigator
    //{
    //    /// <summary>
    //    /// Finds next sentence
    //    /// </summary>
    //    public ISegment Next(IText text, int offset)
    //    {
    //        int textLength = text.TextLength;
    //        //if (offset >= textLength - 1) return SimpleSegment.Invalid;

    //        // слово1    слово2 ..
    //        // ^         ^    ^
    //        // offset    begin end 
    //        // пропустить слово
    //        // пропустить пробелы
    //        // вычислить длину
    //        int begin = offset;
    //        while (begin < textLength && !char.IsWhiteSpace(text.GetCharAt(begin))) begin++;
    //        while (begin < textLength && char.IsWhiteSpace(text.GetCharAt(begin))) begin++;
    //        int end = begin + 1;
    //        while (end < textLength && !char.IsWhiteSpace(text.GetCharAt(end))) end++;

    //        if (begin >= textLength) return SimpleSegment.Invalid;
    //        return new SimpleSegment(begin, end - begin);
    //    }

    //    /// <summary>
    //    /// Finds prev sentence
    //    /// </summary>
    //    public ISegment Prev(IText text, int offset)
    //    {
    //        if (offset <= 0) return SimpleSegment.Invalid;
    //        int textLength = text.TextLength;
    //        if (offset > textLength) offset = textLength; // for this method cursor can be a one char out of string

    //        // слово1    слово2
    //        // ^    ^    ^
    //        // begin end offset
    //        // пропустить пробелы
    //        // пропстить слово - вычислить длину слова
    //        int end = offset - 1;
    //        while (end >= 0 && char.IsWhiteSpace(text.GetCharAt(end))) end--;
    //        int begin = end - 1;
    //        while (begin >= 0 && !char.IsWhiteSpace(text.GetCharAt(begin))) begin--;
    //        begin++;
    //        if (begin == -1 || end == -1) return SimpleSegment.Invalid;
    //        return new SimpleSegment(begin, end - begin + 1);
    //    }
    //}
}