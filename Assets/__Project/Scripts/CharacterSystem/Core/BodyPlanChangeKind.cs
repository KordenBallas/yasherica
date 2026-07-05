namespace CharacterSystem.Core
{
    /// <summary>How an install request affects the body plan.</summary>
    public enum BodyPlanChangeKind
    {
        /// <summary>The governing frame is unchanged and the part fits it: the ordinary,
        /// cheap same-frame swap (the ~80% path — no prompt, no rebuild).</summary>
        InstantSwap,

        /// <summary>The incoming part is a frame-changer that loses the priority resolution:
        /// it is recorded as equipped-but-dormant (no renderer/sockets) without any rebuild.</summary>
        DormantInstall,

        /// <summary>The install hands governance to a different skeleton: the body re-forms,
        /// non-fitting ordinary parts shed, losing frame-changers go dormant.</summary>
        FrameChange,

        /// <summary>The part cannot be installed: it neither fits the governing frame nor
        /// changes governance (e.g. a part authored for a frame that is not in play).</summary>
        Incompatible
    }
}
