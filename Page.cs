namespace sudoku;

public class Page : UserControl
{
    public List<Color> pallet = new List<Color>{
        Color.FromArgb(0xFF, 0xFF, 0xFF),//[0]セルの背景、Solveボタン文字
        Color.FromArgb(0xBF, 0xC6, 0xD4),//[1]グリッド(細)
        Color.FromArgb(0x34, 0x48, 0x61),//[2]グリッド(太い)、問題の数字
        Color.FromArgb(0xE2, 0xEB, 0xF3),//[3]カーソルと同じブロック、行、列、数字ボタン背景
        Color.FromArgb(0xC3, 0xD7, 0xEA),//[4]カーソルと同じ数字
        Color.FromArgb(0xBB, 0xDE, 0xFB),//[5]カーソル
        Color.FromArgb(0x32, 0x5A, 0xAF),//[6]入力した数字、数字ボタン数字、Solveボタン背景
        Color.FromArgb(0x6F, 0x7D, 0x8E),//[7]ペンシルマーク
        Color.FromArgb(0xE5, 0x5C, 0x6C),//[8]間違っている数字
        Color.FromArgb(0xDC, 0xE3, 0xED),//[9]数字ボタン背景(マウスオーバー)
        Color.FromArgb(0x70, 0x91, 0xD5)//[10]Solveボタン背景(マウスオーバー)
    };

    public void ChangePage(Page page)
    {
        if (this.Parent != null)
        {
            this.Parent.Controls.Add(page);
            this.Dispose();
        }
    }

}