namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Reports that the selected driver appeared in no imported session and was replaced by the most
/// frequent driver, so that the views do not stay empty.
/// </summary>
/// <param name="PreviousName">The driver who was selected, after driver merges.</param>
/// <param name="SelectedName">The most frequent driver, now selected and saved.</param>
public sealed record UserNameReplacement(string PreviousName, string SelectedName);
