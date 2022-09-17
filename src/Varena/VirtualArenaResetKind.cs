// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Varena;

/// <summary>
/// Type of reset for <see cref="VirtualArena.Reset(VirtualArenaResetKind)"/>.
/// </summary>
public enum VirtualArenaResetKind {

    /// <summary>
    /// All committed bytes are uncommitted.
    /// </summary>
    Default,

    /// <summary>
    /// Keep all committed bytes.
    /// </summary>
    KeepAllCommitted,

    /// <summary>
    /// Keep minimal committed bytes which is the committed memory page size multiplier.
    /// </summary>
    KeepMinimalCommitted,
}