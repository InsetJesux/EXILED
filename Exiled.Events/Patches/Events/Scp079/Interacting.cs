// -----------------------------------------------------------------------
// <copyright file="Interacting.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Scp079
{
#pragma warning disable SA1118
#pragma warning disable SA1123

    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using Exiled.API.Features;
    using Exiled.Events.EventArgs;

    using HarmonyLib;

    using NorthwoodLib.Pools;

    using UnityEngine;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches <see cref="Scp079PlayerScript.UserCode_CmdInteract(Command079, string, GameObject)"/>.
    /// Adds the <see cref="InteractingTeslaEventArgs"/>, <see cref="InteractingDoorEventArgs"/>, <see cref="Handlers.Scp079.StartingSpeaker"/> and <see cref="Handlers.Scp079.StoppingSpeaker"/> event for SCP-079.
    /// </summary>
    [HarmonyPatch(typeof(Scp079PlayerScript), nameof(Scp079PlayerScript.UserCode_CmdInteract))]
    internal static class Interacting
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

            #region InteractingTeslaEventArgs

            // Index offset.
            int offset = -1;

            // Find first "ldstr Tesla Gate Burst", then add the offset to get "ldloc.3".
            int index = newInstructions.FindIndex(i => i.opcode == OpCodes.Callvirt && (MethodInfo)i.operand == Method(typeof(TeslaGate), nameof(TeslaGate.RpcInstantBurst))) + offset;

            // Get the return label.
            Label returnLabel = newInstructions[newInstructions.Count - 1].labels[0];

            // Declare a local variable of the type "InteractingTeslaEventArgs";
            LocalBuilder interactingTeslaEv = generator.DeclareLocal(typeof(InteractingTeslaEventArgs));

            // var ev = new InteractingTeslaEventArgs(Player.Get(this.gameObject), teslaGameObject.GetComponent<TeslaGate>(), manaFromLabel, manaFromLabel <= this.curMana);
            //
            // Handlers.Map.OnInteractingTesla(ev);
            //
            // if (!ev.IsAllowed)
            //   return;
            CodeInstruction[] instructionsToInsert = new[]
            {
                // Player.Get(this.gameObject)
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(GameObject) })),

                // teslaGameObject.GetComponent<TeslaGate>();
                new CodeInstruction(OpCodes.Ldloc_S, 32),

                // manaFromLabel
                new CodeInstruction(OpCodes.Ldloc_2),

                // var ev = new InteractingTeslaEventArgs(...)
                new CodeInstruction(OpCodes.Newobj, GetDeclaredConstructors(typeof(InteractingTeslaEventArgs))[0]),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc, interactingTeslaEv.LocalIndex),
                new CodeInstruction(OpCodes.Call, Method(typeof(Handlers.Scp079), nameof(Handlers.Scp079.OnInteractingTesla))),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(InteractingTeslaEventArgs), nameof(InteractingTeslaEventArgs.IsAllowed))),
                new CodeInstruction(OpCodes.Brfalse, returnLabel),
                new CodeInstruction(OpCodes.Ldloc, interactingTeslaEv.LocalIndex),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(InteractingTeslaEventArgs), nameof(InteractingTeslaEventArgs.AuxiliaryPowerCost))),
                new CodeInstruction(OpCodes.Stloc_2),
            };

            newInstructions.InsertRange(index, instructionsToInsert);

            #endregion

            #region TriggeringDoorEventArgs

            // Declare a local variable of the type "TriggeringDoorEventArgs";
            LocalBuilder interactingDoorEv = generator.DeclareLocal(typeof(TriggeringDoorEventArgs));

            offset = 10;

            // Find the first ',', then add the offset to get "ldloc.3".
            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_I4_S &&
            (sbyte)instruction.operand == ',') + offset;

            // var ev = new TriggeringDoorEventArgs(Player.Get(this.gameObject), doorVariant, manaFromLabel, manaFromLabel <= this.curMana);
            //
            // Handlers.Scp079.OnTriggeringDoor(ev);
            //
            // if (!ev.IsAllowed)
            //   return;
            newInstructions.InsertRange(index, new[]
            {
                // Player.Get(this.gameObject)
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(GameObject) })),

                // doorVariant
                new CodeInstruction(OpCodes.Ldloc_0),

                // manaFromLabel
                new CodeInstruction(OpCodes.Ldloc_2),

                // !(manaFromLabel > this.curMana) --> manaFromLabel <= this.curMana
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp079PlayerScript), nameof(Scp079PlayerScript._curMana))),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ceq),

                // var ev = new TriggeringDoorEventArgs(...)
                new CodeInstruction(OpCodes.Newobj, GetDeclaredConstructors(typeof(TriggeringDoorEventArgs))[0]),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc_S, interactingDoorEv.LocalIndex),

                // Handlers.Scp079.OnTriggeringDoor(ev);
                new CodeInstruction(OpCodes.Call, Method(typeof(Handlers.Scp079), nameof(Handlers.Scp079.OnTriggeringDoor))),

                // if (!ev.IsAllowed)
                //   return;
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(TriggeringDoorEventArgs), nameof(TriggeringDoorEventArgs.IsAllowed))),
                new CodeInstruction(OpCodes.Brfalse, returnLabel),

                // manaFromLabel = ev.AuxiliaryPowerCost
                new CodeInstruction(OpCodes.Ldloc_S, interactingDoorEv.LocalIndex),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(TriggeringDoorEventArgs), nameof(TriggeringDoorEventArgs.AuxiliaryPowerCost))),
                new CodeInstruction(OpCodes.Stloc_2),
            });

            #endregion

            #region LockingDownEventArgs

            // Declare a local variable of the type "LockingDownEventArgs";
            LocalBuilder lockingDown = generator.DeclareLocal(typeof(LockingDownEventArgs));

            offset = 5;

            // Find the operand "Room Lockdown", then add the offset to get "ldloc.3"
            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldstr &&
            (string)instruction.operand == "Room Lockdown") + offset;

            // var roomGameObject = GameObject.Find(this.currentZone + "/" + this.currentRoom);
            // var ev = new LockingDownEventArgs(player, roomGameObject, manaFromLabel, manaFromLabel <= this.curMana);
            //
            // Handlers.Scp079.OnLockingDown(ev);
            //
            // if (!ev.IsAllowed)
            //   return;
            newInstructions.InsertRange(index, new[]
            {
                // Player.Get(this.gameObject) => player
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(GameObject) })),

                // this.CurrentRoom
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp079PlayerScript), nameof(Scp079PlayerScript.CurrentRoom))),

                // manaFromLabel
                new CodeInstruction(OpCodes.Ldloc_2),

                // var ev = new LockingDownEventArgs(...)
                new CodeInstruction(OpCodes.Newobj, GetDeclaredConstructors(typeof(LockingDownEventArgs))[0]),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc_S, lockingDown.LocalIndex),

                // Handlers.Scp079.OnLockingDown(ev);
                new CodeInstruction(OpCodes.Call, Method(typeof(Handlers.Scp079), nameof(Handlers.Scp079.OnLockingDown))),

                // if (!ev.IsAllowed)
                //   return;
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(LockingDownEventArgs), nameof(LockingDownEventArgs.IsAllowed))),
                new CodeInstruction(OpCodes.Brfalse, returnLabel),

                // manaFromLabel = ev.AuxiliaryPowerCost
                new CodeInstruction(OpCodes.Ldloc_S, lockingDown.LocalIndex),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(LockingDownEventArgs), nameof(LockingDownEventArgs.AuxiliaryPowerCost))),
                new CodeInstruction(OpCodes.Stloc_2),
            });

            #endregion

            #region StartingSpeakerEventArgs

            // Declare a local variable of the type "StartingSpeakerEventArgs";
            LocalBuilder startingSpeakerEv = generator.DeclareLocal(typeof(StartingSpeakerEventArgs));

            offset = -1;

            // Find the first 1.5f, then add the offset to get "ldloc.3".
            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 1.5f) + offset;

            // var ev = new StartingSpeakerEventArgs(Player.Get(this.gameObject), Map.FindParentRoom(this.currentCamera.gameObject), manaFromLabel, manaFromLabel * 1.5f <= this.curMana);
            //
            // Handlers.Scp079.OnStartingSpeaker(ev);
            //
            // if (!ev.IsAllowed)
            //   return;
            newInstructions.InsertRange(index, new[]
            {
                // Player.Get(this.gameObject)
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(GameObject) })),

                // Map.FindParentRoom(this.currentCamera.gameObject)
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp079PlayerScript), nameof(Scp079PlayerScript.currentCamera))),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Map), nameof(Map.FindParentRoom))),

                // manaFromLabel
                new CodeInstruction(OpCodes.Ldloc_2),

                // !(manaFromLabel * 1.5f > this.curMana) --> manaFromLabel * 1.5f <= this.curMana
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldc_R4, 1.5f),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp079PlayerScript), nameof(Scp079PlayerScript._curMana))),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ceq),

                // var ev = new StartingSpeakerEventArgs(...)
                new CodeInstruction(OpCodes.Newobj, GetDeclaredConstructors(typeof(StartingSpeakerEventArgs))[0]),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc_S, startingSpeakerEv.LocalIndex),

                // Handlers.Scp079.OnStartingSpeaker(ev);
                new CodeInstruction(OpCodes.Call, Method(typeof(Handlers.Scp079), nameof(Handlers.Scp079.OnStartingSpeaker))),

                // if (!ev.IsAllowed)
                //   return;
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(StartingSpeakerEventArgs), nameof(StartingSpeakerEventArgs.IsAllowed))),
                new CodeInstruction(OpCodes.Brfalse, returnLabel),

                // manaFromLabel = ev.AuxiliaryPowerCost
                new CodeInstruction(OpCodes.Ldloc_S, startingSpeakerEv.LocalIndex),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(StartingSpeakerEventArgs), nameof(StartingSpeakerEventArgs.AuxiliaryPowerCost))),
                new CodeInstruction(OpCodes.Stloc_2),
            });

            #endregion

            #region StoppingSpeakerEventArgs

            offset = -1;

            // Find the first string.Empty, then add the offset to get "ldarg.0".
            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldsfld &&
            (FieldInfo)instruction.operand == Field(typeof(string), nameof(string.Empty))) + offset;

            // if (string.IsNullOrEmpty(this.Speaker)
            //   return;
            //
            // var ev = new StoppingSpeakerEventArgs(Player.Get(this.gameObject), Map.FindParentRoom(this.currentCamera.gameObject), true);
            //
            // Handlers.Scp079.OnStoppingSpeaker(ev);
            //
            // if (!ev.IsAllowed)
            //   return;
            newInstructions.InsertRange(index, new[]
            {
                // if (string.IsNullOrEmpty(this.Speaker)
                //   return;
                new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(newInstructions[index]),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Scp079PlayerScript), nameof(Scp079PlayerScript.Speaker))),
                new CodeInstruction(OpCodes.Call, Method(typeof(string), nameof(string.IsNullOrEmpty))),
                new CodeInstruction(OpCodes.Brtrue, returnLabel),

                // Player.Get(this.gameObject)
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(GameObject) })),

                // Map.FindParentRoom(this.currentCamera.gameObject)
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp079PlayerScript), nameof(Scp079PlayerScript.currentCamera))),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Map), nameof(Map.FindParentRoom))),

                // true
                new CodeInstruction(OpCodes.Ldc_I4_1),

                // var ev = new StartingSpeakerEventArgs(...)
                new CodeInstruction(OpCodes.Newobj, GetDeclaredConstructors(typeof(StoppingSpeakerEventArgs))[0]),
                new CodeInstruction(OpCodes.Dup),

                // Handlers.Scp079.OnStartingSpeaker(ev);
                new CodeInstruction(OpCodes.Call, Method(typeof(Handlers.Scp079), nameof(Handlers.Scp079.OnStoppingSpeaker))),

                // if (!ev.IsAllowed)
                //   return;
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(StoppingSpeakerEventArgs), nameof(StoppingSpeakerEventArgs.IsAllowed))),
                new CodeInstruction(OpCodes.Brfalse, returnLabel),
            });

            #endregion

            #region ElevatorTeleportingEventArgs

            // Index offset.
            offset = 5;

            // Find first "ldstr Elevator Teleport", then add the offset to get "ldloc.3".
            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldstr &&
            (string)instruction.operand == "Elevator Teleport") + offset;

            // Declare a local variable of the type "ElevatorTeleportingEventArgs";
            LocalBuilder elevatorTeleportEv = generator.DeclareLocal(typeof(ElevatorTeleportingEventArgs));

            // var ev = new ElevatorTeleportingEventArgs(Player.Get(this.gameObject), camera, manaFromLabel, manaFromLabel <= this.curMana);
            //
            // Handlers.Scp079.OnElevatorTeleporting(ev);
            //
            // if (!ev.IsAllowed)
            //   return;
            instructionsToInsert = new[]
            {
                // Player.Get(this.gameObject)
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(GameObject) })),

                // camera
                new CodeInstruction(OpCodes.Ldloc_S, 9),

                // manaFromLabel
                new CodeInstruction(OpCodes.Ldloc_2),

                // !(manaFromLabel > this.curMana) --> manaFromLabel <= this.curMana
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp079PlayerScript), nameof(Scp079PlayerScript._curMana))),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ceq),

                // var ev = new ElevatorTeleportingEventArgs(...)
                new CodeInstruction(OpCodes.Newobj, GetDeclaredConstructors(typeof(ElevatorTeleportingEventArgs))[0]),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc_S, elevatorTeleportEv.LocalIndex),

                // Handlers.Map.OnElevatorTeleporting(ev);
                new CodeInstruction(OpCodes.Call, Method(typeof(Handlers.Scp079), nameof(Handlers.Scp079.OnElevatorTeleporting))),

                // if (!ev.IsAllowed)
                //   return;
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(ElevatorTeleportingEventArgs), nameof(ElevatorTeleportingEventArgs.IsAllowed))),
                new CodeInstruction(OpCodes.Brfalse, returnLabel),

                // manaFromLabel = ev.AuxiliaryPowerCost
                new CodeInstruction(OpCodes.Ldloc_S, elevatorTeleportEv.LocalIndex),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(ElevatorTeleportingEventArgs), nameof(ElevatorTeleportingEventArgs.AuxiliaryPowerCost))),
                new CodeInstruction(OpCodes.Stloc_2),
            };

            newInstructions.InsertRange(index, instructionsToInsert);

            #endregion

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];

            ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }
    }
}
