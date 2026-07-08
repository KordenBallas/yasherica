using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;

namespace Combat.Arena.Networking
{
    /// <summary>
    /// Domain ↔ wire conversion for the lockstep messages. Plain data mapping, no buffers —
    /// round-trip unit-tested. Decoding an intent needs the owning <see cref="IPlayer"/> (actions
    /// carry their player), resolved by PlayerId from the receiving client's own seat list.
    /// </summary>
    public static class ArenaWireCodec
    {
        // ---- commit envelope ----

        public static CommitEnvelopeData ToWire(ArenaCommitEnvelope envelope)
        {
            return new CommitEnvelopeData
            {
                RoundNumber = envelope.RoundNumber,
                PreviousRoundStateHash = envelope.PreviousRoundStateHash,
                Commit = ToWire(envelope.Commit)
            };
        }

        public static ArenaCommitEnvelope FromWire(CommitEnvelopeData data, Func<int, IPlayer> playerById)
        {
            return new ArenaCommitEnvelope(
                data.RoundNumber,
                FromWire(data.Commit, playerById),
                data.PreviousRoundStateHash);
        }

        // ---- round bundle ----

        public static RoundBundleData ToWire(ArenaRoundBundle bundle)
        {
            return new RoundBundleData
            {
                RoundNumber = bundle.RoundNumber,
                DepartedPlayerIds = bundle.DepartedPlayerIds.ToArray(),
                AutoPassedPlayerIds = bundle.AutoPassedPlayerIds.ToArray(),
                Commits = bundle.Commits.Select(ToWire).ToArray()
            };
        }

        public static ArenaRoundBundle FromWire(RoundBundleData data, Func<int, IPlayer> playerById)
        {
            return new ArenaRoundBundle(
                data.RoundNumber,
                data.Commits.Select(c => FromWire(c, playerById)).ToList(),
                data.DepartedPlayerIds.ToList(),
                data.AutoPassedPlayerIds?.ToList());
        }

        // ---- match setup ----

        public static MatchSetupData ToWire(ArenaMatchSetup setup)
        {
            return new MatchSetupData
            {
                MatchSeed = setup.MatchSeed,
                Roster = setup.Roster
                    .Select(s => new RosterSlotData { ClientId = s.ClientId, PlayerId = s.PlayerId, UnitId = s.UnitId })
                    .ToArray()
            };
        }

        public static ArenaMatchSetup FromWire(MatchSetupData data)
        {
            return new ArenaMatchSetup(
                data.MatchSeed,
                data.Roster.Select(s => new ArenaRosterSlot(s.ClientId, s.PlayerId, s.UnitId)).ToList());
        }

        // ---- draft (P4-5) ----

        public static TastedCatalogData ToWire(IReadOnlyList<string> partIds)
        {
            return new TastedCatalogData { PartIds = partIds?.ToArray() ?? Array.Empty<string>() };
        }

        public static IReadOnlyList<string> FromWire(TastedCatalogData data)
        {
            return data.PartIds?.ToList() ?? new List<string>();
        }

        public static DraftStartData ToWire(ArenaDraftStart start)
        {
            return new DraftStartData
            {
                PickTimerSeconds = start.PickTimerSeconds,
                SlotLoadout = start.SlotLoadout.ToArray(),
                Board = start.Board
                    .Select(e => new DraftBoardEntryData { EntryId = e.EntryId, PartId = e.PartId, SlotId = e.SlotId })
                    .ToArray()
            };
        }

        public static ArenaDraftStart FromWire(DraftStartData data)
        {
            return new ArenaDraftStart(
                data.PickTimerSeconds,
                data.SlotLoadout?.ToList() ?? new List<string>(),
                (data.Board ?? Array.Empty<DraftBoardEntryData>())
                    .Select(e => new ArenaDraftBoardEntry(e.EntryId, e.PartId, e.SlotId))
                    .ToList());
        }

        public static DraftPickData ToWire(ArenaDraftPick pick)
        {
            return new DraftPickData
            {
                PickIndex = pick.PickIndex,
                PlayerId = pick.PlayerId,
                EntryId = pick.EntryId,
                WasAutoPick = pick.WasAutoPick
            };
        }

        public static ArenaDraftPick FromWire(DraftPickData data)
        {
            return new ArenaDraftPick(data.PickIndex, data.PlayerId, data.EntryId, data.WasAutoPick);
        }

