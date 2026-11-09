namespace LMU_DeltaRacing.Domain.Entities;

/// <summary>
/// Represents what LMU measured on a car during a lap, beyond its times.
/// </summary>
/// <remarks>
/// Fuel, virtual energy and tyre wear are only written when the server shares fuel data
/// (<c>ClientFuelVisible</c>), and even then not for every car: they are often unknown.
/// Fuel and energy are fractions of a full tank, as the result file gives no tank capacity.
/// </remarks>
/// <param name="TopSpeed">The top speed reached during the lap in km/h, when known.</param>
/// <param name="Fuel">The fuel left at the end of the lap, as a fraction of a full tank, when known.</param>
/// <param name="FuelUsed">
/// The fuel used during the lap, as a fraction of a full tank, when known. It is the fuel at the end
/// of the previous lap minus the fuel at the end of this lap, so it is negative on a refuelled lap.
/// </param>
/// <param name="VirtualEnergy">The virtual energy left at the end of the lap, as a fraction, when known.</param>
/// <param name="VirtualEnergyUsed">
/// The virtual energy used during the lap, as a fraction, when known; negative on a refilled lap.
/// </param>
/// <param name="TyreWear">The tread left on each tyre at the end of the lap, when known.</param>
/// <param name="FrontCompound">The front tyre compound ("Soft", "Medium", "Hard", "Wet"), or an empty string.</param>
/// <param name="RearCompound">The rear tyre compound ("Soft", "Medium", "Hard", "Wet"), or an empty string.</param>
public sealed record LapTelemetry(
    double? TopSpeed = null,
    double? Fuel = null,
    double? FuelUsed = null,
    double? VirtualEnergy = null,
    double? VirtualEnergyUsed = null,
    TyreWear? TyreWear = null,
    string FrontCompound = "",
    string RearCompound = "")
{
    /// <summary>
    /// Gets a telemetry where nothing is known.
    /// </summary>
    public static LapTelemetry Empty { get; } = new();
}

/// <summary>
/// Represents the tread left on the four tyres, from 1 (new tyre) to 0.
/// </summary>
/// <param name="FrontLeft">The tread left on the front left tyre.</param>
/// <param name="FrontRight">The tread left on the front right tyre.</param>
/// <param name="RearLeft">The tread left on the rear left tyre.</param>
/// <param name="RearRight">The tread left on the rear right tyre.</param>
public sealed record TyreWear(double FrontLeft, double FrontRight, double RearLeft, double RearRight);
