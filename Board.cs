using System;
using System.Diagnostics;
using System.Linq;

namespace sudoku;

//LINQのIntersect()で積集合(共通部分)を取得できる

/*ToDo
最終手段の総当たり、候補が少ないマスからやる、矛盾が生じたら(候補がないマスが出来たら)やり直し

難易度による問題作成
・使う解法
・空ける穴の数
・候補の数のバランス
Done空ける穴の抽選を埋まっているマスが多いブロックや行、列を優先する
使う解法を柔軟に指定した解法を少なくとも1回は用いるように
候補の絞り込みが成功したらその回数をカウントするようにする
solutionの配列を返すようにする？

今のままだとVery Easyが難しい
別の難易度の評価基準を設ける
候補の数の平均を少なくする？、候補が一つのマスを多くする？
そもそもCRBE法を実装する？
CRBE法はすでに最初の候補を調べるときに使っている方法
実際にはわかっている数字が多いものに着目してその数字が入るマスが一つに絞られたら埋めていく
やっぱり候補の数字と候補のマスのバランスを考える
Very Easyは単一候補法だけで解けるようにするので、候補の数を少なくするように空けていく
現段階でも単一候補法だけで解けるけど、一個ずつ埋めていかないといけないから難しい
同時にいくつもマスを埋めれるようにする

同時に穴を空けられるようになったけれど
今のままだと、組み合わせで解けないだけのマスが試行済みにカウントされてしまう

ペンシルマークの絞り込みをメソッドにして、絞り込みに使用した解法の回数を返すようにする、Solverで絞り込みを起動するたびに結果を加算していく、Generatorで合計の回数を受け取って評価する

ランダムに穴を開けていく
できた盤面の難易度を評価
難易度が低すぎたり難しすぎる場合はやり直す

JSONファイル
id =
board =
solution =

共有候補法が強力っぽい

cellCountではなくList<>.Countを使う

ファイルでデータを管理
Board 盤面
Solution 解

ペンシルマークの絞り込み
done双子法
　Done?独立双子法
　Done?居候双子法

三つ子法
　独立三つ子法
　居候三つ子法

done共有候補法
　Done?ブロックから行
　Done?ブロックから列
　Done?行からブロック
　Done?列からブロック

done対角線法
*/

public class Board
{
    private int[,] _board = {//[row, col]
        {0, 9, 0,  5, 0, 0,  6, 0, 0},
        {0, 0, 4,  7, 0, 0,  0, 1, 0},
        {0, 0, 0,  0, 0, 0,  0, 0, 0},

        {4, 0, 0,  2, 0, 7,  9, 0, 0},
        {0, 0, 1,  6, 0, 0,  7, 0, 0},
        {0, 0, 3,  0, 0, 0,  2, 0, 0},

        {3, 0, 0,  0, 5, 0,  0, 6, 0},
        {0, 0, 5,  4, 0, 0,  0, 8, 0},
        {0, 0, 0,  0, 2, 3,  0, 0, 0},
    };
    public List<int>[,] _pencilMark = new List<int>[9, 9];//候補のリスト、ペンシルマーク
    public List<int>[] _block = new List<int>[9];//ブロックごとのリスト
    public List<int>[] _row = new List<int>[9];//行ごとのリスト
    public List<int>[] _col = new List<int>[9];//列ごとのリスト
    public int[] _numberCount = new int[9];//数字ごとの個数
    public bool _uniqueCandidate = true;//単一候補法
    public bool _uniqueCell = true;//単一マス法
    public bool _twins = true;//双子法
    public bool _triplets = true;//三つ子法
    public bool _commonCandidate = true;//共有候補法
    public bool _diagonalLine = true;//対角線法

