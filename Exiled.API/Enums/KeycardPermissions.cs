// -----------------------------------------------------------------------
// <copyright file="KeycardPermissions.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Enums
{
    /// <summary>
    /// The types of permissions assigned to keycards.
    /// </summary>
    public enum KeycardPermissions
    {
        /// <summary>
        /// No permissions.
        /// </summary>
        None = 0,

        /// <summary>
        /// Opens checkpoints.
        /// </summary>
        Checkpoints = 1,

        /// <summary>
        /// Opens Exit Gates.
        /// </summary>
        ExitGates = 2,

        /// <summary>
        /// Opens the Intercom door.
        /// </summary>
        Intercom = 4,

        /// <summary>
        /// Opens the Alpha Warhead detonation room on surface.
        /// </summary>
        AlphaWarhead = 8,

        /// <summary>
        /// Opens 914.
        /// </summary>
        ContainmentLevelOne = 16, // 0x0010

        /// <summary>
        /// <see cref="ContainmentLevelOne"/>, <see cref="Checkpoints"/>.
        /// </summary>
        ContainmentLevelTwo = 32, // 0x0020

        /// <summary>
        /// <see cref="ContainmentLevelTwo"/>, <see cref="Intercom"/>, <see cref="AlphaWarhead"/>.
        /// </summary>
        ContainmentLevelThree = 64, // 0x0040

        /// <summary>
        /// <see cref="Checkpoints"/>, Opens Light Containment armory.
        /// </summary>
        ArmoryLevelOne = 128, // 0x0080

        /// <summary>
        /// <see cref="ArmoryLevelOne"/>, <see cref="ExitGates"/>, Opens Heavy Containment armories.
        /// </summary>
        ArmoryLevelTwo = 256, // 0x0100

        /// <summary>
        /// <see cref="ArmoryLevelTwo"/>, <see cref="Intercom"/>, Opens MicroHID room.
        /// </summary>
        ArmoryLevelThree = 512, // 0x0200

        /// <summary>
        /// <see cref="Checkpoints"/>.
        /// </summary>
        ScpOverride = 1024, // 0x0400
    }
}
