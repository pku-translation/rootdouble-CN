/***************************************************************
    CSharp version of LZSS.C
    (based on 4/6/1989 Haruhiko Okumura implementation)
**************************************************************/

// ReSharper disable All
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CSYetiTools
{
    internal static class LZSS
    {
        private const int N = 4096;
        private const int F = 18;
        private const uint Threshold = 2;
        private const int Nil = N;

        public static IEnumerable<byte> Encode(IEnumerable<byte> bytes)
        {
            var textsize = 0;
            var codesize = 0;

            var lson = new int[N + 1];
            var rson = new int[N + 257];
            var dad = new int[N + 1];
            int match_position = 0, match_length = 0;
            var text_buf = new byte[N + F - 1];
            var code_buf = new byte[17];
            byte mask = 1;

            //InitTree();
            for (int i = N + 1; i <= N + 256; ++i) rson[i] = Nil;
            for (int i = 0; i < N; ++i) dad[i] = Nil;

            void InsertNode(int _r)
            {
                int cmp = 1;
                int key_index = _r;
                int p = N + 1 + text_buf[key_index];

                rson[_r] = lson[_r] = Nil; match_length = 0;
                for (; ; )
                {
                    if (cmp >= 0)
                    {
                        if (rson[p] != Nil) p = rson[p];
                        else { rson[p] = _r; dad[_r] = p; return; }
                    }
                    else
                    {
                        if (lson[p] != Nil) p = lson[p];
                        else { lson[p] = _r; dad[_r] = p; return; }
                    }
                    int i = 1;
                    for (; i < F; ++i)
                        if ((cmp = text_buf[key_index + i] - text_buf[p + i]) != 0) break;
                    if (i > match_length)
                    {
                        match_position = p;
                        if ((match_length = i) >= F) break;
                    }
                }
                dad[_r] = dad[p]; lson[_r] = lson[p]; rson[_r] = rson[p];
                dad[lson[p]] = _r; dad[rson[p]] = _r;
                if (rson[dad[p]] == p) rson[dad[p]] = _r;
                else lson[dad[p]] = _r;
                dad[p] = Nil;  /* remove p */
            }

            void DeleteNode(int _p)  /* deletes node p from tree */
            {
                int q;

                if (dad[_p] == Nil) return;  /* not in tree */
                if (rson[_p] == Nil) q = lson[_p];
                else if (lson[_p] == Nil) q = rson[_p];
                else
                {
                    q = lson[_p];
                    if (rson[q] != Nil)
                    {
                        do { q = rson[q]; } while (rson[q] != Nil);
                        rson[dad[q]] = lson[q]; dad[lson[q]] = dad[q];
                        lson[q] = lson[_p]; dad[lson[_p]] = q;
                    }
                    rson[q] = rson[_p]; dad[rson[_p]] = q;
                }
                dad[q] = dad[_p];
                if (rson[dad[_p]] == _p) rson[dad[_p]] = q; else lson[dad[_p]] = q;
                dad[_p] = Nil;
            }

            code_buf[0] = 0;
            int code_buf_ptr = mask = 1;
            int s = 0; int r = N - F;

            using var enumerator = bytes.GetEnumerator();
            int len;
            for (len = 0; len < F && enumerator.MoveNext(); ++len)
                text_buf[r + len] = enumerator.Current;
            if ((textsize = len) == 0) yield break;
            for (int i = 1; i <= F; ++i) InsertNode(r - i);
            InsertNode(r);
            do
            {
                int i;
                if (match_length > len) match_length = len;
                if (match_length <= Threshold)
                {
                    match_length = 1;
                    code_buf[0] |= mask;
                    code_buf[code_buf_ptr++] = text_buf[r];
                }
                else
                {
                    code_buf[code_buf_ptr++] = (byte)match_position;
                    code_buf[code_buf_ptr++] = (byte)(((match_position >> 4) & 0xf0u) | (match_length - (Threshold + 1)));
                }
                if ((mask <<= 1) == 0)
                {
                    for (i = 0; i < code_buf_ptr; ++i)
                        yield return code_buf[i];
                    codesize += code_buf_ptr;
                    code_buf[0] = 0; code_buf_ptr = mask = 1;
                }
                int last_match_length = match_length;
                
                for (i = 0; i < last_match_length && enumerator.MoveNext(); ++i)
                {
                    byte c = enumerator.Current;
                    DeleteNode(s);
                    text_buf[s] = c;
                    if (s < F - 1) text_buf[s + N] = c;
                    s = (s + 1) & (N - 1); r = (r + 1) & (N - 1);
                    InsertNode(r);
                }
                while (i++ < last_match_length)
                {
                    DeleteNode(s);
                    s = (s + 1) & (N - 1); r = (r + 1) & (N - 1);
                    if (--len > 0) InsertNode(r);
                }
            } while (len > 0);
            if (code_buf_ptr > 1)
            {
                for (int i = 0; i < code_buf_ptr; ++i) yield return code_buf[i];
                codesize += code_buf_ptr;
            }
        }

        public static IEnumerable<byte> Decode(IEnumerable<byte> bytes)
        {
            var text_buf = new byte[N];

            int r = N - F;
            uint flags = 0;
            using var enumerator = bytes.GetEnumerator();
            while (true)
            {
                if (((flags >>= 1) & 256u) == 0u)
                {
                    if (!enumerator.MoveNext()) break;
                    flags = enumerator.Current | 0xff00u;     /* uses higher byte cleverly */
                }                           /* to count eight */
                if ((flags & 1u) == 1u)
                {
                    if (!enumerator.MoveNext()) break;
                    byte c = enumerator.Current;
                    yield return c;
                    text_buf[r++] = c;
                    r &= (N - 1);
                }
                else
                {
                    if (!enumerator.MoveNext()) break;
                    uint i = enumerator.Current;
                    if (!enumerator.MoveNext()) break;
                    uint j = enumerator.Current;
                    i |= ((j & 0xf0u) << 4);
                    j = (j & 0x0fu) + Threshold;
                    for (uint k = 0; k <= j; k++)
                    {
                        byte c = text_buf[(i + k) & (N - 1)];
                        yield return c;
                        text_buf[r++] = c;
                        r &= (N - 1);
                    }
                }
            }
        }
    }
}