namespace sudoku;

public partial class MainPage : Page
{
    RoundedRectButton newGame, solver;
    public MainPage()
    {
        this.Dock = DockStyle.Fill;
        this.DoubleBuffered = true;

        newGame = new RoundedRectButton()
        {
            Size = new Size(48 * 3, 48),
            Location = new Point(960 / 2 -  this.Size.Width/ 2, 540 / 2 - this.Size.Height / 2),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial", 12),
            Text = "New Game"
        };
        newGame.Click += (sender, e) =>{
            ChangePage(new DifficultySelectPage());
        };
        this.Controls.Add(newGame);

        //まだSolveモードが出来てない
        /*
        solver = new RoundedRectButton()
        {
            Size = new Size(48 * 3, 48),
            Location = new Point(960 / 2 -  this.Size.Width/ 2, 540 / 2 - this.Size.Height / 2 + 48 + 3),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial", 12),
            Text = "Solver"
        };
        solver.Click += (sender, e) =>{
            ChangePage(new GamePage(GameMode.Solver));
        };
        this.Controls.Add(solver);
        */
    }

}