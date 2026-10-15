using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HwatuSolitaire.Core;

/// <summary>
/// 스테이지 300개를 만들고(끝까지 풀리는지 확인), 모의 풀이로 별 기준 시간을 정해 stages.json 으로 쓴다.
///   dotnet run --project Tools/StageTool -- build      → Assets/Resources/Stages/stages.json + Tools/StageTool/report.tsv
///   dotnet run --project Tools/StageTool -- validate   → json 의 모든 판이 풀리는지 다시 확인
/// </summary>
static class Program
{
    const string JsonPath = "Assets/Resources/Stages/stages.json";
    const string ReportPath = "Tools/StageTool/report.tsv";
    const int Version = 2; // 2: 솔리테어(상하이) 판

    static int Main(string[] args)
    {
        string root = FindRoot();
        string cmd = args.Length > 0 ? args[0] : "build";
        if (cmd == "build") return Build(root);
        if (cmd == "validate") return Validate(root);
        if (cmd == "plan") return ShowPlan(int.Parse(args[1]));
        Console.Error.WriteLine("usage: build | validate");
        return 1;
    }

    static string FindRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
        if (dir == null) throw new Exception("Unity 프로젝트 폴더(Assets)를 찾지 못했습니다.");
        return dir.FullName;
    }

    static int Build(string root)
    {
        var stages = new StageDef[StageGenerator.StageCount];
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Parallel.For(1, StageGenerator.StageCount + 1, n =>
        {
            var s = StageGenerator.Build(n);
            StarTimer.Assign(s);
            stages[n - 1] = s;
        });
        Console.WriteLine("built " + stages.Length + " stages in " + watch.Elapsed.TotalSeconds.ToString("0.0") + "s");

        string json = ToJson(stages);
        string path = Path.Combine(root, JsonPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json, new UTF8Encoding(false));
        Console.WriteLine("wrote " + JsonPath + " (" + json.Length / 1024 + " KB)");

        var report = new StringBuilder("stage\tchapter\tshape\tsize\tlayers\tcards\tmonths\tfreeAtStart\tsim\tstar3\tstar2\tlimit\tstack\n");
        foreach (var s in stages)
        {
            var plan = StageGenerator.MakePlan(s.number);
            var board = s.CreateBoard();
            var (fw, fh) = Layouts.Footprint(new System.Collections.Generic.List<Tile>(s.Layout.Tiles));
            int months = s.cells.Where(v => v >= 0).Select(Board.Month).Distinct().Count();
            int free = Enumerable.Range(0, board.Count).Count(board.Free);
            report.Append(s.number).Append('\t').Append(plan.Chapter + 1).Append('\t').Append(s.shape).Append('\t')
                .Append(fw.ToString(CultureInfo.InvariantCulture)).Append('x').Append(fh.ToString(CultureInfo.InvariantCulture)).Append('\t')
                .Append(s.Layout.MaxZ + 1).Append('\t').Append(board.CardCount).Append('\t').Append(months).Append('\t').Append(free).Append('\t')
                .Append(s.simSeconds.ToString(CultureInfo.InvariantCulture)).Append('\t')
                .Append(s.star3).Append('\t').Append(s.star2).Append('\t').Append(s.timeLimit).Append('\t')
                .Append(s.stackWindow.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
        File.WriteAllText(Path.Combine(root, ReportPath), report.ToString(), new UTF8Encoding(false));
        Console.WriteLine("wrote " + ReportPath);
        return 0;
    }

    /// <summary>스테이지 하나의 판 모양을 층별로 찍어 본다 (디버깅용).</summary>
    static int ShowPlan(int number)
    {
        var plan = StageGenerator.MakePlan(number);
        Console.WriteLine("stage " + number + " shape=" + plan.Shape + " tiles=" + plan.Tiles.Count + " layers=" + plan.Layers + " months=" + plan.MonthCount);
        foreach (var g in plan.Tiles.GroupBy(t => t.Z))
            Console.WriteLine(" z" + g.Key + ": " + string.Join(" ", g.Select(t => t.X + "," + t.Y)));
        return 0;
    }

    static int Validate(string root)
    {
        string text = File.ReadAllText(Path.Combine(root, JsonPath));
        var doc = System.Text.Json.JsonDocument.Parse(text);
        int bad = 0, count = 0;
        foreach (var e in doc.RootElement.GetProperty("stages").EnumerateArray())
        {
            count++;
            var s = new StageDef
            {
                number = e.GetProperty("number").GetInt32(),
                tiles = e.GetProperty("tiles").EnumerateArray().Select(x => x.GetInt32()).ToArray(),
                cells = e.GetProperty("cells").EnumerateArray().Select(x => x.GetInt32()).ToArray(),
            };
            var board = s.CreateBoard();
            bool evenMonths = Enumerable.Range(0, Board.Months).All(m => board.CountMonth(m) % 2 == 0);
            bool solvable = Solver.Solve(board, 7, 400) != null;
            if (!evenMonths || !solvable)
            {
                bad++;
                Console.WriteLine("stage " + s.number + ": evenMonths=" + evenMonths + " solvable=" + solvable);
            }
        }
        Console.WriteLine(count + " stages, " + bad + " problems");
        return bad == 0 && count == StageGenerator.StageCount ? 0 : 1;
    }

    static string ToJson(StageDef[] stages)
    {
        var sb = new StringBuilder();
        sb.Append("{\"version\":").Append(Version).Append(",\"stages\":[\n");
        for (int i = 0; i < stages.Length; i++)
        {
            var s = stages[i];
            sb.Append("{\"number\":").Append(s.number)
              .Append(",\"shape\":\"").Append(s.shape).Append('"')
              .Append(",\"star3\":").Append(s.star3)
              .Append(",\"star2\":").Append(s.star2)
              .Append(",\"timeLimit\":").Append(s.timeLimit)
              .Append(",\"stackWindow\":").Append(s.stackWindow.ToString(CultureInfo.InvariantCulture))
              .Append(",\"simSeconds\":").Append(s.simSeconds.ToString(CultureInfo.InvariantCulture))
              .Append(",\"tiles\":[").Append(string.Join(",", s.tiles)).Append("]")
              .Append(",\"cells\":[").Append(string.Join(",", s.cells)).Append("]}");
            sb.Append(i < stages.Length - 1 ? ",\n" : "\n");
        }
        sb.Append("]}\n");
        return sb.ToString();
    }
}
