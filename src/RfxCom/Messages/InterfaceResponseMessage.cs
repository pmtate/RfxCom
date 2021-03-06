﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RfxCom.Messages
{
    public class InterfaceResponseMessage : Message
    {
        public InterfaceResponseMessage(byte packetLength, PacketType packetType, InterfaceResponseSubType subType, byte sequenceNumber, InterfaceControlCommand controlCommand, TransceiverType transceiverType, byte version, Protocol[] protocols)
        {
            PacketLength = packetLength;
            PacketType = packetType;
            SubType = subType;
            SequenceNumber = sequenceNumber;
            ControlCommand = controlCommand;
            TransceiverType = transceiverType;
            Version = version;
            Protocols = protocols;
        }

        public byte PacketLength { get; private set; }
        public PacketType PacketType { get; set; }
        public InterfaceResponseSubType SubType { get; private set; }
        public byte SequenceNumber { get; private set; }
        public InterfaceControlCommand ControlCommand { get; private set; }
        public TransceiverType TransceiverType { get; private set; }
        public byte Version { get; private set; }
        public Protocol[] Protocols { get; private set; }
        
        public override byte[] ToBytes()
        {
            return new[]
            {
                PacketLength, 
                PacketType, 
                SubType, 
                SequenceNumber, 
                ControlCommand, 
                TransceiverType, 
            };
        }

        public static bool TryParse(byte[] bytes, out InterfaceResponseMessage message)
        {
            message = default (InterfaceResponseMessage);

            if (bytes.Length != PacketLengths.InterfaceResponse + 1)
            {
                return false;
            }

            var packetLength = bytes[0];

            if (packetLength != PacketLengths.InterfaceResponse)
            {
                return false;
            }

            PacketType packetType;

            if (!PacketType.TryParse(bytes[1], out packetType) && packetType != PacketType.InterfaceMessage)
            {
                return false;
            }

            InterfaceResponseSubType subType;

            if (!InterfaceResponseSubType.TryParse(bytes[2], out subType))
            {
                return false;
            }

            var sequenceNumber = bytes[3];
            
            InterfaceControlCommand command;
            
            if (!InterfaceControlCommand.TryParse(bytes[4], out command))
            {
                return false;
            }

            TransceiverType message1;

            if (!TransceiverType.TryParse(bytes[5], out message1))
            {
                return false;
            }

            var message2 = bytes[6];
            var message3 = bytes[7];
            var message4 = bytes[8];
            var message5 = bytes[9];

            var enabledProtocols1 = Protocol.ListEnabled(message3).Where(x => x.MessageNumber == 3);
            var enabledProtocols2 = Protocol.ListEnabled(message4).Where(x => x.MessageNumber == 4);
            var enabledProtocols3 = Protocol.ListEnabled(message5).Where(x => x.MessageNumber == 5);
            var enabledProtocols = enabledProtocols1.Concat(enabledProtocols2).Concat(enabledProtocols3).ToArray();
            
            message = new InterfaceResponseMessage(packetLength, packetType, subType, sequenceNumber, command, message1, message2, enabledProtocols);

            return true;
        }

        public override string ToString()
        {
            var protocols = string.Join(",", Protocols.Select(x => x.Description));
            return String.Format("Firmware Version: {0}, Frequency: {1}, Enabled Protocols: {2}",Version, TransceiverType.Description, protocols);
        }
    }
}