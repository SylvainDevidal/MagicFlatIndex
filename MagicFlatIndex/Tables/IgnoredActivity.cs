namespace MagicFlatIndex.Tables
{
    public class IgnoredActivity : BaseFlatRecord
    {
        public int FiggoId { get; set; }

        private const int SIZE_ID = sizeof(int);
        private const int SIZE_FIGGOID = sizeof(int);

        private const int POS_ID = 0;
        private const int POS_FIGGOID = SIZE_FIGGOID;

        public override byte[] ToBytes()
        {
            byte[] res = new byte[SIZE_ID + SIZE_FIGGOID];

            byte[] tmpId = BitConverter.GetBytes(Id);
            tmpId.CopyTo(res, POS_ID);

            byte[] tmpFiggoId = BitConverter.GetBytes(FiggoId);
            tmpFiggoId.CopyTo(res, POS_FIGGOID);

            return res;
        }

        public static new BaseFlatRecord FromBytes(byte[] bytes)
        {
            return new IgnoredActivity
            { 
                Id = BitConverter.ToInt32(bytes, 0), 
                FiggoId = BitConverter.ToInt32(bytes, POS_FIGGOID)
            };
        }

        public static new int GetSize()
        {
            return SIZE_ID + SIZE_FIGGOID;
        }

        public override string ToString()
        {
            return $"{Id} - {FiggoId}";
        }
    }
}
