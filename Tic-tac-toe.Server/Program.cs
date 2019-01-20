namespace Tic_tac_toe.Server
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using NamedPipeWrapper;

    using Tic_tac_toe.Common;

    public class Program
    {
        /// <summary>
        /// Имя клиентского процесса
        /// </summary>
        private const string ClientProcessName = @"..\..\..\Tic-tac-toe.Client\bin\Debug\Tic-tac-toe.Client.exe";

        /// <summary>
        /// Именнованый канал
        /// </summary>
        private static readonly NamedPipeServer<PipeMessage> PipeServer = new NamedPipeServer<PipeMessage>("Tic-tac-toe");

        /// <summary>
        /// Игровое поле в ходах
        /// </summary>
        private static readonly TypeMove[,] GameField = new TypeMove[5, 5];

        /// <summary>
        /// Клиентские процессы
        /// </summary>
        private static readonly Process[] ClientProcess = new Process[]
                                                              {
                                                                  new Process
                                                                      {
                                                                          StartInfo = new ProcessStartInfo(
                                                                              ClientProcessName, $"1 {TypeMove.Cross}")
                                                                      },
                                                                  new Process
                                                                      {
                                                                          StartInfo = new ProcessStartInfo(
                                                                              ClientProcessName, $"2 {TypeMove.Nougth}")
                                                                      }
                                                              };

        /// <summary>
        /// Текущий ход
        /// </summary>
        private static TypeMove currentMove = TypeMove.None;

        /// <summary>
        /// Есть ли победитель
        /// </summary>
        private static bool hasWinner = false;

        static Program()
        {
            InitialStrokeGameField();
        }

        public static void Main(string[] args)
        {
            PipeServer.ClientConnected += PipeServerOnClientConnected;
            PipeServer.ClientMessage += PipeServerOnClientMessage;
            PipeServer.ClientDisconnected += PipeServerOnClientDisconnected;

            PipeServer.Start();
            Console.WriteLine("Сервер запущен.");
            Console.WriteLine();

            for (int i = 0; i < ClientProcess.Length; i++)
            {
                ClientProcess[i].Start();
                Console.WriteLine($"Игровое поле {i + 1} запущено.");
            }

            Console.WriteLine();
            Console.Write("Для закрытия нажмите любою клавишу...");
            Console.WriteLine();
            Console.WriteLine();

            Console.ReadKey();
            Console.WriteLine();

            // уничтожение процессов и остановка сервера
            for (int i = 0; i < ClientProcess.Length; i++)
            {
                ClientProcess[i].Kill();
                Console.WriteLine($"Игровое поле {i + 1} закрыто.");
            }

            Console.WriteLine();

            PipeServer.Stop();
            Console.WriteLine($"Сервер остановлен.");

            Thread.Sleep(1000);
        }

        private static void PipeServerOnClientMessage(NamedPipeConnection<PipeMessage, PipeMessage> connection, PipeMessage message)
        {
            // только обновить
            if (message.GetGameFieldOnly || hasWinner)
            {
                connection.PushMessage(new PipeMessage() { GameField = GameField, GetGameFieldOnly = true });
                return;
            }

            var pipeMessage = new PipeMessage();

            // если еще не было хода, то установка кто ходит первый
            if (currentMove == TypeMove.None)
            {
                currentMove = message.TypeMove;

                Console.WriteLine($"Первый ход был за {currentMove.TypeMoveToStr()}.");
            }

            // вывод хода
            Console.WriteLine(message.Message);

            // если ходит не тот
            if (currentMove != message.TypeMove)
            {
                pipeMessage.PipeMessageType = PipeMessageType.Alien; // запрет хода
                pipeMessage.Message = $"Очередь ходить {currentMove.TypeMoveToStr()}.";
                pipeMessage.GameField = GameField;

                Console.WriteLine($"Очередь ходить {currentMove.TypeMoveToStr()}.");

                connection.PushMessage(pipeMessage);

                return;
            }

            // если в этой клетки нет хода
            if (GameField[message.MovePoint.X, message.MovePoint.Y] == TypeMove.None)
            {
                // ставим ход
                GameField[message.MovePoint.X, message.MovePoint.Y] = message.TypeMove;

                // проверака победы
                switch (CheckWinner())
                {
                    case TypeMove.None:
                        pipeMessage.PipeMessageType = PipeMessageType.Allow; // разрешаем
                        pipeMessage.Message =
                            $"Разрешен ход [{message.MovePoint.X}, {message.MovePoint.Y}] {currentMove.TypeMoveToStr()}.";

                        Console.WriteLine($"Разрешен ход [{message.MovePoint.X}, {message.MovePoint.Y}] {currentMove.TypeMoveToStr()}.");
                        break;

                    case TypeMove.Cross:
                        hasWinner = true;
                        pipeMessage.PipeMessageType = PipeMessageType.WinCross;
                        pipeMessage.Message =
                            $"Ход [{message.MovePoint.X}, {message.MovePoint.Y}] приносит победу крестику.";
                        Console.WriteLine($"Ход [{message.MovePoint.X}, {message.MovePoint.Y}] приносит победу крестику.");
                        break;

                    case TypeMove.Nougth:
                        hasWinner = true;
                        pipeMessage.PipeMessageType = PipeMessageType.WinNought;
                        pipeMessage.Message =
                            $"Ход [{message.MovePoint.X}, {message.MovePoint.Y}] приносит победу нолику.";
                        Console.WriteLine($"Ход [{message.MovePoint.X}, {message.MovePoint.Y}] приносит победу нолику.");
                        break;

                    case TypeMove.Full:
                        hasWinner = true;
                        pipeMessage.PipeMessageType = PipeMessageType.Standoff;
                        pipeMessage.Message =
                            $"Ход [{message.MovePoint.X}, {message.MovePoint.Y}] заканчивает игру ничьей.";
                        Console.WriteLine($"Ход [{message.MovePoint.X}, {message.MovePoint.Y}] заканчивает игру ничьей.");
                        break;
                }
            }
            else
            {
                pipeMessage.PipeMessageType = PipeMessageType.Alien;
                pipeMessage.Message = $"Ход запрещен. Ход [{message.MovePoint.X}, {message.MovePoint.Y}] существует.";

                Console.WriteLine($"Ход запрещен. Ход [{message.MovePoint.X}, {message.MovePoint.Y}] существует.");
            }

            pipeMessage.GameField = GameField;

            connection.PushMessage(pipeMessage);

            // установка кто следущий ходит
            currentMove = currentMove == TypeMove.Cross ? TypeMove.Nougth : TypeMove.Cross;
        }

        /// <summary>
        /// Подключения пользователя к серверу
        /// </summary>
        private static void PipeServerOnClientConnected(NamedPipeConnection<PipeMessage, PipeMessage> connection)
        {
            if (connection.Id > 2)
                return;

            Console.WriteLine($"Игрок {connection.Id} подключен к серверу.");
            Console.WriteLine();
        }

        /// <summary>
        /// Отклчюение пользователя от сервера
        /// </summary>
        private static void PipeServerOnClientDisconnected(NamedPipeConnection<PipeMessage, PipeMessage> connection)
        {
            if (connection.Id > 2)
                return;

            Console.WriteLine($"Игрок {connection.Id} отключен от сервера.");
            Console.WriteLine();
        }

        /// <summary>
        /// Начальная инициализация игрового поля
        /// </summary>
        private static void InitialStrokeGameField()
        {
            for (int i = 0; i < GameField.GetLength(0); i++)
            {
                for (int j = 0; j < GameField.GetLength(1); j++)
                {
                    GameField[i, i] = TypeMove.None;
                }
            }
        }

        /// <summary>
        /// Проверка победы
        /// </summary>
        private static TypeMove CheckWinner()
        {
            // проверка ничьи
            int countEmpty = 0;
            for (int i = 0; i < GameField.GetLength(0); i++)
            {
                for (int j = 0; j < GameField.GetLength(1); j++)
                {
                    if (GameField[i, j] == TypeMove.None)
                        countEmpty++;
                }
            }

            if (countEmpty == 0)
                return TypeMove.Full;

            // проверка по ширине все крестики/нолики
            int count0;
            int countX;
            for (int i = 0; i < GameField.GetLength(0); i++)
            {
                countX = 0;
                count0 = 0;
                for (int j = 0; j < GameField.GetLength(1); j++)
                {
                    if (GameField[i, j] == TypeMove.None)
                        break;

                    if (GameField[i, j] == TypeMove.Cross)
                        countX++;

                    if (GameField[i, j] == TypeMove.Nougth)
                        count0++;
                }

                if (count0 == 5) return TypeMove.Nougth;

                if (countX == 5) return TypeMove.Cross;
            }

            // проверка по высоте все крестики/нолики
            for (int j = 0; j < GameField.GetLength(1); j++)
            {
                countX = 0;
                count0 = 0;
                for (int i = 0; i < GameField.GetLength(0); i++)
                {
                    if (GameField[i, j] == TypeMove.None)
                        break;

                    if (GameField[i, j] == TypeMove.Cross)
                        countX++;

                    if (GameField[i, j] == TypeMove.Nougth)
                        count0++;
                }

                if (count0 == 5) return TypeMove.Nougth;

                if (countX == 5) return TypeMove.Cross;
            }

            // проверка левой диагонали
            countX = 0;
            count0 = 0;
            for (int i = 0; i < GameField.GetLength(0); i++)
            {
                if (GameField[i, i] == TypeMove.None)
                    break;

                if (GameField[i, i] == TypeMove.Cross)
                    countX++;

                if (GameField[i, i] == TypeMove.Nougth)
                    count0++;


                if (count0 == 5) return TypeMove.Nougth;

                if (countX == 5) return TypeMove.Cross;
            }

            // проверка правой диагонали
            countX = 0;
            count0 = 0;
            for (int i = 0; i < GameField.GetLength(0); i++)
            {
                if (GameField[i, GameField.GetLength(0) - 1 - i] == TypeMove.None)
                    break;

                if (GameField[i, GameField.GetLength(0) - 1 - i] == TypeMove.Cross)
                    countX++;

                if (GameField[i, GameField.GetLength(0) - 1 - i] == TypeMove.Nougth)
                    count0++;


                if (count0 == 5) return TypeMove.Nougth;

                if (countX == 5) return TypeMove.Cross;
            }

            return TypeMove.None;
        }
    }
}
