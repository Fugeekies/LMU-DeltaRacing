using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Application.Abstractions;

/// <summary>
/// Parses an LMU result XML stream into a domain session.
/// </summary>
public interface IResultXmlParser
{
    /// <summary>
    /// Parses a result XML document.
    /// </summary>
    /// <param name="stream">The readable XML stream.</param>
    /// <returns>The session represented by the document.</returns>
    Session Parse(Stream stream);
}
