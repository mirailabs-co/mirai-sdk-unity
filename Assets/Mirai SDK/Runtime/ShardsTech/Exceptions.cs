using System;

namespace Mirai
{
    public class DappException:Exception
    {
        public DappException()
        {
        }

        public DappException(string message)
            : base(message)
        {
        }

        public DappException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class ShardsTechException : Exception
    {
        public ShardsTechException()
        {
        }

        public ShardsTechException(string message)
            : base(message)
        {
        }

        public ShardsTechException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}