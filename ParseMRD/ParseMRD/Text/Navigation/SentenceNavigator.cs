using System;
using System.Linq;

namespace MaxReaderModel.Text.Navigation
{
	/// <summary>
	/// Scans text forward or backward from the given location
	/// and returns segment for the next (or prev) Sentence delimiter
	/// </summary>
    public class SentenceNavigator: IUnitNavigator
    {
        static readonly char[] separators = { '.', '!', '?', '…', '⁉', '⁈', '‼', '⁇' };
        //static readonly char[] newline = { '\r', '\n' };
        //static readonly char[] quotes = { '"', '«', '»'};
        const int maxChars = 3000;
        //const int maxLines = 30;
        //IUnitNavigator _wordNav = new SuperNavigator(new WordNavigator());

        private bool _sentSepTail(char c)
        {
            return separators.Contains(c) || char.IsWhiteSpace(c);
        }

        /// <summary>
        /// Finds next sentence
        /// - В предложении последним словом не может быть одна буква.
        /// TODO - кавычки не могут быть разорваны
        /// TODO - Пустая строка также является разделителем.
        /// TODO - Максимальное количество строк в предложении - 30.
        /// </summary>
        public ISegment Next(IText text, int offset)
        {
            int textLength = text.TextLength;
            if (offset == -1) return new SimpleSegment(-1, 1);
	        if (offset >= textLength - 1) return SimpleSegment.Invalid;

			int curOffset = offset;
            while (true)
            {
                int searchCount = Math.Min(textLength - curOffset, maxChars);
                int pos = text.IndexOfAny(separators, curOffset, searchCount);
                if (pos >= 0)
                {
                    // проверим: В предложении последним словом не может быть одна буква.
                    int wrdBeg = pos - 1;
                    while (wrdBeg >= 0 && char.IsLetterOrDigit(text.GetCharAt(wrdBeg))) wrdBeg--;
                    wrdBeg++;
                    if (pos - wrdBeg > 1 || wrdBeg == 0)
                    {
                        int end = pos + 1;
                        while (end < textLength && _sentSepTail(text.GetCharAt(end))) end++;
                        //if (end >= textLength) end = textLength - 1;
                        return new SimpleSegment(pos, end - pos);
                    }
                    else curOffset = pos + 1;
                }
                else
                {
                    // не нашли или ограничение
                    if (textLength - offset > maxChars)
                    {
                        return new SimpleSegment(offset+maxChars, 1);
                    }
                    return SimpleSegment.Invalid;
                }
            }
        }
        
        /// <summary>
        /// Finds prev sentence
        /// </summary>
        public ISegment Prev(IText text, int offset)
        {
            int textLength = text.TextLength;
            if (offset <= 0) return SimpleSegment.Invalid;
            int curOffset = offset-1;
            while (curOffset > 0)
            {
                int searchCount = Math.Min(curOffset, maxChars);
                int pos = text.LastIndexOfAny(separators, curOffset, searchCount);
                if (pos >= 0)
                {
					// Пропустим все разделители (например !???)
	                while (pos > 0 && separators.Contains(text.GetCharAt(pos))) pos--;
					// проверим: В предложении последним словом не может быть одна буква.
					int wrdBeg = pos - 1;
					//    Убедимся, что это не самое первое предложение - иначе и одна буква пойдет
	                int searchCount2 = Math.Min(wrdBeg, maxChars);
	                int pos2 = text.LastIndexOfAny(separators, wrdBeg, searchCount2);
					//    Пропускаем буквы
					while (wrdBeg >= 0 && char.IsLetterOrDigit(text.GetCharAt(wrdBeg))) wrdBeg--;
                    wrdBeg++;
                    if (pos - wrdBeg > 1 || wrdBeg == 0 || pos2 == -1)
                    {
                        int end = pos + 1;
                        while (end < textLength && _sentSepTail(text.GetCharAt(end))) end++;
                        //if (end >= textLength) end = textLength - 1;
                        return new SimpleSegment(pos, end - pos);
                    }
                    else curOffset = pos - 1; // Искать далее
                }
                else
                {
                    // не нашли или ограничение
                    if (curOffset - searchCount <= 0)
                    {
                        return new SimpleSegment(-1, 1);
                    }
                    return SimpleSegment.Invalid;
                }
            }
	        return SimpleSegment.Invalid;
        }
    }
}