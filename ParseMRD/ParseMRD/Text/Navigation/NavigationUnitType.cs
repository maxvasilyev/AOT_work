namespace MaxReaderModel.Text.Navigation
{
    /// <summary>
    /// Enumerates text element by which navigation is possible
    /// Перечисление Элементов текста по которым возможна навигация
    /// </summary>
    public enum NavigationUnitType
    {
        Character,
        Word,
        Line,
        Sentence,
        Paragraph,
        Page,
        Section
    }
}