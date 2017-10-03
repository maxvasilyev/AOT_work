using System.Linq;

namespace ParseMRD
{
	public class AccentHelper
	{
		public const string C_VOWELS = "АОУЭИЫЯЕЁЮаоуэиыяеёюAOUEIaouei";

		/// <summary>
		/// Gets number of vowels in the given word
		/// </summary>
		/// <param name="word">e.g. cAstlE</param>
		/// <returns>e.g. 2</returns>
		public static int CountVowels(string word)
		{
			return word.Count(l => C_VOWELS.IndexOf(l) >= 0);
		}

		/// <summary>
		/// Gets index of the vovel in the word, where 0 - last vowel, 1 - before last, etc.
		/// e.g.
		/// word = cAstlE, vowelNo = 0,  return 5
		/// word = cAstlE, vowelNo = 1,  return 1
		/// word = cAstlE, vowelNo = 2+, return -1 (undefined)
		/// </summary>
		public static int LastVowelIndex(string word, int vowelNo)
		{
			if (vowelNo == 255) return -1;
			int idx = word.Length - 1;
			while (idx > 0 && vowelNo >= 0)
			{

				if (C_VOWELS.IndexOf(word[idx]) >= 0)
				{
					if (vowelNo == 0) return idx;
					vowelNo--;
				}
				idx--;
			}
			return -1;
		}

		public static string SetAccent(string word, int vowelNo)
		{
			int idx = LastVowelIndex(word, vowelNo);
			if (idx != -1)
			{
				return word.Insert(idx+1, "<");
			}
			return word;
		}
	}
}