using System.Diagnostics;

namespace sudoku;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        this.Controls.Add(new MainPage());
    }
}
