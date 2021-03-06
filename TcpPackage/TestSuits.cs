﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Incubator.TcpPackage
{
    public class TestSuits
    {
        private static List<byte> raw_data;

        static TestSuits()
        {
            raw_data = new List<byte>();
            var body = "login|123456";
            var body_bytes = Encoding.UTF8.GetBytes(body);
            var head = body_bytes.Length;
            var head_bytes = BitConverter.GetBytes(head);
            raw_data.AddRange(head_bytes);
            raw_data.AddRange(body_bytes);
        }

        private static void ResetData()
        {
            raw_data = new List<byte>();
            var body = "login|123456";
            var body_bytes = Encoding.UTF8.GetBytes(body);
            var head = body_bytes.Length;
            var head_bytes = BitConverter.GetBytes(head);
            raw_data.AddRange(head_bytes);
            raw_data.AddRange(body_bytes);
        }

        public static void FunA()
        {
            Console.WriteLine("=== recv < head ===");
            var gen = new AGenerator(raw_data);
            var data1 = Parser.ReadFullyWithPrefix(gen);
            Console.WriteLine("A = " + Encoding.UTF8.GetString(data1, 0, data1.Length));
            Console.WriteLine();
            ResetData();
        }

        public static void FunB()
        {
            Console.WriteLine("=== recv = head ===");
            var gen = new BGenerator(raw_data);
            var data2 = Parser.ReadFullyWithPrefix(gen);
            Console.WriteLine("B = " + Encoding.UTF8.GetString(data2, 0, data2.Length));
            Console.WriteLine();
            ResetData();
        }

        public static void FunC()
        {
            Console.WriteLine("=== recv < head + message ===");
            var gen = new CGenerator(raw_data);
            var data3 = Parser.ReadFullyWithPrefix(gen);
            Console.WriteLine("C = " + Encoding.UTF8.GetString(data3, 0, data3.Length));
            Console.WriteLine();
            ResetData();
        }

        public static void FunD()
        {
            Console.WriteLine("=== recv = head + message ===");
            var gen = new DGenerator(raw_data);
            var data4 = Parser.ReadFullyWithPrefix(gen);
            Console.WriteLine("D = " + Encoding.UTF8.GetString(data4, 0, data4.Length));
            Console.WriteLine();
            ResetData();
        }

        public static void FunE()
        {
            Console.WriteLine("=== recv > head + message ===");
            var gen = new EGenerator(raw_data);
            var data5 = Parser.ReadFullyWithPrefix(gen);
            Console.WriteLine("E = " + Encoding.UTF8.GetString(data5, 0, data5.Length));
            Console.WriteLine();
            ResetData();
        }
    }
}
