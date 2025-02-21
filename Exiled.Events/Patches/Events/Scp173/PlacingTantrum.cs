// -----------------------------------------------------------------------
// <copyright file="PlacingTantrum.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Player
{
#pragma warning disable SA1118
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using Exiled.Events.EventArgs;

    using HarmonyLib;

    using Mirror;

    using NorthwoodLib.Pools;

    using UnityEngine;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches <see cref="PlayableScps.Scp173.ServerDoTantrum"/>.
    /// Adds the <see cref="Handlers.Scp173.PlacingTantrum"/> event.
    /// </summary>
    [HarmonyPatch(typeof(PlayableScps.Scp173), nameof(PlayableScps.Scp173.ServerDoTantrum))]
    internal static class PlacingTantrum
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

            var returnLabel = newInstructions[newInstructions.Count - 1].labels[0];

            var ev = generator.DeclareLocal(typeof(PlacingTantrumEventArgs));

            var offset = 1;

            var index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Call &&
            (MethodInfo)instruction.operand == Method(typeof(NetworkServer), nameof(NetworkServer.Spawn), new[] { typeof(GameObject), typeof(NetworkConnection) })) + offset;

            newInstructions.RemoveRange(index, 3);

            offset = -9;

            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Call &&
            (MethodInfo)instruction.operand == Method(typeof(NetworkServer), nameof(NetworkServer.Spawn), new[] { typeof(GameObject), typeof(NetworkConnection) })) + offset;

            // var ev = new PlacingTantrumEventArgs(this, Player, gameObject, cooldown, true);
            //
            // Handlers.Player.OnPlacingTantrum(ev);
            //
            // if (!ev.IsAllowed)
            //   return;
            //
            // cooldown = ev.Cooldown;
            newInstructions.InsertRange(index, new[]
            {
                // this
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Dup),

                // Player.Get(this.Hub)
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(PlayableScps.Scp173), nameof(PlayableScps.Scp173.Hub))),
                new CodeInstruction(OpCodes.Call, Method(typeof(API.Features.Player), nameof(API.Features.Player.Get), new[] { typeof(ReferenceHub) })),

                // gameObject
                new CodeInstruction(OpCodes.Ldloc_0),

                // cooldown
                new CodeInstruction(OpCodes.Ldc_R4, PlayableScps.Scp173.TantrumBaseCooldown),

                // true
                new CodeInstruction(OpCodes.Ldc_I4_1),

                // var ev = new PlacingTantrumEventArgs(...)
                new CodeInstruction(OpCodes.Newobj, GetDeclaredConstructors(typeof(PlacingTantrumEventArgs))[0]),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc_S, ev.LocalIndex),

                // Handlers.Player.OnPlacingTantrum(ev)
                new CodeInstruction(OpCodes.Call, Method(typeof(Handlers.Scp173), nameof(Handlers.Scp173.OnPlacingTantrum))),

                // if (!ev.IsAllowed)
                //   return;
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(PlacingTantrumEventArgs), nameof(PlacingTantrumEventArgs.IsAllowed))),
                new CodeInstruction(OpCodes.Brfalse_S, returnLabel),
            });

            offset = 1;

            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Call &&
            (MethodInfo)instruction.operand == Method(typeof(NetworkServer), nameof(NetworkServer.Spawn), new[] { typeof(GameObject), typeof(NetworkConnection) })) + offset;

            newInstructions.InsertRange(index, new[]
            {
                // this._tantrumCooldownRemaining = ev.Cooldown
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, ev.LocalIndex),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(PlacingTantrumEventArgs), nameof(PlacingTantrumEventArgs.Cooldown))),
                new CodeInstruction(OpCodes.Stfld, Field(typeof(PlayableScps.Scp173), nameof(PlayableScps.Scp173._tantrumCooldownRemaining))),
            });

            for (int i = 0; i < newInstructions.Count; i++)
                yield return newInstructions[i];

            ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }
    }
}
