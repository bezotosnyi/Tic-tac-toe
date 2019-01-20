namespace Tic_tac_toe
{
    using System;
    using System.Drawing;
    using System.Threading;
    using System.Windows.Forms;

    using NamedPipeWrapper;

    using Tic_tac_toe.Common;

    public partial class Form1 : Form
    {
        /// <summary>
        /// Именнованый канал
        /// </summary>
        private readonly NamedPipeClient<PipeMessage> pipeClient = new NamedPipeClient<PipeMessage>("Tic-tac-toe");

        /// <summary>
        /// Массив координат линиий сетки
        /// </summary>
        private readonly Line[] gridLines = new Line[8];

        /// <summary>
        /// Кисть для рисования сетки
        /// </summary>
        private readonly Pen gridPen = new Pen(Color.Black, 10f);

        /// <summary>
        /// Кисть для рисование крестика
        /// </summary>
        private readonly Pen crossPen = new Pen(Color.Red, 10f);

        /// <summary>
        /// Кисть для рисования нолика
        /// </summary>
        private readonly Pen noughtPen = new Pen(Color.Blue, 10f);

        /// <summary>
        /// Игровое поле
        /// </summary>
        private readonly GameField[,] gameField = new GameField[5, 5];

        /// <summary>
        /// Тип хода игрока
        /// </summary>
        private readonly TypeMove typeMove = TypeMove.None;

        /// <summary>
        /// Поток для обновления данных
        /// </summary>
        private readonly Thread updateThread;

        /// <summary>
        /// Мьютекс для синхронизации
        /// </summary>
        private readonly Mutex mutex = new Mutex();

        /// <summary>
        /// Id игрока
        /// </summary>
        private readonly int playerId;

        public Form1()
        {
            this.InitializeComponent();
        }

        public Form1(int playerId, TypeMove typeMove)
        {
            this.playerId = playerId;
            this.typeMove = typeMove;

            this.InitializeComponent();

            var moveStr = typeMove == TypeMove.Cross ? "крестик" : "нолик";
            this.Text += $" : [игрок {playerId} - {moveStr}]";

            this.updateThread = new Thread(this.UpdateGameField);
        }

        /// <summary>
        /// Обновление игрового поля каждые 10мс
        /// </summary>
        private void UpdateGameField()
        {
            while (true)
            {
                this.mutex.WaitOne();

                this.pipeClient.PushMessage(new PipeMessage() { GetGameFieldOnly = true });
                this.Invalidate();

                this.mutex.ReleaseMutex();

                Thread.Sleep(10);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // инициализация игрового поля
            this.InitializeGameField();

            // получаем рабочую ширину и высоту экрана
            var resolution = Screen.PrimaryScreen.WorkingArea.Size;
            this.Width = resolution.Height / 2;
            this.Height = resolution.Height / 2;

            // чтобы окна были рядом и по левому краю
            this.Location = playerId == 1 ? new Point(resolution.Width - this.Width, 0) :
                                new Point(resolution.Width - this.Width, (int)(resolution.Height - this.Height));

            // при запуске формы расчитываем координаты сетки
            this.CalculationGameGrid();

            // рассчитываем координаты игрового поля (крестиков и ноликов)
            this.CalculateGameField();

            // установка метода обработки сообщений от канала
            this.pipeClient.ServerMessage += this.PipeClientOnServerMessage;
            this.pipeClient.Start(); // запуск канала

            // запуск потока обновления
            this.updateThread.Start();
        }

        /// <summary>
        /// Метод расчета координат линий сетки
        /// </summary>
        private void CalculationGameGrid()
        {
            // получаем ширину и высоту рабочей области формы
            var width = this.ClientRectangle.Width;
            var height = this.ClientRectangle.Height;

            // рассчитываем горизонтальные линии
            this.gridLines[0] = new Line(new PointF(0f, height / 5), new PointF(width, height / 5));
            this.gridLines[1] = new Line(new PointF(0f, 2 * height / 5), new PointF(width, 2 * height / 5));
            this.gridLines[2] = new Line(new PointF(0f, 3 * height / 5), new PointF(width, 3 * height / 5));
            this.gridLines[3] = new Line(new PointF(0f, 4 * height / 5), new PointF(width, 4 * height / 5));

            // рассчитываем вертикальные линии
            this.gridLines[4] = new Line(new PointF(width / 5, 0f), new PointF(width / 5, height));
            this.gridLines[5] = new Line(new PointF(2 * width / 5, 0f), new PointF(2 * width / 5, height));
            this.gridLines[6] = new Line(new PointF(3 * width / 5, 0f), new PointF(3 * width / 5, height));
            this.gridLines[7] = new Line(new PointF(4 * width / 5, 0f), new PointF(4 * width / 5, height));

            //  рассчитываем толщину линий сетки (первое что в голову пришло :D)
            this.gridPen.Width = this.crossPen.Width = this.noughtPen.Width = (width + height) / 200;
        }

        /// <summary>
        /// Инициализация игрового поля
        /// </summary>
        private void InitializeGameField()
        {
            // начальная инициализация игрового поля
            for (int i = 0; i < this.gameField.GetLength(0); i++)
            {
                for (int j = 0; j < this.gameField.GetLength(1); j++)
                {
                    this.gameField[i, j] = new GameField { TypeMove = TypeMove.None };
                }
            }

            // расчет координат игрового поля
            this.CalculateGameField();
        }

        /// <summary>
        /// Расчет игрового поля
        /// </summary>
        private void CalculateGameField()
        {
            // получаем ширину и высоту рабочей области формы
            var width = this.ClientRectangle.Width;
            var height = this.ClientRectangle.Height;

            // расчет координат ВСЕХ крестиков и ноликов
            for (int i = 0; i < this.gameField.GetLength(0); i++)
            {
                for (int j = 0; j < this.gameField.GetLength(1); j++)
                {
                    // рассчитываем крестик
                    this.gameField[i, j].Cross = new Cross(
                        new Line(
                            new PointF(j * width / 5, i * height / 5),
                            new PointF((j + 1) * width / 5, (i + 1) * height / 5)),
                        new Line(
                            new PointF((j + 1) * width / 5, i * height / 5),
                            new PointF(j * width / 5, (i + 1) * height / 5)));

                    // рассчитываем нолик
                    this.gameField[i, j].Nought = new Nought(
                        new RectangleF(new PointF(j * width / 5, i * height / 5), new SizeF(width / 5, height / 5)));
                }
            }
        }

        /// <summary>
        /// При изменении размеров формы
        /// </summary>
        private void Form1_Resize(object sender, EventArgs e)
        {
            this.CalculateGameField(); // расчитываем новое полежение крестиков/ноликов
            this.CalculationGameGrid(); // расчитываем координаты сетки
            this.Invalidate(); // перерисовка поля
        }

        /// <summary>
        /// Рисование
        /// </summary>
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // рисуем игровое поле
            foreach (var line in this.gridLines)
            {
                e.Graphics.DrawLine(this.gridPen, line.StartPoint, line.EndPoint);
            }

            // рисуем крестики/нолики
            foreach (var stroke in this.gameField)
            {
                switch (stroke.TypeMove)
                {
                    case TypeMove.Cross:
                        e.Graphics.DrawLine(
                            this.crossPen,
                            stroke.Cross.LeftLine.StartPoint,
                            stroke.Cross.LeftLine.EndPoint);

                        e.Graphics.DrawLine(
                            this.crossPen,
                            stroke.Cross.RightLine.StartPoint,
                            stroke.Cross.RightLine.EndPoint);
                        break;

                    case TypeMove.Nougth:
                        e.Graphics.DrawEllipse(this.noughtPen, stroke.Nought.Circle);
                        break;
                }
            }
        }

        /// <summary>
        /// клик миши на форме
        /// </summary>
        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            // левый клик
            if (e.Button == MouseButtons.Left)
                this.SetMove(e.Location);
        }

        /// <summary>
        /// По клику поиск и установка крестика/нолика
        /// </summary>
        private void SetMove(Point location)
        {
            bool find = false;

            for (int i = 0; i < this.gameField.GetLength(0); i++)
            {
                for (int j = 0; j < this.gameField.GetLength(1); j++)
                {
                    // если клик в определенном квадрате
                    if (location.X >= this.gameField[i, j].Cross.LeftLine.StartPoint.X
                        && location.X <= this.gameField[i, j].Cross.RightLine.StartPoint.X
                        && location.Y >= this.gameField[i, j].Cross.LeftLine.StartPoint.Y
                        && location.Y <= this.gameField[i, j].Cross.LeftLine.EndPoint.Y)
                    {
                        // отправляем на сервер для проверки хода
                        this.SendMoveToServer(new Point(i, j));
                        find = true;
                        break;
                    }
                }

                if (find)
                    break;
            }

            this.Invalidate();
        }

        /// <summary>
        /// Отправка координат движения на сервер
        /// </summary>
        private void SendMoveToServer(Point movePoint)
        {
            var moveStr = this.typeMove == TypeMove.Cross ? "крестик" : "нолик";

            this.pipeClient.PushMessage(
                new PipeMessage()
                    {
                        Message = $"Игрок {this.playerId} ходит [{movePoint.X}, {movePoint.Y}] {moveStr}",
                        MovePoint = movePoint,
                        TypeMove = this.typeMove
                    });
        }

        /// <summary>
        /// Обработка сообщений от сервера
        /// </summary>
        private void PipeClientOnServerMessage(
            NamedPipeConnection<PipeMessage, PipeMessage> connection,
            PipeMessage message)
        {
            // только обновить
            if (message.GetGameFieldOnly)
            {
                this.UpdateGameField(message.GameField);
                return;
            }

            switch (message.PipeMessageType)
            {
                case PipeMessageType.Allow:
                    this.UpdateGameField(message.GameField);
                    this.Invalidate();
                    break;

                case PipeMessageType.Alien:
                    MessageBox.Show(message.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;

                case PipeMessageType.WinNought:
                    MessageBox.Show(message.Message, @"Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case PipeMessageType.WinCross:
                    MessageBox.Show(message.Message, @"Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case PipeMessageType.Standoff:
                    MessageBox.Show(message.Message, @"Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
        }

        /// <summary>
        /// Обновление данных из сервера
        /// </summary>
        private void UpdateGameField(TypeMove[,] typeMoves)
        {
            for (int i = 0; i < this.gameField.GetLength(0); i++)
            {
                for (int j = 0; j < this.gameField.GetLength(1); j++)
                {
                    this.gameField[i, j].TypeMove = typeMoves[i, j];
                }
            }
        }

        /// <summary>
        /// При закрытии формы уничтожаем поток обновления
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.updateThread.IsAlive)
                this.updateThread.Abort();
        }
    }
}