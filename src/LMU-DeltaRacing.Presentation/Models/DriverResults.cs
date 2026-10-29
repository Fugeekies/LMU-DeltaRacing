using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Reads a driver result the same way in every view.
/// </summary>
public static class DriverResults
{
    /// <summary>
    /// Returns the rank of a driver within their class. The overall position keeps the same order
    /// between drivers of one class, so it is a valid fallback when the class position is missing.
    /// </summary>
    public static int? ClassRank(this Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        return driver.ClassPosition ?? driver.Position;
    }

    /// <summary>
    /// Returns the car shown for a driver: the model, else the car number ("#7"), else "—".
    /// </summary>
    public static string CarName(this Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        return !string.IsNullOrWhiteSpace(driver.CarType)
            ? driver.CarType.Trim()
            : string.IsNullOrWhiteSpace(driver.CarNumber)
                ? DisplayFormat.Missing
                : $"#{driver.CarNumber.Trim()}";
    }

    /// <summary>
    /// Returns the WEC category of the driver's car.
    /// </summary>
    public static string Category(this Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        return CarCategory.FromCarClass(driver.CarClass);
    }
}
