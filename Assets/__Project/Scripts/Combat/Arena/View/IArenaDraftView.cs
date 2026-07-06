using System;
using System.Collections.Generic;
using UnityEngine;

namespace Combat.Arena.View
{
    /// <summary>
    /// The draft screen's uGUI half (G4): the board viewport (stage texture + pointer UVs),
    /// whose-pick banner, snake-order line, timer, the local dual readout, the opponents'
    /// name + parts readouts, remote-pick flights, and the "your monster" completion beat.
    /// Dumb by contract — meaning lives in the presenter.
    /// </summary>
    public interface IArenaDraftView
    {
        /// <summary>A click inside the board viewport, as stage-camera viewport UV (0..1).</summary>
        event Action<Vector2> BoardClicked;

        /// <summary>The completion beat's Continue button.</summary>
        event Action BeatConfirmed;

        void ShowPanel();
        void HidePanel();

        void SetTurn(string pickerName, bool isLocalTurn);
        void SetOrderLine(string orderLine);
        void SetTimer(float secondsLeft, bool visible);
        void SetBoardTexture(Texture texture);
        void SetLocalReadout(string text);
        void SetOpponentsReadout(string text);

        /// <summary>A remote pick flies from its board spot toward the opponents' readout.</summary>
        void PlayRemotePickFlight(Vector2 boardViewportUv, Sprite partIcon);

        /// <summary>The screen position of a stage viewport UV (for popover placement).</summary>
        Vector2 BoardUvToScreen(Vector2 boardViewportUv);

        void ShowBeat(string title, string partsText);
        void HideBeat();
    }
}
