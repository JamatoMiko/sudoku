using System.Diagnostics;

namespace sudoku;

//実際のゲーム画面
//矢印キーで移動
//数字キーで入力
//BackSpaceキーで消す
//Mキーでペンシルマーク切り替え
//Uキーで一つ戻す
//EnterキーでSolve
//Nキーで新しいゲームを開始
//Rキーでリセット

/*ToDo
実行環境に依存しないようにビルド、-outで出力場所を指定、出来たファイルを7zipで圧縮してGitHubにアップロード

コントロールを作るかどうかを分岐するんじゃなくて、Visibleを分岐させる

・ゲームクリア、難易度とタイムを表示
・ゲームオーバー、リスタート（オリジナルのボード、間違った回数リセット、タイマーリセット）、新規ゲームを開始する
・クリアとかゲームオーバーのパネルを表示している間は裏のコントロールを無効にする(Enabledプロパティ)、
　画面全体を一つのパネルにする、ボードを一つのパネルの上に描画する(座標を移動しやすく、クリックの判定を正確にする)、
　BringToFrontで最前面に表示
・Solveモードの実装、やり直しを簡単にする

色とかを何処かに保存、今はPageのフィールド

正誤判定、間違えれる回数に上限

_historyでペンシルマークも管理できるようにする
Undo機能、Board型のリスト?

SolverのQOL改善
リセットできるように
途中で修正できるように
_changeableを別で作る？
Solverのボタンは別にのところにする

問題を作成するゲームモード
・埋められた盤面を作成する
・出来た問題を解けるかチェックする
・難易度を判定する、絞り込み方法を順番に解放していって解けた段階の難易度を返す
・盤面を保存する

複数のゲームモード
・ランダム生成
・問題を入力してボタンを押すと解いてくれる、入力した数字は濃い色で表示、絞り込み方法を設定、ボタンを押したらGlue()呼び出し、boardのSolver呼び出し、solutionは破棄

入力し終えた数字のラベルを押せなくする?

入力し終えたブロック、行、列、盤面全体をアニメーション
*/

