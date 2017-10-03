using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaxReaderModel.Text;
using MaxReaderModel.Text.Navigation;
using MaxReaderViewModel;

namespace ParseMRD
{
	public class TextProcessor : IDisposable
	{
		private StreamReader _stream;
		private IText _text;
		private MorphAn _morph;
		private IUnitNavigator _wordNav;
		private IUnitNavigator _sentenceNav;
		private ISegment _currentSentSegment;
		private ISegment _currentWordSegment;
		private string _currentSent;
		private string _currentWord;
		
		public List<string> NotFoundWords = new List<string>();
		public List<string> SingleAccentWords = new List<string>();
		public List<string> ManyAccentWords = new List<string>();

		public void Process()
		{
			_currentWordSegment = _wordNav.Next(_text, _currentWordSegment.Offset);
			_currentSentSegment = _sentenceNav.Next(_text, _currentSentSegment.Offset);
			_currentSent = _text.GetText(_currentSentSegment);
			while (!SimpleSegment.IsInvalid(_currentWordSegment))
			{
				_currentWord = _text.GetText(_currentWordSegment);
				if (_currentWordSegment.Offset > _currentSentSegment.EndOffset)
				{
					_currentSentSegment = _sentenceNav.Next(_text, _currentSentSegment.Offset);
					_currentSent = _text.GetText(_currentSentSegment);
				}
				ProcessWord();
				_currentWordSegment = _wordNav.Next(_text, _currentWordSegment.Offset);
			}			

			// Save stat
			File.WriteAllText(  "notFound.txt", NotFoundWords.Count + Environment.NewLine);
			File.AppendAllLines("notFound.txt", NotFoundWords);
			File.WriteAllText("SingleAccentWords.txt", SingleAccentWords.Count + Environment.NewLine);
			File.AppendAllLines("SingleAccentWords.txt", SingleAccentWords);
			File.WriteAllText("ManyAccentWords.txt", ManyAccentWords.Count + Environment.NewLine);
			File.AppendAllLines("ManyAccentWords.txt", ManyAccentWords);
		}

		public virtual void ProcessWord()
		{
			Console.WriteLine(_currentWord);
			FormInterpretations forms = _morph.Lookup(_currentWord);
			if (forms != null)
			{
				if (forms.AccentToAncodes.Count == 1)
				{
					SingleAccentWords.Add(AccentHelper.SetAccent(_currentWord, forms.AccentToAncodes.Keys.First()));
				}
				else
				{
					ManyAccentWords.Add(_currentWord);
				}
			}
			else
			{
				NotFoundWords.Add(_currentWord);
			}
		}

		public TextProcessor(string txtPath, MorphAn morph)
		{
			_morph = morph;
			StreamReader reader = FileReader.OpenFile(txtPath);
			_text = FileReader.CreateVirtualText(reader);
			_wordNav = new SuperNavigator(new WordNavigator());
			_sentenceNav = new SuperNavigator(new SentenceNavigator());
			_currentSentSegment = new SimpleSegment(-1, 0);
			_currentWordSegment = new SimpleSegment(-1, 0);
		}

		public void Dispose()
		{
			_stream?.Dispose();
		}
	}
}