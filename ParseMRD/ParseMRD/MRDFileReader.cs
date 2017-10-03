using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DawgSharp;

namespace ParseMRD
{
	/// <summary>
	/// Reads *.mrd file (raw text perpesentation of "morphological dictionary"
	/// </summary>
	public class MRDFileReader
	{
		private List<FlexiaModel> _flexiaModels = new List<FlexiaModel>();
		private List<AccentModel> _accentModels = new List<AccentModel>();
		private List<UserSession> _userSessions = new List<UserSession>();
		private List<PrefixSet> _prefixSets = new List<PrefixSet>();
		private List<Lemma> _lemmas = new List<Lemma>();

		private Gramtab _gramtab;

		public List<Lemma> Lemmas => _lemmas;
		public List<FlexiaModel> FlexiaModels => _flexiaModels;
		public List<AccentModel> AccentModels => _accentModels;
		public List<UserSession> UserSessions => _userSessions;
		public List<PrefixSet> PrefixSets => _prefixSets;

		private void _parseFlexiaModel(string line)
		{
			//%< flexion > *< ancode >
			//	or %< flexion > *< ancode > *< prefix >
			FlexiaModel model = new FlexiaModel();
			foreach (string item in line.Split(new[] {'%'}, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] parts = item.Split('*');
				var flexia = new Flexia();
				flexia.Flexion = parts[0];
				flexia.Ancode = parts[1].Substring(0, 2);
				flexia.AncodeNo = _gramtab.AncodeToInt(flexia.Ancode);
				_gramtab.CountFrequency(flexia.Ancode);
				if (parts.Length > 2)
				{
					flexia.Prefix = parts[2];
				}
				model.Forms.Add(flexia);
			}
			_flexiaModels.Add(model);
		}

		private void _parseAccentModel(string line)
		{
			var model = new AccentModel();
			foreach (string item in line.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries))
			{
				int accent = int.Parse(item);
				model.AccentForms.Add(accent);
			}
			_accentModels.Add(model);

		}

		private void _parseUserSession(string line)
		{
			string[] parts = line.Split(';');
			var session = new UserSession();
			session.UserName = parts[0];
			session.Start = DateTime.Parse(parts[1], CultureInfo.InstalledUICulture);
			session.End = DateTime.Parse(parts[2], CultureInfo.InstalledUICulture);
			_userSessions.Add(session);
		}

