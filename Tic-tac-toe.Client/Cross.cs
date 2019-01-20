namespace Tic_tac_toe
{
    /// <summary>
    /// Класс "Крестик"
    /// </summary>
    public class Cross
    {
        /// <summary>
        /// Левая линия
        /// </summary>
        public Line LeftLine { get; set; }

        /// <summary>
        /// Правая линия
        /// </summary>
        public Line RightLine { get; set; }

        public Cross(Line leftLine, Line rightLine)
        {
            this.LeftLine = leftLine;
            this.RightLine = rightLine;
        }
    }
}