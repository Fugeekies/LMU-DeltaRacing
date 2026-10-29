using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies how classes, finish statuses and cars are read for every view.
/// </summary>
[TestClass]
public sealed class DriverResultsTests
{
    /// <summary>
    /// Verifies that the LMU classes of the fixtures and their full names map to WEC categories.
    /// </summary>
    [TestMethod]
    [DataRow("Hyper", "Hypercar")]
    [DataRow("Hypercar", "Hypercar")]
    [DataRow("LMP2", "LMP2")]
    [DataRow("P2", "LMP2")]
    [DataRow("LMP3", "LMP3")]
    [DataRow("p3", "LMP3")]
    [DataRow("GTE", "LMGTE")]
    [DataRow("LMGTE", "LMGTE")]
    [DataRow("GT3", "LMGT3")]
    [DataRow(" LMGT3 ", "LMGT3")]
    [DataRow("Formula", "Formula")]
    [DataRow("", "—")]
    [DataRow(null, "—")]
    public void Car_classes_map_to_wec_categories(string? carClass, string expected)
    {
        CarCategory.FromCarClass(carClass).Should().Be(expected);
    }

    /// <summary>
    /// Verifies that categories are ordered from the fastest class, unknown classes last.
    /// </summary>
    [TestMethod]
    public void Categories_are_ordered_from_the_fastest_class()
    {
        CarCategory.Known.Select(CarCategory.Order).Should().Equal(0, 1, 2, 3, 4);
        CarCategory.Order("Formula").Should().Be(CarCategory.Known.Count);
    }

    /// <summary>
    /// Verifies that the finish status wins over the class position.
    /// </summary>
    [TestMethod]
    [DataRow("Finished Normally", 1, HomeResultKind.Win, "P1")]
    [DataRow("Finished Normally", 3, HomeResultKind.Podium, "P3")]
    [DataRow("Finished Normally", 4, HomeResultKind.Classified, "P4")]
    [DataRow("", null, HomeResultKind.Classified, "—")]
    [DataRow("DNF", 1, HomeResultKind.Retired, "DNF")]
    [DataRow(" dq ", 2, HomeResultKind.Disqualified, "DQ")]
    [DataRow("None", 1, HomeResultKind.Unclassified, "NC")]
    public void Finish_status_wins_over_the_class_position(string status, int? classPosition, HomeResultKind kind, string label)
    {
        RaceResults.GetKind(status, classPosition).Should().Be(kind);
        RaceResults.GetLabel(kind, classPosition).Should().Be(label);
    }

    /// <summary>
    /// Verifies that the class position falls back to the overall position.
    /// </summary>
    [TestMethod]
    public void Class_rank_falls_back_to_the_overall_position()
    {
        CreateDriver(position: 5, classPosition: 2).ClassRank().Should().Be(2);
        CreateDriver(position: 5, classPosition: null).ClassRank().Should().Be(5);
        CreateDriver(position: null, classPosition: null).ClassRank().Should().BeNull();
    }

    /// <summary>
    /// Verifies that the car is the model, else the car number, else a dash.
    /// </summary>
    [TestMethod]
    public void Car_name_is_the_model_then_the_number()
    {
        CreateDriver(carType: " Porsche 911 GT3 R ", carNumber: "7").CarName().Should().Be("Porsche 911 GT3 R");
        CreateDriver(carType: "", carNumber: "7").CarName().Should().Be("#7");
        CreateDriver(carType: " ", carNumber: "").CarName().Should().Be("—");
    }

    private static Driver CreateDriver(
        int? position = 1,
        int? classPosition = 1,
        string carType = "Car",
        string carNumber = "1") =>
        new("Driver A", carNumber, "GT3", "Team", position, [], carType, classPosition: classPosition);
}
