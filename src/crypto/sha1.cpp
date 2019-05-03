// Copyright (c) 2014-2018 The Bitcoin Core developers
// Distributed under the MIT software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

#include <crypto/sha1.h>

#include <crypto/common.h>

#include <string.h>
#include <stdio.h>

#if defined(__aarch32__) || defined(__aarch64__)
#include <arm_neon.h>
#define USING_ARMV8_CRYPTO_EXT
#endif

#if defined(__aarch32__) || defined(__aarch64__)
// Neon version of memcpy for ArmV8. Customised to process 16bytes.
void inline NeonMemCpy16bytes(uint32_t* ptr, uint32_t* x)
{
    alignas(16) uint32x4_t *dst = reinterpret_cast<uint32x4_t*>(ptr);
    alignas(16) uint32x4_t *src = reinterpret_cast<uint32x4_t*>(x);
    *dst = *src;
}
#endif

// Internal implementation code.
namespace
{
/// Internal SHA-1 implementation.
namespace sha1
{

#if !defined(USING_ARMV8_CRYPTO_EXT)
/** One round of SHA-1. */
void inline Round(uint32_t a, uint32_t& b, uint32_t c, uint32_t d, uint32_t& e, uint32_t f, uint32_t k, uint32_t w)
{
    e += ((a << 5) | (a >> 27)) + f + k + w;
    b = (b << 30) | (b >> 2);
}

uint32_t inline f1(uint32_t b, uint32_t c, uint32_t d) { return d ^ (b & (c ^ d)); }
uint32_t inline f2(uint32_t b, uint32_t c, uint32_t d) { return b ^ c ^ d; }
uint32_t inline f3(uint32_t b, uint32_t c, uint32_t d) { return (b & c) | (d & (b | c)); }

uint32_t inline left(uint32_t x) { return (x << 1) | (x >> 31); }

const uint32_t k1 = 0x5A827999ul;
const uint32_t k2 = 0x6ED9EBA1ul;
const uint32_t k3 = 0x8F1BBCDCul;
const uint32_t k4 = 0xCA62C1D6ul;

#else

alignas(16) uint32_t sha[5] {
    0x67452301ul, 0xEFCDAB89ul, 0x98BADCFEul, 0x10325476ul, 0xC3D2E1F0ul
};

#endif // USING_ARMV8_CRYPTO_EXT

/** Initialize SHA-1 state. */
void inline Initialize(uint32_t* s)
{
#if defined(__aarch32__) || defined(__aarch64__)
    NeonMemCpy16bytes(s, sha);
#else
    s[0] = 0x67452301ul;
    s[1] = 0xEFCDAB89ul;
    s[2] = 0x98BADCFEul;
    s[3] = 0x10325476ul;
#endif
    s[4] = 0xC3D2E1F0ul;
}

/** Perform a SHA-1 transformation, processing a 64-byte chunk. */
void Transform(uint32_t* s, const unsigned char* chunk)
{

#if defined(USING_ARMV8_CRYPTO_EXT)

   alignas(16) uint32x4_t ABCD;
   alignas(16) uint32_t E0;

   // Load magic constants
   alignas(16) const uint32x4_t C0 = vdupq_n_u32(0x5A827999);
   alignas(16) const uint32x4_t C1 = vdupq_n_u32(0x6ED9EBA1);
   alignas(16) const uint32x4_t C2 = vdupq_n_u32(0x8F1BBCDC);
   alignas(16) const uint32x4_t C3 = vdupq_n_u32(0xCA62C1D6);

   ABCD = vld1q_u32(&s[0]);
   E0 = s[4];

   alignas(16) const uint32x4_t* input32 = reinterpret_cast<const uint32x4_t*>(chunk);

      // Save current hash
      alignas(16) const uint32x4_t ABCD_SAVED = ABCD;
      alignas(16) const uint32_t E0_SAVED = E0;

      alignas(16) uint32x4_t MSG0, MSG1, MSG2, MSG3;
      alignas(16) uint32x4_t TMP0, TMP1;
      alignas(16) uint32_t E1;

      MSG0 = *input32++;
      MSG1 = *input32++;
      MSG2 = *input32++;
      MSG3 = *input32++;

      MSG0 = vreinterpretq_u32_u8(vrev32q_u8(vreinterpretq_u8_u32(MSG0)));
      MSG1 = vreinterpretq_u32_u8(vrev32q_u8(vreinterpretq_u8_u32(MSG1)));
      MSG2 = vreinterpretq_u32_u8(vrev32q_u8(vreinterpretq_u8_u32(MSG2)));
      MSG3 = vreinterpretq_u32_u8(vrev32q_u8(vreinterpretq_u8_u32(MSG3)));

      TMP0 = vaddq_u32(MSG0, C0);
      TMP1 = vaddq_u32(MSG1, C0);

      // Rounds 0-3
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1cq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG2, C0);
      MSG0 = vsha1su0q_u32(MSG0, MSG1, MSG2);

