using System;
using System.Collections.Generic;
using System.Text;

using MagicFlatIndex;

namespace TestFlatFile
{
    class Person : BaseFlatRecord
    {
        private const int SIZE_CHAR = 2;

        private const int SIZE_ID = sizeof(int);
        private const int SIZE_NOM = 20;
        private const int SIZE_PRENOM = 20;

        private const int POS_ID = 0;
        private const int POS_NOM = POS_ID + SIZE_ID;
        private const int POS_PRENOM = POS_NOM + SIZE_NOM * SIZE_CHAR;

        private string nom;
        private string prenom;

        
        public string Nom { 
            get
            {
                return nom;
            }
            set
            {
                nom = value.Truncate(SIZE_NOM);
            }
        }
        public string Prenom
        {
            get
            {
                return prenom;
            }
            set
            {
                prenom = value.Truncate(SIZE_PRENOM);
            }
        }

        public override byte[] ToBytes()
        {
            byte[] res = new byte[SIZE_ID + SIZE_NOM * SIZE_CHAR + SIZE_PRENOM * SIZE_CHAR];

            byte[] tmpId = BitConverter.GetBytes(Id);
            tmpId.CopyTo(res, POS_ID);

            byte[] tmpNom = Encoding.Unicode.GetBytes(nom);
            tmpNom.CopyTo(res, POS_NOM);

            byte[] tmpPrenom = Encoding.Unicode.GetBytes(prenom);
            tmpPrenom.CopyTo(res, POS_PRENOM);

            return res;
        }

        public static new BaseFlatRecord FromBytes(byte[] bytes)
        {
            int id = BitConverter.ToInt32(bytes, 0);
            string nom = Encoding.Unicode.GetString(bytes, POS_NOM, SIZE_NOM * SIZE_CHAR).TrimEnd();
            string prenom = Encoding.Unicode.GetString(bytes, POS_PRENOM, SIZE_PRENOM * SIZE_CHAR).TrimEnd();

            return new Person { Id = id, Nom = nom, Prenom = prenom };
        }

        public static new int GetSize()
        {
            return SIZE_ID + SIZE_NOM * SIZE_CHAR + SIZE_PRENOM * SIZE_CHAR;
        }

        public override string ToString()
        {
            return $"{Id} - {Prenom} {Nom}";
        }
    }
}
