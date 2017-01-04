namespace RtmpSharp.Net.Messages
{
    class Acknowledgement : RtmpMessage
    {
        public uint TotalRead;

        public Acknowledgement(uint read) : base(PacketContentType.Acknowledgement)
        {
            TotalRead = read;
        }
    }
}
