namespace Tic_tac_toe
{
    using System;
    using System.Windows.Forms;

    using Tic_tac_toe.Common;

    public static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(
                args.Length == 0
                    ? new Form1()
                    : new Form1(int.Parse(args[0]), (TypeMove)Enum.Parse(typeof(TypeMove), args[1])));
        }
    }
}
