using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DawgSharp;

namespace ParseMRD
{
	/// <summary>
	/// Morphological analyzer
	/// Gets word interpretations
	/// 
	/// Например, на входе слово "замок", на выходе набор морфологических интерпретаций:
	/// ЗА'МОК accent 1 (ударение на ПРЕДпоследнюю гласную)
	/// С мр, ед, им
	/// С мр, ед, вн
	/// ЗАМО'К accent 0 (ударение на последнюю гласную)
	/// С мр, ед, им
	/// С мр, ед, вн
	/// Г дст, прш, мр, ед //  Что, Иван, сходил ли ты на погреб? Там, говорят, всё замокло от вчерашнего дождя. М. Ю. Лермонтов
	/// </summary>
	public class MorphAn
	{
		private readonly Gramtab _gramtab;
		private readonly string _workDir;
		private Dawg<FormInterpretations> _dawg;

		#region Build / Save / Load
		public MorphAn(Gramtab gramtab, string workDir)
		{
			_gramtab = gramtab;
			_workDir = workDir;
		}

		public void LoadDAWG()
		{
			string path = Path.Combine(_workDir, "forms.dawg");
			if (File.Exists(path))
			{
				DateTime start = DateTime.Now;
				_dawg = Dawg<FormInterpretations>.Load(File.Open(path, FileMode.Open), ReadPayload);
				Console.WriteLine("DAWG load time: {0}", DateTime.Now - start);
				Console.WriteLine("DAWG nodes: {0}", _dawg.GetNodeCount());
				Console.WriteLine("DAWG count {0}", _dawg.Count());
			}
			else
			{
				_rebuildDAWG(path);
			}			
		}

		private void _rebuildDAWG(string path)
		{
			MRDFileReader mrdFile = new MRDFileReader(_gramtab);
			mrdFile.LoadMrd(Path.Combine(_workDir, @"Dicts\morphs.mrd"));
			_dawg = CreateDAWG(mrdFile);
			_dawg.SaveTo(File.Create(path), WritePayload);
		}

		public static Dawg<FormInterpretations> CreateDAWG(MRDFileReader mrdFile)
		{
			DateTime start = DateTime.Now;
			Console.WriteLine("Inserting forms in DAWG... Please wait...");
			DawgSharp.DawgBuilder<FormInterpretations> dawgBuilder = new DawgBuilder<FormInterpretations>();
			UInt64 cntForms = 0;
			foreach (WordForm f in mrdFile.AllForms)
			{
				string word = f.Prefix + f.Flexia.Prefix + f.Lemma.Base + f.Flexia.Flexion;
				FormInterpretations payload = null;
				dawgBuilder.TryGetValue(word, out payload);
				if (payload == null)
				{
					payload = new FormInterpretations();
					dawgBuilder.Insert(word, payload);
				}
				payload.Add(f);
				cntForms++;
			}
			Console.WriteLine("All forms count: " + cntForms);
			Console.WriteLine("Building... please wait...");
			Dawg<FormInterpretations> dawg = dawgBuilder.BuildDawg();
			Console.WriteLine("DAWG create time: {0}", DateTime.Now - start);
			return dawg;
		}

		private void WritePayload(BinaryWriter binaryWriter, FormInterpretations formInterpretations)
		{
			// cnt
			// <acc1> <ancode-list1>
			// <acc2> <ancode-list2>
			//
			// <ancode-list> := <cnt-byte> <ushort>
			binaryWriter.Write((byte)formInterpretations.AccentToAncodes.Count);
			foreach (var kvp in formInterpretations.AccentToAncodes)
			{
				binaryWriter.Write((byte)kvp.Key);
				binaryWriter.Write((byte)kvp.Value.Count);
				foreach (ushort ancodeNo in kvp.Value)
				{
					binaryWriter.Write((ushort)ancodeNo);
				}
			}
		}

