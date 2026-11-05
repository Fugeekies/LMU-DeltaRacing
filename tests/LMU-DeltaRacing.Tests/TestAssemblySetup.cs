using LMU_DeltaRacing.Presentation.Localization;

namespace LMU_DeltaRacing.Tests;

/// <summary>
/// Runs the tests with the French interface, the language of reference of the expected texts,
/// whatever the language of the machine (a CI runner is in English).
/// </summary>
[TestClass]
public static class TestAssemblySetup
{
    /// <summary>
    /// Shows the French texts in every test thread that does not choose its own culture.
    /// </summary>
    [AssemblyInitialize]
    public static void UseFrenchInterface(TestContext context)
    {
        _ = context;
        UiLanguage.Apply(UiLanguage.French);
    }
}
