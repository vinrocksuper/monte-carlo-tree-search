public class GameModel
{
    // Is it the red player's turn?
    public bool IsRed { get; set; } = true;

    // The last move played.
    public int LastMove { get; set; } = -1;

    // The height of each column. Used to check whether another token can be placed.
    public int[] Columns { get; set; } = new int[7];

    // We track red and yellow states so that the MCTS algorithm can learn no matter what their color is.
    // This is possible because Connect 4 is symmetric... the moves are the same no matter what side is being played.
    // The state of the board from the red player's perspective.
    public char[] RedState { get; set; } = new char[42];

    // The state of the board from the yellow player's perspective.
    public char[] YellowState { get; set; } = new char[42];

    // Has the red player won?
    public bool RedWin { get; set; } = false;

    // Has the yellow player won?
    public bool YellowWin { get; set; } = false;

    // How many valid moves are there?
    public int ValidMoves { get; set; } = 0;

    // Is it game over?
    public bool GameOver
    {
        get 
        { 
            for (int i = 0; i < 7; i++)
                if (Columns[i] < 6) return RedWin || YellowWin;

            return true;
        }
    }

    // Initialize the new game model.
    public GameModel()
    {
        Initialize();
    }

    // The initialization method.
    private void Initialize()
    {
        // Initialize the red and yellow states to empty boards.
        for (int i = 0; i < 42; i++)
        {
            RedState[i] = '0';
            YellowState[i] = '0';
        }

        // Count the number of initial valid moves.
        for (int i = 0; i < 7; i++)
            if (ValidMove(i)) ValidMoves++;
    }

    // Returns whether there is a valid move at the current column.
    public bool ValidMove (int column)
    {
        // If the height of the column is six, it is full.
        if (Columns[column] == 6) return false;
        return true;
    }

    // Adds a new piece to the game board at the given column.
    public bool AddPiece (int column)
    {
        // Increase the row count at the given column.
        int row = Columns[column];
        Columns[column]++;

        // Update the red and yellow states and check for win condition.
        if (IsRed)
        {
            RedState[FlattenBoard(row, column)] = '+';
            YellowState[FlattenBoard(row, column)] = '-';
            RedWin = CheckWin(RedState);
        }
        else
        {
            RedState[FlattenBoard(row, column)] = '-';
            YellowState[FlattenBoard(row, column)] = '+';
            YellowWin = CheckWin(YellowState);
        }

        // Update the last move and turn.
        LastMove = column;
        IsRed = !IsRed;

        // Return whether the game is over.
        return GameOver;
    }

    // Flattens a row, column tuple into an index into a flat array that represents the board.
    private int FlattenBoard (int row, int column)
    {
        return row * 7 + column;
    }

    // Checks a given board to see if the current player has won.
    public bool CheckWin (char[] board)
    {
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 7; column++)
            {
                if (board[FlattenBoard(row, column)].Equals('+') && board[FlattenBoard(row + 1, column)].Equals('+') && board[FlattenBoard(row + 2, column)].Equals('+') && board[FlattenBoard(row + 3, column)].Equals('+'))
                {
                    return true;
                }
            }
        }

        for (int column = 0; column < 4; column++)
        {
            for (int row = 0; row < 6; row++)
            {
                if (board[FlattenBoard(row, column)].Equals('+') && board[FlattenBoard(row, column + 1)].Equals('+') && board[FlattenBoard(row, column + 2)].Equals('+') && board[FlattenBoard(row, column + 3)].Equals('+'))
                {
                    return true;
                }
            }
        }

        for (int column = 3; column < 7; column++)
        {
            for (int row = 0; row < 3; row++)
            {
                if (board[FlattenBoard(row, column)].Equals('+') && board[FlattenBoard(row + 1, column - 1)].Equals('+') && board[FlattenBoard(row + 2, column - 2)].Equals('+') && board[FlattenBoard(row + 3, column - 3)].Equals('+'))
                    return true;
            }
        }

        for (int column = 3; column < 7; column++)
        {
            for (int row = 3; row < 6; row++)
            {
                if (board[FlattenBoard(row, column)].Equals('+') && board[FlattenBoard(row - 1, column - 1)].Equals('+') && board[FlattenBoard(row - 2, column - 2)].Equals('+') && board[FlattenBoard(row - 3, column - 3)].Equals('+'))
                    return true;
            }
        }
        return false;
    }

    // Get the current state based on the current player.
    public string GetState()
    {
        if (IsRed) return new string(RedState);
        return new string(YellowState);
    }

    // Write the current state to a string.
    public override string ToString()
    {
        string output = "";

        for (int row = 5; row >= 0; row--)
        {
            for (int column = 0; column < 7; column++)
            {
                int flat = FlattenBoard(row, column);
                if (RedState[flat].Equals('0')) output += "0 ";
                else if (RedState[flat].Equals('+')) output += "R ";
                else output += "Y ";
            }

            output += "\n";
        }

        return output;
    }

    // Clone the state.
    public GameModel Clone()
    {
        GameModel clone = new GameModel();

        clone.IsRed = IsRed;
        clone.Columns = (int[])Columns.Clone();
        clone.RedState = (char[])RedState.Clone();
        clone.YellowState = (char[])YellowState.Clone();
        clone.RedWin = RedWin;
        clone.YellowWin = YellowWin;

        return clone;
    }
}
