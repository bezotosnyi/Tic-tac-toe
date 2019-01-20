namespace Tic_tac_toe.Common
{
    public enum PipeMessageType
    {
        /// <summary>
        /// Разрешить ход
        /// </summary>
        Allow,

        /// <summary>
        /// Запретить ход
        /// </summary>
        Alien,

        /// <summary>
        /// Победа ноликов
        /// </summary>
        WinNought,

        /// <summary>
        /// Победа крестиков
        /// </summary>
        WinCross,

        /// <summary>
        /// Ничья
        /// </summary>
        Standoff
    }
}