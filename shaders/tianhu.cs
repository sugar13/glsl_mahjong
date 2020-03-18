// compute shader
// 判断天和，依赖公用函数

#version 460 core

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const uint SHOUPAI_N = 28;

layout (std430, binding = 0) readonly buffer in_buffer
{
    // 参数
    layout(offset = 0) uint shoupai[14]; // 参数：14张手牌
};

layout (std430, binding = 1) writeonly buffer out_buffer
{
    // 返回值
    layout(offset = 0) uint out_hule; // 0=没和, 1=和了
    layout(offset = 4) uint out_paixing[1]; // 1=一般, 2=七对, 3=国士
    layout(offset = 8) uint out_hupai[14];
};

uint has_hule(uint tile_n, uint fulu_n, const uint tiles[SHOUPAI_N],
    out uint out_paixing[1], out uint out_hupai[14]);

void main()
{
    uint tiles[SHOUPAI_N];
    for (uint i = 0; i < 14; i++)
    {
        tiles[i] = shoupai[i];
    }
    out_hule = has_hule (14, 0, tiles, out_paixing, out_hupai);

    return;
}

// end of file