public class GamePage : Page
{
    GameMode _gameMode;
    Difficulty _difficulty;
    Board board, solution, original;
    System.Windows.Forms.Timer timer;
    List<(int Row, int Col, int LastNumber)> _history = new List<(int, int, int)>();
    bool[,] _isPlayerNumber = new bool[9, 9];
    (int Row, int Col) _cursor = (0, 0);
    public int CellSize {get; set;}
    public int ThickLineWidth {get; set;}
    public int ThinLineWidth {get; set;}
    int _boardSize;
    List<Keys> specialKeys = new List<Keys>{//入力を受け付ける特殊キーのリスト
        Keys.Left,
        Keys.Right,
        Keys.Up,
        Keys.Down
    };
    List<Keys> pressedKeys = new List<Keys>();//押されているキーのリスト
    bool[,,] _pencilMark = new bool[9, 9, 9];
    bool _isPencilMark;
    bool _isSolved;
    int _wrongCount;
    int _maxWrongCount;
    bool _autoPencilMark;
    RoundedRectButton[] numbers = new RoundedRectButton[9];
    RoundedRectButton solve, newGame, delete, unDo, pageBack, reset;
    ToggleSwitch pencilMarkSwitch;
    Label difficultyLabel, wrongCount, timeLabel;
    int _min, _sec;
    string time;
    public GamePage(GameMode gameMode = GameMode.Random, Difficulty difficulty = Difficulty.Medium)
    {
        this.Dock = DockStyle.Fill;
        this.DoubleBuffered = true;

        CellSize = 48;
        ThickLineWidth = 3;
        ThinLineWidth = 1;
        _boardSize = CellSize * 9 + ThickLineWidth * 4 + ThinLineWidth * 6;

        timer = new System.Windows.Forms.Timer(){
            Interval = 1000
        };
        timer.Tick += (sender, e) => {
            _sec++;
            if (_sec >= 60)
            {
                _sec = 0;
                _min++;
            }
            time = "";
            if (_min < 10)
            {
                time += $"0{_min}:";
            }
            else
            {
                time += $"{_min}:";
            }
            if (_sec < 10)
            {
                time += $"0{_sec}";
            }
            else
            {
                time += $"{_sec}";
            }
            Invalidate();
        };

        _gameMode = gameMode;
        _difficulty = difficulty;
        board = new Board();
        solution = new Board();

        _isPencilMark = false;
        _autoPencilMark = false;

        //数字ボタン
        for (int num = 1; num < 10; num++)
        {
            numbers[num - 1] = new RoundedRectButton()
            {
                Size = new Size(CellSize * 2, CellSize * 2),
                Location = new Point(_boardSize + ThickLineWidth * 6 + CellSize + Board.StartingCell(num - 1).Col / 3 * (CellSize * 2 + ThickLineWidth) + ThickLineWidth * 2, ThickLineWidth + CellSize + ThinLineWidth * 2 + Board.StartingCell(num - 1).Row / 3 * (CellSize * 2 + ThickLineWidth)),
                DefaultBackColor = pallet[3],
                HoverBackColor = pallet[9],
                BorderSize = 0,
                ForeColor = pallet[6],
                Font = new Font("Arial", CellSize / 3 * 2),
                Text = $"{num}"
            };
            numbers[num - 1].Click += (sender, e) =>//クリック
            {
                var button = (RoundedRectButton) sender;
                int num = int.Parse(button.Text);
                InputNumber(num);
            };
        }
        this.Controls.AddRange(numbers);

        //消去ボタン
        delete = new RoundedRectButton(){
            Location = new Point(_boardSize + ThickLineWidth * 6 + CellSize + 2 * (CellSize * 2 + ThickLineWidth) + ThickLineWidth * 2, ThickLineWidth + CellSize + ThinLineWidth * 2 +  3 * (CellSize * 2 + ThickLineWidth)),
            Size = new Size(CellSize * 2, CellSize * 2),
            DefaultBackColor = pallet[3],
            HoverBackColor = pallet[9],
            BorderSize = 0,
            ForeColor = pallet[6],
            Font = new Font("Arial", CellSize / 3 * 2),
            Text = "X"
        };
        delete.Click += (sender, e) =>{
            InputNumber();
        };
        this.Controls.Add(delete);

        //Undoボタン
        unDo = new RoundedRectButton(){
            Location = new Point(_boardSize + ThickLineWidth * 6 + CellSize + 0 * (CellSize * 2 + ThickLineWidth) + ThickLineWidth * 2, ThickLineWidth + CellSize + ThinLineWidth * 2 +  3 * (CellSize * 2 + ThickLineWidth)),
            Size = new Size(CellSize * 2, CellSize * 2),
            DefaultBackColor = pallet[3],
            HoverBackColor = pallet[9],
            BorderSize = 0,
            ForeColor = pallet[6],
            Font = new Font("Arial", CellSize / 3 * 2),
            Text = "←"
        };
        unDo.Click += (sender, e) =>{
            Undo();
        };
        this.Controls.Add(unDo);

        //ページバックボタン
        pageBack = new RoundedRectButton(){
            Location = new Point(ThickLineWidth, ThickLineWidth),
            Size = new Size(CellSize * 2, CellSize),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial", CellSize / 4),
            Text = "< Back"
        };
        pageBack.Click += (sender, e) =>{
            if (_gameMode == GameMode.Random)
            {
                ChangePage(new DifficultySelectPage());
            }
            else if (_gameMode == GameMode.Solver)
            {
                ChangePage(new MainPage());
            }
        };
        this.Controls.Add(pageBack);

        //ペンシルマークスイッチ
        pencilMarkSwitch = new ToggleSwitch()
        {
            Location = new Point(_boardSize + ThickLineWidth * 7 + CellSize * 5 + CellSize / 3 + ThickLineWidth * 2, ThickLineWidth + CellSize / 6),
            Size = new Size(CellSize  * 2 / 3 * 2, CellSize / 3 * 2),
            OffColor = Color.FromArgb(0xAD, 0xB6, 0xC2),
            OnColor = Color.FromArgb(0x32, 0x5A, 0xAF),
            IsOn = _isPencilMark
        };
        pencilMarkSwitch.MouseClick += (sender, e) =>
        {
            _isPencilMark = !_isPencilMark;
            Console.WriteLine($"PencilMark : {_isPencilMark}");
        };
        this.Controls.Add(pencilMarkSwitch);

        //難易度
        if (_gameMode == GameMode.Random)
        {
        difficultyLabel = new Label(){
                Size = new Size(CellSize * 2 + ThinLineWidth * 2, CellSize),
                Location = new Point(ThickLineWidth * 2 + CellSize * 3 + ThinLineWidth * 2 + CellSize / 2 + ThickLineWidth * 2, ThickLineWidth),
                BackColor = Color.Transparent,
                ForeColor = pallet[2],
                Font = new Font("Arial", CellSize / 4),
                Text = difficulty switch
                {
                    Difficulty.VeryEasy => "Very Easy",
                    Difficulty.Easy => "Easy",
                    Difficulty.Medium => "Medium",
                    Difficulty.Hard => "Hard",
                    _ => ""
                },
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(difficultyLabel);
        }

        //誤答回数
        if (_gameMode == GameMode.Random)
        {
            wrongCount = new Label(){
                Size = new Size(CellSize * 2, CellSize),
                Location = new Point(_boardSize + ThickLineWidth * 6 + CellSize + ThickLineWidth * 2, ThickLineWidth),
                BackColor = Color.Transparent,
                ForeColor = pallet[2],
                Font = new Font("Arial", CellSize / 4),
                Text = $"{_wrongCount} / {_maxWrongCount}",
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(wrongCount);
        }

        //時間
        if (_gameMode == GameMode.Random)
        {
            timeLabel = new Label(){
                Size = new Size(CellSize * 2 + ThinLineWidth * 2, CellSize),
                Location = new Point(_boardSize + ThickLineWidth * 6 + CellSize + ThickLineWidth * 2 + CellSize * 2 + ThickLineWidth, ThickLineWidth),
                BackColor = Color.Transparent,
                ForeColor = pallet[2],
                Font = new Font("Arial", CellSize / 4),
                Text = $"00:00",
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(timeLabel);
        }

        //リセットボタン
        reset = new RoundedRectButton(){
            Size = new Size(CellSize * 2 , CellSize),
            Location = new Point(_boardSize + ThickLineWidth * 6 + CellSize  + ThickLineWidth * 2, ThickLineWidth + CellSize + ThinLineWidth  * 2 + 4 * (CellSize * 2 + ThickLineWidth) + ThickLineWidth),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial",  CellSize / 4),
            Text = "Reset"
        };
        reset.Click += (sender, e) => {
                Reset();
        };
        this.Controls.Add(reset);

        //新規ゲームボタン
        newGame = new RoundedRectButton(){
            Size = new Size(CellSize * 2, CellSize),
            Location = new Point(_boardSize + ThickLineWidth * 6 + CellSize + CellSize * 2 + ThickLineWidth + ThickLineWidth * 2, ThickLineWidth + CellSize + ThinLineWidth  * 2 + 4 * (CellSize * 2 + ThickLineWidth) + ThickLineWidth),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial",  CellSize / 4),
            Text = "New Game"
        };
        newGame.Click += (sender, e) => {
            NewGame();
        };
        this.Controls.Add(newGame);

        //Solveボタン
        if (_gameMode == GameMode.Solver)
        {
            solve = new RoundedRectButton(){
                Size = new Size(CellSize * 2, CellSize),
                Location = new Point(_boardSize + ThickLineWidth * 6 + CellSize + (CellSize * 2 + ThickLineWidth) * 2 + ThickLineWidth * 2, ThickLineWidth + CellSize + ThinLineWidth * 2 + 4 * (CellSize * 2 + ThickLineWidth) + ThickLineWidth),
                DefaultBackColor = pallet[6],
                HoverBackColor = pallet[10],
                BorderSize = 0,
                ForeColor = pallet[0],
                Font = new Font("Arial",  CellSize / 4),
                Text = "Solve!"
            };
            solve.Click += (sender, e) => {
                Solve();
            };
            this.Controls.Add(solve);
        }
        NewGame();
    }
    protected override void OnMouseClick(MouseEventArgs e)//マウスクリックしたときのイベント
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left)
        {
            var row = (e.Y - (CellSize + ThickLineWidth * 4)) / (CellSize + ThinLineWidth);
            var col = (e.X - ThickLineWidth * 4) / (CellSize + ThinLineWidth);
            if (row >= 0 && row < 9 && col >= 0 && col < 9)
            {
                _cursor = (row, col);
            }
        }
        this.Invalidate();
    }
    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)//キー入力のプレビュー
    {
        base.OnPreviewKeyDown(e);
        var key = e.KeyCode;
        if (specialKeys.Contains(key))//受け付ける特殊キーのリストに含まれている場合、入力されたキーとして認識する
        {
            e.IsInputKey = true;
        }
    }
    protected override void OnKeyDown(KeyEventArgs e)//キー入力したときのイベント
    {
        base.OnKeyDown(e);
        var key = e.KeyCode;
        if (!pressedKeys.Contains(key))//キーが押された瞬間だけ実行する
        {
            pressedKeys.Add(key);
            if (key >= Keys.D0 && key <= Keys.D9)//数字キー
            {
                int num = key - Keys.D0;//押されたキーのキーコードから「0」のキーコードを引いた数が押された数になる
                InputNumber(num);
            }
            switch (key)
            {
                case Keys.Left:
                    if (_cursor.Col > 0)
                    {
                        _cursor.Col--;
                    }
                    break;
                case Keys.Right:
                    if (_cursor.Col < 8)
                    {
                        _cursor.Col++;
                    }
                    break;
                case Keys.Up:
                    if (_cursor.Row > 0)
                    {
                        _cursor.Row--;
                    }
                    break;
                case Keys.Down:
                    if (_cursor.Row < 8)
                    {
                        _cursor.Row++;
                    }
                    break;
                case Keys.Back:
                    InputNumber();
                    break;
                case Keys.M:
                    _isPencilMark = !_isPencilMark;
                    pencilMarkSwitch.IsOn = !pencilMarkSwitch.IsOn;
                    Console.WriteLine($"PencilMark : {_isPencilMark}");
                    break;
                case Keys.U:
                    Undo();
                    break;
                case Keys.Enter:
                    if (_gameMode == GameMode.Solver)
                    {
                        Solve();
                    }
                    break;
                case Keys.N:
                    NewGame();
                    break;
                case Keys.R:
                    Reset();
                    break;
            }
            this.Invalidate();
        }
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        var key = e.KeyCode;
        if (pressedKeys.Contains(key))
        {
            pressedKeys.RemoveAll(n => n == key);
        }
    }
    private void NewGame()//新規ゲームを開始する
    {
        timer.Stop();
        switch (_gameMode)
        {
            case GameMode.Random:
                board.Generator(_difficulty, out solution);
                break;
            case GameMode.Solver:
                board.InitializeBoard();
                break;
        }
        original = new Board(board);//オリジナルのボードを作成
        for (int row = 0; row < 9; row++)//空いているマスをプレイヤーが入力した数字とする
        {
            for (int col = 0; col < 9; col++)
            {
                if (board.GetCell(row, col) != 0)
                {
                    _isPlayerNumber[row, col] = false;
                }
                else
                {
                    _isPlayerNumber[row, col] = true;
                }
            }
        }
        _cursor = (0, 0);
        _wrongCount = 0;
        _maxWrongCount = 3;
        _isSolved = false;
        _min = 0;
        _sec = 0;
        timer.Start();
        reset.Visible = true;
        this.Invalidate();
    }
    private void Reset()
    {
        board = new Board(original);//オリジナルのボードをコピー
        for (int row = 0; row < 9; row++)//空いているマスをプレイヤーが入力した数字とする
        {
            for (int col = 0; col < 9; col++)
            {
                if (board.GetCell(row, col) != 0)
                {
                    _isPlayerNumber[row, col] = false;
                }
                else
                {
                    _isPlayerNumber[row, col] = true;
                }
            }
        }
        _history = new List<(int Row, int Col, int LastNumber)>();//履歴を消去
        _cursor = (0, 0);
        this.Invalidate();
    }
    private void InputNumber(int num = 0)//カーソルの位置に数字を入力する
    {
        var row = _cursor.Row;
        var col = _cursor.Col;
        var lastNumber = board.GetCell(row, col);
        if (!_isPencilMark)
        {
            if (_isPlayerNumber[row, col])
            {
                if (lastNumber == num)//同じ数字だったら消去する
                {
                    board.SetCell(row, col);
                }
                else//違う数字だったら入力して履歴に残す
                {
                    _history.Add((row, col, lastNumber));
                    board.SetCell(row, col, num);
                    if (_gameMode == GameMode.Random)
                    {
                        //正誤判定
                        if (num != 0 && num != solution.GetCell(row, col))
                        {
                            _wrongCount++;
                            if (_wrongCount > _maxWrongCount)
                            {
                                timer.Stop();
                                reset.Visible = false;
                            }
                        }
                        if (board.CompareBoard(solution, out _))
                        {
                            timer.Stop();
                            reset.Visible = false;
                        }
                    }
                    //入力したマスのペンシルマークを空にする
                    for (int i = 0; i < 9; i++)
                    {
                        _pencilMark[_cursor.Row, _cursor.Col, i] = false;
                    }
                    //同じブロック、行、列のペンシルマークから入力した数字を取り除く
                    if (num != 0)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            for (int j = 0; j < 9; j++)
                            {
                                if (Board.IdentifyBlock(i, j) == Board.IdentifyBlock(row, col) || i == row || j == col)
                                {
                                    _pencilMark[i, j, num - 1] = false;
                                }
                            }
                        }
                    }
                }
            }
        }
        else//ペンシルマーク
        {
            if (board.GetCell(row, col) == 0)
            {
                if (num != 0)
                {
                    _pencilMark[row, col, num - 1] = !_pencilMark[row, col, num - 1];
                }
                else
                {
                    //ペンシルマークを取り除く
                    for (int i = 0; i < 9; i++)
                    {
                        _pencilMark[_cursor.Row, _cursor.Col, i] = false;
                    }
                }
            }
        }
        this.Invalidate();
    }
    private void Undo()//ひとつ前に戻す
    {
        if (_history.Count > 0)
        {
            var targetIndex = _history.Count - 1;
            _cursor = (_history[targetIndex].Row, _history[targetIndex].Col);
            board.SetCell(_history[targetIndex].Row, _history[targetIndex].Col, _history[targetIndex].LastNumber);
            _history.RemoveAt(targetIndex);
            Invalidate();
        }
    }
    private void Solve()
    {
        if (!_isSolved)
        {
            for (int row = 0; row < 9; row++)//空いているマスをコンピューターが入力する数字とする
            {
                for (int col = 0; col < 9; col++)
                {
                    if (board.GetCell(row, col) == 0)
                    {
                        _isPlayerNumber[row, col] = false;
                    }
                    else
                    {
                        _isPlayerNumber[row, col] = true;
                    }
                }
            }
            _isSolved = board.Solver();
            this.Invalidate();
        }
    }
    protected override void OnPaint(PaintEventArgs e)//描画処理
    {
        base.OnPaint(e);

        if (_autoPencilMark)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    for (int num = 1; num < 10; num++)
                    {
                        if (board._pencilMark[i, j].Contains(num))
                        {
                            _pencilMark[i, j, num - 1] = true;
                        }
                        else
                        {
                            _pencilMark[i, j, num - 1] = false;
                        }
                    }
                }
            }
        }

        if (wrongCount != null)
        {
            if (_wrongCount > _maxWrongCount)
            {
                wrongCount.ForeColor = pallet[8];
            }
            else
            {
                wrongCount.ForeColor = pallet[2];
            }
            wrongCount.Text = $"{_wrongCount} / {_maxWrongCount}";
        }
        if (timeLabel != null)
        {
            timeLabel.Text = $"{time}";
        }
        var g = e.Graphics;
        //グリッド(太)
        g.FillRectangle(new SolidBrush(pallet[2]), ThickLineWidth * 2, CellSize + ThickLineWidth * 2, _boardSize, _boardSize);
        for (int block = 0; block < 9; block++)
        {
            var row = Board.StartingCell(block).Row;
            var col = Board.StartingCell(block).Col;
            //グリッド(細)
            g.FillRectangle(new SolidBrush(pallet[1]), (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * col / 3 + ThickLineWidth + ThickLineWidth * 2, (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * row / 3 + ThickLineWidth + CellSize + ThickLineWidth * 2, CellSize * 3 + ThinLineWidth * 2, CellSize * 3 + ThinLineWidth * 2);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var color = pallet[0];
                    var num = board.GetCell(row + i, col + j);
                    //セル
                    if (block == Board.IdentifyBlock(_cursor.Row, _cursor.Col) || row + i == _cursor.Row || col + j == _cursor.Col)//カーソルと同じブロック、行、列
                    {
                        color = pallet[3];
                    }
                    if (num != 0 && num == board.GetCell(_cursor.Row, _cursor.Col))//カーソルと同じ数字
                    {
                        color = pallet[4];
                    }
                    if (row + i == _cursor.Row && col + j == _cursor.Col)//カーソル
                    {
                        color = pallet[5];
                    }
                    g.FillRectangle(new SolidBrush(color), (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * col / 3 + ThickLineWidth + (CellSize + ThinLineWidth) * j + ThickLineWidth * 2, (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * row / 3 + ThickLineWidth + (CellSize + ThinLineWidth) * i + CellSize + ThickLineWidth * 2, CellSize, CellSize);
                    if (num != 0)
                    {
                        if (_gameMode == GameMode.Random)
                        {
                            if (_isPlayerNumber[row + i, col + j])//プレイヤーが入力した数字
                            {
                                if (board.GetCell(row + i, col + j) == solution.GetCell(row + i, col + j))
                                {
                                    color = pallet[6];
                                }
                                else//間違っている数字
                                {
                                    color = pallet[8];
                                }
                            }
                            else//問題の数字
                            {
                                color = pallet[2];
                            }
                        }
                        else if (_gameMode == GameMode.Solver)
                        {
                            if (_isPlayerNumber[row + i, col + j])//プレイヤーが入力した数字＝問題の数字
                            {
                                color = pallet[2];
                                for (int m = 0; m < 9; m++)
                                {
                                    for (int n = 0; n < 9; n++)
                                    {
                                        if (m != row + i || n != col + j)
                                        {
                                        if (board.GetCell(m, n) == num)
                                        {
                                            if (Board.IdentifyBlock(m, n) == Board.IdentifyBlock(row + i, col + j) || m == row + i || n == col + j)
                                            {
                                                color = pallet[8];//重複している数字
                                            }
                                        }
                                        }
                                    }
                                }
                            }
                            else//コンピューターが入力した数字
                            {
                                color = pallet[6];
                            }
                        }
                        g.DrawString($"{num}", new Font("Arial", CellSize / 3 * 2), new SolidBrush(color), (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * col / 3 + ThickLineWidth + (CellSize + ThinLineWidth) * j + CellSize / 9 + ThickLineWidth * 2, (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * row / 3 + ThickLineWidth + (CellSize + ThinLineWidth) * i + CellSize + ThickLineWidth * 2);
                    }
                    else//ペンシルマーク
                    {
                        for (int m = 0; m < 9; m++)
                        {
                            if (_pencilMark[row + i, col + j, m])
                            {
                                g.DrawString($"{m + 1}", new Font("Arial", CellSize / 5), new SolidBrush(pallet[7]), (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * col / 3 + ThickLineWidth + (CellSize + ThinLineWidth) * j + CellSize / 3 * Board.StartingCell(m).Col / 3 + CellSize / 16 + ThickLineWidth * 2, (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * row / 3 + ThickLineWidth + (CellSize + ThinLineWidth) * i + CellSize / 3 * Board.StartingCell(m).Row / 3 + CellSize + ThickLineWidth * 2);
                            }
                        }
                    }
                }
            }
        }
        //数字カウント
        //グリッド(太)
        g.FillRectangle(new SolidBrush(pallet[2]), _boardSize + ThickLineWidth + ThickLineWidth * 2, CellSize + ThickLineWidth * 2, ThickLineWidth * 2 + CellSize, _boardSize);
        //グリッド(細)
        g.FillRectangle(new SolidBrush(pallet[1]), _boardSize + ThickLineWidth * 2 + ThickLineWidth * 2, CellSize + ThickLineWidth * 2 + ThickLineWidth, CellSize, _boardSize - ThickLineWidth * 2);
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var color = pallet[0];
                //マス
                if (i * 3 + j + 1 == board.GetCell(_cursor.Row, _cursor.Col))
                {
                    color = pallet[5];
                }
                g.FillRectangle(new SolidBrush(color), _boardSize + ThickLineWidth * 2 + ThickLineWidth * 2, ThickLineWidth + (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * i + (CellSize + ThinLineWidth) * j + CellSize + ThickLineWidth * 2, CellSize, CellSize);
                //カウント
                g.DrawString($"{Math.Max(9 - board._numberCount[i * 3 + j], 0)}", new Font("Arial", CellSize / 3 * 2), new SolidBrush(pallet[2]), _boardSize + ThickLineWidth * 2 + CellSize / 9 + ThickLineWidth * 2, ThickLineWidth + (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * i + (CellSize + ThinLineWidth) * j + CellSize + ThickLineWidth * 2);
                //数字
                g.DrawString($"{i * 3 + j + 1}", new Font("Arial", CellSize / 5), new SolidBrush(pallet[7]), _boardSize + ThickLineWidth * 2 + CellSize / 16 + ThickLineWidth * 2, ThickLineWidth + (ThickLineWidth + CellSize * 3 + ThinLineWidth * 2) * i + (CellSize + ThinLineWidth) * j + CellSize + ThickLineWidth * 2);
            }
        }
    }
}