using System;
using Impostor.Api.Innersloth;

namespace Impostor.Api.Net.Messages.S2C
{
    public class Message01JoinGameS2C
    {
        public static void SerializeJoin(IMessageWriter writer, bool clear, int gameCode, IClientPlayer player, int hostId)
        {
            if (clear)
            {
                writer.Clear(MessageType.Reliable);
            }

            writer.StartMessage(MessageFlags.JoinGame);
            writer.Write(gameCode);
            writer.Write(player.Client.Id);
            writer.Write(hostId);
            writer.Write(player.Client.Name);
            player.Client.PlatformSpecificData.Serialize(writer);
            writer.WritePacked(player.Character?.PlayerInfo.PlayerLevel ?? 1);
            writer.EndMessage();
        }

        public static void SerializeError(IMessageWriter writer, bool clear, DisconnectReason reason, string? message = null)
        {
            if (clear)
            {
                writer.Clear(MessageType.Reliable);
            }

            writer.StartMessage(MessageFlags.JoinGame);
            writer.Write((int)reason);

            if (reason == DisconnectReason.Custom)
            {
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                writer.Write(message);
            }

            writer.EndMessage();
        }

        public static void Deserialize(IMessageReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
