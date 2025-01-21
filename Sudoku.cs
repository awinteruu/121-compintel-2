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
    int[] digits = new int[81];
    uint[] rowFrequencies = new uint[9];
    uint[] colFrequencies = new uint[9];
    uint[] boxFrequencies = new uint[9];
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

    int coordinateToBoxIndex(int i, int j)
    {
        return (i / 3) * 3 + (j / 3);
    }

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

    bool hasPossibilities(int i, int j)
    {
        int boxIndex = coordinateToBoxIndex(i, j);
        uint impossible = rowFrequencies[i] | colFrequencies[j] | boxFrequencies[boxIndex];

        return impossible != 0b111111111 || digits[i * 9 + j] >= 0;
    }

    int possibilityCount(int i, int j)
    {
        int boxIndex = coordinateToBoxIndex(i, j);
        uint impossible = rowFrequencies[i] | colFrequencies[j] | boxFrequencies[boxIndex];

        return (int)(9 - Popcnt.PopCount(impossible));
    }

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

    public bool Solve()
    {
        return SolveForwardCheckingMCV();
    }

    public bool SolveForwardCheckingMCV()
    {
        List<(int, int)> empty = [];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (digits[i * 9 + j] == -1)
                    empty.Add((i, j));
            }
        }

        int[] guesses = new int[empty.Count];
        int[] guessPositionIndices = new int[empty.Count];
        int guessIdx = 0;
        while (guessIdx < empty.Count)
        {
            if (guessIdx < 0)
                return false;

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

            guessPositionIndices[guessIdx] = bestPositionIndex;
            while (guesses[guessIdx] < 9)
            {
                var (i, j) = empty[guessPositionIndices[guessIdx]];
                int nextGuess = guesses[guessIdx];
                if (fillDigitForwardChecking(i, j, nextGuess))
                {
                    guessIdx++;
                    break;
                }

                guesses[guessIdx]++;
            }

            if (guessIdx >= empty.Count)
                return true;

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

    public bool SolveForwardChecking()
    {
        List<(int, int)> empty = [];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (digits[i * 9 + j] == -1)
                    empty.Add((i, j));
            }
        }

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
                if (fillDigitForwardChecking(i, j, nextGuess))
                {
                    guessIdx++;
                    break;
                }

                guesses[guessIdx]++;
            }

            if (guessIdx >= empty.Count)
                return true;

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

    public bool SolveChronologicalBacktracking()
    {
        List<(int, int)> empty = [];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (digits[i * 9 + j] == -1)
                    empty.Add((i, j));
            }
        }

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
                if (fillDigit(i, j, nextGuess))
                {
                    guessIdx++;
                    break;
                }

                guesses[guessIdx]++;
            }

            if (guessIdx >= empty.Count)
                return true;

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
