namespace Tic_tac_toe
{
    using System.Drawing;

    /// <summary>
    /// Класс "Нолик"
    /// </summary>
    public class Nought
    {
        public RectangleF Circle { get; set; }

        public Nought(RectangleF circle)
        {
            this.Circle = circle;
        }
    }
}