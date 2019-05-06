#include <stdint.h>
#include <stdlib.h>

extern "C"
{
void sha256_transform(uint32_t *state, const uint32_t *block, int swap);
void sha256_block_data_order (uint32_t *ctx, const void *in, size_t num);
}

namespace sha256_arm
{
    void Transform(uint32_t* s, const unsigned char* chunk, size_t blocks)
    {
        sha256_block_data_order(s, chunk, blocks);
        /*while(blocks--)
        {
            ::sha256_transform(s, (uint32_t*) chunk, 0);
            chunk += 64;
        }*/
    }
}
