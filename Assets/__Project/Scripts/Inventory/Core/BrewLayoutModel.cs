using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Stable per-artifact brew placement (Track F): each artifact holds its
    /// lattice spot for as long as nothing below it in its own column changes.
    /// New artifacts take the lowest free spot (bottom-up pile fill); a removal
    /// lets gravity close the gap **column-scoped** (FR4, owner revision
    /// 2026-07-07): only the bubbles that were resting above the removed one —
    /// its vertical column — fall one place down; everything beside and below
    /// it keeps its spot. No arbitrary re-sort ever. Pure domain state; the
    /// glide/splash animation is view-layer juice.
    /// </summary>
    public class BrewLayoutModel
    {
        private readonly IReadOnlyList<BrewSpot> _spots;
        private readonly Dictionary<int, int> _spotIndexByInstance = new Dictionary<int, int>();
        private readonly HashSet<int> _occupiedSpots = new HashSet<int>();

        // Per-column spot indices ordered bottom-up — the rails the settle
        // drops bubbles along.
        private readonly Dictionary<int, List<int>> _columnSpotIndices = new Dictionary<int, List<int>>();

        public BrewLayoutModel(IReadOnlyList<BrewSpot> spots)
        {
            if (spots == null || spots.Count == 0)
            {
                throw new ArgumentException("The brew lattice must contain at least one spot.", nameof(spots));
            }

            _spots = spots;
            for (int i = 0; i < spots.Count; i++)
            {
                if (!_columnSpotIndices.TryGetValue(spots[i].Column, out var column))
                {
                    column = new List<int>();
                    _columnSpotIndices[spots[i].Column] = column;
                }

                // Builder order is bottom-up, so each column list is too.
                column.Add(i);
            }
        }

        public int Capacity => _spots.Count;

        /// <summary>Top of the highest occupied spot, or 0 when the pot is empty.</summary>
        public float HighestOccupiedY
        {
            get
            {
                float highest = 0f;
                foreach (int spotIndex in _occupiedSpots)
                {
                    if (_spots[spotIndex].Y > highest)
                    {
                        highest = _spots[spotIndex].Y;
                    }
                }

                return highest;
            }
        }

        /// <summary>
        /// Reconciles the assignment with the current pot contents: stale
        /// artifacts release their spots (their columns settle down), new ones
        /// (in list order) stack onto the pile.
        /// </summary>
        public void Sync(IReadOnlyList<int> instanceIds)
        {
            var current = new HashSet<int>(instanceIds);
            var stale = new List<int>();
            foreach (var pair in _spotIndexByInstance)
            {
                if (!current.Contains(pair.Key))
                {
                    stale.Add(pair.Key);
                }
            }

            var touchedColumns = new HashSet<int>();
            foreach (int instanceId in stale)
            {
                touchedColumns.Add(_spots[_spotIndexByInstance[instanceId]].Column);
                _occupiedSpots.Remove(_spotIndexByInstance[instanceId]);
                _spotIndexByInstance.Remove(instanceId);
            }

            foreach (int column in touchedColumns)
            {
                SettleColumn(column);
            }

            for (int i = 0; i < instanceIds.Count; i++)
            {
                TryOccupy(instanceIds[i], out _);
            }
        }

        /// <summary>
        /// Assigns the lowest free spot to the artifact (idempotent for an
        /// already-placed one). False only when the lattice is exhausted.
        /// </summary>
        public bool TryOccupy(int instanceId, out BrewSpot spot)
        {
            if (_spotIndexByInstance.TryGetValue(instanceId, out int existing))
            {
                spot = _spots[existing];
                return true;
            }

            for (int i = 0; i < _spots.Count; i++)
            {
                if (_occupiedSpots.Add(i))
                {
                    _spotIndexByInstance[instanceId] = i;
                    spot = _spots[i];
                    return true;
                }
            }

            spot = default;
            return false;
        }

        public void Release(int instanceId)
        {
            if (_spotIndexByInstance.TryGetValue(instanceId, out int spotIndex))
            {
                _spotIndexByInstance.Remove(instanceId);
                _occupiedSpots.Remove(spotIndex);
                SettleColumn(_spots[spotIndex].Column);
            }
        }

        public bool TryGetSpot(int instanceId, out BrewSpot spot)
        {
            if (_spotIndexByInstance.TryGetValue(instanceId, out int spotIndex))
            {
                spot = _spots[spotIndex];
                return true;
            }

            spot = default;
            return false;
        }

        /// <summary>
        /// Gravity, column-scoped (FR4): the column's occupants re-pack onto
        /// its lowest spots keeping their bottom-up order, so a gap left by a
        /// removal collapses downward while every other column stays put.
        /// </summary>
        private void SettleColumn(int column)
        {
            var columnSpots = _columnSpotIndices[column];

            var occupants = new List<KeyValuePair<int, int>>();
            foreach (var pair in _spotIndexByInstance)
            {
                if (_spots[pair.Value].Column == column)
                {
                    occupants.Add(pair);
                }
            }

            occupants.Sort((a, b) => _spots[a.Value].Row.CompareTo(_spots[b.Value].Row));

            for (int i = 0; i < occupants.Count; i++)
            {
                int targetSpot = columnSpots[i];
                if (occupants[i].Value != targetSpot)
                {
                    _occupiedSpots.Remove(occupants[i].Value);
                    _occupiedSpots.Add(targetSpot);
                    _spotIndexByInstance[occupants[i].Key] = targetSpot;
                }
            }
        }
    }
}