        public static DraftPickAppliedData ToWire(ArenaDraftPickApplied applied)
        {
            return new DraftPickAppliedData
            {
                Pick = ToWire(applied.Pick),
                DepartedPlayerIds = applied.DepartedPlayerIds.ToArray()
            };
        }

        public static ArenaDraftPickApplied FromWire(DraftPickAppliedData data)
        {
            return new ArenaDraftPickApplied(
                FromWire(data.Pick),
                data.DepartedPlayerIds?.ToList() ?? new List<int>());
        }

        // ---- X1 state snapshot / rejoin / resync / address book ----

        public static StateSnapshotData ToWire(ArenaStateSnapshot snapshot)
        {
            return new StateSnapshotData
            {
                Version = snapshot.Version,
                RoundNumber = snapshot.RoundNumber,
                LastRoundHash = snapshot.LastRoundHash,
                Units = snapshot.Units.Select(ToWire).ToArray()
            };
        }

        public static ArenaStateSnapshot FromWire(StateSnapshotData data)
        {
            return new ArenaStateSnapshot(
                data.Version,
                data.RoundNumber,
                data.LastRoundHash,
                (data.Units ?? Array.Empty<UnitSnapshotData>()).Select(FromWire).ToList());
        }

        private static UnitSnapshotData ToWire(ArenaUnitSnapshot unit)
        {
            return new UnitSnapshotData
            {
                UnitId = unit.UnitId,
                OwnerPlayerId = unit.OwnerPlayerId,
                Q = unit.Q,
                R = unit.R,
                CurrentHP = unit.CurrentHP,
                MaxHP = unit.MaxHP,
                Facing = unit.Facing,
                Abilities = unit.Abilities
                    .Select(a => new AbilityCooldownData { AbilityId = a.AbilityId, CurrentCooldown = a.CurrentCooldown })
                    .ToArray(),
                Statuses = unit.Statuses
                    .Select(s => new StatusSnapshotData { EffectId = s.EffectId, Duration = s.Duration, StackCount = s.StackCount })
                    .ToArray()
            };
        }

        private static ArenaUnitSnapshot FromWire(UnitSnapshotData data)
        {
            return new ArenaUnitSnapshot(
                data.UnitId,
                data.OwnerPlayerId,
                data.Q,
                data.R,
                data.CurrentHP,
                data.MaxHP,
                data.Facing,
                (data.Abilities ?? Array.Empty<AbilityCooldownData>())
                    .Select(a => new ArenaAbilityCooldownSnapshot(a.AbilityId, a.CurrentCooldown))
                    .ToList(),
                (data.Statuses ?? Array.Empty<StatusSnapshotData>())
                    .Select(s => new ArenaStatusSnapshot(s.EffectId, s.Duration, s.StackCount))
                    .ToList());
        }

        public static RejoinPackageData ToWire(ArenaRejoinPackage package)
        {
            return new RejoinPackageData
            {
                TargetPlayerId = package.TargetPlayerId,
                Setup = ToWire(package.Setup),
                Loadouts = package.LoadoutByPlayerId
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new LoadoutEntryData
                    {
                        PlayerId = pair.Key,
                        SlotIds = pair.Value.Keys.ToArray(),
                        PartIds = pair.Value.Values.ToArray()
                    })
                    .ToArray(),
                Snapshot = ToWire(package.Snapshot),
                DepartedPlayerIds = package.DepartedPlayerIds.ToArray(),
                HasBundle = package.CurrentRoundBundleOrNull != null,
                Bundle = package.CurrentRoundBundleOrNull != null
                    ? ToWire(package.CurrentRoundBundleOrNull)
                    : default
            };
        }

        public static ArenaRejoinPackage FromWire(RejoinPackageData data, Func<int, IPlayer> playerById)
        {
            var loadouts = new Dictionary<int, IReadOnlyDictionary<string, string>>();
            foreach (var entry in data.Loadouts ?? Array.Empty<LoadoutEntryData>())
            {
                var slots = new Dictionary<string, string>();
                for (int i = 0; i < (entry.SlotIds?.Length ?? 0); i++)
                {
                    slots[entry.SlotIds[i]] = entry.PartIds[i];
                }

                loadouts[entry.PlayerId] = slots;
            }

            return new ArenaRejoinPackage(
                data.TargetPlayerId,
                FromWire(data.Setup),
                loadouts,
                FromWire(data.Snapshot),
                data.DepartedPlayerIds?.ToList(),
                data.HasBundle ? FromWire(data.Bundle, playerById) : null);
        }

