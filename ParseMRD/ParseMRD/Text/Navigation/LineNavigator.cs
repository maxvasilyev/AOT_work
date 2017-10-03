namespace MaxReaderModel.Text.Navigation
{
	/// <summary>
	/// Scans text forward or backward from the given location
	/// and returns segment for the next (or prev) Line delimiter
	/// </summary>
	public class LineNavigator : IUnitNavigator
    {
        static readonly char[] newline = { '\r', '\n' };

		/// <summary>
		/// Finds next line delimiter
		/// On last line returns SimpleSegment.Invalid
		/// </summary>
		public ISegment Next(IText text, int offset)
        {
            int textLength = text.TextLength;
	        if (offset == -1) return new SimpleSegment(-1, 1);
	        if (offset >= textLength - 1) return SimpleSegment.Invalid;

			int pos = text.IndexOfAny(newline, offset, textLength - offset);
            if (pos >= 0)
            {
                if (text.GetCharAt(pos) == '\r')
                {
                    if (pos + 1 < textLength && text.GetCharAt(pos + 1) == '\n')
                        return new SimpleSegment(pos, 2);
                }
                return new SimpleSegment(pos, 1);
            }
            return SimpleSegment.Invalid;
        }

		/// <summary>
		/// Finds prev line delimiter
		/// On first line returns SimpleSegment.Invalid
		/// </summary>
		public ISegment Prev(IText text, int offset)
        {
            //int textLength = text.TextLength;
            int pos = text.LastIndexOfAny(newline, offset-1, offset);
            if (pos >= 0)
            {
                if (text.GetCharAt(pos) == '\n')
                {
                    if (pos - 1 >= 0 && text.GetCharAt(pos - 1) == '\r')
                        return new SimpleSegment(pos - 1, 2);
                }
                return new SimpleSegment(pos, 1);
            }
            return SimpleSegment.Invalid;
        }
    }
}