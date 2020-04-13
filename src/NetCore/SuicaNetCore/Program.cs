/*
 felicalib - FeliCa access wrapper library

 Copyright (c) 2007-2010, Takuya Murakami, All rights reserved.
 Copyright (c) 2020, Jun-ichi Okada, All rights reserved.

 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions are
 met:

 1. Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer. 

 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution. 

 3. Neither the name of the project nor the names of its contributors
    may be used to endorse or promote products derived from this software
    without specific prior written permission. 

 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Linq;
using FelicaLibWrapper;


namespace SuicaNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            using var felica = new Felica();
            OutputSuica(felica);
        }

        private const int ServiceSuicaHistory = 0x090f;

        private static void OutputSuica(Felica felica)
        {

            felica.Polling((int)SystemCode.Suica);
            Console.WriteLine($"IDm: {string.Join("",felica.IDm().Select(e => e.ToString("x02")))}");

            for (int i = 0; ; i++)
            {
                byte[] bytes = felica.ReadWithoutEncryption(ServiceSuicaHistory, i);
                if( null == bytes)
                {
                    break;
                }
                dumpSuicaHistory(bytes.AsSpan());
            }
        }

        private static void dumpSuicaHistory(ReadOnlySpan<byte> span)
        {
            int ctype = span[0];            // 端末種
            int proc = span[1];             // 処理
            int date = read2b(span.Slice(4,2));    // 日付
            int balance = read2b(span.Slice(10,2));// 残高
            balance = reverse2b(balance);
            int seq = read4b(span.Slice(12,4));
            int region = seq & 0xff;        // Region
        
            seq >>= 8;                  // 連番

            int in_line, in_sta, out_line, out_sta;
            int yy, mm, dd;
            int time;
            out_line = -1;
            out_sta = -1;
            time = -1;

            switch (ctype)
            {
                case 0xC7:  // 物販
                case 0xC8:  // 自販機          
                    time = read2b(span.Slice(6, 2));
                    in_line = span[8];
                    in_sta = span[9];
                    break;

                case 0x05:  // 車載機
                    in_line = read2b(span.Slice(6,2));
                    in_sta = read2b(span.Slice(8,2));
                    break;

                default:
                    in_line = span[6];
                    in_sta = span[7];
                    out_line = span[8];
                    out_sta = span[9];
                    break;
            }

            Console.Write($"端末種:{consoleType(ctype)} ");
            Console.Write($"処理:{procType(proc)} ");
            // 日付
            yy = date >> 9;
            mm = (date >> 5) & 0xf;
            dd = date & 0x1f;
            Console.Write($"{yy:d02}/{mm:d02}/{dd:d02} ");

            // 時刻
            if (time > 0)
            {
                int hh = time >> 11;
                int min = (time >> 5) & 0x3f;

                Console.Write($" {hh:d02}:{min:d02} ");
            }
            Console.Write($"入:{in_line:x}/{in_sta:x} ");

            if (out_line != -1)
            {
                Console.Write($"出:{out_line:x}/{out_sta:x} ");
            }

            Console.Write($"残高:{balance} ");
            Console.Write($"連番:{seq} ");
            Console.WriteLine();
        }

        private static int read4b(ReadOnlySpan<byte> span)
        {
            int v;
            v = (span[0]) << 24;
            v |= (span[1]) << 16;
            v |= (span[2]) << 8;
            v |= span[3];
            return v;
        }

        private static int read2b(ReadOnlySpan<byte> span)
        {
            int v;
            v = span[0] << 8;
            v |= span[1];
            return v;
        }

        private static int reverse2b(int v)
        {
            var hi = (v & 0xff00) >> 8;
            var low = (v & 0x00ff) << 8;
            return hi | low;
        }

        private static string consoleType(int ctype)
        {
            return ctype switch
            {
                0x03 => "清算機",
                0x05 => "車載端末",
                0x08 => "券売機",
                0x12 => "券売機",
                0x16 => "改札機",
                0x17 => "簡易改札機",
                0x18 => "窓口端末",
                0x1a => "改札端末",
                0x1b => "携帯電話",
                0x1c => "乗継清算機",
                0x1d => "連絡改札機",
                0xc7 => "物販",
                0xc8 => "自販機",
                _ => "???"
            };
        }

        private static string procType(int proc)
        {
            return proc switch
            {
                0x01=> "運賃支払",
                0x02=> "チャージ",
                0x03=> "券購",
                0x04=> "清算",
                0x07=> "新規",
                0x0d=> "バス",
                0x0f=> "バス",
                0x14=> "オートチャージ",
                0x46=> "物販",
                0x49=> "入金",
                0xc6=> "物販(現金併用)",
                _ => "???"
            };
        }
    }
}
