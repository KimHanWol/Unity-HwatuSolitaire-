using System.Collections.Generic;
using HwatuSolitaire.Core;
using Xunit;

public class PathFinderTests
{
    // 판을 글자로 적는다: '.' 빈칸, '#' 막힌 칸, 'a'~'l' 1~12월 패(그 달의 첫 패), 'A'~'L' 그 달의 둘째 패.
    static Board Parse(params string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        var b = new Board(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = rows[y][x];
                if (c == '.') b[x, y] = Board.Empty;
                else if (c == '#') b[x, y] = Board.Block;
                else if (char.IsLower(c)) b[x, y] = (c - 'a') * 4;
                else b[x, y] = (c - 'A') * 4 + 1;
            }
        return b;
    }

    [Fact]
    public void AdjacentSameMonthConnects()
    {
        var b = Parse("aA", "bc");
        Assert.NotNull(PathFinder.FindPath(b, new Cell(0, 0), new Cell(1, 0)));
    }

    [Fact]
    public void DifferentMonthNeverMatches()
    {
        var b = Parse("ab");
        Assert.Null(PathFinder.FindPath(b, new Cell(0, 0), new Cell(1, 0)));
    }

    [Fact]
    public void PathMayUseOuterBorder()
    {
        // 가운데 줄이 꽉 막혀 있어도 판 바깥으로 돌아 이어진다 (위로 나가서 꺾고 꺾어 내려오기).
        var b = Parse(
            "abcA",
            "defg");
        var path = PathFinder.FindPath(b, new Cell(0, 0), new Cell(3, 0));
        Assert.NotNull(path);
        Assert.Equal(4, path.Count); // 시작, 꺾임 2번, 끝
        Assert.Equal(-1, path[1].Y);
    }

    [Fact]
    public void ThreeTurnsIsTooMany()
    {
        // a 에서 A 로 가려면 꺾임이 3번 필요하다.
        var b = Parse(
            "bbbbb",
            "a..#b",
            "bb#.b",
            "b#..A",
            "bbbbb");
        Assert.Null(PathFinder.FindPath(b, new Cell(0, 1), new Cell(4, 3)));
    }

    [Fact]
    public void TwoTurnsInsideBoard()
    {
        var b = Parse(
            "bbbbb",
            "a...b",
            "bbb.b",
            "bbb.A",
            "bbbbb");
        var path = PathFinder.FindPath(b, new Cell(0, 1), new Cell(4, 3));
        Assert.NotNull(path);
        Assert.True(path.Count <= 4);
    }

    [Fact]
    public void BlocksStopPaths()
    {
        var b = Parse(
            "###",
            "#a#",
            "#A#",
            "###");
        Assert.NotNull(PathFinder.FindPath(b, new Cell(1, 1), new Cell(1, 2)));
        var c = Parse(
            "#####",
            "#a#A#",
            "#####");
        Assert.Null(PathFinder.FindPath(c, new Cell(1, 1), new Cell(3, 1)));
    }
}

public class GravityTests
{
    [Fact]
    public void DownCompactsColumnsAroundBlocks()
    {
        var b = new Board(1, 5, new[] { 0, Board.Empty, Board.Block, 4, Board.Empty });
        var slides = Gravity.Apply(b, GravityMode.Down);
        Assert.Equal(new[] { Board.Empty, 0, Board.Block, Board.Empty, 4 }, b.ToArray());
        Assert.Equal(2, slides.Count);
    }

    [Fact]
    public void SplitOutMovesHalvesApart()
    {
        var b = new Board(4, 1, new[] { Board.Empty, 0, 4, Board.Empty });
        Gravity.Apply(b, GravityMode.SplitOut);
        Assert.Equal(new[] { 0, Board.Empty, Board.Empty, 4 }, b.ToArray());
    }

    [Fact]
    public void SplitInGathersToCenter()
    {
        var b = new Board(4, 1, new[] { 0, Board.Empty, Board.Empty, 4 });
        Gravity.Apply(b, GravityMode.SplitIn);
        Assert.Equal(new[] { Board.Empty, 0, 4, Board.Empty }, b.ToArray());
    }
}

public class StageTests
{
    [Fact]
    public void EveryPlanHasEvenCardsAndEachMonthEven()
    {
        for (int n = 1; n <= StageGenerator.StageCount; n++)
        {
            var plan = StageGenerator.MakePlan(n);
            var board = StageGenerator.Deal(plan, new Rng(n));
            Assert.True(board.CardCount % 2 == 0, "stage " + n);
            for (int m = 0; m < Board.Months; m++) Assert.True(board.CountMonth(m) % 2 == 0, "stage " + n + " month " + m);
        }
    }

    [Fact]
    public void BuiltStagesAreSolvable()
    {
        foreach (int n in new[] { 1, 26, 60, 110, 175, 240, 300 })
        {
            var stage = StageGenerator.Build(n);
            Assert.NotNull(Solver.Solve(stage.CreateBoard(), stage.Gravity, 1, 60));
        }
    }

    [Fact]
    public void StarTimesAreOrdered()
    {
        var stage = StageGenerator.Build(40);
        StarTimer.Assign(stage);
        Assert.True(stage.star3 < stage.star2);
        Assert.True(stage.star2 < stage.timeLimit);
        Assert.Equal(3, stage.StarsFor(stage.star3));
        Assert.Equal(2, stage.StarsFor(stage.star3 + 1));
    }

    [Fact]
    public void ShuffleKeepsCardsAndFindsMove()
    {
        var stage = StageGenerator.Build(80);
        var board = stage.CreateBoard();
        var before = new List<int>(board.ToArray());
        before.Sort();
        Rules.Shuffle(board, stage.Gravity, new Rng(5));
        var after = new List<int>(board.ToArray());
        after.Sort();
        Assert.Equal(before, after);
        Assert.True(Rules.HasMove(board));
    }
}
