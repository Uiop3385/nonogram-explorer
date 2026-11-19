using System.Data;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Transactions;
using System.Windows;
using System.Windows.Media;

namespace Nonogramme
{
    //This class is used to represent a nonogram loaded from a text file.
    //The format (text files with the .non extension) and method for processing nonograms are based on the links below   
    //https://lyndon.codes/2021/07/03/nonograms/
    //https://codefloe.com/mikix/nonogram-db/
    public class Nonogram
    {
        public enum CellState { Empty = 0, Filled = 1 }

        public List<List<int>> Rows { get; set; } = new List<List<int>>();
        public List<List<int>> Cols { get; set; } = new List<List<int>>();
        //The Grid attribute is used to store the solution
        public CellState[,] Grid { get; set; }
        //The CurrentGame attribute is used to store the current state of the game grid.
        public CellState[,] CurrentGame { get; set; }

        public int width;
        public int height;

        public bool isValid = true;

        //Function that takes the content of the text file and parses it to instantiate an object of the Nonogram class.
        // (1) You must implement the ParseFromText function
        public static Nonogram ParseFromText(string text)
        {
            var n = new Nonogram();

            const int SKIP = -1;
            const int METADATA = 0;
            const int ROWS = 1;
            const int COLS = 2;

            int state = 0;

            int currentRow = 0;
            int currentCol = 0;

            try
            {
                using (var reader = new StringReader(text))
                {
                    for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        if (line.Length == 0) state = SKIP;

                        if (state == METADATA)
                        {
                            if (line.StartsWith("width")) n.width = Int32.Parse(line.Substring(6));
                            if (line.StartsWith("height")) n.height = Int32.Parse(line.Substring(7));

                            if (n.width < 0 || n.height < 0)
                            {
                                throw new InvalidDataException("Column and/or row count is negative");
                            }
                        }
                        else if (state == ROWS)
                        {
                            int rowHintTotal = 0;
                            int rowZeroCount = 0;
                            n.Rows.Add(new List<int>());
                            string[] row_hints = line.Split(',');
                            foreach (string hint in row_hints)
                            {
                                int hintInteger = Int32.Parse(hint);

                                if (hintInteger < 0)
                                {
                                    throw new IndexOutOfRangeException("Negative hint: " + hint + " (on row " + (currentRow + 1) + ")");
                                }
                                if (hintInteger == 0) rowZeroCount++;

                                n.Rows.ElementAt(currentRow).Add(hintInteger);

                                // there's a 1-cell gap between each hint
                                rowHintTotal += hintInteger + 1;
                            }

                            // we must substract one because the last hint won't have a space
                            rowHintTotal--;

                            if (rowHintTotal > n.width)
                            {
                                throw new OverflowException("Row N°"+ (currentRow+1) +"'s hint total exceeds puzzle width (" + n.width + ")");
                            }

                            if (rowZeroCount > 1)
                            {
                                throw new DuplicateNameException("There cannot be more than 1 zero-hint per row");
                            }

                            if (rowZeroCount == 1 && row_hints.Length > 1)
                            {
                                throw new InvalidDataException("A zero-hint cannot contain other hints");
                            }

                            currentRow++;
                        }
                        else if (state == COLS)
                        {
                            int colHintTotal = 0;
                            int colZeroCount = 0;
                            n.Cols.Add(new List<int>());
                            string[] col_hints = line.Split(',');
                            foreach (string hint in col_hints)
                            {
                                int hintInteger = Int32.Parse(hint);

                                if (hintInteger < 0)
                                {
                                    throw new IndexOutOfRangeException("Negative hint: " + hint + " (on column " + (currentCol + 1) + ")");
                                }
                                if (hintInteger == 0) colZeroCount++;

                                n.Cols.ElementAt(currentCol).Add(hintInteger);
                                colHintTotal += hintInteger + 1;
                            }

                            colHintTotal--;

                            if (colHintTotal > n.height)
                            {
                                throw new OverflowException("Column N°" + (currentCol+1) + "'s hint total exceeds puzzle height (" + n.height + ")");
                            }

                            if (colZeroCount > 1)
                            {
                                throw new DuplicateNameException("There cannot be more than 1 zero hint per column");
                            }

                            if (colZeroCount == 1 && col_hints.Length > 1)
                            {
                                throw new InvalidDataException("A zero-hint cannot contain other hints");
                            }

                            currentCol++;
                        }

                        if (line.Equals("rows")) state = ROWS;
                        if (line.Equals("columns")) state = COLS;
                        if (line.StartsWith("goal"))
                        {
                            string goal = line.Substring(6);
                            goal = goal.Replace("\"", "");

                            if (goal.Length != n.height * n.width)
                            {
                                throw new ArgumentOutOfRangeException("Goal string");
                            }

                            n.Grid = new CellState[n.height, n.width];

                            int i = 0;
                            int j = 0;
                            int totalCellsInGoal = 0;

                            foreach (char correctState in goal)
                            {
                                if (i >= n.width)
                                {
                                    i = 0;
                                    j++;
                                }

                                int goalDigit = Int32.Parse(correctState.ToString());

                                if (goalDigit < 0 || goalDigit > 1)
                                {
                                    throw new IndexOutOfRangeException("Goal digit was not 0 or 1");
                                } 

                                n.Grid.SetValue((CellState)goalDigit, j, i);
                                i++;
                                totalCellsInGoal++;
                            }
                        }
                    }
                }

                if (n.Cols.Count == 0 && n.Rows.Count == 0)
                {
                    throw new ApplicationException("A parsing error occured.\nThis is probably not a puzzle file, make sure you've selected\na valid puzzle file.");
                }

                if (currentCol != n.width || currentRow != n.height)
                {
                    throw new ArgumentOutOfRangeException("Hint count");
                }

                n.CurrentGame = new CellState[n.height, n.width];
            }
            catch (Exception e)
            {
                n.isValid = false;
                MessageBox.Show("A parsing error occurred.\nThe puzzle contains invalid data.\nThis happened on parsing phase " + state.ToString() + ".\nFull error:\n" + e.ToString(), "Parse error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return n;
        }
        
        public void Solve()
        {
            // OPTIONAL: When called, this function should solve the current Nonogram.
            // This is not trivial; several algorithms exist.
        }
    }
}