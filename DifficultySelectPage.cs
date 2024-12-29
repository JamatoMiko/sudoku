namespace sudoku;

public class DifficultySelectPage : Page
{
    RoundedRectButton[] difficulty = new RoundedRectButton[4];
    RoundedRectButton pageBack;
    public DifficultySelectPage()
    {
        this.Dock = DockStyle.Fill;
        this.DoubleBuffered = true;

        difficulty[0] = new RoundedRectButton(){
            Size = new Size(48 * 3, 48),
            Location = new Point(960 / 2 -  this.Size.Width/ 2, 540 / 2 - this.Size.Height / 2),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial", 12),
            Text = "Vary Easy"
        };
        difficulty[0].Click += (sender, e) => {
            ChangePage(new GamePage(GameMode.Random, Difficulty.VeryEasy));
        };
        difficulty[1] = new RoundedRectButton(){
            Size = new Size(48 * 3, 48),
            Location = new Point(960 / 2 -  this.Size.Width/ 2, 540 / 2 - this.Size.Height / 2 + 48 + 3),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial", 12),
            Text = "Easy"
        };
        difficulty[1].Click += (sender, e) => {
            ChangePage(new GamePage(GameMode.Random, Difficulty.Easy));
        };
        difficulty[2] = new RoundedRectButton(){
            Size = new Size(48 * 3, 48),
            Location = new Point(960 / 2 -  this.Size.Width/ 2, 540 / 2 - this.Size.Height / 2 + (48 + 3) * 2),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial", 12),
            Text = "Medium"
        };
        difficulty[2].Click += (sender, e) => {
            ChangePage(new GamePage(GameMode.Random, Difficulty.Medium));
        };
        difficulty[3] = new RoundedRectButton(){
            Size = new Size(48 * 3, 48),
            Location = new Point(960 / 2 -  this.Size.Width/ 2, 540 / 2 - this.Size.Height / 2 + (48 + 3) * 3),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial", 12),
            Text = "Hard"
        };
        difficulty[3].Click += (sender, e) => {
            ChangePage(new GamePage(GameMode.Random, Difficulty.Hard));
        };
        this.Controls.AddRange(difficulty);

        pageBack = new RoundedRectButton(){
            Location = new Point(3, 3),
            Size = new Size(48 * 2, 48),
            DefaultBackColor = pallet[6],
            HoverBackColor = pallet[10],
            BorderSize = 0,
            ForeColor = pallet[0],
            Font = new Font("Arial", 12),
            Text = "< Back"
        };
        pageBack.Click += (sender, e) =>{
            ChangePage(new MainPage());
        };
        this.Controls.Add(pageBack);
    }
}