using System.Diagnostics;

namespace sudoku;

/*ToDo
マウス操作
数字の入力をメソッド化、キー入力とマウス入力どちらからも呼び出せるように
入力し終わった数字を押せなくする?やり直せなくなる、カウントを表示するだけにする、9を超えたら赤くする

正誤判定

Undo機能、Board型のリスト?
*/

//実際のゲーム画面

//矢印キーで移動
//数字キーで入力
//BackSpaceキーで消す
//Mキーでペンシルマーク切り替え

public partial class GamePage : UserControl
{
    Board board;
    Board solution;
    Font font;
    Pen pen;
    Brush brush;
    (int Row, int Col) _cursor = (0, 0);
    bool[,,] _pencilMark = new bool[9, 9, 9];//[row, col, num]
    bool _isPencilMark;
    bool _autoPencilMark = false;//ペンシルマークの自動記入
    int _cellSize;
    int _lineWidth;
    int _fontSize;
    List<Keys> _specialKey;
    public GamePage(Difficulty difficulty = Difficulty.Medium)
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;
        this.DoubleBuffered = true;

        _cellSize = 48;
        _lineWidth = 3;
        _fontSize =  _cellSize - _cellSize / 3;

        _specialKey = new List<Keys>{//受け付ける特殊キーのリスト
            Keys.Left,
            Keys.Right,
            Keys.Up,
            Keys.Down,
            //Keys.Back
        };