        public static ResyncCommandData ToWire(ArenaResyncCommand command)
        {
            return new ResyncCommandData
            {
                TargetPlayerId = command.TargetPlayerId,
                Snapshot = ToWire(command.Snapshot)
            };
        }

        public static ArenaResyncCommand FromWire(ResyncCommandData data)
        {
            return new ArenaResyncCommand(data.TargetPlayerId, FromWire(data.Snapshot));
        }

        public static ResyncAckData ToWire(ArenaResyncAck ack)
        {
            return new ResyncAckData { PlayerId = ack.PlayerId, RoundNumber = ack.RoundNumber };
        }

        public static ArenaResyncAck FromWire(ResyncAckData data)
        {
            return new ArenaResyncAck(data.PlayerId, data.RoundNumber);
        }

        public static EndpointData ToWire(ArenaEndpoint endpoint)
        {
            return new EndpointData { PlayerId = endpoint.PlayerId, Address = endpoint.Address };
        }

        public static ArenaEndpoint FromWire(EndpointData data)
        {
            return new ArenaEndpoint(data.PlayerId, data.Address);
        }

        public static AddressBookData ToWire(ArenaAddressBook book)
        {
            return new AddressBookData { Endpoints = book.Endpoints.Select(ToWire).ToArray() };
        }

        public static ArenaAddressBook FromWire(AddressBookData data)
        {
            return new ArenaAddressBook(
                (data.Endpoints ?? Array.Empty<EndpointData>()).Select(FromWire).ToList());
        }

        // ---- commits / steps ----

        public static PlayerCommitData ToWire(ArenaCommit commit)
        {
            return new PlayerCommitData
            {
                PlayerId = commit.PlayerId,
                UnitId = commit.UnitId,
                FinalFacing = (int)commit.FinalFacing,
                Steps = commit.Steps.Select(ToWire).ToArray()
            };
        }

        public static ArenaCommit FromWire(PlayerCommitData data, Func<int, IPlayer> playerById)
        {
            var owner = playerById(data.PlayerId);
            var steps = data.Steps.Select(s => FromWire(s, owner)).ToList();
            return new ArenaCommit(data.PlayerId, data.UnitId, (HexDirection)data.FinalFacing, steps);
        }

        public static IntentData ToWire(EnemyIntent intent)
        {
            var data = new IntentData
            {
                UnitId = intent.UnitId,
                IsMove = intent.IsMove,
                HasFacing = intent.CommittedFacing.HasValue,
                Facing = intent.CommittedFacing.HasValue ? (int)intent.CommittedFacing.Value : 0,
                OriginQ = intent.CommittedOrigin.Q,
                OriginR = intent.CommittedOrigin.R,
                CellsQR = FlattenCells(intent.CommittedCells)
            };

            if (intent.IsMove)
            {
                var destination = intent.MoveDestination.Value;
                data.TargetQ = destination.Q;
                data.TargetR = destination.R;
            }
            else
            {
                data.AbilityId = intent.AbilityId;
            }

            return data;
        }

        public static EnemyIntent FromWire(IntentData data, IPlayer owner)
        {
            var facing = data.HasFacing ? (HexDirection?)(HexDirection)data.Facing : null;
            var origin = new HexCoordinates(data.OriginQ, data.OriginR);

            IAction action = data.IsMove
                ? new MoveAction(owner, data.UnitId, new HexCoordinates(data.TargetQ, data.TargetR))
                : (IAction)new ScheduleAbilityAction(owner, data.UnitId, data.AbilityId, facing);

            return new EnemyIntent(data.UnitId, action, facing, origin, UnflattenCells(data.CellsQR));
        }

        private static int[] FlattenCells(IReadOnlyList<HexCoordinates> cells)
        {
            var flat = new int[cells.Count * 2];
            for (int i = 0; i < cells.Count; i++)
            {
                flat[i * 2] = cells[i].Q;
                flat[i * 2 + 1] = cells[i].R;
            }

            return flat;
        }

        private static List<HexCoordinates> UnflattenCells(int[] flat)
        {
            var cells = new List<HexCoordinates>();
            if (flat == null)
                return cells;

            for (int i = 0; i + 1 < flat.Length; i += 2)
            {
                cells.Add(new HexCoordinates(flat[i], flat[i + 1]));
            }

            return cells;
        }
    }
}