		private FormInterpretations ReadPayload(BinaryReader binaryReader)
		{
			// cnt
			// <acc1> <ancode-list1>
			// <acc2> <ancode-list2>
			//
			// <ancode-list> := <cnt-byte> <ushort>			
			var p = new FormInterpretations();
			int count = binaryReader.ReadByte();
			while (count > 0)
			{
				int accent = binaryReader.ReadByte();
				int listCnt = binaryReader.ReadByte();
				List<ushort> list = new List<ushort>();
				while (listCnt > 0)
				{
					list.Add(binaryReader.ReadUInt16());
					listCnt--;
				}
				p.AccentToAncodes.Add(accent, list);
				count--;
			}
			return p;
		}
		#endregion

		public FormInterpretations Lookup(string word)
		{
			word = word.ToUpperInvariant();
			FormInterpretations forms = _dawg[word];
			return forms;
		}

		public void PrintLookup(string word)
		{
			FormInterpretations forms = Lookup(word);
			if (forms != null)
			{
				foreach (var kvp in forms.AccentToAncodes)
				{
					Console.WriteLine("Accent {0} {1}", kvp.Key, AccentHelper.SetAccent(word, kvp.Key));
					foreach (ushort ancodeNo in kvp.Value)
					{
						Console.WriteLine("    {0}", _gramtab.LookupByNo(ancodeNo));
					}
				}
			}
			else
			{
				Console.WriteLine(word + " not found");
			}
		}

		public void PrintStat()
		{
			StreamWriter manyAccentWords = File.CreateText(Path.Combine(_workDir, "manyAccentWords.txt"));
			//- ударение НЕ задано (255)
			//-ударение задано однозначно(совпадает во всех интерпретациях)
			//-ударение задано омонимично(различается в интерпретациях)
			int cntUndefinedAccentOneVowel = 0;
			int cntUndefinedAccentMoreVowels = 0;
			int cntSingleAccentOneVowel = 0;
			int cntSingleAccentMoreVowels = 0;
			int cntTwoAccent = 0;
			int cntManyAccent = 0;
			int cntManyAccentReal = 0;
			foreach (var kvp in _dawg)
			{
				string form = kvp.Key;
				int vowels = AccentHelper.CountVowels(form);
				if (kvp.Value.AccentToAncodes.Count == 1)
				{
					int ac = kvp.Value.AccentToAncodes.Keys.First();
					if (ac == 255)
					{
						if (vowels == 1)
						{
							cntUndefinedAccentOneVowel++;
						}
						else cntUndefinedAccentMoreVowels++;
					}
					else
					{
						if (vowels == 1)
						{
							cntSingleAccentOneVowel++;
						}
						else cntSingleAccentMoreVowels++;
					}
				}
				else if (kvp.Value.AccentToAncodes.Count == 2)
				{
					cntTwoAccent++;
				}
				else
				{
					cntManyAccent++;
					if (kvp.Value.AccentToAncodes.Keys.All(a => a != 255))
					{
						cntManyAccentReal++;
						manyAccentWords.WriteLine("{0}, {1}, {2}", vowels, form,
							string.Join("-", kvp.Value.AccentToAncodes.Keys));
					}
				}
			}
			Console.WriteLine("cntUndefinedAccent           {0}", cntUndefinedAccentOneVowel+cntUndefinedAccentMoreVowels);
			Console.WriteLine("cntUndefinedAccentOneVowel   {0}", cntUndefinedAccentOneVowel);
			Console.WriteLine("cntUndefinedAccentMoreVowels {0}", cntUndefinedAccentMoreVowels);
			Console.WriteLine("cntSingleAccent              {0}", cntSingleAccentOneVowel + cntSingleAccentMoreVowels);
			Console.WriteLine("cntSingleAccentOneVowel      {0}", cntSingleAccentOneVowel);
			Console.WriteLine("cntSingleAccentMoreVowels    {0}", cntSingleAccentMoreVowels);
			Console.WriteLine("cntTwoAccent                 {0}", cntTwoAccent);
			Console.WriteLine("cntManyAccent                {0}", cntManyAccent);
			Console.WriteLine("cntManyAccentReal            {0}", cntManyAccentReal);
			Console.WriteLine("sum                          {0}",
				cntUndefinedAccentOneVowel+cntUndefinedAccentMoreVowels+
				cntSingleAccentOneVowel+cntSingleAccentMoreVowels+
				cntTwoAccent+cntManyAccent);
			manyAccentWords.Close();
		}
	}
}