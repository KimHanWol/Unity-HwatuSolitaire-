using System.Collections.Generic;
using HwatuSolitaire.Core;
using Xunit;

public class FreeRuleTests
{
    static Board Row(params int[] cards)
    {
        var tiles = new List<Tile>();
        for (int i = 0; i < cards.Length; i++) tiles.Add(new Tile(i * 2, 0, 0));
        return new Board(new Layout(tiles), cards);
    }

    [Fact]
    public void SideNeighboursDoNotBlock()
    {
        // 타일 매치 방식: 옆에 패가 붙어 있어도 위만 비어 있으면 열린 패
        var b = Row(0, 4, 8, 1);
        for (int i = 0; i < 4; i++) Assert.True(b.Free(i));
        Assert.True(Rules.CanMatch(b, 0, 3));
        Assert.True(Rules.CanMatch(b, 1, 1) == false);
        Assert.False(Rules.CanMatch(b, 0, 1));
    }

    [Fact]
    public void CoveredTileIsNotFree()
    {
        // 아래 한 장, 반 칸 어긋나 위에 한 장
        var layout = new Layout(new[] { new Tile(0, 0, 0), new Tile(1, 1, 1) });
        var b = new Board(layout, new[] { 0, 1 });
        Assert.False(b.Exposed(0));
        Assert.False(b.Free(0));
        Assert.True(b.Free(1));
        b[1] = Board.Empty;
        Assert.True(b.Free(0));
    }
}

public class DealTests
{
    [Fact]
    public void DealtBoardsAlwaysClearInDealOrder()
    {
        foreach (var name in Layouts.Names)
        {
            var tiles = Layouts.Build(name, 6, 5, 3);
            if (tiles.Count % 2 == 1) tiles.RemoveAt(tiles.Count - 1);
            var layout = new Layout(tiles);
            var slots = new List<int>();
            for (int i = 0; i < layout.Count; i++) slots.Add(i);
            var cards = new List<int>();
            for (int i = 0; i < layout.Count; i++) cards.Add((i / 2 % 12) * 4 + i % 2);
            int[] dealt = null;
            var rng = new Rng(1);
            for (int t = 0; t < 50 && dealt == null; t++) dealt = Rules.Deal(layout, slots, cards, rng);
            Assert.True(dealt != null, name);
            Assert.NotNull(Solver.Solve(new Board(layout, dealt), 5, 200));
        }
    }

    [Fact]
    public void ShuffleKeepsCardsAndStaysSolvable()
    {
        var stage = StageGenerator.Build(120);
        var board = stage.CreateBoard();
        // 몇 짝 지운 뒤 섞기
        for (int i = 0; i < 5; i++)
        {
            var m = Rules.FindMoves(board)[0];
            Rules.Remove(board, m.A, m.B);
        }
        var before = new List<int>(board.ToArray());
        before.Sort();
        Rules.Shuffle(board, new Rng(9));
        var after = new List<int>(board.ToArray());
        after.Sort();
        Assert.Equal(before, after);
        Assert.NotNull(Solver.Solve(board, 3, 200));
    }
}

public class StuckShuffleTests
{
    [Fact]
    public void ShuffleSpreadsTwoStackedCardsOntoFloor()
    {
        // 두 장이 바로 포개져 남으면 위 한 장만 열려 짝을 지을 수 없다 → 바닥 자리로 펼친다.
        var layout = new Layout(new[] { new Tile(0, 0, 0), new Tile(0, 0, 1), new Tile(4, 0, 0) });
        var b = new Board(layout, new[] { 0, 1, Board.Empty });
        Assert.False(Rules.HasMove(b));
        var move = Rules.Shuffle(b, new Rng(3));
        Assert.True(Rules.HasMove(b));
        Assert.Equal(-1, move[2]);
    }
}

public class StageTests
{
    [Fact]
    public void EveryPlanFitsScreenAndIsEven()
    {
        for (int n = 1; n <= StageGenerator.StageCount; n++)
        {
            var plan = StageGenerator.MakePlan(n);
            Assert.True(plan.Tiles.Count % 2 == 0, "stage " + n);
            var (w, h) = Layouts.Footprint(plan.Tiles);
            Assert.True(w <= StageGenerator.MaxColumns && h <= StageGenerator.MaxRows, "stage " + n);
        }
    }

    [Fact]
    public void BuiltStagesAreSolvableAndMonthsEven()
    {
        foreach (int n in new[] { 1, 26, 60, 110, 175, 240, 300 })
        {
            var stage = StageGenerator.Build(n);
            var board = stage.CreateBoard();
            for (int m = 0; m < Board.Months; m++) Assert.True(board.CountMonth(m) % 2 == 0);
            Assert.NotNull(Solver.Solve(board, 1, 200));
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
    public void LayoutRoundTrips()
    {
        var stage = StageGenerator.Build(200);
        var again = Layout.FromFlat(stage.tiles);
        Assert.Equal(stage.Layout.Count, again.Count);
    }
}
