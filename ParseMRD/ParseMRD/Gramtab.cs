using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ParseMRD
{
	/// <summary>
	/// Represents table of Ancodes which contains all possible full morphological patterns for the words.
	/// Например, анкод "аб" означает "Существительное мр,ед,рд"
	/// </summary>
	public class Gramtab
	{
		private Dictionary<string, GramtabItem> _gramtabs = new Dictionary<string, GramtabItem>(); // Key - ancode
		private List<GramtabItem> _gramtabList = new List<GramtabItem>();

		public Dictionary<string, GramtabItem> Gramtabs => _gramtabs;

		public void LoadGramtab(string fileName)
		{
			using (FileStream file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (StreamReader reader = new StreamReader(file, Encoding.Default))
			{
				UInt16 no = 0;
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
					{
						string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						string ancode = parts[0];
						GramtabItem g = new GramtabItem();
						g.PartOfSpeech = parts[2];
						if (parts.Length > 3)
						{
							g.Grammems = parts[3];
						}
						g.No = no++;
						_gramtabs.Add(ancode, g);
						_gramtabList.Add(g);
					}
				}
			}
			Console.WriteLine("Gramtabs:     " + Gramtabs.Count);
		}

		public string LookupAncode(string ancode)
		{
			if (!string.IsNullOrWhiteSpace(ancode))
			{
				if (!_gramtabs.ContainsKey(ancode)) return "Unknown ancode: " + ancode;
				GramtabItem g = _gramtabs[ancode];
				return string.Format("{0} {1}", g.PartOfSpeech, g.Grammems);
			}
			return "";
		}

		public UInt16 AncodeToInt(string ancode)
		{
			return _gramtabs[ancode].No;
		}

		public void CountFrequency(string ancode)
		{
			_gramtabs[ancode].Freq++;
		}

		public string LookupByNo(ushort no)
		{
			GramtabItem g = _gramtabList[no];
			return string.Format("{0} {1}", g.PartOfSpeech, g.Grammems);			
		}
	}
}