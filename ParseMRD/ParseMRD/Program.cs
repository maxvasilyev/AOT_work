using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DawgSharp;

namespace ParseMRD
{
	class Program
	{
		static string workDir; // "\Data"
		static Gramtab gramtab;

		static void Main(string[] args)
		{
			workDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			// get from ParseMRD\ParseMRD\bin\debug
			while (!Directory.Exists(Path.Combine(workDir, "Data")))
			{
				workDir = Path.GetDirectoryName(workDir);
			}
			workDir = Path.Combine(workDir, "Data");

			gramtab = new Gramtab();
			gramtab.LoadGramtab(Path.Combine(workDir, @"Dicts\rgramtab.tab"));
			
			// Здесь выбираю тестовый метод, смотря над чем работаю
			ParseMRDFile();
			//LoadDAWG();
			//DAWGTest();
			//SetTextAccent(@"Texts\text1.txt");

			Console.WriteLine("Hit a key...");
			Console.ReadKey();
		}

		private static void SetTextAccent(string textName)
		{
			MorphAn m = new MorphAn(gramtab, workDir);
			m.LoadDAWG();
			string outDir = Path.Combine(workDir, "TextsOut");
			if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
			Directory.SetCurrentDirectory(outDir);
			TextProcessor p = new TextProcessor(Path.Combine(workDir, textName), m);
			p.Process();
		}

		private static void LoadDAWG()
		{
			MorphAn m = new MorphAn(gramtab, workDir);
			m.LoadDAWG();
			Console.WriteLine("Calculating statistics...");
			m.PrintStat();

			// Поиск слов
			m.PrintLookup("СТАРИНАМИ");
			m.PrintLookup("ЭЛЕКТРОПРОВОДА");
			m.PrintLookup("ГЛАЗА");
			//m.PrintLookup("ЗАМОК");
			//m.PrintLookup("ВЕСТИ");
			//m.PrintLookup("глокая");
			//m.PrintLookup("шпион");
			//m.PrintLookup("супершпион");
		}

		private static void ParseMRDFile()
		{
			MRDFileReader mrdFile = new MRDFileReader(gramtab);
			mrdFile.LoadMrd(Path.Combine(workDir, @"Dicts\morphs.mrd"));
			mrdFile.PrintStat();
			mrdFile.PrintStat2();

			//mrdFile.PrintLemma("КЛЕ");
			mrdFile.PrintLemma("ГЛАЗ");
			//mrdFile.PrintLemma("ЗАМ");

			mrdFile.WriteAllForms(Path.Combine(workDir, "forms.txt"));			
		}

		private static void DAWGTest()
		{
			DawgSharp.DawgBuilder<string> builder = new DawgBuilder<string>();
			builder.Insert("МЕГА", "1");
			builder.Insert("ГИГА", "2");
			builder.Insert("СУПЕР", "3");
			builder.Insert("ПРЕ", "4");
			builder.Insert("ПРЕД", "5");
			builder.Insert("СУПЕРГЕТЕРО", "6");
			DawgSharp.Dawg<string> d = builder.BuildDawg();

			string r1 = d["СУПЕР"]; // Есть, r1 = "3"
			string r2 = d["НАНОФУСЬКА"]; // Нету, r2 = null
			string r3 = d["СУПЕРШПИОН"]; // Есть начало, но слово не совпадает r3 = null

			int commonPrefixLength = d.GetLongestCommonPrefixLength("СУПЕРШПИОН");
			Console.WriteLine(commonPrefixLength); // 5 супер
			string prefix = "СУПЕРШПИОН".Substring(0, commonPrefixLength);

			string r4 = d[prefix]; // r4 = 3 есть

			// поиск СУПЕР*
			foreach (KeyValuePair<string, string> kvp in d.MatchPrefix(prefix))
			{
				Console.WriteLine("{0} {1}", kvp.Key, kvp.Value);
			}


			// поиск ПР*
			foreach (KeyValuePair<string, string> kvp in d.MatchPrefix("ПР"))
			{
				Console.WriteLine("{0} {1}", kvp.Key, kvp.Value);
			}

			
		}
	}
}
