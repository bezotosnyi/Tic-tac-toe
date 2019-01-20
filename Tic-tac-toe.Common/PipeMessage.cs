namespace Tic_tac_toe.Common
{
    using System;
    using System.Drawing;

    [Serializable]
    public class PipeMessage
    {
        /// <summary>
        /// Тип сообщения
        /// </summary>
        public PipeMessageType PipeMessageType { get; set; }

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Тип хода
        /// </summary>
        public TypeMove TypeMove { get; set; }

        /// <summary>
        /// Координаты хода
        /// </summary>
        public Point MovePoint { get; set; }

        /// <summary>
        /// Игровое поле
        /// </summary>
        public TypeMove[,] GameField { get; set; }

        /// <summary>
        /// Только обновить
        /// </summary>
        public bool GetGameFieldOnly { get; set; } = false;
    }
}