      // Rounds 4-7
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1cq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG3, C0);
      MSG0 = vsha1su1q_u32(MSG0, MSG3);
      MSG1 = vsha1su0q_u32(MSG1, MSG2, MSG3);

      // Rounds 8-11
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1cq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG0, C0);
      MSG1 = vsha1su1q_u32(MSG1, MSG0);
      MSG2 = vsha1su0q_u32(MSG2, MSG3, MSG0);

      // Rounds 12-15
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1cq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG1, C1);
      MSG2 = vsha1su1q_u32(MSG2, MSG1);
      MSG3 = vsha1su0q_u32(MSG3, MSG0, MSG1);

      // Rounds 16-19
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1cq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG2, C1);
      MSG3 = vsha1su1q_u32(MSG3, MSG2);
      MSG0 = vsha1su0q_u32(MSG0, MSG1, MSG2);

      // Rounds 20-23
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG3, C1);
      MSG0 = vsha1su1q_u32(MSG0, MSG3);
      MSG1 = vsha1su0q_u32(MSG1, MSG2, MSG3);

      // Rounds 24-27
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG0, C1);
      MSG1 = vsha1su1q_u32(MSG1, MSG0);
      MSG2 = vsha1su0q_u32(MSG2, MSG3, MSG0);

      // Rounds 28-31
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG1, C1);
      MSG2 = vsha1su1q_u32(MSG2, MSG1);
      MSG3 = vsha1su0q_u32(MSG3, MSG0, MSG1);

      // Rounds 32-35
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG2, C2);
      MSG3 = vsha1su1q_u32(MSG3, MSG2);
      MSG0 = vsha1su0q_u32(MSG0, MSG1, MSG2);

      // Rounds 36-39
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG3, C2);
      MSG0 = vsha1su1q_u32(MSG0, MSG3);
      MSG1 = vsha1su0q_u32(MSG1, MSG2, MSG3);

      // Rounds 40-43
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1mq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG0, C2);
      MSG1 = vsha1su1q_u32(MSG1, MSG0);
      MSG2 = vsha1su0q_u32(MSG2, MSG3, MSG0);

      // Rounds 44-47
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1mq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG1, C2);
      MSG2 = vsha1su1q_u32(MSG2, MSG1);
      MSG3 = vsha1su0q_u32(MSG3, MSG0, MSG1);

      // Rounds 48-51
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1mq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG2, C2);
      MSG3 = vsha1su1q_u32(MSG3, MSG2);
      MSG0 = vsha1su0q_u32(MSG0, MSG1, MSG2);

      // Rounds 52-55
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1mq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG3, C3);
      MSG0 = vsha1su1q_u32(MSG0, MSG3);
      MSG1 = vsha1su0q_u32(MSG1, MSG2, MSG3);

      // Rounds 56-59
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1mq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG0, C3);
      MSG1 = vsha1su1q_u32(MSG1, MSG0);
      MSG2 = vsha1su0q_u32(MSG2, MSG3, MSG0);

      // Rounds 60-63
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG1, C3);
      MSG2 = vsha1su1q_u32(MSG2, MSG1);
      MSG3 = vsha1su0q_u32(MSG3, MSG0, MSG1);

      // Rounds 64-67
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E0, TMP0);
      TMP0 = vaddq_u32(MSG2, C3);
      MSG3 = vsha1su1q_u32(MSG3, MSG2);
      MSG0 = vsha1su0q_u32(MSG0, MSG1, MSG2);

      // Rounds 68-71
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E1, TMP1);
      TMP1 = vaddq_u32(MSG3, C3);
      MSG0 = vsha1su1q_u32(MSG0, MSG3);

      // Rounds 72-75
      E1 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E0, TMP0);

      // Rounds 76-79
      E0 = vsha1h_u32(vgetq_lane_u32(ABCD, 0));
      ABCD = vsha1pq_u32(ABCD, E1, TMP1);

      // Add state back
      E0 += E0_SAVED;
      ABCD = vaddq_u32(ABCD_SAVED, ABCD);

   // Save digest

   vst1q_u32(&s[0], ABCD);
   s[4] = E0;

