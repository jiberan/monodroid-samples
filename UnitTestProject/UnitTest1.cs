using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {

            //ident
            GetMessage(1, 0, 2);

            //gps
            GetMessage(3, 0, 3);

            //tachometr 
            GetMessage(3, 0, 6);

            //tachograf
            GetMessage(3, 0, 8);

            //datumcas
            GetMessage(3, 0, 2);
        }




        public byte[] GetMessage(byte idPaketu, byte idTransakce, byte idVeliciny)
        {
            var data = new List<byte>();
            data.Add(179); //0xB3


            if (idPaketu == 3)
            { // dotaz na velicinu
                if (idVeliciny == 8)
                {
                    //tachograf

                    //karta1
                    data.Add(53); //delka paketu
                    data.Add(4); //id paketu
                    data.Add(idTransakce); //id transakce
                    data.Add((byte)0); //id transakce
                    data.AddRange(GetBytes((ushort)8)); //id transakce

                    //data.Add((GetVelicina(Resource.Id.driverCard1))); //datove pole (velicina)
                    //data.AddRange(GetTextInfo(Resource.Id.driverCard1Id)); //datove pole (velicina)

                    ////karta2
                    //data.Add((GetVelicina(Resource.Id.driverCard2))); //datove pole (velicina)
                    //data.AddRange(GetTextInfo(Resource.Id.driverCard2Id)); //datove pole (velicina)

                }
                else if (idVeliciny == 2)
                {
                    //datum cas
                    var datetime = DateTime.Now;

                    data.Add(13); //delka paketu
                    data.Add(4); //id paketu
                    data.Add(idTransakce); //id transakce
                    data.Add((byte)0); //id transakce


                    data.Add((byte)datetime.Day);
                    data.Add((byte)datetime.Month);
                    data.Add((byte)Convert.ToInt32(datetime.ToString("yy")));
                    data.Add((byte)datetime.Hour);
                    data.Add((byte)datetime.Minute);
                    data.Add((byte)datetime.Second);
                }

                else if (idVeliciny == 3)
                {
                    //GPS
                    data.Add(22); //delka paketu
                    data.Add(4); //id paketu
                    data.Add(idTransakce); //id transakce
                    data.Add((byte)0); //id transakce

                    data.Add(1);  //platna pozice
                    data.Add(0);  //2D
                    data.Add(0);  //3D
                    data.Add(0);  //sirka
                    data.Add(0);  //delka
                    data.Add(0); //rezerva
                    data.Add(0); //rezerva

                    var x = (50.0183058 * 60) / 0.0001;
                    data.AddRange(BitConverter.GetBytes(Convert.ToInt32(x)));


                    var y = (14.5503456 * 60) / 0.0001;
                    data.AddRange(BitConverter.GetBytes(Convert.ToInt32(y)));
                }

                else if (idVeliciny == 7)
                {
                    //tachometr
                    data.Add(11); //delka paketu
                    data.Add(4); //id paketu
                    data.Add(idTransakce); //id transakce
                    data.Add((byte)0); //id transakce

                    var tach = new Random().Next(10000000, 90000000); //4byte long dle pdf? nevim jak
                    data.AddRange(BitConverter.GetBytes(tach));
                }
            }
            else if (idPaketu == 1)
            {
                //dotaz na identifikaci

                data.Add(28); //delka paketu
                data.Add(2); //id paketu
                data.Add(idTransakce); //id transakce
                data.Add((byte)0); //id transakce

                var tach = new Random().Next(10000000, 90000000); //4byte long dle pdf? nevim jak
                data.AddRange(BitConverter.GetBytes(tach));

                data.Add((byte)1); //verze protokolu

                data.AddRange(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6});
            }

            var crc = GetCRC(data.ToArray());
            data.AddRange(GetBytes(crc)); //crc

            var bytes = data.ToArray();
            return bytes;
        }

        byte GetVelicina()
        {
            return Convert.ToByte("007");
        }

        byte[] GetTextInfo()
        {

            byte[] bytes = Encoding.ASCII.GetBytes("4");
            List<byte> newBytes = new List<byte>();
            newBytes.AddRange(bytes);
            newBytes.AddRange(new byte[21 - bytes.Length]);
            return newBytes.ToArray();
        }

        public static ushort GetCRC(byte[] buff)
        {
            ushort d = 62000;
            ushort crc;
            byte i, k;
            ushort pomW;
            crc = 0xFFFF;
            for (i = 0; i < buff.Length; i++)
            {
                int pom_i = buff[i] << 8;
                pomW = (ushort)(pom_i);
                for (k = 0; k < 8; k++)
                {
                    var val = (crc ^ pomW) & 0x8000;
                    if (val > 0)
                    {
                        int crci = (crc << 1) ^ 0x1021;
                        crc = (ushort)crci;
                    }
                    else
                        crc <<= 1;
                    pomW <<= 1;
                }
            }
            return (crc);
        }
        private byte[] GetBytes(ushort val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            Array.Reverse(intBytes);

            return intBytes;
        }
    }

}
