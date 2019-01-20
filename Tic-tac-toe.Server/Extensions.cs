namespace Tic_tac_toe.Server
{
    using System;

    using Tic_tac_toe.Common;

    public static class Extensions
    {
        public static string TypeMoveToStr(this TypeMove typeMove)
        {
            switch (typeMove)
            {
                case TypeMove.Cross:
                    return "крестик";

                case TypeMove.Nougth:
                    return "нолик";

                default:
                    return string.Empty;
            }
        }
    }
}