		private void _parsePrefixSet(string line)
		{
			var ps = new PrefixSet();
			foreach (string item in line.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
			{
				ps.Prefixes.Add(item);
			}
			_prefixSets.Add(ps);
		}

		private void _parseLemma(string line)
		{
			// <base> <flex_model_no> <accent_model_no> <session_no> <type_ancode> <prefix_set_no>
			// АНАГРАММ 50 42 1 Фа -
			string[] parts = line.Split(' ');
			Lemma l = new Lemma();
			l.Base = parts[0];
			l.FlexModelNo = int.Parse(parts[1]);
			l.AccentModelNo = int.Parse(parts[2]);
			l.SessionNo = int.Parse(parts[3]);
			if (parts[4] != "-")
			{
				l.TypeAncode = parts[4];
				l.TypeAncodeNo = _gramtab.AncodeToInt(l.TypeAncode);
				_gramtab.CountFrequency(l.TypeAncode);
			}
			if (parts[5] != "-")
			{
				l.PrefixSetNo = int.Parse(parts[5]);
			}
			_lemmas.Add(l);
		}

		private void _readSection(StreamReader reader, Action<string> parseLine)
		{
			int count;
			string s = reader.ReadLine();
			if (s == null || !int.TryParse(s, out count))
			{
				throw new InvalidDataException("Expected number of all records of the section.");
			}
			while (count > 0)
			{
				s = reader.ReadLine();
				parseLine(s);
				count--;
			}
		}

		
		public void LoadMrd(string fileName)
		{
			DateTime start = DateTime.Now;
			using (FileStream file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (StreamReader reader = new StreamReader(file, Encoding.Default))
			{
				_readSection(reader, _parseFlexiaModel);
				_readSection(reader, _parseAccentModel);
				_readSection(reader, _parseUserSession);
				_readSection(reader, _parsePrefixSet);
				_readSection(reader, _parseLemma);
			}
			Console.WriteLine("MRD load time: {0}", DateTime.Now - start);
		}

		public void PrintStat()
		{
			Console.WriteLine("FlexiaModels: " + _flexiaModels.Count);
			Console.WriteLine("AccentModels: " + _accentModels.Count);
			Console.WriteLine("PrefixSets:   " + _prefixSets.Count);
			Console.WriteLine("Lemmas:       " + _lemmas.Count);
			Console.WriteLine("Lemmas with PrefixSets: " + _lemmas.Count(l => l.PrefixSetNo.HasValue));
			Console.WriteLine("FlexiaModels min forms: " + _flexiaModels.Min(fm => fm.Forms.Count));
			Console.WriteLine("FlexiaModels max forms: " + _flexiaModels.Max(fm => fm.Forms.Count));
			Console.WriteLine("FlexiaModels avg forms: " + _flexiaModels.Average(fm => fm.Forms.Count));
			Console.WriteLine("AccentModels all 255: " + _accentModels.Count(am => am.AccentForms.All(i => i == 255)));
			Console.WriteLine("AccentModels any 255: " + _accentModels.Count(am => am.AccentForms.Any(i => i == 255)));
			Console.WriteLine("AccentModels all not 255: " + _accentModels.Count(am => am.AccentForms.All(i => i != 255)));
		}

		public void PrintStat2()
		{
			// !!!
			// Здесь число интерпретаций, а не форм слова

			// Общее число форм
			List<WordForm> forms = AllForms.ToList();
			Console.WriteLine("forms: {0}", forms.Count);

			// статистика по гласным
			foreach (var g in forms
				.Select(f => AccentHelper.CountVowels(f.ToString()))
				.GroupBy(i => i)
				.OrderBy(g => g.Key))
			{
				Console.WriteLine("форм где {0} гласных: {1}", g.Key, g.Count());
			}

			//количество форм, где менее двух гласных(ударение итак однозначно)
			int cntVo01 = forms.Count(f => AccentHelper.CountVowels(f.ToString()) <= 1);
			Console.WriteLine("количество форм, где менее двух гласных(ударение итак однозначно): " + cntVo01);

			//количество форм с двумя и более гласными, для которых
			var forms2 = forms.Where(f => AccentHelper.CountVowels(f.ToString()) > 1).ToList();
			//var forms15 = forms.Where(f => AccentHelper.CountVowels(f.ToString()) == 15).ToList();
			//foreach (var f in forms15)
			//{
			//	Console.WriteLine(f);
			//}

			Console.WriteLine("количество форм с двумя и более гласными: " + forms2.Count);
			//	-ударение НЕ задано(255)
			Console.WriteLine("  ударение НЕ задано(255): " + forms2.Count(f => f.Accent == 255));
			
			// Следующие 2 посчитаны в MorphAn.PrintStat
			//	-ударение задано однозначно(совпадает во всех интерпретациях)
			//-ударение задано омонимично(различается в интерпретациях)
			
			//-FlexiaModels с приставками
			var flexModelWithPre = _flexiaModels.Where(fm => fm.Forms.Any(f => !string.IsNullOrWhiteSpace(f.Prefix))).ToList();
			var flexModelWithPreAll = _flexiaModels.Where(fm => fm.Forms.All(f => !string.IsNullOrWhiteSpace(f.Prefix))).ToList();
			Console.WriteLine("FlexiaModels с приставками: {0}", flexModelWithPre.Count);
			Console.WriteLine("FlexiaModels все с приставками: {0}", flexModelWithPreAll.Count);
			//-формы с приставками
			var formsWithPre = forms.Where(form => !string.IsNullOrWhiteSpace(form.Flexia.Prefix)).ToList();
			Console.WriteLine("формы с приставками: {0}", formsWithPre.Count);
			

			// все леммы, имеющие Prefix Set
			//var prefixLemmas = Lemmas.Where(l => l.PrefixSetNo.HasValue).ToList();
			//Console.WriteLine("PrefixSet lemmas: {0}", prefixLemmas.Count);
			//foreach (var g in prefixLemmas.GroupBy(l => l.PrefixSetNo.Value))
			//{
			//	Console.WriteLine("{0}", string.Join("; ", _prefixSets[g.Key].Prefixes));
			//	foreach (Lemma l in g)
			//	{
			//		Console.WriteLine("    {0}", l.Base);
			//	}
			//}

		}

		public void PrintLemma(string wordBase)
		{
			foreach (Lemma l in Lemmas.Where(l => l.Base == wordBase))
			{
				PrintLemma(l);
			}

		}

		public void PrintLemma(Lemma l)
		{
			FlexiaModel flexModel = _flexiaModels[l.FlexModelNo];
			AccentModel accentModel = _accentModels[l.AccentModelNo];
			UserSession session = _userSessions[l.SessionNo];
			Console.WriteLine("=== {0} {1} {2} {3} ===", l.Base, _gramtab.LookupAncode(l.TypeAncode), flexModel.Forms.Count,
				accentModel.AccentForms.Count);
			int formNo = 0;
			foreach (Flexia form in flexModel.Forms)
			{
				if (!string.IsNullOrWhiteSpace(form.Prefix))
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.Write(form.Prefix);
				}
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(l.Base);
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write(form.Flexion);
				int accent = accentModel.AccentForms[formNo];
				Console.Write(" {0} ", accent);
				string accWord = AccentHelper.SetAccent(form.Prefix + l.Base + form.Flexion, accent);
				Console.Write(" " + accWord + " ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write("\t" + _gramtab.LookupAncode(form.Ancode));
				Console.ResetColor();
				Console.WriteLine();
				formNo++;
			}
			if (l.PrefixSetNo.HasValue)
			{
				PrefixSet prefixSet = _prefixSets[l.PrefixSetNo.Value];
				Console.WriteLine("PrefixSets:");
				foreach (string pref in prefixSet.Prefixes)
				{
					Console.WriteLine(pref);
				}
			}
			Console.WriteLine("-------------------");
		}

		public IEnumerable<WordForm> AllForms
		{
			get
			{
				foreach (Lemma lemma in Lemmas)
				{
					foreach (WordForm form in GetLemmaForms(lemma, string.Empty))
					{
						yield return form;
					}
					//if (lemma.PrefixSetNo.HasValue)
					//{
					//	PrefixSet prefixSet = _prefixSets[lemma.PrefixSetNo.Value];
					//	foreach (string pref in prefixSet.Prefixes)
					//	{
					//		foreach (WordForm form in GetLemmaForms(lemma, pref))
					//		{
					//			yield return form;
					//		}
					//	}
					//}
				}
			}
		}

		public IEnumerable<WordForm> GetLemmaForms(Lemma l, string prefix)
		{
			FlexiaModel flexModel = _flexiaModels[l.FlexModelNo];
			AccentModel accentModel = _accentModels[l.AccentModelNo];
			//UserSession session = _userSessions[l.SessionNo];
			int formNo = 0;
			foreach (Flexia form in flexModel.Forms)
			{
				int accent = accentModel.AccentForms[formNo];
				yield return new WordForm()
				{
					Lemma = l,
					Prefix = prefix,
					Flexia = form,
					Accent = accent
				};
				formNo++;
			}
		}


		public void WriteAllForms(string path)
		{
			UInt64 cntForms = 0;
			using (StreamWriter writer = File.CreateText(path))
			{
				foreach (WordForm f in AllForms)
				{
					string word = f.Prefix + f.Flexia.Prefix + f.Lemma.Base + f.Flexia.Flexion;
					writer.WriteLine("{0}; {1}; {2}", word, f.Accent, _gramtab.LookupAncode(f.Flexia.Ancode));
					cntForms++;
				}
			}
			Console.WriteLine("All forms count: " + cntForms);
		}
				
		public MRDFileReader(Gramtab gramtab)
		{
			_gramtab = gramtab;
		}
	}

	public class FormInterpretations
	{
		public Dictionary<int, List<UInt16>> AccentToAncodes = new Dictionary<int, List<UInt16>>();

		public void Add(WordForm wordForm)
		{
			List<UInt16> list;
			if (AccentToAncodes.ContainsKey(wordForm.Accent))
			{
				list = AccentToAncodes[wordForm.Accent];
			}
			else
			{
				list = new List<ushort>();
				AccentToAncodes.Add(wordForm.Accent, list);
			}
			if (!list.Contains(wordForm.Flexia.AncodeNo))
			{
				list.Add(wordForm.Flexia.AncodeNo);
			}
			//throw new Exception("Already exists!");
		}
	}
	
	public class FlexiaModel
	{
		public List<Flexia> Forms = new List<Flexia>();
	}

	public class Flexia
	{
		public string Flexion;
		public string Prefix;
		public string Ancode;
		public UInt16 AncodeNo;
	}

	public class AccentModel
	{
		public List<int> AccentForms = new List<int>();
	}

	public class PrefixSet
	{
		public List<string> Prefixes = new List<string>();
	}

	public class UserSession
	{
		public string UserName;
		public DateTime Start;
		public DateTime End;
	}

	public class Lemma
	{
		// <base> <flex_model_no> <accent_model_no> <session_no> <type_ancode> <prefix_set_no>
		public string Base;
		public int FlexModelNo;
		public int AccentModelNo;
		public int SessionNo;
		public string TypeAncode;
		public UInt16 TypeAncodeNo;
		public int? PrefixSetNo;
	}

	public class WordForm
	{
		public Lemma Lemma;
		public Flexia Flexia;
		public int Accent;
		public string Prefix;

		public override string ToString()
		{
			return string.Concat(Prefix, Flexia.Prefix, Lemma.Base, Flexia.Flexion);
		}
	}
}