#else // Regular C implementation

    uint32_t a = s[0], b = s[1], c = s[2], d = s[3], e = s[4];
    uint32_t w0, w1, w2, w3, w4, w5, w6, w7, w8, w9, w10, w11, w12, w13, w14, w15;

    Round(a, b, c, d, e, f1(b, c, d), k1, w0 = ReadBE32(chunk + 0));
    Round(e, a, b, c, d, f1(a, b, c), k1, w1 = ReadBE32(chunk + 4));
    Round(d, e, a, b, c, f1(e, a, b), k1, w2 = ReadBE32(chunk + 8));
    Round(c, d, e, a, b, f1(d, e, a), k1, w3 = ReadBE32(chunk + 12));
    Round(b, c, d, e, a, f1(c, d, e), k1, w4 = ReadBE32(chunk + 16));
    Round(a, b, c, d, e, f1(b, c, d), k1, w5 = ReadBE32(chunk + 20));
    Round(e, a, b, c, d, f1(a, b, c), k1, w6 = ReadBE32(chunk + 24));
    Round(d, e, a, b, c, f1(e, a, b), k1, w7 = ReadBE32(chunk + 28));
    Round(c, d, e, a, b, f1(d, e, a), k1, w8 = ReadBE32(chunk + 32));
    Round(b, c, d, e, a, f1(c, d, e), k1, w9 = ReadBE32(chunk + 36));
    Round(a, b, c, d, e, f1(b, c, d), k1, w10 = ReadBE32(chunk + 40));
    Round(e, a, b, c, d, f1(a, b, c), k1, w11 = ReadBE32(chunk + 44));
    Round(d, e, a, b, c, f1(e, a, b), k1, w12 = ReadBE32(chunk + 48));
    Round(c, d, e, a, b, f1(d, e, a), k1, w13 = ReadBE32(chunk + 52));
    Round(b, c, d, e, a, f1(c, d, e), k1, w14 = ReadBE32(chunk + 56));
    Round(a, b, c, d, e, f1(b, c, d), k1, w15 = ReadBE32(chunk + 60));

    Round(e, a, b, c, d, f1(a, b, c), k1, w0 = left(w0 ^ w13 ^ w8 ^ w2));
    Round(d, e, a, b, c, f1(e, a, b), k1, w1 = left(w1 ^ w14 ^ w9 ^ w3));
    Round(c, d, e, a, b, f1(d, e, a), k1, w2 = left(w2 ^ w15 ^ w10 ^ w4));
    Round(b, c, d, e, a, f1(c, d, e), k1, w3 = left(w3 ^ w0 ^ w11 ^ w5));
    Round(a, b, c, d, e, f2(b, c, d), k2, w4 = left(w4 ^ w1 ^ w12 ^ w6));
    Round(e, a, b, c, d, f2(a, b, c), k2, w5 = left(w5 ^ w2 ^ w13 ^ w7));
    Round(d, e, a, b, c, f2(e, a, b), k2, w6 = left(w6 ^ w3 ^ w14 ^ w8));
    Round(c, d, e, a, b, f2(d, e, a), k2, w7 = left(w7 ^ w4 ^ w15 ^ w9));
    Round(b, c, d, e, a, f2(c, d, e), k2, w8 = left(w8 ^ w5 ^ w0 ^ w10));
    Round(a, b, c, d, e, f2(b, c, d), k2, w9 = left(w9 ^ w6 ^ w1 ^ w11));
    Round(e, a, b, c, d, f2(a, b, c), k2, w10 = left(w10 ^ w7 ^ w2 ^ w12));
    Round(d, e, a, b, c, f2(e, a, b), k2, w11 = left(w11 ^ w8 ^ w3 ^ w13));
    Round(c, d, e, a, b, f2(d, e, a), k2, w12 = left(w12 ^ w9 ^ w4 ^ w14));
    Round(b, c, d, e, a, f2(c, d, e), k2, w13 = left(w13 ^ w10 ^ w5 ^ w15));
    Round(a, b, c, d, e, f2(b, c, d), k2, w14 = left(w14 ^ w11 ^ w6 ^ w0));
    Round(e, a, b, c, d, f2(a, b, c), k2, w15 = left(w15 ^ w12 ^ w7 ^ w1));

    Round(d, e, a, b, c, f2(e, a, b), k2, w0 = left(w0 ^ w13 ^ w8 ^ w2));
    Round(c, d, e, a, b, f2(d, e, a), k2, w1 = left(w1 ^ w14 ^ w9 ^ w3));
    Round(b, c, d, e, a, f2(c, d, e), k2, w2 = left(w2 ^ w15 ^ w10 ^ w4));
    Round(a, b, c, d, e, f2(b, c, d), k2, w3 = left(w3 ^ w0 ^ w11 ^ w5));
    Round(e, a, b, c, d, f2(a, b, c), k2, w4 = left(w4 ^ w1 ^ w12 ^ w6));
    Round(d, e, a, b, c, f2(e, a, b), k2, w5 = left(w5 ^ w2 ^ w13 ^ w7));
    Round(c, d, e, a, b, f2(d, e, a), k2, w6 = left(w6 ^ w3 ^ w14 ^ w8));
    Round(b, c, d, e, a, f2(c, d, e), k2, w7 = left(w7 ^ w4 ^ w15 ^ w9));
    Round(a, b, c, d, e, f3(b, c, d), k3, w8 = left(w8 ^ w5 ^ w0 ^ w10));
    Round(e, a, b, c, d, f3(a, b, c), k3, w9 = left(w9 ^ w6 ^ w1 ^ w11));
    Round(d, e, a, b, c, f3(e, a, b), k3, w10 = left(w10 ^ w7 ^ w2 ^ w12));
    Round(c, d, e, a, b, f3(d, e, a), k3, w11 = left(w11 ^ w8 ^ w3 ^ w13));
    Round(b, c, d, e, a, f3(c, d, e), k3, w12 = left(w12 ^ w9 ^ w4 ^ w14));
    Round(a, b, c, d, e, f3(b, c, d), k3, w13 = left(w13 ^ w10 ^ w5 ^ w15));
    Round(e, a, b, c, d, f3(a, b, c), k3, w14 = left(w14 ^ w11 ^ w6 ^ w0));
    Round(d, e, a, b, c, f3(e, a, b), k3, w15 = left(w15 ^ w12 ^ w7 ^ w1));

    Round(c, d, e, a, b, f3(d, e, a), k3, w0 = left(w0 ^ w13 ^ w8 ^ w2));
    Round(b, c, d, e, a, f3(c, d, e), k3, w1 = left(w1 ^ w14 ^ w9 ^ w3));
    Round(a, b, c, d, e, f3(b, c, d), k3, w2 = left(w2 ^ w15 ^ w10 ^ w4));
    Round(e, a, b, c, d, f3(a, b, c), k3, w3 = left(w3 ^ w0 ^ w11 ^ w5));
    Round(d, e, a, b, c, f3(e, a, b), k3, w4 = left(w4 ^ w1 ^ w12 ^ w6));
    Round(c, d, e, a, b, f3(d, e, a), k3, w5 = left(w5 ^ w2 ^ w13 ^ w7));
    Round(b, c, d, e, a, f3(c, d, e), k3, w6 = left(w6 ^ w3 ^ w14 ^ w8));
    Round(a, b, c, d, e, f3(b, c, d), k3, w7 = left(w7 ^ w4 ^ w15 ^ w9));
    Round(e, a, b, c, d, f3(a, b, c), k3, w8 = left(w8 ^ w5 ^ w0 ^ w10));
    Round(d, e, a, b, c, f3(e, a, b), k3, w9 = left(w9 ^ w6 ^ w1 ^ w11));
    Round(c, d, e, a, b, f3(d, e, a), k3, w10 = left(w10 ^ w7 ^ w2 ^ w12));
    Round(b, c, d, e, a, f3(c, d, e), k3, w11 = left(w11 ^ w8 ^ w3 ^ w13));
    Round(a, b, c, d, e, f2(b, c, d), k4, w12 = left(w12 ^ w9 ^ w4 ^ w14));
    Round(e, a, b, c, d, f2(a, b, c), k4, w13 = left(w13 ^ w10 ^ w5 ^ w15));
    Round(d, e, a, b, c, f2(e, a, b), k4, w14 = left(w14 ^ w11 ^ w6 ^ w0));
    Round(c, d, e, a, b, f2(d, e, a), k4, w15 = left(w15 ^ w12 ^ w7 ^ w1));

    Round(b, c, d, e, a, f2(c, d, e), k4, w0 = left(w0 ^ w13 ^ w8 ^ w2));
    Round(a, b, c, d, e, f2(b, c, d), k4, w1 = left(w1 ^ w14 ^ w9 ^ w3));
    Round(e, a, b, c, d, f2(a, b, c), k4, w2 = left(w2 ^ w15 ^ w10 ^ w4));
    Round(d, e, a, b, c, f2(e, a, b), k4, w3 = left(w3 ^ w0 ^ w11 ^ w5));
    Round(c, d, e, a, b, f2(d, e, a), k4, w4 = left(w4 ^ w1 ^ w12 ^ w6));
    Round(b, c, d, e, a, f2(c, d, e), k4, w5 = left(w5 ^ w2 ^ w13 ^ w7));
    Round(a, b, c, d, e, f2(b, c, d), k4, w6 = left(w6 ^ w3 ^ w14 ^ w8));
    Round(e, a, b, c, d, f2(a, b, c), k4, w7 = left(w7 ^ w4 ^ w15 ^ w9));
    Round(d, e, a, b, c, f2(e, a, b), k4, w8 = left(w8 ^ w5 ^ w0 ^ w10));
    Round(c, d, e, a, b, f2(d, e, a), k4, w9 = left(w9 ^ w6 ^ w1 ^ w11));
    Round(b, c, d, e, a, f2(c, d, e), k4, w10 = left(w10 ^ w7 ^ w2 ^ w12));
    Round(a, b, c, d, e, f2(b, c, d), k4, w11 = left(w11 ^ w8 ^ w3 ^ w13));
    Round(e, a, b, c, d, f2(a, b, c), k4, w12 = left(w12 ^ w9 ^ w4 ^ w14));
    Round(d, e, a, b, c, f2(e, a, b), k4, left(w13 ^ w10 ^ w5 ^ w15));
    Round(c, d, e, a, b, f2(d, e, a), k4, left(w14 ^ w11 ^ w6 ^ w0));
    Round(b, c, d, e, a, f2(c, d, e), k4, left(w15 ^ w12 ^ w7 ^ w1));

    s[0] += a;
    s[1] += b;
    s[2] += c;
    s[3] += d;
    s[4] += e;

