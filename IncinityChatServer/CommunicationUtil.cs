using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace IncityChatServer.Util
{
    class CommunicationUtil
    {
        /// <summary>
        /// Decode message from client's stream.
        /// </summary>
        /// <param name="bytes">encoded message</param>
        /// <returns>decoded message</returns>
        public static String DecodeMessage(Byte[] bytes)
        {
            String message = "";
            Byte parameters = bytes[0];

            if (parameters == 129)
            {
                Console.WriteLine("Parameters OK, FIN frame");
            } else
            {
                Console.WriteLine("Error in parameters");
            }

            try
            {
                UInt64 len = 0;
                Byte[] key = new Byte[4] { bytes[2], bytes[3], bytes[4], bytes[5] };
                Byte[] length = { bytes[1] };
                int srcOffset = 1 + 1 + 4; // params + length byte + key byte

                if (length[0] - 128 <= 125)
                {
                    len = bytes[1] -= 128;
                }
                else if (bytes[1] - 128 == 126)
                {
                    len = BitConverter.ToUInt16(new Byte[2] { bytes[3], bytes[2] }, 0);
                    key = new Byte[4] { bytes[4], bytes[5], bytes[6], bytes[7] };
                    srcOffset = 1 + 1 + 2 + 4;
                }
                else if (bytes[1] - 128 == 127)
                {
                    len = BitConverter.ToUInt64(bytes, 1);
                    key = new Byte[4] { bytes[10], bytes[11], bytes[12], bytes[13] };
                    srcOffset = 1 + 1 + 8 + 4;
                } else
                {
                    Console.WriteLine("Error in message length");
                }

                Console.WriteLine("Length: {0}", len);

                Byte[] decoded = new Byte[(int)len];
                Byte[] encoded = new Byte[(int)len];
                Buffer.BlockCopy(bytes, srcOffset, encoded, 0, (int)len);

                for (UInt64 i = 0; i < len; i++)
                {
                    decoded[i] = (Byte)(encoded[i] ^ key[i % 4]);
                }

                message = Encoding.UTF8.GetString(decoded);
            } catch (FormatException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return message;
        }

        /// <summary>
        /// Encode message sent from server.
        /// </summary>
        /// <param name="message">message to encode</param>
        /// <returns>encoded message</returns>
        public static Byte[] EncodeMessage(String message)
        {
            Byte header = 0x81; // always FIN frame and opcode 0x01 - text
            int length = message.Length;
            int lengthSize = 1;
            Byte[] lengthBytes = BitConverter.GetBytes(message.Length);

            if (message.Length > 125)
            {
                lengthSize = 2;
                lengthBytes = BitConverter.GetBytes((UInt16) message.Length);
            } else if (message.Length > 65535)
            {
                lengthSize = 8;
                lengthBytes = BitConverter.GetBytes((UInt64)message.Length);
            }

            Byte[] response = new Byte[1 + lengthSize + message.Length];
            response[0] = header;

            if (lengthSize == 1)
            {
                response[1] = lengthBytes[0];
            } else if (lengthSize == 2)
            {
                response[1] = lengthBytes[0];
                response[2] = lengthBytes[1];
            } else if (lengthSize == 8)
            {
                response[1] = lengthBytes[0];
                response[2] = lengthBytes[1];
                response[3] = lengthBytes[2];
                response[4] = lengthBytes[3];
                response[5] = lengthBytes[4];
                response[6] = lengthBytes[5];
                response[7] = lengthBytes[6];
                response[8] = lengthBytes[7];
            }

            Buffer.BlockCopy(Encoding.UTF8.GetBytes(message), 0, response, 1 + lengthSize, message.Length);

            return response;
        }
    }
}
