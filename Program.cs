// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program()
{
    static void Main()
    {
        Console.WriteLine("Welcome to this sudoku-solver");
        Console.WriteLine("Input the location of a text file containing some sudokus (relative to the executable path), or input a puzzle description directly " +
            "(consisting of digits, with or without spaces and with dots or zeros to represent unknown cells)");

        // To change the version of the sudoku that is used, the names of the classes 'Sudoku' and 'SudokuOrig' should be swapped,
        // although it would have been easier to use inheritance, this turned out to slow down the solver. Which is why we opted for this method.
        while (true)
        {
            string input = Console.ReadLine()!;
            if (input == "exit")
                break;

            var puzzles = ReadFile(input, out bool success);
            if (success)
            {
                foreach (var sudoku in puzzles)
                    Solve(sudoku, SolveType.MCV);
            }
            else
            {
                string defaultFormat = ParseFormat(input);
                if (defaultFormat == "")
                {
                    Console.WriteLine($"Incorrect sudoku format or invalid file location");
                    continue;
                }
                Solve(new Sudoku(defaultFormat), SolveType.MCV);
            }
        }

        //Solve(ReadFile("puzzels/Sudoku_puzzels_5.txt", out _)[0]);
        //BenchmarkTime(ReadFile("puzzels/Sudoku_puzzels_5.txt", out _), 50000, SolveType.MCV);
    }

    /// <summary>
    /// Tries to solve the sudoku and reports its results
    /// </summary>
    /// <param name="sudoku"></param>
    static void Solve(Sudoku sudoku, SolveType solveType)
    {
        bool success = sudoku.Solve(solveType);
        if (success)
        {
            Console.WriteLine("Sudoku was solved successfully: ");
            Console.WriteLine(sudoku);
        }
        else
        {
            Console.WriteLine("Failed to solve sudoku!");
            sudoku.Clear();
            Console.WriteLine(sudoku);
        }
    }

    /// <summary>
    /// Benchmarks a list of sudokus by trying different parameters
    /// </summary>
    /// <param name="sudokuList"></param>
    static void BenchmarkTime(List<Sudoku> sudokuList, int maxIterations, SolveType solveType)
    {
        int batchSize = 20000;
        int batchCount = maxIterations / batchSize;

        Stopwatch stopwatch = new Stopwatch();

        List<double> times = [];
        bool success = true;
        for (int batch = 0; batch < batchCount; batch++)
        {
            stopwatch.Restart();

            for (int i = 0; i < batchSize; i++)
            {
                foreach (Sudoku sudoku in sudokuList)
                {
                    sudoku.Clear();
                    bool result = sudoku.Solve(solveType);
                    if (!result)
                    {
                        success = false;
                        break;
                    }
                }

                if (!success)
                    break;
            }

            stopwatch.Stop();
            times.Add(stopwatch.Elapsed.TotalNanoseconds / (double)batchSize / (double)sudokuList.Count);

            if (!success)
            {
                Console.Write("FAIL");
                return;
            }
        }

        double averageTime = 0;
        foreach (double time in times)
            averageTime += time;

        averageTime /= batchCount;
        double stdTime = 0;
        foreach (double time in times)
            stdTime += (time - averageTime) * (time - averageTime);

        stdTime = Math.Sqrt(stdTime / batchCount);
        double confTime = stdTime / Math.Sqrt(batchCount) * 1.96; //95% confidence interval

        Console.Write($"{(int)averageTime}+-{(int)confTime} ");
        Console.Write($"{(int)averageTime} ");
    }

    /// <summary>
    /// Reads in a file and converts it to a list of sudokus
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="success"></param>
    /// <returns></returns>
    static List<Sudoku> ReadFile(string fileName, out bool success)
    {
        List<Sudoku> sudokuList = [];
        try
        {
            using StreamReader sr = new StreamReader(fileName);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine()!.Trim();
                if (Regex.Match(line, @"^[0-9.]").Success)
                {
                    sudokuList.Add(new Sudoku(ParseFormat(line)));
                }
            }
        }
        catch
        {
            success = false;
            return [];
        }

        success = true;
        return sudokuList;
    }

    /// <summary>
    /// Parses a format into the default format
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    static string ParseFormat(string str)
    {
        str = str.Trim();
        if (!Regex.Match(str, @"^[0-9.]").Success)
            return "";

        if (str.Length == 81 * 2 - 1)
            return str;

        StringBuilder result = new StringBuilder();
        foreach (char c in str)
        {
            if (c == ' ')
                continue;
            else if (c == '.' || c == '0')
                result.Append('0');
            else
                result.Append(c);

            result.Append(' ');
        }
        result.Remove(result.Length - 1, 1);

        string resultString = result.ToString();
        if (resultString.Length != 81 * 2 - 1)
            return "";

        return resultString;
    }
}
