using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

enum SolveType
{
    ChronologicalBacktracking, ForwardChecking, MCV
}

struct SudokuOrig
{
    // Represents, for each square, the possible digits in a bitmask consisting of the first 9 bits
    uint[] digits = new uint[81];

    // Stores whether a square is fixed (given) or not (empty) at the start of the solve
    bool[,] isFixed = new bool[9, 9];

    public SudokuOrig(string line)
    {
        string[] numbers = line.Split(" ");
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                int number = int.Parse(numbers[9 * i + j]) - 1;
                if (number != -1)
                {
                    digits[i * 9 + j] = 1u << number;
                    isFixed[i, j] = true;
                }
                else
                {
                    digits[i * 9 + j] = 0b111111111;
                    isFixed[i, j] = false;
                }
            }
        }
    }

    /// <summary>
    /// Clears the sudoku after it has been solved
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (!isFixed[i, j])
                    digits[i * 9 + j] = 0b111111111;
            }
        }
    }

    /// <summary>
    /// Solves the sudoku
    /// </summary>
    /// <returns>Whether it succeeded</returns>
    public bool Solve(SolveType solveType)
    {
        bool success = false;
        uint[] sudoku = null;

        if (solveType == SolveType.ChronologicalBacktracking)
            (success, sudoku) = SolveChronologicalBacktracking(digits);
        else if (solveType == SolveType.ForwardChecking)
            (success, sudoku) = SolveForwardChecking(digits);
        else if (solveType == SolveType.MCV)
            (success, sudoku) = SolveMCV(digits);

        if (success && sudoku != null)
            this.digits = sudoku;

        return success;
    }

    /// <summary>
    /// Solves the sudoku recursively with a most constrained variable heuristic and forward checking
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    static (bool, uint[]?) SolveMCV(uint[] input)
    {
        // First it finds the most constrained variable
        int bestPosition = -1;
        int leastPossible = 10;
        for (int position = 0; position < 81; position++)
        {
            int posI = position / 9;
            int posJ = position % 9;

            int count = (int)Popcnt.PopCount(input[posI * 9 + posJ]);
            if (count < leastPossible && count != 1)
            {
                leastPossible = count;
                bestPosition = position;
            }
        }
        if (bestPosition == -1) // If all squares have been filled in already the algorithm terminates
            return (true, input);

        // Otherwise it tries all possibilities for the square it found, which shouldn't be too many because of the MCV
        int i = bestPosition / 9;
        int j = bestPosition % 9;
        if (Popcnt.PopCount(input[bestPosition]) == 1)
            return SolveMCV(input);

        // It goes over all digits
        for (int digit = 0; digit < 9; digit++)
        {
            uint digitBit = 1u << digit;
            if ((input[bestPosition] & digitBit) == 0) // If the digit isn't in the domain, the algorithm skips it
                continue;

            // Otherwise it makes a copy of the current state (input)
            uint[] copy = new uint[81];
            for (int idx = 0; idx < 81; idx++)
                copy[idx] = input[idx];

            // And then checks whether filling in this digit results in an empty domain somewhere else (forward checking)
            // Only one row, column and box need to be checked for this
            bool isPossible = true;
            for (int idx = 0; idx < 9; idx++)
            {
                copy[idx * 9 + j] &= ~digitBit;
                copy[i * 9 + idx] &= ~digitBit;

                int boxI = idx / 3 + (i / 3) * 3;
                int boxJ = idx % 3 + (j / 3) * 3;
                copy[boxI * 9 + boxJ] &= ~digitBit;
                if (copy[idx * 9 + j] == 0 || copy[i * 9 + idx] == 0 || copy[boxI * 9 + boxJ] == 0)
                {
                    isPossible = false;
                    break;
                }
            }

            if (!isPossible) // Skip the digit if it does result in an empty domain
                continue;

            // Otherwise continue recursively
            copy[bestPosition] = digitBit;

            var (success, sudoku) = SolveMCV(copy);
            if (success)
                return (true, sudoku);
        }

        return (false, null);
    }

    /// <summary>
    /// Solves the sudoku with forward checking, no MCV
    /// </summary>
    /// <param name="input">Current state</param>
    /// <param name="position">Position of the next square to fill in</param>
    /// <returns></returns>
    static (bool, uint[]?) SolveForwardChecking(uint[] input, int position = 0)
    {
        if (position == 81) // If the next position is 81, then the sudoku is solved
            return (true, input);

        // Otherwise it tries all possibilities for the square it found
        int i = position / 9;
        int j = position % 9;
        if (Popcnt.PopCount(input[position]) == 1)
            return SolveForwardChecking(input, position + 1);

        // It goes over all digits
        for (int digit = 0; digit < 9; digit++)
        {
            uint digitBit = 1u << digit;
            if ((input[position] & digitBit) == 0) // If the digit isn't in the domain, the algorithm skips it
                continue;

            // Otherwise it makes a copy of the current state (input)
            uint[] copy = new uint[81];
            for (int idx = 0; idx < 81; idx++)
                copy[idx] = input[idx];

            // And then checks whether filling in this digit results in an empty domain somewhere else (forward checking)
            // Only one row, column and box need to be checked for this
            bool isPossible = true;
            for (int idx = 0; idx < 9; idx++)
            {
                copy[idx * 9 + j] &= ~digitBit;
                copy[i * 9 + idx] &= ~digitBit;

                int boxI = idx / 3 + (i / 3) * 3;
                int boxJ = idx % 3 + (j / 3) * 3;
                copy[boxI * 9 + boxJ] &= ~digitBit;
                if (copy[idx * 9 + j] == 0 || copy[i * 9 + idx] == 0 || copy[boxI * 9 + boxJ] == 0)
                {
                    isPossible = false;
                    break;
                }
            }

            if (!isPossible) // Skip the digit if it does result in an empty domain
                continue;

            copy[position] = digitBit;

            // Otherwise continue recursively
            var (success, sudoku) = SolveForwardChecking(copy, position + 1);
            if (success)
                return (true, sudoku);
        }

        return (false, null);
    }

    /// <summary>
    /// Solves the sudoku with just chronological backtracking
    /// </summary>
    /// <param name="input"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    static (bool, uint[]?) SolveChronologicalBacktracking(uint[] input, int position = 0)
    {
        if (position == 81) // If the next position is 81, then the sudoku is solved
            return (true, input);

        // Otherwise it tries all possibilities for the square it found
        int i = position / 9;
        int j = position % 9;
        if (Popcnt.PopCount(input[position]) == 1)
            return SolveChronologicalBacktracking(input, position + 1);

        // It goes over all digits
        for (int digit = 0; digit < 9; digit++)
        {
            uint digitBit = 1u << digit;
            if ((input[position] & digitBit) == 0) // If the digit isn't in the domain, the algorithm skips it
                continue;

            // Otherwise it makes a copy of the current state (input)
            uint[] copy = new uint[81];
            for (int idx = 0; idx < 81; idx++)
                copy[idx] = input[idx];

            // And then checks whether that digit is not present in the row, column or box yet, no forward checking though
            bool isPossible = true;
            for (int idx = 0; idx < 9; idx++)
            {
                int boxI = idx / 3 + (i / 3) * 3;
                int boxJ = idx % 3 + (j / 3) * 3;
                if (copy[idx * 9 + j] == digitBit || copy[i * 9 + idx] == digitBit || copy[boxI * 9 + boxJ] == digitBit)
                {
                    isPossible = false;
                    break;
                }
            }

            if (!isPossible) // Skip the digit if it does result in an empty domain
                continue;

            copy[position] = digitBit;

            // Otherwise continue recursively
            var (success, sudoku) = SolveChronologicalBacktracking(copy, position + 1);
            if (success)
                return (true, sudoku);
        }

        return (false, null);
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
                char digit = ' ';
                if (Popcnt.PopCount(digits[i * 9 + j]) == 1)
                {
                    for (int idx = 0; idx < 9; idx++)
                    {
                        if ((digits[i * 9 + j] & (1u << idx)) != 0)
                        {
                            digit = (char)(idx + '1');
                        }
                    }
                }

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