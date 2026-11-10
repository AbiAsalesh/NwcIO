using NwcIO.Model.TimeLiner;
using Xunit;

namespace NwcIO.Tests;

public class TimeLinerTests
{
    [Fact]
    public void TimeLinerSchedule_ComputesDatesAndProgress()
    {
        var baseDate = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

        var task1 = new NwcTimeLinerTask("T1", "Excavation", baseDate, baseDate.AddDays(10), NwcTimeLinerTaskType.Construct)
        {
            PlannedCost = 50000m,
        };
        task1.AttachedNodeIds.Add(101);

        var task2 = new NwcTimeLinerTask("T2", "Foundations", baseDate.AddDays(10), baseDate.AddDays(25), NwcTimeLinerTaskType.Construct)
        {
            PlannedCost = 120000m,
        };
        task2.AttachedNodeIds.Add(102);

        var schedule = new NwcTimeLinerSchedule([task1, task2]);

        Assert.Equal(baseDate, schedule.OverallStart);
        Assert.Equal(baseDate.AddDays(25), schedule.OverallEnd);
        Assert.Equal(170000m, schedule.TotalPlannedCost);

        // Simulation query on day 5 (during task 1)
        var midDate = baseDate.AddDays(5);
        var active = schedule.GetActiveTasksAt(midDate);
        var activeTask = Assert.Single(active);
        Assert.Equal("Excavation", activeTask.Name);

        // Simulation states for node 101 and 102
        Assert.Equal(NwcTimeLinerSimulationState.InProgress, schedule.GetNodeStateAt(101, midDate));
        Assert.Equal(NwcTimeLinerSimulationState.NotStarted, schedule.GetNodeStateAt(102, midDate));

        // Simulation query on day 15 (after task 1, during task 2)
        var laterDate = baseDate.AddDays(15);
        Assert.Equal(NwcTimeLinerSimulationState.Completed, schedule.GetNodeStateAt(101, laterDate));
        Assert.Equal(NwcTimeLinerSimulationState.InProgress, schedule.GetNodeStateAt(102, laterDate));

        // Simulation overall progress: 5 days / 25 days = 20%
        double progress = schedule.GetSimulationProgressAt(midDate);
        Assert.Equal(0.2, progress, precision: 2);
    }

    [Fact]
    public void TimeLinerSchedule_RoundtripsThroughCsv()
    {
        var baseDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new NwcTimeLinerSchedule(
        [
            new NwcTimeLinerTask("1", "Demolish Old Shed", baseDate, baseDate.AddDays(3), NwcTimeLinerTaskType.Demolish)
            {
                PlannedCost = 15000m,
                ActualCost = 14200m,
                AttachedSelectionSetName = "ShedElements"
            },
            new NwcTimeLinerTask("2", "Erect Steel Frame", baseDate.AddDays(4), baseDate.AddDays(20), NwcTimeLinerTaskType.Construct)
            {
                PlannedCost = 350000m,
                AttachedSelectionSetName = "SteelStructure"
            }
        ]);

        using var ms = new MemoryStream();
        NwcTimeLinerCsvSerializer.Write(schedule, ms);
        ms.Position = 0;

        var loaded = NwcTimeLinerCsvSerializer.Read(ms);
        Assert.Equal(2, loaded.Tasks.Count);

        var t1 = loaded.Tasks[0];
        Assert.Equal("1", t1.Id);
        Assert.Equal("Demolish Old Shed", t1.Name);
        Assert.Equal(NwcTimeLinerTaskType.Demolish, t1.TaskType);
        Assert.Equal(15000m, t1.PlannedCost);
        Assert.Equal(14200m, t1.ActualCost);
        Assert.Equal("ShedElements", t1.AttachedSelectionSetName);

        var t2 = loaded.Tasks[1];
        Assert.Equal("2", t2.Id);
        Assert.Equal("Erect Steel Frame", t2.Name);
        Assert.Equal(NwcTimeLinerTaskType.Construct, t2.TaskType);
        Assert.Equal(350000m, t2.PlannedCost);
    }

    [Fact]
    public void TimeLinerSchedule_RoundtripsThroughNwcFile()
    {
        var start = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new NwcTimeLinerSchedule(
        [
            new NwcTimeLinerTask("A1", "Groundworks", start, start.AddDays(14)) { PlannedCost = 75000m }
        ]);

        var doc = new NwcDocumentBuilder()
            .SetFileInfo("ScheduleModel.nwc")
            .SetTimeLiner(schedule)
            .Build();

        using var ms = new MemoryStream();
        doc.Save(ms);
        ms.Position = 0;

        var reopened = NwcDocument.Open(ms);
        Assert.NotNull(reopened.TimeLiner);
        var task = Assert.Single(reopened.TimeLiner.Tasks);
        Assert.Equal("A1", task.Id);
        Assert.Equal("Groundworks", task.Name);
        Assert.Equal(75000m, task.PlannedCost);
    }
}
