namespace Tic_tac_toe
{
    using System.Drawing;

    /// <summary>
    /// Класс линия
    /// </summary>
    public class Line
    {
        /// <summary>
        /// Начальная точка линии
        /// </summary>
        public PointF StartPoint { get; set; }

        /// <summary>
        /// Конечная точка линии
        /// </summary>
        public PointF EndPoint { get; set; }

        public Line(PointF startPoint, PointF endPoint)
        {
            this.StartPoint = startPoint;
            this.EndPoint = endPoint;
        }
    }
}