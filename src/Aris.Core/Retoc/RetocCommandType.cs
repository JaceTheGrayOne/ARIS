namespace Aris.Core.Retoc;

/// <summary>
/// Retoc CLI subcommands.
/// Maps directly to the subcommands supported by the retoc binary.
/// See: https://github.com/trumank/retoc
/// </summary>
public enum RetocCommandType
{
    /// <summary>
    /// Extract manifest data from .utoc file.
    /// </summary>
    Manifest,

    /// <summary>
    /// Display container information.
    /// </summary>
    Info,

    /// <summary>
    /// List files in .utoc (directory index).
    /// </summary>
    List,

    /// <summary>
    /// Validate IO Store container integrity.
    /// </summary>
    Verify,

    /// <summary>
    /// Extract chunks (files) from .utoc.
    /// </summary>
    Unpack,

    /// <summary>
    /// Extract raw chunks from container.
    /// </summary>
    UnpackRaw,

    /// <summary>
    /// Pack directory of raw chunks into container.
    /// </summary>
    PackRaw,

    /// <summary>
    /// Convert assets and shaders from Zen to Legacy format.
    /// </summary>
    ToLegacy,

    /// <summary>
    /// Convert assets and shaders from Legacy to Zen format.
    /// </summary>
    ToZen,

    /// <summary>
    /// Retrieve chunk by index and output to stdout.
    /// </summary>
    Get,

    /// <summary>
    /// Execute dump test operation.
    /// </summary>
    DumpTest,

    /// <summary>
    /// Generate script objects global container from UE reflection data (.jmap).
    /// </summary>
    GenScriptObjects,

    /// <summary>
    /// Output script objects from container.
    /// </summary>
    PrintScriptObjects
}
