using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Redis.OM;

namespace Redis.OM
{
    public static class RespHelper
    {
        public static byte[] BuildCommand(string commandName, params string[] args)
        {
            var sb = new StringBuilder();
            var count = 1 + args.Length;

            sb.Append($"*{count}\r\n" +
                $"${commandName.Length}\r\n" +
                $"{commandName}\r\n");

            foreach(var arg in args)
            {
                sb.Append($"${arg.Length}\r\n");
                sb.Append($"{arg}\r\n");
            }

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static double CurrentTime => DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

        public static int GetPositionOfNextCRLF(Socket socket)
        {
            const int BYTES_TO_SCAN = 50;
            const int TIMEOUT = 50; // timeout in ms
            var startTime = CurrentTime;
            var numBytesScanned = 0;
            var peekBuffer = new byte[BYTES_TO_SCAN];
            var pos = -1;
            while (pos < 0 && (startTime - CurrentTime) < TIMEOUT)
            {
                socket.Receive(peekBuffer, SocketFlags.Peek);                
                var str = Encoding.ASCII.GetString(peekBuffer);
                if(numBytesScanned == 0 && str[0] == '-')
                {
                    var err = "";
                    while (socket.Available > 0)
                    {
                        socket.Receive(peekBuffer);
                        err += Encoding.ASCII.GetString(peekBuffer);
                    }
                    throw new RedisClientException(err);
                }
                pos = str.IndexOf("\r\n");
                numBytesScanned += BYTES_TO_SCAN;
            }
            return pos;
        }

        public static async Task<int> GetPositionOfNextCRLFAsync(Socket socket)
        {
            const int BYTES_TO_SCAN = 50;
            const int TIMEOUT = 50; // timeout in ms
            var startTime = CurrentTime;
            var numBytesScanned = 0;
            var peekBuffer = new ArraySegment<byte>(new byte[BYTES_TO_SCAN]);
            var pos = -1;
            while (pos < 0 && (startTime - CurrentTime) < TIMEOUT)
            {
                await socket.ReceiveAsync(peekBuffer, SocketFlags.Peek);
                var str = Encoding.ASCII.GetString(peekBuffer.ToArray());
                if (numBytesScanned == 0 && str[0] == '-')
                {
                    var err = "";
                    while (socket.Available > 0)
                    {
                        await socket.ReceiveAsync(peekBuffer,SocketFlags.None);
                        err += Encoding.ASCII.GetString(peekBuffer.ToArray());
                    }
                    throw new RedisClientException(err);
                }
                pos = str.IndexOf("\r\n");
                numBytesScanned += BYTES_TO_SCAN;
            }
            return pos;
        }

        public static async Task<RedisReply> ReadBulkStringAsync(Socket socket, string currentString)
        {
            if (currentString == "-1")
                return "";
            var size = int.Parse(currentString);
            var buffer = new ArraySegment<byte>(new byte[size + 2]);
            //var buffer = new byte[size + 2];
            await socket.ReceiveAsync(buffer, SocketFlags.None);
            return Encoding.ASCII.GetString(buffer.Take(buffer.Count()-2).ToArray());
        }

        public static RedisReply ReadBulkString(Socket socket, string currentString)
        {
            if (currentString == "-1")
                return "";
            var size = int.Parse(currentString);
            var buffer = new byte[size+2];
            socket.Receive(buffer);
            return Encoding.ASCII.GetString(buffer.Take(buffer.Length - 2).ToArray());            
        }

        public static async Task<RedisReply> ReadArrayAsync(Socket socket, string currentString)
        {
            var responseList = new List<RedisReply>();
            var size = int.Parse(currentString);
            if(size < 0)
            {
                return responseList.ToArray();
            }
            var reply = new string[size];
            for (var i = 0; i < size; i++)
            {
                var endOfNext = await GetPositionOfNextCRLFAsync(socket);
                var tmp = new ArraySegment<byte>(new byte[endOfNext + 2]);
                await socket.ReceiveAsync(tmp, SocketFlags.None);
                var str = Encoding.ASCII.GetString(tmp.Skip(1).Take(endOfNext-1).ToArray());
                var nextString = tmp.Array[0] switch
                {
                    0x24 => await ReadBulkStringAsync (socket, str),        // $ -> this is a bulk string
                    0x2A => await ReadArrayAsync(socket, str),  // * -> this is an array
                    0x2B or 0x3A => (RedisReply)str,                       // + or : -> string or integer, we'll handle both the same for now
                    0x2D => throw new RedisClientException(str),// - -> this is an error, throw an exception
                    _ => throw new RedisClientException("did " +// Ok, well we aren't dealing with RESP here, throw toss an exception
                    "not find a discernable string from socket")
                };
                responseList.Add(nextString);

            }
            return responseList.ToArray();
        }

        public static RedisReply ReadArray(Socket socket, string currentString)
        {
            var responseList = new List<RedisReply>();
            var size = int.Parse(currentString);
            if (size < 0)
            {
                return responseList.ToArray();
            }
            var reply = new string[size];
            for(var i = 0; i < size; i++)
            {
                var endOfNext = GetPositionOfNextCRLF(socket);
                var tmp = new byte[endOfNext + 2];
                socket.Receive(tmp);
                var str = Encoding.ASCII.GetString(tmp.Skip(1).Take(endOfNext - 1).ToArray());
                var nextString = tmp[0] switch
                {
                    0x24 => ReadBulkString(socket, str),        // $ -> this is a bulk string
                    0x2A => ReadArray(socket, str),  // * -> this is an array
                    0x2B or 0x3A => (RedisReply)str ,                       // + or : -> string or integer, we'll handle both the same for now
                    0x2D => throw new RedisClientException(str),// - -> this is an error, throw an exception
                    _ => throw new RedisClientException("did " +// Ok, well we aren't dealing with RESP here, throw toss an exception
                    "not find a discernable string from socket")
                };
                responseList.Add(nextString);

            }
            return responseList.ToArray();
        }

        public static async Task<RedisReply> GetNextReplyFromSocketAsync(Socket socket)
        {
            var nextLinesSize = await GetPositionOfNextCRLFAsync(socket);
            if (nextLinesSize >= 0)
            {
                var buffer = new ArraySegment<byte>( new byte[nextLinesSize + 2]);
                await socket.ReceiveAsync(buffer, SocketFlags.None);                
                var str = Encoding.ASCII.GetString(buffer.Skip(1).Take(nextLinesSize-1).ToArray());
                return buffer.Array[0] switch
                {
                    0x24 => await ReadBulkStringAsync(socket, str),  // $ -> this is a bulk string
                    0x2A => await ReadArrayAsync(socket, str),                 // * -> this is an array
                    0x2B => str,                             // + - this is a plain string
                    0x3A => int.Parse(str),                  // : -> integer
                    0x2D => throw new RedisClientException(str),    // - -> this is an error, throw an exception
                    _ => throw new RedisClientException("did " +    // Ok, well we aren't dealing with RESP here, throw toss an exception
                    "not find a discernable string from socket")
                };
            }
            throw new RedisClientException("Did not find a discernalbe string on socket"); // nothign was found, throw an exception
        }

        public static RedisReply GetNextReplyFromSocket(Socket socket)
        {
            var nextLinesSize = GetPositionOfNextCRLF(socket);
            if (nextLinesSize >= 0)
            {
                var buffer = new byte[nextLinesSize + 2];
                socket.Receive(buffer);
                var str = Encoding.ASCII.GetString(buffer.Skip(1).Take(nextLinesSize - 1).ToArray());
                return buffer[0] switch
                {
                    0x24 => ReadBulkString(socket, str),  // $ -> this is a bulk string
                    0x2A => ReadArray(socket, str),                 // * -> this is an array
                    0x2B => str,                             // + - this is a plain string
                    0x3A => int.Parse(str),                  // : -> integer
                    0x2D => throw new RedisClientException(str),    // - -> this is an error, throw an exception
                    _ => throw new RedisClientException("did " +    // Ok, well we aren't dealing with RESP here, throw toss an exception
                    "not find a discernable string from socket") 
                };
            }
            throw new RedisClientException("Did not find a discernalbe string on socket"); // nothign was found, throw an exception
        }
    }
}