    Random random = new Random();
    //コンストラクタ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public Board(Board board) : this()//引数にBoard型を渡すとそれをコピー
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                SetCell(row, col, board.GetCell(row, col));
                //_uniqueCandidate = board._uniqueCandidate;
                //_uniqueCell = board._uniqueCell;
                //_twins = board._twins;
                //_triplets = board._triplets;
                //_commonCandidate = board._commonCandidate;
                //_diagonalLine = board._diagonalLine;
            }
        }
    }
    public Board()
    {
        for (int i = 0; i < 9; i++)//各要素を初期化
        {
            _block[i] = new List<int>();
            _row[i] = new List<int>();
            _col[i] = new List<int>();
            for (int j = 0; j < 9; j++)
            {
                _pencilMark[i, j] = new List<int>();
            }
        }
        UpdateInformation();
    }
    //メソッド////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public int GetCell(int row, int col)//_boardから数字を取得
    {
        return _board[row, col];
    }
    public void SetCell(int row, int col, int num = 0)//_boardに数字を入力
    {
        _board[row, col] = num;
        UpdateInformation();//情報を更新
    }
    public void InitializeBoard()//盤面を全て空にする
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                SetCell(row, col, 0);
            }
        }
    }
    public void Generator(Difficulty difficulty, out Board solution)//指定された難易度の問題を作成し、Board型の解を返す
    {
        switch (difficulty)
        {
            case Difficulty.VeryEasy://単一候補法だけで解ける
                _uniqueCandidate = true;
                _uniqueCell = false;
                _twins = false;
                _triplets = false;
                _commonCandidate = false;
                _diagonalLine = false;
                break;
            case Difficulty.Easy://単一候補法と単一マス法だけで解ける、候補の数が少ない
                _uniqueCandidate = true;
                _uniqueCell = true;
                _twins = false;
                _triplets = false;
                _commonCandidate = false;
                _diagonalLine = false;
                break;
            case Difficulty.Medium://単一候補法と単一マス法だけで解ける、候補の数が多い
                _uniqueCandidate = true;
                _uniqueCell = true;
                _twins = false;
                _triplets = false;
                _commonCandidate = false;
                _diagonalLine = false;
                break;
            case Difficulty.Hard://未定
                _uniqueCandidate = true;
                _uniqueCell = true;
                _twins = true;
                _triplets = true;
                _commonCandidate = true;
                _diagonalLine = true;
                break;
        }
        //埋められた盤面を作成
        GenerateCompletedBoard();
        solution = new Board(this);
        //1.盤面を保存
        //2.1マス空けてSolverを起動
        //3.解けなかった場合保存てあった盤面に戻して2に戻る
        //4.解けた場合1に戻る
        int blank = 0;
        int maxBlank = 80;//後で変更、難易度による
        int tryNum = 1;//同時に空ける穴の数、後で変更、ランダムにする?
        var preBoard = new int[9, 9];
        while (blank < maxBlank)
        {
            //盤面を保存
            for (int i = 0; i < 9; i++)
                {
                for (int j = 0; j < 9; j++)
                {
                    preBoard[i, j] = GetCell(i, j);
                }
            }
            //要素が多い順にブロックを並べる
            var blockCounts = new List<(int Block, int Count)>();
            for (int i = 0; i < 9; i++)
            {
                blockCounts.Add((i, _block[i].Count));
            }
            blockCounts.Sort((x, y) => y.Count.CompareTo(x.Count));
            int targetIndex = 0;
            var triedCell = new bool[9, 9];
            while (true)
            {
                int[] row = new int[tryNum];
                int[] col = new int[tryNum];
                bool flag = false;
                while (true)
                {
                    for (int i = 0; i < tryNum; i++)
                    {
                        row[i] = StartingCell(blockCounts[targetIndex].Block).Row + random.Next(0, 3);
                        col[i] = StartingCell(blockCounts[targetIndex].Block).Col + random.Next(0, 3);
                        if (GetCell(row[i], col[i]) > 0)//数字が存在する場合
                        {
                            if (!triedCell[row[i], col[i]])//まだ試していない場合
                            {
                                flag = true;
                            }
                            else
                            {
                                flag = false;
                                break;
                            }
                        }
                        else
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                    {
                        break;
                    }
                }
                //穴をあける
                for (int i = 0; i < tryNum; i++)
                {
                    SetCell(row[i], col[i], 0);
                }
                if (Solver())//解けた場合
                {
                    //元の盤面に戻す
                    for (int i = 0; i < 9; i++)
                    {
                        for (int j = 0; j < 9; j++)
                        {
                            SetCell(i, j, preBoard[i, j]);
                        }
                    }
                    //穴をあける
                    for (int i = 0; i < tryNum; i++)
                    {
                        SetCell(row[i], col[i], 0);
                    }
                    blank++;
                    break;
                }
                else//解けなかった場合
                {
                    //元の盤面に戻す(穴をあけない)
                    for (int i = 0; i < 9; i++)
                    {
                        for (int j = 0; j < 9; j++)
                        {
                            SetCell(i, j, preBoard[i, j]);
                        }
                    }
                    for (int i = 0; i < tryNum; i++)
                    {
                        triedCell[row[i], col[i]] = true;//試行済みする
                    }
                    int untriedCell = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (GetCell(StartingCell(blockCounts[targetIndex].Block).Row + i, StartingCell(blockCounts[targetIndex].Block).Col + j)  > 0)
                            {
                                if (triedCell[StartingCell(blockCounts[targetIndex].Block).Row + i, StartingCell(blockCounts[targetIndex].Block).Col + j] == false)
                                {
                                    untriedCell++;
                                }
                            }
                        }
                    }
                    if (untriedCell == 0)
                    {
                        targetIndex++;//次に多いブロックに変更
                        if (targetIndex > 8)
                        {
                            DebugBoard();
                            Debug.WriteLine($"{blank}/{maxBlank}");
                            return;
                        }
                    }
                }
            }
        }
        DebugBoard();
        Debug.WriteLine($"{blank}/{maxBlank}");
    }
    public void GenerateCompletedBoard()//埋められた盤面を生成
    {
        while (true)
        {
            if (GeneratingLoop())
            {
                break;
            }
        }
        DebugBoard();
        return;

        bool GeneratingLoop()
        {
            InitializeBoard();
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    //候補がある場合候補からランダムに入力、ない場合falseを返す
                    if (_pencilMark[row, col].Count > 0)
                    {
                        SetCell(row, col, _pencilMark[row, col][random.Next(0, _pencilMark[row, col].Count)]);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
    public bool Solver()//盤面を解決
    {
        int cnt = 0;
        int preCnt;
        while (cnt < 81)
        {
            //解けたかどうかの判定
            preCnt = cnt;
            cnt = 0;
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (GetCell(row, col) > 0)
                    {
                        cnt += 1;
                    }
                }
            }
            if (cnt == preCnt)
            {
                //Debug.WriteLine($"Result : Failed({cnt}/81)");
                //DebugBoard();
                //DebugPencilMark();
                return false;
            }

            if (_uniqueCandidate)//単一候補法
            {
                for (int row = 0; row < 9; row++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        //if (GetCell(row, col) == 0)
                        //{
                            if (_pencilMark[row, col].Count == 1)
                            {
                                SetCell(row, col, _pencilMark[row, col][0]);
                            }
                        //}
                    }
                }
            }

            if (_uniqueCell)//単一マス法
            {
                int cellCount;
                for (int num = 1; num < 10; num++)
                {
                    //ブロック
                    for (int block = 0; block < 9; block++)
                    {
                        var candidateCells = new List<(int Row, int Col)>();
                        cellCount = 0;
                        int row = StartingCell(block).Row;
                        int col = StartingCell(block).Col;
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                if (_pencilMark[row + i, col + j].Contains(num))
                                {
                                    candidateCells.Add((row + i, col + j));
                                    cellCount++;
                                }
                            }
                        }
                        if (cellCount == 1)
                        {
                            SetCell(candidateCells[0].Row, candidateCells[0].Col, num);
                        }
                    }
                    //行
                    for (int row = 0; row < 9; row++)
                    {
                        var candidateCells = new List<(int Row, int Col)>();
                        cellCount = 0;
                        for (int col = 0; col < 9; col++)
                        {
                            if (_pencilMark[row, col].Contains(num))
                            {
                                    candidateCells.Add((row, col));
                                    cellCount++;
                            }
                        }
                        if (cellCount == 1)
                        {
                            SetCell(candidateCells[0].Row, candidateCells[0].Col, num);
                        }
                    }
                    //列
                    for (int col = 0; col < 9; col++)
                    {
                        var candidateCells = new List<(int Row, int Col)>();
                        cellCount = 0;
                        for (int row = 0; row < 9; row++)
                        {
                            if (_pencilMark[row, col].Contains(num))
                            {
                                    candidateCells.Add((row, col));
                                    cellCount++;
                            }
                        }
                        if (cellCount == 1)
                        {
                            SetCell(candidateCells[0].Row, candidateCells[0].Col, num);
                        }
                    }
                }
            }
            //DebugBoard();
        }
        //Debug.WriteLine($"Result : Succeeded");
        //DebugBoard();
        return true;
    }
    public void UpdateInformation()//情報を更新
    {
        //数字の個数をカウント
        for (int i = 0; i < 9; i++)//配列の初期化
        {
            _numberCount[i] = 0;
        }
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int num = GetCell(row, col);
                if (num > 0)
                {
                    _numberCount[num - 1] += 1;
                }
            }
        }
        //DebugNumberCount();

        //ブロック内の数字をリストする
        for (int i = 0; i < 9; i++)//リストを空にする
        {
            _block[i].Clear();
        }
        for (int block = 0; block < 9; block++)
        {
            int row = StartingCell(block).Row;
            int col = StartingCell(block).Col;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int num = GetCell(row + i, col + j);
                    if (num > 0)
                    {
                        _block[block].Add(num);
                    }
                }
            }
        }
        //DebugBlock();

        //行内の数字をリストする
        for (int i = 0; i < 9; i++)//リストを空にする
        {
            _row[i].Clear();
        }
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int num = GetCell(row, col);
                if (num > 0)
                {
                    _row[row].Add(num);
                }
            }
        }
        //DebugRow();

        //列内の数字をリストする
        for (int i = 0; i < 9; i++)//リストを空にする
        {
            _col[i].Clear();
        }
        for (int col = 0; col < 9; col++)
        {
            for (int row = 0; row < 9; row++)
            {
                int num = GetCell(row, col);
                if (num > 0)
                {
                    _col[col].Add(num);
                }
            }
        }
        //DebugCol();

        //候補の数字をリストする（ペンシルマークする）
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                _pencilMark[row, col].Clear();//リストを空にする
                int block = IdentifyBlock(row, col);
                if (GetCell(row, col) == 0)
                {
                    for (int num = 1; num < 10; num++)
                    {
                        if (!_block[block].Contains(num))
                        {
                            if (!_row[row].Contains(num))
                            {
                                if (!_col[col].Contains(num))
                                {
                                    _pencilMark[row, col].Add(num);
                                }
                            }
                        }
                    }
                }
            }
        }

        //ペンシルマークを絞り込む
        if (_twins)//双子法
        {
            //独立双子法
            //1 つの行・列あるいはブロックの中で，2 つのマス p1 と p2 に，2 つの数字 n1, n2 の同じ組が存在するとき，その行・列あるいはブロックにある p1, p2 以外のマスから n1 とn2 の候補は消える
            　//候補の数が2つのみのマス、同じ数字が他のマスに存在していてもよい、他のマスからその数字を消す
            　//同じ2つの数字しか存在しない2つのマスを見つける
            　　//マスごとに候補の数字の数をカウントする
            　　//候補の数がどちらも2つのみでその数が一致するマスを見つける
            　　//他のマスから候補の2つの数字を消す
            //居候双子法
            　//1 つの行・列あるいはブロックの中で，2 つの数字 n1, n2 が，2 つのマス p1 と p2 にのみ存在するとき，p1, p2 の２つのマスでは，n1, n2 以外の候補は消える
            　//候補のマスが2つのみの数字、他の数字が同じマスに存在していてもよい（居候）、そのマスから他の数字を消す
            　//同じ2つのマスにしか存在しない2つの数字を見つける
            　　//数字ごとに候補のマスの数をカウントする
            　　//候補のマスが2つのみでそのマスが一致するペアを見つける
            　　//候補の2つのマスから他の数字を消す
            //配列をディープコピー
            var pencilMarkCopy = new List<int>[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    pencilMarkCopy[row, col] = new List<int> (_pencilMark[row, col]);
                }
            }
            //独立双子法
            //ブロック
            for (int block = 0; block < 9; block++)
            {
                var candidateCells = new List<(int Row, int Col)>();//候補が2つのみのマスのリスト、そのマスの_pencilMarkは[0]と[1]しかない
                int row = StartingCell(block).Row;
                int col = StartingCell(block).Col;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (_pencilMark[row + i, col + j].Count == 2)
                        {
                            candidateCells.Add((row + i, col + j));
                        }
                    }
                }
                for (int i = 0; i < candidateCells.Count; i++)//マスの2つずつの組み合わせ
                {
                    for (int j = i + 1; j < candidateCells.Count; j++)
                    {
                        if (_pencilMark[candidateCells[i].Row, candidateCells[i].Col].Contains(_pencilMark[candidateCells[j].Row, candidateCells[j].Col][0]) && _pencilMark[candidateCells[i].Row, candidateCells[i].Col].Contains(_pencilMark[candidateCells[j].Row, candidateCells[j].Col][1]))//候補の数字が一致する場合
                        {
                            //他のマスから候補の数字を消す
                            for (int m = 0; m < 3; m++)
                            {
                                for (int n = 0; n < 3; n++)
                                {
                                    if (row + m != candidateCells[i].Row || col + n != candidateCells[i].Col)//一つ目のマスと違う場合
                                    {
                                        if (row + m != candidateCells[j].Row || col + n != candidateCells[j].Col)//二つ目のマスとも違う場合
                                        {
                                            pencilMarkCopy[row + m, col + n].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][0]);
                                            pencilMarkCopy[row + m, col + n].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][1]);
                                            //_pencilMark[row + m, col + n].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][0]);
                                            //_pencilMark[row + m, col + n].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][1]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //行
            for (int row = 0; row < 9; row++)
            {
                var candidateCells = new List<(int Row, int Col)>();//候補が2つのみのマスのリスト、そのマスの_pencilMarkは[0]と[1]しかない
                for (int col = 0; col < 9; col++)
                {
                        if (_pencilMark[row, col].Count == 2)
                        {
                            candidateCells.Add((row, col));
                        }
                }
                for (int i = 0; i < candidateCells.Count; i++)//マスの2つずつの組み合わせ
                {
                    for (int j = i + 1; j < candidateCells.Count; j++)
                    {
                        if (_pencilMark[candidateCells[i].Row, candidateCells[i].Col].Contains(_pencilMark[candidateCells[j].Row, candidateCells[j].Col][0]) && _pencilMark[candidateCells[i].Row, candidateCells[i].Col].Contains(_pencilMark[candidateCells[j].Row, candidateCells[j].Col][1]))//候補の数字が一致する場合
                        {
                            //他のマスから候補の数字を消す
                            for (int col = 0; col < 9; col++)
                            {
                                if (row != candidateCells[i].Row || col != candidateCells[i].Col)//一つ目のマスと違う場合
                                {
                                    if (row != candidateCells[j].Row || col != candidateCells[j].Col)//二つ目のマスとも違う場合
                                    {
                                        pencilMarkCopy[row, col].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][0]);
                                        pencilMarkCopy[row, col].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][1]);
                                        //_pencilMark[row + m, col + n].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][0]);
                                        //_pencilMark[row + m, col + n].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][1]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //列
            for (int col = 0; col < 9; col++)
            {
                var candidateCells = new List<(int Row, int Col)>();//候補が2つのみのマスのリスト、そのマスの_pencilMarkは[0]と[1]しかない
                for (int row = 0; row < 9; row++)
                {
                        if (_pencilMark[row, col].Count == 2)
                        {
                            candidateCells.Add((row, col));
                        }
                }
                for (int i = 0; i < candidateCells.Count; i++)//マスの2つずつの組み合わせ
                {
                    for (int j = i + 1; j < candidateCells.Count; j++)
                    {
                        if (_pencilMark[candidateCells[i].Row, candidateCells[i].Col].Contains(_pencilMark[candidateCells[j].Row, candidateCells[j].Col][0]) && _pencilMark[candidateCells[i].Row, candidateCells[i].Col].Contains(_pencilMark[candidateCells[j].Row, candidateCells[j].Col][1]))//候補の数字が一致する場合
                        {
                            //他のマスから候補の数字を消す
                            for (int row = 0; row < 9; row++)
                            {
                                if (row != candidateCells[i].Row || col != candidateCells[i].Col)//一つ目のマスと違う場合
                                {
                                    if (row != candidateCells[j].Row || col != candidateCells[j].Col)//二つ目のマスとも違う場合
                                    {
                                        pencilMarkCopy[row, col].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][0]);
                                        pencilMarkCopy[row, col].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][1]);
                                        //_pencilMark[row + m, col + n].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][0]);
                                        //_pencilMark[row + m, col + n].Remove(_pencilMark[candidateCells[i].Row, candidateCells[i].Col][1]);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //居候双子法
            //ブロック
            for (int block = 0; block < 9; block++)
            {
                var candidateCells = new List<(int Row, int Col)>[9];//数字ごとの候補のマスのリスト
                for (int i = 0; i < 9; i++)
                {
                    candidateCells[i] = new List<(int Row, int Col)>();
                }
                var cellCount = new int[9];
                int row = StartingCell(block).Row;
                int col = StartingCell(block).Col;
                for (int num = 1; num < 10; num++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (_pencilMark[row + i, col + j].Contains(num))
                            {
                                candidateCells[num - 1].Add((row + i, col + j));
                                cellCount[num - 1]++;
                            }
                        }
                    }
                }
                for (int num1 = 1; num1 < 10; num1 ++)//1～9までの2つずつの組み合わせ
                {
                    for (int num2 = num1 + 1; num2 < 10; num2++)
                    {
                        if (cellCount[num1 - 1] == 2 && cellCount[num2 - 1] == 2)//2つとも候補のマスが2つのみ場合
                        {
                            if (candidateCells[num1 - 1].Contains(candidateCells[num2 -1][0]) && candidateCells[num1 - 1].Contains(candidateCells[num2 -1][1]))//候補のマスが一致する場合
                            {
                                for (int num = 1; num < 10; num++)
                                {
                                    if (num != num1 && num != num2)//他の数字の場合
                                    {
                                        //候補のマスから他の数字を消す
                                        pencilMarkCopy[candidateCells[num1 - 1][0].Row, candidateCells[num1 - 1][0].Col].Remove(num);
                                        pencilMarkCopy[candidateCells[num1 - 1][1].Row, candidateCells[num1 - 1][1].Col].Remove(num);
                                        //_pencilMark[candidateCells[num1 - 1][0].Row, candidateCells[num1 - 1][0].Col].Remove(num);
                                        //_pencilMark[candidateCells[num1 - 1][1].Row, candidateCells[num1 - 1][1].Col].Remove(num);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //行
            for (int row = 0; row < 9; row++)
            {
                var candidateCells = new List<(int Row, int Col)>[9];//数字ごとの候補のマスのリスト
                for (int i = 0; i < 9; i++)
                {
                    candidateCells[i] = new List<(int Row, int Col)>();
                }
                var cellCount = new int[9];
                for (int num = 1; num < 10; num++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        if (_pencilMark[row, col].Contains(num))
                        {
                            candidateCells[num - 1].Add((row, col));
                            cellCount[num - 1]++;
                        }
                    }
                }
                for (int num1 = 1; num1 < 10; num1 ++)//1～9までの2つずつの組み合わせ
                {
                    for (int num2 = num1 + 1; num2 < 10; num2++)
                    {
                        if (cellCount[num1 - 1] == 2 && cellCount[num2 - 1] == 2)//2つとも候補のマスが2つのみ場合
                        {
                            if (candidateCells[num1 - 1].Contains(candidateCells[num2 -1][0]) && candidateCells[num1 - 1].Contains(candidateCells[num2 -1][1]))//候補のマスが一致する場合
                            {
                                for (int num = 1; num < 10; num++)
                                {
                                    if (num != num1 && num != num2)//他の数字の場合
                                    {
                                        //候補のマスから他の数字を消す
                                        pencilMarkCopy[candidateCells[num1 - 1][0].Row, candidateCells[num1 - 1][0].Col].Remove(num);
                                        pencilMarkCopy[candidateCells[num1 - 1][1].Row, candidateCells[num1 - 1][1].Col].Remove(num);
                                        //_pencilMark[candidateCells[num1 - 1][0].Row, candidateCells[num1 - 1][0].Col].Remove(num);
                                        //_pencilMark[candidateCells[num1 - 1][1].Row, candidateCells[num1 - 1][1].Col].Remove(num);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //列
            for (int col = 0; col < 9; col++)
            {
                var candidateCells = new List<(int Row, int Col)>[9];//数字ごとの候補のマスのリスト
                for (int i = 0; i < 9; i++)
                {
                    candidateCells[i] = new List<(int Row, int Col)>();
                }
                var cellCount = new int[9];
                for (int num = 1; num < 10; num++)
                {
                    for (int row = 0; row < 9; row++)
                    {
                        if (_pencilMark[row, col].Contains(num))
                        {
                            candidateCells[num - 1].Add((row, col));
                            cellCount[num - 1]++;
                        }
                    }
                }
                for (int num1 = 1; num1 < 10; num1 ++)//1～9までの2つずつの組み合わせ
                {
                    for (int num2 = num1 + 1; num2 < 10; num2++)
                    {
                        if (cellCount[num1 - 1] == 2 && cellCount[num2 - 1] == 2)//2つとも候補のマスが2つのみ場合
                        {
                            if (candidateCells[num1 - 1].Contains(candidateCells[num2 -1][0]) && candidateCells[num1 - 1].Contains(candidateCells[num2 -1][1]))//候補のマスが一致する場合
                            {
                                for (int num = 1; num < 10; num++)
                                {
                                    if (num != num1 && num != num2)//他の数字の場合
                                    {
                                        //候補のマスから他の数字を消す
                                        pencilMarkCopy[candidateCells[num1 - 1][0].Row, candidateCells[num1 - 1][0].Col].Remove(num);
                                        pencilMarkCopy[candidateCells[num1 - 1][1].Row, candidateCells[num1 - 1][1].Col].Remove(num);
                                        //_pencilMark[candidateCells[num1 - 1][0].Row, candidateCells[num1 - 1][0].Col].Remove(num);
                                        //_pencilMark[candidateCells[num1 - 1][1].Row, candidateCells[num1 - 1][1].Col].Remove(num);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //元の配列に反映
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    _pencilMark[row, col] = new List<int> (pencilMarkCopy[row, col]);
                }
            }
        }

        if (_triplets)//三つ子法
        {
            //配列をディープコピー
            var pencilMarkCopy = new List<int>[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    pencilMarkCopy[row, col] = new List<int> (_pencilMark[row, col]);
                }
            }
            //独立三つ子法
            //居候三つ子法
            //元の配列に反映
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    _pencilMark[row, col] = new List<int> (pencilMarkCopy[row, col]);
                }
            }
        }

        if (_commonCandidate)//共有候補法
        {
            //配列をディープコピー
            var pencilMarkCopy = new List<int>[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    pencilMarkCopy[row, col] = new List<int> (_pencilMark[row, col]);
                }
            }
            //ブロックから行を消す
            //ブロックから列を消す
            　//ブロック内である数字の候補が特定の行にしか存在しない場合、
            　//その行の他のブロックのマスからその数字の候補を消す(List<T>.Remove()を使う、リストに数字が存在しなくても例外は発生しないので調べなくていい)
            //行からブロックを消す
            　//行内である数字の候補が特定のブロックにしか存在しない場合、
            　//そのブロックの他の行のマスからその数字の候補を消す
            //列からブロックを消す
            //ブロック
            for (int block = 0; block < 9; block++)
            {
                for (int num = 1; num < 10; num++)
                    {
                    int rowCount = 0;
                    int colCount = 0;
                    int candidateRow = -1;
                    int candidateCol = -1;
                    int row = StartingCell(block).Row;
                    int col = StartingCell(block).Col;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (_pencilMark[row + i, col + j].Contains(num))
                            {
                                if (row + i != candidateRow)
                                {
                                    rowCount++;
                                }
                                candidateRow = row + i;
                                if (col + j != candidateCol)
                                {
                                    colCount++;
                                }
                                candidateCol = col + j;
                            }
                        }
                    }
                    if (rowCount == 1)//候補の行が一つだけの場合
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            if (IdentifyBlock(candidateRow, i) != block)//現在のブロックとは別のブロックの場合
                            {
                                pencilMarkCopy[candidateRow, i].Remove(num);
                                //_pencilMark[candidateRow, i].Remove(num);
                            }
                        }
                    }
                    if (colCount == 1)//候補の列が一つだけの場合
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            if (IdentifyBlock(i, candidateCol) != block)//現在のブロックとは別のブロックの場合
                            {
                                pencilMarkCopy[i, candidateCol].Remove(num);
                                //_pencilMark[i, candidateCol].Remove(num);
                            }
                        }
                    }
                }
            }
            //行
            for (int row = 0; row < 9; row++)
            {
                for (int num = 1; num < 10; num++)
                {
                    int blockCount = 0;
                    int candidateBlock = -1;
                    for (int col = 0; col < 9; col++)
                    {
                        if (_pencilMark[row, col].Contains(num))
                        {
                            if (IdentifyBlock(row, col) != candidateBlock)
                            {
                                blockCount++;
                            }
                            candidateBlock = IdentifyBlock(row, col);
                        }
                    }
                    if (blockCount == 1)//候補のブロックが1つだけの場合
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                if (StartingCell(candidateBlock).Row + i != row)//そのブロックの他の行のマスの候補から消す
                                {
                                    pencilMarkCopy[StartingCell(candidateBlock).Row + i, StartingCell(candidateBlock).Col + j].Remove(num);
                                }
                            }
                        }
                    }
                }
            }
            //列
            for (int col = 0; col < 9; col++)
            {
                for (int num = 1; num < 10; num++)
                {
                    int blockCount = 0;
                    int candidateBlock = -1;
                    for (int row = 0; row < 9; row++)
                    {
                        if (_pencilMark[row, col].Contains(num))
                        {
                            if (IdentifyBlock(row, col) != candidateBlock)
                            {
                                blockCount++;
                            }
                            candidateBlock = IdentifyBlock(row, col);
                        }
                    }
                    if (blockCount == 1)//候補のブロックが1つだけの場合
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                if (StartingCell(candidateBlock).Col + j != col)//そのブロックの他の列のマスの候補から消す
                                {
                                    pencilMarkCopy[StartingCell(candidateBlock).Row + i, StartingCell(candidateBlock).Col + j].Remove(num);
                                }
                            }
                        }
                    }
                }
            }
            //元の配列に反映
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    _pencilMark[row, col] = new List<int> (pencilMarkCopy[row, col]);
                }
            }
        }

        if (_diagonalLine)//対角線法
        {
            //ある数字に着目する
            //その数字が候補として存在するマスが2つのみの行を調べる
            //その行が2つある場合4つのマスの列を比較する
            //列が一致した場合その2つの列の他のマスの候補から数字を消す
            //配列をディープコピー
            var pencilMarkCopy = new List<int>[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    pencilMarkCopy[row, col] = new List<int> (_pencilMark[row, col]);
                }
            }
            for (int num = 1; num < 10; num++)
            {
                var candidateRow = new List<int>();//候補の行のリスト
                var candidateCol = new List<int>[9];//行ごとの候補の列のリスト、候補のマスが2つのみの場合[0]と[1]しか存在しない
                for (int i = 0; i < 9; i++)
                {
                    candidateCol[i] = new List<int>();
                }
                for (int row = 0; row < 9; row++)
                {
                    int cellCount = 0;
                    for (int col = 0; col < 9; col++)
                    {
                        if (_pencilMark[row, col].Contains(num))
                        {
                            candidateCol[row].Add(col);
                            cellCount++;
                        }
                    }
                    if (cellCount == 2)//候補のマスが2つのみの場合
                    {
                        candidateRow.Add(row);//候補の行に追加
                    }
                }
                for (int i = 0; i < candidateRow.Count; i++)//候補の行の2つずつの組み合わせ
                {
                    for (int j = i + 1; j < candidateRow.Count; j++)
                    {
                        //iの[0]が左上、jの[0]が左下、iの[1]が右上、jの[1]が右下
                        if (candidateCol[candidateRow[i]][0] == candidateCol[candidateRow[j]][0] && candidateCol[candidateRow[i]][1] == candidateCol[candidateRow[j]][1])//それぞれの列が一致する場合
                        {
                            //それぞれの列の他のマスの候補から数字を消す
                            for (int m = 0; m < 9; m++)
                            {
                                if (m != candidateRow[i] && m != candidateRow[j])//他の行の場合
                                {
                                    pencilMarkCopy[m, candidateCol[candidateRow[i]][0]].Remove(num);
                                    pencilMarkCopy[m, candidateCol[candidateRow[i]][1]].Remove(num);
                                }
                            }
                        }
                    }
                }
            }
            //元の配列に反映
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    _pencilMark[row, col] = new List<int> (pencilMarkCopy[row, col]);
                }
            }
        }
        //DebugPencilMark();
    }
    public bool CompareBoard(Board board, out bool[,] result)//Board型のオブジェクトを渡すとそれと比較し、結果をbool型変数とbool型2次元配列で返す、呼び出すときにoutの後にアンダーバーで結果を破棄できる
    {
        int cnt = 0;
        result  = new bool[9, 9];
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (GetCell(row, col) == board.GetCell(row, col))//マスが一致する場合
                {
                    result[row, col] = true;
                    cnt++;
                }
            }
        }
        Debug.WriteLine($"Result : {cnt}/81");
        Debug.WriteLine("-------------------------");
        for (int row = 0; row < 9; row++)
        {
            var content = "|";
            for (int col = 0; col < 9; col++)
            {
                if (result[row, col] == true)
                {
                    content += " ○";
                }
                else
                {
                    content += " x";
                }
                if (col == 2 || col == 5 || col == 8)
                {
                    content += " |";
                }
            }
            Debug.WriteLine($"{content}");
            if (row == 2 || row == 5 || row == 8)
            {
                Debug.WriteLine("-------------------------");
            }
        }
        if (cnt == 81)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static (int Row, int Col) StartingCell(int block)//ブロックから左上の座標を特定
    {
        (int, int) point = (0, 0);
        switch (block)
        {
            case 0:
                point = (0, 0);
                break;
            case 1:
                point = (0, 3);
                break;
            case 2:
                point = (0, 6);
                break;
            case 3:
                point = (3, 0);
                break;
            case 4:
                point = (3, 3);
                break;
            case 5:
                point = (3, 6);
                break;
            case 6:
                point = (6, 0);
                break;
            case 7:
                point = (6, 3);
                break;
            case 8:
                point = (6, 6);
                break;
        }
        return point;
    }
    public static int IdentifyBlock(int row, int col)//座標からブロックを特定
    {
        int block = 0;
        if (row < 3)
        {
            if (col < 3)
            {
                block = 0;
            }
            else if (col < 6)
            {
                block = 1;
            }
            else
            {
                block = 2;
            }
        }
        else if (row < 6)
        {
            if (col < 3)
            {
                block = 3;
            }
            else if (col < 6)
            {
                block = 4;
            }
            else
            {
                block = 5;
            }
        }
        else
        {
            if (col < 3)
            {
                block = 6;
            }
            else if (col < 6)
            {
                block = 7;
            }
            else
            {
                block = 8;
            }
        }
        return block;
    }
    //デバッグ表示////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public void DebugBoard()//_board
    {
        Debug.WriteLine("-------------------------");
        for (int i = 0; i < 9;)
        {
            Debug.WriteLine($"| {_board[i, 0]} {_board[i, 1]} {_board[i, 2]} | {_board[i, 3]} {_board[i, 4]} {_board[i, 5]} | {_board[i, 6]} {_board[i, 7]} {_board[i++, 8]} |");
            Debug.WriteLine($"| {_board[i, 0]} {_board[i, 1]} {_board[i, 2]} | {_board[i, 3]} {_board[i, 4]} {_board[i, 5]} | {_board[i, 6]} {_board[i, 7]} {_board[i++, 8]} |");
            Debug.WriteLine($"| {_board[i, 0]} {_board[i, 1]} {_board[i, 2]} | {_board[i, 3]} {_board[i, 4]} {_board[i, 5]} | {_board[i, 6]} {_board[i, 7]} {_board[i++, 8]} |");
            Debug.WriteLine("-------------------------");
        }
    }
    public void DebugNumberCount()//_numberCount
    {
        Debug.WriteLine($"_numberCount");
        for (int i = 0; i < 9; i++)
        {
            Debug.WriteLine($"{i + 1}({_numberCount[i]}/9)");
        }
    }
    public void DebugBlock()
    {
        Debug.WriteLine($"_block");
        for (int block = 0; block < 9; block++)
        {
            Debug.Write($"{block}");
            var content = string.Join(", ", _block[block]);
            Debug.WriteLine($"({content})");
        }
    }
    public void DebugRow()
    {
        Debug.WriteLine($"_row");
        for (int row = 0; row < 9; row++)
        {
            Debug.Write($"{row}");
            var content = string.Join(", ", _row[row]);
            Debug.WriteLine($"({content})");
        }
    }
    public void DebugCol()
    {
        Debug.WriteLine($"_col");
        for (int col = 0; col < 9; col++)
        {
            Debug.Write($"{col}");
            var content = string.Join(", ", _col[col]);
            Debug.WriteLine($"({content})");
        }
    }
    public void DebugPencilMark()
    {
        Debug.WriteLine("_pencilMark");
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var content = string.Join(", ", _pencilMark[row, col]);
                Debug.WriteLine($"({row}, {col})({content})");
            }
        }
    }
}