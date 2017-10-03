namespace MaxReaderModel.Text.Navigation
{
    /// <summary>
    /// Enumerates text element by which navigation is possible
    /// ������������ ��������� ������ �� ������� �������� ���������
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