        board = new Board();
        board.Generator(difficulty);
        solution = new Board(board);
        solution.Solver();
    }
    protected override void OnMouseClick(MouseEventArgs e)//マウスクリックイベント
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left)//左クリック
        {
            int row = e.Y / (_cellSize + _lineWidth);
            int col = e.X / (_cellSize + _lineWidth);
            if (row >= 0 && row < 9 && col >= 0 && col < 9)
            {
                _cursor = (row, col);
            }
        }
        this.Invalidate();
    }
    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        var key = e.KeyCode;
        if (_specialKey.Contains(key))//受け付ける特殊キーのリストに含まれている場合、入力されたキーとして認識する
        {
            e.IsInputKey = true;
        }
    }
    protected override void OnKeyDown(KeyEventArgs e)//キー入力イベント
    {
        base.OnKeyDown(e);
        var key = e.KeyCode;
        if (key >= Keys.D0 && key <= Keys.D9)//数字キー
        {
            int num = key - Keys.D0;//押されたキーのキーコードから「0」のキーコードを引いた数が押された数になる
            if (_isPencilMark == false)
            {
                if (board._changeable[_cursor.Row, _cursor.Col])
                {
                    if (board.GetCell(_cursor.Row, _cursor.Col) == num)//同じ数字が押された場合空にする
                    {
                        board.SetCell(_cursor.Row, _cursor.Col);
                    }
                    else
                    {
                        board.SetCell(_cursor.Row, _cursor.Col, num);
                    }
                    for (int i = 0; i < 9; i++)//ペンシルマークを空にする
                    {
                        _pencilMark[_cursor.Row, _cursor.Col, i] = false;
                    }
                    //同じブロック、行、列のペンシルマークから入力した数字を取り除く
                    if (num > 0)
                    {
                        int row = Board.StartingCell(Board.IdentifyBlock(_cursor.Row, _cursor.Col)).Row;
                        int col = Board.StartingCell(Board.IdentifyBlock(_cursor.Row, _cursor.Col)).Col;
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                if (_pencilMark[row + i, col + j, num - 1] == true)
                                {
                                    _pencilMark[row + i, col + j, num - 1] = false;
                                }
                            }
                        }
                        for (int i = 0; i < 9; i++)
                        {
                            if (_pencilMark[_cursor.Row, i, num - 1] == true)
                            {
                                _pencilMark[_cursor.Row, i, num - 1] = false;
                            }
                        }
                        for (int i = 0; i < 9; i++)
                        {
                            if (_pencilMark[i, _cursor.Col, num - 1] == true)
                            {
                                _pencilMark[i, _cursor.Col, num - 1] = false;
                            }
                        }
                    }
                }
            }
            else
            {
                if (board.GetCell(_cursor.Row, _cursor.Col) == 0)
                {
                    if (_pencilMark[_cursor.Row, _cursor.Col, num - 1] == true)
                    {
                        _pencilMark[_cursor.Row, _cursor.Col, num - 1] = false;
                    }
                    else
                    {
                        _pencilMark[_cursor.Row, _cursor.Col, num - 1] = true;
                    }
                }
            }
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
            case Keys.Back://BackSpaceキー
                if (_isPencilMark == false)
                {
                    if (board._changeable[_cursor.Row, _cursor.Col])
                    {
                        board.SetCell(_cursor.Row, _cursor.Col);
                    }
                }
                else
                {
                    for (int i = 0; i < 9; i++)//ペンシルマークを空にする
                    {
                        _pencilMark[_cursor.Row, _cursor.Col, i] = false;
                    }
                }
                break;
            case Keys.M:
                if (_isPencilMark == true)
                {
                    _isPencilMark = false;
                    Debug.WriteLine("PencilMark : OFF");
                }
                else
                {
                    _isPencilMark = true;
                    Debug.WriteLine("PencilMark : ON");
                }
                break;

        }
        this.Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)//描画イベント
    {
        base.OnPaint(e);

        //Debug.WriteLine($"({_cursor.Row}, {_cursor.Col})");

        if (_autoPencilMark)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    for (int num = 1; num < 10; num++)
                    {
                        if (board._pencilMark[row, col].Contains(num))
                        {
                            _pencilMark[row, col, num - 1] = true;
                        }
                    }
                }
            }
        }

        //ハイライト
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var cell = new Rectangle(col * (_cellSize + _lineWidth) + 1, row * (_cellSize + _lineWidth) + 1, _cellSize + _lineWidth, _cellSize + _lineWidth);
                //カーソルと同じブロック、行、ブロック
                if (Board.IdentifyBlock(row, col) == Board.IdentifyBlock(_cursor.Row, _cursor.Col) || row == _cursor.Row || col == _cursor.Col)
                {
                    brush = new SolidBrush(Color.FromArgb(0xE2, 0xEB, 0xF3));
                    e.Graphics.FillRectangle(brush, cell);
                }
                //カーソルと同じ数字
                if (board.GetCell(row, col) > 0 && board.GetCell(row, col) == board.GetCell(_cursor.Row, _cursor.Col))
                {
                    brush = new SolidBrush(Color.FromArgb(0xC3, 0xD7, 0xEA));
                    e.Graphics.FillRectangle(brush, cell);
                }
                //カーソル
                if (row == _cursor.Row && col == _cursor.Col)
                {
                    brush = new SolidBrush(Color.FromArgb(0xBB, 0xDE, 0xFB));
                    e.Graphics.FillRectangle(brush, cell);
                }
            }
        }


        //グリッド
        pen = new Pen(Color.FromArgb(0xBF, 0xC6, 0xD4), _lineWidth/2);//細
        /*
        for (int i = 0; i < 28; i++)
        {
            e.Graphics.DrawLine(pen, i * (_cellSize + _lineWidth) / 3 + 1, 0, i * (_cellSize + _lineWidth) / 3 + 1, 9 * (_cellSize + _lineWidth) + 1);
            e.Graphics.DrawLine(pen, 0, i * (_cellSize + _lineWidth) / 3 + 1, 9 * (_cellSize + _lineWidth) + 1, i * (_cellSize + _lineWidth) / 3 + 1);
        }
        */
        for (int i = 0; i < 10; i++)
        {
            e.Graphics.DrawLine(pen, i * (_cellSize + _lineWidth) + 1, 0, i * (_cellSize + _lineWidth) + 1, 9 * (_cellSize + _lineWidth) + 1);
            e.Graphics.DrawLine(pen, 0, i * (_cellSize + _lineWidth) + 1, 9 * (_cellSize + _lineWidth) + 1, i * (_cellSize + _lineWidth) + 1);
        }
        pen = new Pen(Color.FromArgb(0x34, 0x48, 0x61), _lineWidth);//太
        for (int i = 0; i < 10; i += 3)
        {
            e.Graphics.DrawLine(pen, i * (_cellSize + _lineWidth) + 1, 0, i * (_cellSize + _lineWidth) + 1, 9 * (_cellSize + _lineWidth) + 1);
            e.Graphics.DrawLine(pen, 0, i * (_cellSize + _lineWidth) + 1, 9 * (_cellSize + _lineWidth) + 1, i * (_cellSize + _lineWidth) + 1);
        }

        //数字
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (board.GetCell(row, col) == 0)//空
                {
                    //ペンシルマーク
                    font = new Font("Arial", _fontSize / 3);
                    brush = new SolidBrush(Color.FromArgb(0x6E, 0x7C, 0x8C));
                    for (int num = 1; num < 10; num++)
                    {
                        if (_pencilMark[row, col, num - 1] == true)
                        {
                            e.Graphics.DrawString($"{num}", font, brush, col * (_cellSize + _lineWidth) + 1 + (Board.StartingCell(num - 1).Col / 3) * (_cellSize / 3 + 1)  + (_cellSize / 3 - _fontSize / 3) / 3, row * (_cellSize + _lineWidth) + 1 + (Board.StartingCell(num - 1).Row / 3) * (_cellSize / 3 + 1)  + (_cellSize / 3 - _fontSize / 3) / 4);
                        }
                    }
                }
                else
                {
                    font = new Font("Arial", _fontSize);
                    if (board._changeable[row, col] == false)//変更不可の数字
                    {
                        brush = new SolidBrush(Color.FromArgb(0x34, 0x48, 0x61));
                        e.Graphics.DrawString($"{board.GetCell(row, col)}", font, brush, col * (_cellSize + _lineWidth) + 1 + (_cellSize - _fontSize) / 3, row * (_cellSize + _lineWidth) + 1 + (_cellSize - _fontSize) / 4);
                    }
                    else//入力した数字
                    {
                        if (board.GetCell(row, col) != solution.GetCell(row, col))//間違っている数字
                        {
                            brush = new SolidBrush(Color.FromArgb(0xE5, 0x5C, 0x6C));
                        }
                        else{//合っている数字
                            brush = new SolidBrush(Color.FromArgb(0x32, 0x5A, 0xAF));
                        }
                        e.Graphics.DrawString($"{board.GetCell(row, col)}", font, brush, col * (_cellSize + _lineWidth) + 1 + (_cellSize - _fontSize) / 3, row * (_cellSize + _lineWidth) + 1 + (_cellSize - _fontSize) / 4);
                    }
                }
            }
        }
    }
}