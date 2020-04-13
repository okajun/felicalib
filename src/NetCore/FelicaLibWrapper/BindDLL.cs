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
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace FelicaLibWrapper
{
    /// <summary>
    /// DLL遅延バインディングクラス
    /// </summary>
    public class BindDLL : IDisposable
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)]  string lpProcName);

        private IntPtr _pModule;

        /// <summary>
        /// DLLのロード・オブジェクト生成
        /// </summary>
        /// <param name="szFilename">バインドするDLL名</param>
        public BindDLL(string szFilename)
        {
            _pModule = BindDLL.LoadLibrary(szFilename);
            if (_pModule != IntPtr.Zero)
            {
                return;
            }
            int nResult = Marshal.GetHRForLastWin32Error();
            throw Marshal.GetExceptionForHR(nResult);
        }

        /// <summary>
        /// 指定名のアンマネージ関数ポインタをデリゲートに変換
        /// </summary>
        /// <param name="szProcName">アンマネージ関数名</param>
        /// <param name="typDelegate">変換するデリゲートのType</param>
        /// <returns>変換したデリゲート</returns>
        public Delegate GetDelegate(string szProcName, Type typDelegate)
        {
            IntPtr pProc = BindDLL.GetProcAddress(_pModule, szProcName);
            if (pProc != IntPtr.Zero)
            {
                Delegate oDG = Marshal.GetDelegateForFunctionPointer(pProc, typDelegate);
                return oDG;
            }
            int nResult = Marshal.GetHRForLastWin32Error();
            throw Marshal.GetExceptionForHR(nResult);
        }

        #region IDisposable メンバ

        public void Dispose()
        {
            if (_pModule != IntPtr.Zero)
            {
                BindDLL.FreeLibrary(_pModule);
            }
        }

        #endregion
    }
}
