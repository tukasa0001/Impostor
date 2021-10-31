using Impostor.Api.Net.Inner.Objects;

namespace Impostor.Api.Events.Player
{
    public interface IPlayerReportEvent : IPlayerEvent, IEventCancelable
    {
        /// <summary>
        ///     Gets the message sent by the player.
        /// </summary>
        IInnerPlayerControl? Body { get; }
    }
}
