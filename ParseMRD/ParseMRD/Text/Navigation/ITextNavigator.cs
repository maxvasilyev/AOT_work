namespace MaxReaderModel.Text.Navigation
{
    public interface ITextNavigator : IUnitNavigator
    {
        /// <summary>
        /// Selected navigation unit type
        /// </summary>
        NavigationUnitType CurrentUnit { get; set; }

        /// <summary>
        /// Increase CurrentUnit to larger one
        /// </summary>
        void NextUnit();
        /// <summary>
        /// Decrease CurrentUnit to smaller one
        /// </summary>
        void PrevUnit();
    }
}