namespace MaxReaderModel.Text.Navigation
{
	/// <summary>
	/// The interface defines text unit delimiter finder (e.g. word, line, sentence, etc)
	/// and text unit (itself) finder.
	/// Type of unit is defined by implementation.
	/// Scans text forward or backward from the given location skipping the text unit body (e.g. word chars)
	/// and marks the text unit delimiter (e.g. spaces).
	/// <see cref="SuperNavigator"/> looks ahead to the second delimeter and marks the text unit itself (e.g. word).
	/// 
	/// ��������� ���������� ������ ��� ������ ������������ ��������� ������ ������
	/// (����, �����, ����������� � �.�.), � ����� ����� ���� ������.
	/// ��� ����� ������ ������������ �����������.
	/// ������������� ����� ������ ��� ����� � ��������� �������,
	/// ��������� ����� ������ (��������, ������� �����) � �������� ����������� (��������, �������).
	/// <see cref="SuperNavigator"/> ���� ������ ����������� � �������� ���� ����� ������ (��������, �����).
	/// </summary>
	public interface IUnitNavigator
    {
        /// <summary>
        /// Gets the location of the next unit, or SimpleSegment.Invalid
        /// if none is found.
        /// </summary>
        ISegment Next(IText text, int offset);

        /// <summary>
        /// Gets the location of the previous unit, or SimpleSegment.Invalid
        /// if none is found.
        /// </summary>
        ISegment Prev(IText text, int offset);
    }
}