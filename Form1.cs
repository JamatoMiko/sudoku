using System.Diagnostics;

namespace sudoku;

public partial class Form1 : Form
{
    GamePage gamePage;
    public Form1()
    {
        InitializeComponent();
        gamePage = new GamePage();
        this.Controls.Add(gamePage);
    }
}
