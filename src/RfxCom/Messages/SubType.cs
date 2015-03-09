﻿namespace RfxCom.Messages
{
    public class PacketType : Field<PacketType>
    {
        public static PacketType InterfaceCommand = new PacketType(0x00, "Interface Command");
        public static PacketType InterfaceMessage = new PacketType(0x01, "Interface Message");
        public static PacketType Chime = new PacketType(0x16, "Byron");

        private PacketType(byte value, string description) : base(value, description)
        {
        }
        
    }

    public class SubType : Field<SubType>
    {
        public static SubType ModeCommand = new SubType(0x00, "Mode Command");
        
        internal SubType(byte value, string description) : base(value, description)
        {
        }
    }

   
    
}