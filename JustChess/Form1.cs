using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Threading;

namespace JustChess
{
    public partial class Form1 : Form
    {
        List<int[]> PossibleMoves { get; set; }
        int[] SelectedPiece { get; set; }

        private Panel[,] BoardPanel = new Panel[8, 8];
        private Button[] Buttons = new Button[1];
        Board activeboard;
        public Board ActiveBoard { get { return activeboard; } set { activeboard = value; PanelUpdate(); } }
        public List<Board> PossibleBoards { get; set; }
        private int tilesize;
        double ImageScale = 1;
        double LastScale = 0;
        private int[] formsize { get; set; }
        public Form1()
        {
            ActiveBoard = new Board(new Player(true), new Player(false), new Piece[8, 8], true).initBoard();
            MaximizeBox = false;
            InitializeComponent();
            Text = "JustChess";
            for (int i = 0; i < 8; i++)
            {
                for (int ii = 0; ii < 8; ii++)
                {
                    var newPanel = new Panel
                    {
                        Size = new Size(tilesize, tilesize),
                        Location = new Point(tilesize * i, tilesize * ii)
                    };

                    newPanel.Click += newPanel_Click;
                    newPanel.BackgroundImageLayout = ImageLayout.Center;
                    if (!(ActiveBoard.Pieces[ii, i] is Empty))
                    {
                        newPanel.BackgroundImage = ScaleImage(ActiveBoard.Pieces[ii, i].PieceImage, 40);
                        newPanel.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
                    }

                    //Add panel to controls
                    Controls.Add(newPanel);
                    //Add panel to board
                    BoardPanel[i, ii] = newPanel;

                    //Color the board
                    if (i % 2 == 0)
                        newPanel.BackColor = ii % 2 != 0 ? Color.Black : Color.White;
                    else
                        newPanel.BackColor = ii % 2 != 0 ? Color.White : Color.Black;
                }
            }
            Button button = new Button
            {
                Size = new Size(tilesize * 2, tilesize / 2),
                Location = new Point(tilesize * 3, tilesize * 8),
                Text = "Reset"
            };
            Buttons[0] = button;
            button.Click += Button_Click;
            Controls.Add(button);

            //Set dynamic scaling
            this.ResizeEnd += Form1_Resize;
            formsize = new int[] { 0, 0 };
            Size = new Size(500, 500);
            Form1_Resize(this, new EventArgs());
        }
        private Image ScaleImage(Image original, int size)
        {
            if (original is null) { return null; }
            Bitmap b2 = new Bitmap(original, new Size(size, size));
            for (int i = 0; i < b2.Height; i++)
            {
                for (int ii = 0; ii < b2.Width; ii++)
                {
                    Color pixelColor = b2.GetPixel(i, ii);
                    if (pixelColor.GetBrightness() > .5) { b2.SetPixel(i, ii, Color.Transparent); }
                    else { b2.SetPixel(i, ii, Color.HotPink); }
                }
            }
            return b2;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (Size.Width == formsize[0] && Size.Height == formsize[1]) { return; }
            Control control = (Control)sender;
            tilesize = control.Size.Width / 9;
            // Set form dimentions
            control.Size = new Size(control.Size.Width - (tilesize / 2), control.Size.Width + (tilesize / 2));
            //Assuming the form is >16 px resize the board
            if (control.Size.Width > 16)
            {
                int fontsize = 8; ImageScale = 1;
                if (tilesize < 40) { fontsize = 4; ImageScale = .5; }
                if (tilesize > 60) { fontsize = 12; ImageScale = 1.4; }
                if (tilesize > 75) { fontsize = 16; ImageScale = 2; }

                for (int i = 0; i < 8; i++)
                {
                    for (int ii = 0; ii < 8; ii++)
                    {
                        BoardPanel[i, ii].Size = new Size(tilesize, tilesize);
                        BoardPanel[i, ii].Location = new Point(tilesize * i, tilesize * ii);
                    }
                }
                PanelUpdate();
                Buttons[0].Size = new Size(tilesize * 2, tilesize / 2);
                Buttons[0].Location = new Point(tilesize * 3, tilesize * 8);
                Buttons[0].Font = new Font("Tahoma", fontsize);
            }
            formsize[0] = Serializer.DeepClone(Size.Width);
            formsize[1] = Serializer.DeepClone(Size.Height);
        }
        private void Button_Click(object sender, EventArgs e)
        {
            Board b = new Board(new Player(true), new Player(false), new Piece[8, 8], true).initBoard();
            UpdateImages(b); ActiveBoard = b;
            PanelUpdate();
        }
        void UpdateImages(Board compare)
        {
            for (int j = 0; j < 8; j++)
            {
                for (int jj = 0; jj < 8; jj++)
                {
                    if (ActiveBoard.Pieces[j, jj] != compare.Pieces[j, jj])
                    {
                        BoardPanel[jj, j].BackgroundImage = ScaleImage(compare.Pieces[j, jj].PieceImage, (int)(40 * ImageScale));
                    }
                }
            }
        }
        void newPanel_Click(object sender, EventArgs e)
        {
            Panel pan = sender as Panel;
            if (pan is null) { return; }
            Piece pic = ActiveBoard.Pieces[pan.Location.Y / tilesize, pan.Location.X / tilesize];
            //Select piece if valid
            if (!(pic is Empty) && pic.Player.IsW == ActiveBoard.WTurn)
            {
                SelectedPiece = new int[] { pic.PosX, pic.PosY };
                PossibleBoards = ActiveBoard.GenMoveByType(pic, true);
                PossibleMoves = new List<int[]>();
                foreach (Board b in PossibleBoards)
                {
                    PossibleMoves.Add(new int[] { b.RecentMove[0], b.RecentMove[1] });
                }
                PanelUpdate();
                return;
            }
            //Move if valid
            if (SelectedPiece != null)
            {
                for (int i = 0; i < PossibleMoves.Count; i++)
                {
                    if ((pic.PosX != PossibleMoves[i][0]) || (pic.PosY != PossibleMoves[i][1])) { continue; }
                    //Only update panels as needed
                    UpdateImages(PossibleBoards[i]);
                    ActiveBoard = PossibleBoards[i];
                    break;
                }
                SelectedPiece = null;
                PossibleMoves = null;

                PanelUpdate();
                return;
            }
        }

        void PanelUpdate()
        {
            if (BoardPanel[0, 0] is null) { return; }
            bool resize = false;
            if (LastScale != ImageScale) { resize = true; LastScale = ImageScale; }
            for (int i = 0; i < 8; i++)
            {
                for (int ii = 0; ii < 8; ii++)
                {
                    var p = BoardPanel[i, ii];
                    if (i % 2 == 0)
                        p.BackColor = ii % 2 != 0 ? Color.Black : Color.White;
                    else
                        p.BackColor = ii % 2 != 0 ? Color.White : Color.Black;
                    if (resize)
                    {
                        p.BackgroundImage = ScaleImage(ActiveBoard.Pieces[ii, i].PieceImage, (int)(40 * ImageScale));
                    }
                }
            }
            if (SelectedPiece != null) { BoardPanel[SelectedPiece[1], SelectedPiece[0]].BackColor = Color.BlanchedAlmond; }
            if (PossibleMoves != null) { foreach (int[] i in PossibleMoves) { BoardPanel[i[1], i[0]].BackColor = Color.Orange; } }
        }
    }
}