#endif // USING_ARMV8_CRYPTO_EXT

}

} // namespace sha1

} // namespace

////// SHA1

CSHA1::CSHA1() : bytes(0)
{
    sha1::Initialize(s);
}

CSHA1& CSHA1::Write(const unsigned char* data, size_t len)
{
    alignas(16) const unsigned char* end = data + len;
    size_t bufsize = bytes % 64;
    if (bufsize && bufsize + len >= 64) {
        // Fill the buffer, and process it.
        memcpy(buf + bufsize, data, 64 - bufsize);
        bytes += 64 - bufsize;
        data += 64 - bufsize;
        sha1::Transform(s, buf);
        bufsize = 0;
    }
    while (end >= data + 64) {
        // Process full chunks directly from the source.
        sha1::Transform(s, data);
        bytes += 64;
        data += 64;
    }
    if (end > data) {
        // Fill the buffer with what remains.
        memcpy(buf + bufsize, data, end - data);
        bytes += end - data;
    }
    return *this;
}

#if defined(__aarch32__) || defined(__aarch64__)
// Neon version of bswap32 for aarch64. Customised to process 16bytes.
void inline WriteBE32Neon16bytes(unsigned char* ptr, uint32_t* x)
{
    alignas(16) uint32x4_t *dst = reinterpret_cast<uint32x4_t*>(ptr);
    *dst = vreinterpretq_u32_u8(vrev32q_u8(vreinterpretq_u8_u32(vld1q_u32(x))));
}
#endif

void CSHA1::Finalize(unsigned char hash[OUTPUT_SIZE])
{
    alignas(16) static const unsigned char pad[64] = {0x80};
    alignas(16) unsigned char sizedesc[8];
    WriteBE64(sizedesc, bytes << 3);
    Write(pad, 1 + ((119 - (bytes % 64)) % 64));
    Write(sizedesc, 8);
#if defined(__aarch32__) || defined(__aarch64__)
    WriteBE32Neon16bytes(&hash[0], &s[0]);
#else
    WriteBE32(hash, s[0]);
    WriteBE32(hash + 4, s[1]);
    WriteBE32(hash + 8, s[2]);
    WriteBE32(hash + 12, s[3]);
#endif
    WriteBE32(hash + 16, s[4]); // Final 4 bytes including what Neon bwswap32 misses
}

CSHA1& CSHA1::Reset()
{
    bytes = 0;
    sha1::Initialize(s);
    return *this;
}
