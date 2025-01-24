using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

struct Sudoku
{
    // All the digits in the sudoku, if no digit is present in a square yet, the value is -1
    int[] digits = new int[81];

    // In each row, column and box a digit may appear or not, which is represented by 27 9-bit bitmasks
    // for all 9 + 9 + 9 rows, columns and boxes
    uint[] rowFrequencies = new uint[9];
    uint[] colFrequencies = new uint[9];
    uint[] boxFrequencies = new uint[9];

    // Stores whether a digit is fixed (already given)
    bool[,] isFixed = new bool[9, 9];

    public Sudoku(string line)
    {
        string[] numbers = line.Split(" ");
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                int number = int.Parse(numbers[9 * i + j]) - 1;
                if (number != -1)
                {
                    digits[i * 9 + j] = number;
                    rowFrequencies[i] |= 1u << number;
                    colFrequencies[j] |= 1u << number;
                    boxFrequencies[coordinateToBoxIndex(i, j)] |= 1u << number;
                    isFixed[i, j] = true;
                }
                else
                {
                    digits[i * 9 + j] = -1;
                }
            }
        }
    }

    /// <summary>
    /// Clears the sudoku except for all fixed digits
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (!isFixed[i, j])
                {
                    int number = digits[i * 9 + j];
                    if (number == -1)
                        continue;

                    digits[i * 9 + j] = -1;
                    rowFrequencies[i] &= ~(1u << number);
                    colFrequencies[j] &= ~(1u << number);
                    boxFrequencies[coordinateToBoxIndex(i, j)] &= ~(1u << number);
                }
            }
        }
    }

    /// <summary>
    /// Helper function to convert a coordinate into an indexing of the 9 boxes
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    int coordinateToBoxIndex(int i, int j)
    {
        return (i / 3) * 3 + (j / 3);
    }

    /// <summary>
    /// Fills in a digit at row i and column j
    /// returns true when this is possible and the digit doesn't yet appear in the relevant rows, columns or boxes,
    /// otherwise it doesn't do anything and returns false
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <param name="digit"></param>
    /// <returns></returns>
    bool fillDigit(int i, int j, int digit)
    {
        uint digitBin = 1u << digit;
        int boxIndex = coordinateToBoxIndex(i, j);
        if (((rowFrequencies[i] | colFrequencies[j] | boxFrequencies[boxIndex]) & digitBin) != 0)
            return false;

        digits[i * 9 + j] = digit;
        rowFrequencies[i] |= digitBin;
        colFrequencies[j] |= digitBin;
        boxFrequencies[boxIndex] |= digitBin;

        return true;
    }

    /// <summary>
    /// Returns whether it would be possible to fill in *any* digit into the square at row i and column j
    /// without violating the sudoku constraints
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    bool hasPossibilities(int i, int j)
    {
        int boxIndex = coordinateToBoxIndex(i, j);
        uint impossible = rowFrequencies[i] | colFrequencies[j] | boxFrequencies[boxIndex];

        return impossible != 0b111111111 || digits[i * 9 + j] >= 0;
    }

    /// <summary>
    /// Counts the number of digits that would be possible to place in row i and column j, according to the
    /// current state of the sudoku (i.e. not directly violating any sudoku constraints)
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    int possibilityCount(int i, int j)
    {
        int boxIndex = coordinateToBoxIndex(i, j);
        uint impossible = rowFrequencies[i] | colFrequencies[j] | boxFrequencies[boxIndex];

        return (int)(9 - Popcnt.PopCount(impossible));
    }

    /// <summary>
    /// Returns whether it would be possible to fill in *any* digit into the square at row i and column j
    /// without violating the sudoku constraints nor inducing an empty domain in some other square
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <param name="digit"></param>
    /// <returns></returns>
    bool fillDigitForwardChecking(int i, int j, int digit)
    {
        uint digitBin = 1u << digit;
        int boxIndex = coordinateToBoxIndex(i, j);
        if (((rowFrequencies[i] | colFrequencies[j] | boxFrequencies[boxIndex]) & digitBin) != 0)
            return false;

        digits[i * 9 + j] = digit;
        rowFrequencies[i] |= digitBin;
        colFrequencies[j] |= digitBin;
        boxFrequencies[boxIndex] |= digitBin;

        int boxI = (i / 3) * 3;
        int boxJ = (j / 3) * 3;
        for (int idx = 0; idx < 9; idx++)
        {
            if (!hasPossibilities(idx, j) || !hasPossibilities(i, idx) || !hasPossibilities(boxI + idx / 3, boxJ + idx % 3))
            {
                digits[i * 9 + j] = -1;
                rowFrequencies[i] ^= digitBin;
                colFrequencies[j] ^= digitBin;
                boxFrequencies[boxIndex] ^= digitBin;

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Removes the digit at row i and column j to make that square empty
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    void clearDigit(int i, int j)
    {
        int boxIndex = coordinateToBoxIndex(i, j);
        int digit = digits[i * 9 + j];

        digits[i * 9 + j] = -1;

        uint digitBin = 1u << digit;
        rowFrequencies[i] ^= digitBin;
        colFrequencies[j] ^= digitBin;
        boxFrequencies[boxIndex] ^= digitBin;
    }

    /// <summary>
    /// Solves the sudoku
    /// </summary>
    /// <returns></returns>
    public bool Solve(SolveType solveType)
    {
        if (solveType == SolveType.ChronologicalBacktracking)
            return SolveChronologicalBacktracking();
        else if (solveType == SolveType.ForwardChecking)
            return SolveForwardChecking();
        else if (solveType == SolveType.MCV)
            return SolveMCV();

        return false;
    }

    /// <summary>
    /// Solves the sudoku with forward checking and a most constrained variable heuristic
    /// </summary>
    /// <returns></returns>
    bool SolveMCV()
    {
        // First the algorithm makes a list of the empty squares
        List<(int, int)> empty = [];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (digits[i * 9 + j] == -1)
                    empty.Add((i, j));
            }
        }

        // Instead of using recursion to solve the sudoku, everything is done iteratively for performance
        // and the stacks that would normally be made by the compiler are now defined explicitly
        int[] guesses = new int[empty.Count];
        int[] guessPositionIndices = new int[empty.Count];
        int guessIdx = 0;
        while (guessIdx < empty.Count)
        {
            if (guessIdx < 0) // If guessIdx is less than 0, the search is done and no solution has been found
                return false;

            // Find the square with the least possibilities (MCV)
            int bestPositionIndex = -1;
            int bestPossibleCount = 10;
            for (int idx = 0; idx < empty.Count; idx++)
            {
                var (i, j) = empty[idx];
                if (digits[i * 9 + j] >= 0)
                    continue;

                int possibleCount = possibilityCount(i, j);
                if (possibleCount < bestPossibleCount)
                {
                    bestPossibleCount = possibleCount;
                    bestPositionIndex = idx;
                }

                if (bestPossibleCount == 1)
                    break;
            }

            // Try a digit as indicated by guesses[guessIdx], increase it afterwards in case of no success
            // if it is 9, it means that all possibilities have been tried and the algorithm needs to backtrack.
            guessPositionIndices[guessIdx] = bestPositionIndex;
            while (guesses[guessIdx] < 9)
            {
                var (i, j) = empty[guessPositionIndices[guessIdx]];
                int nextGuess = guesses[guessIdx];
                if (fillDigitForwardChecking(i, j, nextGuess)) // Forward checking is used
                {
                    guessIdx++;
                    break;
                }

                guesses[guessIdx]++;
            }

            // If all empty squares have been filled in correctly, the algorithm terminates
            if (guessIdx >= empty.Count)
                return true;

            // guesses[guessIdx] is 9 and the algorithm needs to backtrack
            if (guesses[guessIdx] >= 9)
            {
                guesses[guessIdx] = 0;
                guessIdx--;

                var (i, j) = empty[guessPositionIndices[guessIdx]];
                clearDigit(i, j);

                guesses[guessIdx]++;
            }
        }

        return true;
    }

    bool SolveForwardChecking()
    {
        // First the algorithm makes a list of the empty squares
        List<(int, int)> empty = [];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (digits[i * 9 + j] == -1)
                    empty.Add((i, j));
            }
        }

        // Instead of using recursion to solve the sudoku, everything is done iteratively for performance
        // and the stacks that would normally be made by the compiler are now defined explicitly
        int[] guesses = new int[empty.Count];
        int guessIdx = 0;
        while (guessIdx < empty.Count)
        {
            if (guessIdx < 0) // If guessIdx is less than 0, the search is done and no solution has been found
                return false;

            // Try a digit as indicated by guesses[guessIdx], increase it afterwards in case of no success
            // if it is 9, it means that all possibilities have been tried and the algorithm needs to backtrack.
            while (guesses[guessIdx] < 9)
            {
                var (i, j) = empty[guessIdx];
                int nextGuess = guesses[guessIdx];
                if (fillDigitForwardChecking(i, j, nextGuess)) // Forward checking is used
                {
                    guessIdx++;
                    break;
                }

                guesses[guessIdx]++;
            }

            // If all empty squares have been filled in correctly, the algorithm terminates
            if (guessIdx >= empty.Count)
                return true;

            // guesses[guessIdx] is 9 and the algorithm needs to backtrack
            if (guesses[guessIdx] >= 9)
            {
                guesses[guessIdx] = 0;
                guessIdx--;

                var (i, j) = empty[guessIdx];
                clearDigit(i, j);

                guesses[guessIdx]++;
            }
        }

        return true;
    }

    bool SolveChronologicalBacktracking()
    {
        // First the algorithm makes a list of the empty squares
        List<(int, int)> empty = [];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (digits[i * 9 + j] == -1)
                    empty.Add((i, j));
            }
        }

        // Instead of using recursion to solve the sudoku, everything is done iteratively for performance
        // and the stacks that would normally be made by the compiler are now defined explicitly
        int[] guesses = new int[empty.Count];
        int guessIdx = 0;
        while (guessIdx < empty.Count)
        {
            if (guessIdx < 0)
                return false;

            while (guesses[guessIdx] < 9)
            {
                var (i, j) = empty[guessIdx];
                int nextGuess = guesses[guessIdx];
                if (fillDigit(i, j, nextGuess)) // No forward checking is used
                {
                    guessIdx++;
                    break;
                }

                guesses[guessIdx]++;
            }

            // If all empty squares have been filled in correctly, the algorithm terminates
            if (guessIdx >= empty.Count)
                return true;

            // guesses[guessIdx] is 9 and the algorithm needs to backtrack
            if (guesses[guessIdx] >= 9)
            {
                guesses[guessIdx] = 0;
                guessIdx--;

                var (i, j) = empty[guessIdx];
                clearDigit(i, j);

                guesses[guessIdx]++;
            }
        }

        return true;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 9; i++)
        {
            if (i % 3 == 0)
                sb.Append("+------+------+------+\n");

            for (int j = 0; j < 9; j++)
            {
                char digit = (char)(digits[i * 9 + j] + '1');
                if (j % 3 == 0)
                    sb.Append($"| {digit}");
                else
                    sb.Append($" {digit}");
            }
            sb.Append("|\n");
        }
        sb.Append("+------+------+------+\n");

        return sb.ToString();
    }
}