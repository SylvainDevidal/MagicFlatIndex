using System;
using System.Collections.Generic;
using System.Text;

namespace MagicFlatIndex
{
    public abstract class BaseFlatRecord
    {
        public int Id { get; set; }
        public abstract byte[] ToBytes();
        public static BaseFlatRecord FromBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }
        public static int GetSize()
        {
            throw new NotImplementedException();
        }
    }
}
