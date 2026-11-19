using Microsoft.Win32; // English version
using System.IO;
using System.Linq;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Nonogramme.Nonogram;

namespace Nonogramme
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Nonogram loadedPuzzle;
        private const int HINT_SIZE = 23;
        private List<Cell> cells;
        private bool puzzleListValid = false;
        private string selectedFolder;
        private List<String> puzzleFileNames;
        private int lastSelectedPuzzle = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SavePuzzle();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog();
            if (dlg.ShowDialog() == true)
            {
                ResetGrid();
                puzzleListValid = false;
                LBXPuzzleList.Items.Clear();
                selectedFolder = dlg.FolderName;
                puzzleFileNames = new List<String>();

                foreach (string file in System.IO.Directory.EnumerateFiles(selectedFolder))
                {
                    if (Path.GetFileName(file).EndsWith(".non") || Path.GetFileName(file).EndsWith(".txt"))
                    {
                        string listText = Path.GetFileName(file);
                        puzzleFileNames.Add(listText);
                        if (File.Exists(file + ".progress"))
                            listText += " (in progress)";
                        LBXPuzzleList.Items.Add(listText);
                    }
                }

                if (LBXPuzzleList.Items.Count == 0)
                {
                    LBXPuzzleList.Items.Add("No valid files found");
                    LBXPuzzleList.Items.Add("in this directory!");
                    LBXPuzzleList.Items.Add("Nonograms must have");
                    LBXPuzzleList.Items.Add("the .non or .txt");
                    LBXPuzzleList.Items.Add("file extension.");

                    BtnSolve.IsEnabled = false;
                    BtnTest.IsEnabled = false;
                }
                else
                {
                    puzzleListValid = true;
                    LBXPuzzleList.SelectedIndex = 0;
                    BtnSolve.IsEnabled = true;
                    BtnTest.IsEnabled = true;
                }
            }
        }

        private void LBXPuzzleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (puzzleListValid)
            {
                if (loadedPuzzle != null)
                {
                    SavePuzzle();
                    if (!LBXPuzzleList.Items[lastSelectedPuzzle].ToString().EndsWith(" (in progress)"))
                    {
                        string currentText = LBXPuzzleList.Items[lastSelectedPuzzle].ToString();
                        LBXPuzzleList.Items[lastSelectedPuzzle] = currentText + " (in progress)";
                    }
                }
                loadedPuzzle = Nonogram.ParseFromText(
                    File.ReadAllText(
                        Path.Combine(selectedFolder, puzzleFileNames[LBXPuzzleList.SelectedIndex])
                    )
                );
                if (loadedPuzzle.isValid)
                {
                    lastSelectedPuzzle = LBXPuzzleList.SelectedIndex;

                    string progressFilePath = Path.Combine(selectedFolder, puzzleFileNames[LBXPuzzleList.SelectedIndex]) + ".progress";

                    BuildGridUI();
                    if (File.Exists(progressFilePath))
                    {
                        ReloadProgress(File.ReadAllText(progressFilePath));
                    }
                }
                else
                {
                    LBXPuzzleList.SelectedIndex = lastSelectedPuzzle;
                }
            }
        }

        private void SavePuzzle()
        {
            using (StreamWriter progressFile = new StreamWriter(Path.Combine(
                selectedFolder, puzzleFileNames[lastSelectedPuzzle]) + ".progress"
            ))
            {
                UpdateCurrentGame();

                if (CheckSolution(loadedPuzzle.CurrentGame, loadedPuzzle.Grid) && AreWhiteCellsChecked())
                {
                    progressFile.WriteLine("Completed");
                }

                int i = 0;
                int j = 0;

                foreach (Cell cell in cells)
                {
                    if (i > loadedPuzzle.width)
                    {
                        i = 0;
                        j++;
                    }

                    int cellState = (int)cell.GetState();

                    if (cellState == 1)
                    {
                        progressFile.Write(cellState);
                    }
                    else
                    {
                        if (cell.GetIsChecked())
                            progressFile.Write("X");
                        else
                            progressFile.Write("0");
                    }

                    i++;
                }
            }
        }

        private void ReloadProgress(string text)
        {
            using (var reader = new StringReader(text))
            {
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    if (line.Equals("Completed"))
                    {
                        // future processing
                        continue;
                    }
                    else
                    {
                        int i = 0;
                        int j = 0;
                        foreach (Cell cell in cells)
                        {
                            if (i >= loadedPuzzle.width)
                            {
                                i = 0;
                                j++;
                            }

                            char progressValue = line[i + (j*loadedPuzzle.width)];

                            if (progressValue == '1')
                            {
                                cell.SetState(CellState.Filled);
                            }
                            else
                            {
                                cell.SetState(CellState.Empty);
                                if (progressValue == 'X') cell.SetIsChecked(true);
                            }

                            i++;
                        }
                    }
                }
            }
        }

        private void ResetGrid()
        {
            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();
        }

        private void BuildGridUI()
        {
            ResetGrid();

            cells = new List<Cell>();

            int row = 1;
            int col = 1;

            for (int i = 0; i <= loadedPuzzle.width; i++)
            {
                ColumnDefinition columnDef = new ColumnDefinition();
                columnDef.Width = new GridLength();
                MainGrid.ColumnDefinitions.Add(columnDef);

                var hintStackPanel = new StackPanel();
                
                Grid.SetColumn(hintStackPanel, i + 1);

                if (i < loadedPuzzle.width)
                {
                    foreach (int hintValue in loadedPuzzle.Cols.ElementAt(i))
                    {
                        var hint = new Label();
                        hint.Content = hintValue;
                        hint.HorizontalAlignment = HorizontalAlignment.Center;
                        hintStackPanel.Children.Add(hint);
                    }
                }

                MainGrid.Children.Add(hintStackPanel);
            }

            for (int i = 0; i <= loadedPuzzle.height; i++)
            {
                RowDefinition rowDef = new RowDefinition();
                rowDef.Height = new GridLength();
                MainGrid.RowDefinitions.Add(rowDef);

                var hintStackPanel = new StackPanel();
                hintStackPanel.Orientation = Orientation.Horizontal;
                Grid.SetRow(hintStackPanel, i + 1);

                if (i < loadedPuzzle.height)
                {
                    foreach (int hintValue in loadedPuzzle.Rows.ElementAt(i))
                    {
                        var hint = new Label();
                        hint.Content = hintValue;
                        hint.HorizontalAlignment = HorizontalAlignment.Center;

                        // this makes sure all hints are aligned by giving them all the same width,
                        // so double-digit hints don't mess up the spacing
                        hint.Width = HINT_SIZE;

                        hintStackPanel.Children.Add(hint);
                    }
                }

                MainGrid.Children.Add(hintStackPanel);
            }

            foreach (CellState cell in loadedPuzzle.CurrentGame)
            {
                if (row >= loadedPuzzle.width + 1)
                {
                    row = 1;
                    col++;
                }
                Cell newCell = new Cell(cell, col, row);
                // the current last position is the product of the current row and column, -1 to each to account for the shift to fit the hints
                cells.Add(newCell);
                MainGrid.Children.Add(newCell);
                row++;
            }
        }

        //This procedure simply checks whether the game's current grid matches the solution grid.
        //If they match, a MessageBox can be displayed to inform the user that they have won.
        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            UpdateCurrentGame();
            if (CheckSolution(loadedPuzzle.CurrentGame, loadedPuzzle.Grid))
            {
                if (AreWhiteCellsChecked())
                {
                    MessageBox.Show("Congratulations, you've successfully solved this puzzle!", "Puzzle solved!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("You've marked all cells that need to be black, but all\nwhite cells must be checked to complete the puzzle.", "Almost there!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Unfortunately, your grid has one or more errors.\nCheck back on your solution then try again!", "Invalid solution", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //BONUS
        private void BtnSolve_Click(object sender, RoutedEventArgs e)
        {
            loadedPuzzle.Solve();
        }

        //Utility function to retrieve the control located at coordinates (row, col) in grid g
        //You will need to cast it.
        private UIElement GetUIElementFromPosition(Grid g, int row, int col)
        {
            return g.Children.Cast<UIElement>().First(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == col);
        }

        private void UpdateCurrentGame()
        {
            foreach (Cell cell in cells)
            {
                // -1 is to account for the shift to fit the hints
                int row = cell.GetRow() - 1;
                int col = cell.GetCol() - 1;
                loadedPuzzle.CurrentGame[row, col] = cell.GetState();
            }
        }

        private bool CheckSolution(CellState[,] attempt, CellState[,] solution)
        {
            return attempt.Cast<CellState>().SequenceEqual(solution.Cast<CellState>());
        }

        // checks if all unmarked cells are checked
        private bool AreWhiteCellsChecked()
        {
            foreach (Cell cell in cells)
            {
                if (cell.GetState() == CellState.Empty && !cell.GetIsChecked()) return false;
            }
            return true;
        }

        private class Cell : Button
        {
            private const int CELL_SIZE = 30;
            private CellState state;
            private int row;
            private int col;

            private bool isChecked;

            public Cell(CellState state, int row, int col)
            {
                this.state = state;
                this.Height = CELL_SIZE;
                this.Width = CELL_SIZE;

                /*
                 * isChecked is to allow the user to add a check to a cell, which serves as an indicator to
                 * know which cells needn't be marked, and is toggled by pressing/holding the right mouse button.
                 * 
                 * a checked cell can't be filled in until it's unchecked, and a filled in cell can't be checked.
                 */
                this.isChecked = false;

                UpdateBackground();

                this.row = row;
                this.col = col;
                Grid.SetRow(this, row);
                Grid.SetColumn(this, col);

                // this allows the user to drag over multiple buttons to change all of their states
                this.ClickMode = ClickMode.Hover;

                this.Click += SwapState;
                // we also add this event so the first click and single clicks still work
                this.MouseLeftButtonDown += SwapState;

                // same logic for checks
                this.Click += ToggleCheck;
                this.MouseRightButtonDown += ToggleCheck;

                this.FontWeight = FontWeights.ExtraBold;
                this.FontSize = 16;
            }

            private void SwapState(object sender, RoutedEventArgs e)
            {
                // we have to check if the mouse button is being held down, or else simply hovering will change the state
                if (!isChecked && System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    state = (state == CellState.Empty) ? CellState.Filled : CellState.Empty;
                    UpdateBackground();
                }
            }

            private void ToggleCheck(object sender, RoutedEventArgs e)
            {
                if (state == CellState.Empty && System.Windows.Input.Mouse.RightButton == MouseButtonState.Pressed)
                {
                    isChecked = !isChecked;
                    UpdateCheck();
                }
            }

            private void UpdateBackground()
            {
                this.Background = (state == CellState.Empty) ? Brushes.White : Brushes.Black;
            }

            private void UpdateCheck()
            {
                if (isChecked)
                {
                    this.Content = "X";

                }
                else
                {
                    this.Content = "";
                }
            }

            public CellState GetState() { return state; }

            public int GetRow() { return row; }

            public int GetCol() { return col; }

            public bool GetIsChecked() { return isChecked; }

            public void SetState(CellState state)
            {
                this.state = state;
                UpdateBackground();
            }

            public void SetIsChecked(bool isChecked)
            {
                this.isChecked = isChecked;
                UpdateCheck();
            }
        }
    }
}