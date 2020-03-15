// compute shader
// 判断能否从给定的手牌中剔除一些牌达成和了形

#version 460 core

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const uint SHOUPAI_N = 20;

const uint PAIXING_YIBAN  = 1;
const uint PAIXING_QIDUI  = 2;
const uint PAIXING_GUOSHI = 3;

layout (std430, binding = 0) readonly buffer in_buffer
{
    // 参数
    layout(offset = 0) uint tile_num; // 手牌的总数，最多20张
    layout(offset = 4) uint fulu_num; // 已有副露（含暗杠）的数目
    layout(offset = 8) uint shoupai[SHOUPAI_N]; // 参数：手牌
};

layout (std430, binding = 1) writeonly buffer out_buffer
{
    // 返回值
    layout(offset = 0) uint out_hule; // 0=没和, 1=和了
    layout(offset = 4) uint out_paixing[1]; // 1=一般, 2=七对, 3=国士
    layout(offset = 8) uint out_hupai[14];
};

const uint NO_TILE = 0x00; // 没有牌

const uint TILE_1M = 0x11; // 一萬 🀇
const uint TILE_2M = 0x12; // 二萬 🀈
const uint TILE_3M = 0x13; // 三萬 🀉
const uint TILE_4M = 0x14; // 四萬 🀊
const uint TILE_5M = 0x15; // 五萬 🀋
const uint TILE_6M = 0x16; // 六萬 🀌
const uint TILE_7M = 0x17; // 七萬 🀍
const uint TILE_8M = 0x18; // 八萬 🀎
const uint TILE_9M = 0x19; // 九萬 🀏

const uint TILE_1S = 0x21; // 一索 🀐
const uint TILE_2S = 0x22; // 二索 🀑
const uint TILE_3S = 0x23; // 三索 🀒
const uint TILE_4S = 0x24; // 四索 🀓
const uint TILE_5S = 0x25; // 五索 🀔
const uint TILE_6S = 0x26; // 六索 🀕
const uint TILE_7S = 0x27; // 七索 🀖
const uint TILE_8S = 0x28; // 八索 🀗
const uint TILE_9S = 0x29; // 九索 🀘

const uint TILE_1P = 0x31; // 一筒 🀙
const uint TILE_2P = 0x32; // 二筒 🀚
const uint TILE_3P = 0x33; // 三筒 🀛
const uint TILE_4P = 0x34; // 四筒 🀜
const uint TILE_5P = 0x35; // 五筒 🀝
const uint TILE_6P = 0x36; // 六筒 🀞
const uint TILE_7P = 0x37; // 七筒 🀟
const uint TILE_8P = 0x38; // 八筒 🀠
const uint TILE_9P = 0x39; // 九筒 🀡

const uint TILE_1Z = 0x41; // 東 🀀
const uint TILE_2Z = 0x49; // 南 🀁
const uint TILE_3Z = 0x51; // 西 🀂
const uint TILE_4Z = 0x59; // 北 🀃
const uint TILE_5Z = 0x61; // 白 🀆
const uint TILE_6Z = 0x69; // 發 🀅
const uint TILE_7Z = 0x71; // 中 🀄

const uint TILE_0M = 0x95; // 赤五萬 🀋
const uint TILE_0S = 0xA5; // 赤五索 🀔
const uint TILE_0P = 0xB5; // 赤五筒 🀝

uint has_hule(uint tile_n, uint fulu_n, const uint tiles[SHOUPAI_N],
    out uint out_paixing[1], out uint out_hupai[14])
{
    uint tmp_num = tile_n;
    uint tmp_tiles[SHOUPAI_N] = tiles;

    // 清理赤宝
    uint tile = NO_TILE;
    for (uint i = 0; i < tile_n; i++)
    {
        tile = tmp_tiles[i];
        if (tile == TILE_0M)
        {
            tmp_tiles[i] = TILE_5M;
        }
        else if (tile == TILE_0S)
        {
            tmp_tiles[i] = TILE_5S;
        }
        else if (tile == TILE_0P)
        {
            tmp_tiles[i] = TILE_5P;
        }
    }

    // 冒泡排序
    for (uint i = 0; i < tile_n - 1; i++)
    {
        bool dirty = false;
        for (uint j = tile_n - 1; j > i; j--)
        {
            tile = tmp_tiles[j];
            if (tile < tmp_tiles[j - 1])
            {
                tmp_tiles[j] = tmp_tiles[j - 1];
                tmp_tiles[j - 1] = tile;
                dirty = true;
            }
        }

        if (!dirty)
        {
            break;
        }
    }

    // 移除孤立的牌，仅适用于一般形和七对子
    for (uint i = 0; i < tile_n; i++)
    {
        tile = tmp_tiles[i];

        if (i == 0)
        {
            if (tile < tmp_tiles[i + 1] - 1)
            {
                tmp_num--;
                continue;
            }
        }
        else if (i < tile_n - 1)
        {
            if (tile > tmp_tiles[i - 1] + 1 &&
                tile < tmp_tiles[i + 1] - 1)
            {
                tmp_num--;
                continue;
            }
        }
        else
        {
            if (tile > tmp_tiles[i - 1] + 1)
            {
                tmp_num--;
                continue;
            }
        }

        if (tile_n > tmp_num)
        {
            tmp_tiles[i - (tile_n - tmp_num)] = tile;
        }
    }

    // 判断一般形
    const uint SET_KEZI = 0;
    const uint SET_DUIZI = 1;
    const uint SET_SHUNZI = 2;
    const uint SET_INVALID = 3;

    uint pos_i = 0;
    uint set_i = 0;
    uint pos_j = 0;
    uint set_j = 0;
    uint pos_k = 0;
    uint set_k = 0;
    uint pos_l = 0;
    uint set_l = 0;
    uint pos_m = 0;
    uint set_m = 0;

    uint hule = 0;
    while (hule == 0)
    {
        uint new_num = tmp_num;
        uint eat_num = 14 - 3 * fulu_n;
        uint new_tiles[SHOUPAI_N] = tmp_tiles;

        // 第一组
        if (pos_i + eat_num > new_num)
        {
            break;
        }

        tile = new_tiles[pos_i];

        if (eat_num == 14 &&
            pos_i > 0 &&
            new_tiles[pos_i - 1] == tile)
        {
            pos_i++;

            continue;
        }

        if (eat_num == 14 &&
            set_i == SET_KEZI)
        {
            if (new_tiles[pos_i + 2] == tile)
            {
                out_hupai[0] = tile;
                out_hupai[1] = tile;
                out_hupai[2] = tile;

                for (uint i = 0; pos_i + 3 + i < new_num; i++)
                {
                    new_tiles[i] = new_tiles[pos_i + 3 + i];
                }

                new_num -= pos_i + 3;
                eat_num -= 3;
            }
            else
            {
                set_i = SET_DUIZI;
            }
        }

        if (eat_num == 14 &&
            set_i == SET_DUIZI)
        {
            if (new_tiles[pos_i + 1] == tile)
            {
                out_hupai[12] = tile;
                out_hupai[13] = tile;

                for (uint i = 0; pos_i + 2 + i < new_num; i++)
                {
                    new_tiles[i] = new_tiles[pos_i + 2 + i];
                }

                new_num -= pos_i + 2;
                eat_num -= 2;
            }
            else
            {
                set_i = SET_SHUNZI;
            }
        }

        if (eat_num == 14 &&
            set_i == SET_SHUNZI)
        {
            uint count = 1;

            for (uint i = 1; pos_i + i < new_num; i++)
            {
                if (count < 3 && new_tiles[pos_i + i] == tile + count)
                {
                    count++;
                }
                else
                {
                    new_tiles[i - count] = new_tiles[pos_i + i];
                }
            }

            if (count == 3)
            {
                out_hupai[0] = tile;
                out_hupai[1] = tile + 1;
                out_hupai[2] = tile + 2;

                new_num -= pos_i + 3;
                eat_num -= 3;
            }
            else
            {
                pos_i++;
                set_i = SET_KEZI;
                continue;
            }
        }

        if (set_i == SET_INVALID)
        {
            pos_i++;
            set_i = SET_KEZI;
            continue;
        }

        // 第二组
        if (pos_j + eat_num > new_num)
        {
            set_i++;
            pos_j = 0;
            continue;
        }

        tile = new_tiles[pos_j];

        if (eat_num >= 11 &&
            pos_j > 0 &&
            new_tiles[pos_j - 1] == tile)
        {
            pos_j++;
            continue;
        }

        if (eat_num >= 11 &&
            set_j == SET_KEZI)
        {
            if (new_tiles[pos_j + 2] == tile)
            {
                if (eat_num == 12)
                {
                    out_hupai[0] = tile;
                    out_hupai[1] = tile;
                    out_hupai[2] = tile;
                }
                else
                {
                    out_hupai[3] = tile;
                    out_hupai[4] = tile;
                    out_hupai[5] = tile;
                }

                for (uint i = 0; pos_j + 3 + i < new_num; i++)
                {
                    new_tiles[i] = new_tiles[pos_j + 3 + i];
                }

                new_num -= pos_j + 3;
                eat_num -= 3;
            }
            else
            {
                if (eat_num == 11)
                {
                    set_j = SET_DUIZI;
                }
                else
                {
                    set_j = SET_SHUNZI;
                }
            }
        }

        if (eat_num == 11 &&
            set_j == SET_DUIZI)
        {
            if (new_tiles[pos_j + 1] == tile)
            {
                out_hupai[12] = tile;
                out_hupai[13] = tile;

                for (uint i = 0; pos_j + 2 + i < new_num; i++)
                {
                    new_tiles[i] = new_tiles[pos_j + 2 + i];
                }

                new_num -= pos_j + 2;
                eat_num -= 2;
            }
            else
            {
                set_j = SET_SHUNZI;
            }
        }

        if (eat_num >= 11 &&
            set_j == SET_SHUNZI)
        {
            uint count = 1;

            for (uint i = 1; pos_j + i < new_num; i++)
            {
                if (count < 3 && new_tiles[pos_j + i] == tile + count)
                {
                    count++;
                }
                else
                {
                    new_tiles[i - count] = new_tiles[pos_j + i];
                }
            }

            if (count == 3)
            {
                if (eat_num == 12)
                {
                    out_hupai[0] = tile;
                    out_hupai[1] = tile + 1;
                    out_hupai[2] = tile + 2;
                }
                else
                {
                    out_hupai[3] = tile;
                    out_hupai[4] = tile + 1;
                    out_hupai[5] = tile + 2;
                }

                new_num -= pos_j + 3;
                eat_num -= 3;
            }
            else
            {
                pos_j++;
                set_j = SET_KEZI;
                continue;
            }
        }

        if (set_j == SET_INVALID)
        {
            pos_j++;
            set_j = SET_KEZI;
            continue;
        }

        // 第三组
        if (pos_k + eat_num > new_num)
        {
            set_j++;
            pos_k = 0;
            continue;
        }

        tile = new_tiles[pos_k];

        if (eat_num >= 8 &&
            pos_k > 0 &&
            new_tiles[pos_k - 1] == tile)
        {
            pos_k++;
            continue;
        }

        if (eat_num >= 8 &&
            set_k == SET_KEZI)
        {
            if (new_tiles[pos_k + 2] == tile)
            {
                if (eat_num == 9)
                {
                    out_hupai[3] = tile;
                    out_hupai[4] = tile;
                    out_hupai[5] = tile;
                }
                else
                {
                    out_hupai[6] = tile;
                    out_hupai[7] = tile;
                    out_hupai[8] = tile;
                }

                for (uint i = 0; pos_k + 3 + i < new_num; i++)
                {
                    new_tiles[i] = new_tiles[pos_k + 3 + i];
                }

                new_num -= pos_k + 3;
                eat_num -= 3;
            }
            else
            {
                if (eat_num == 8)
                {
                    set_k = SET_DUIZI;
                }
                else
                {
                    set_k = SET_SHUNZI;
                }
            }
        }

        if (eat_num == 8 &&
            set_k == SET_DUIZI)
        {
            if (new_tiles[pos_k + 1] == tile)
            {
                out_hupai[12] = tile;
                out_hupai[13] = tile;

                for (uint i = 0; pos_k + 2 + i < new_num; i++)
                {
                    new_tiles[i] = new_tiles[pos_k + 2 + i];
                }

                new_num -= pos_k + 2;
                eat_num -= 2;
            }
            else
            {
                set_k = SET_SHUNZI;
            }
        }

        if (eat_num >= 8 &&
            set_k == SET_SHUNZI)
        {
            uint count = 1;

            for (uint i = 1; pos_k + i < new_num; i++)
            {
                if (count < 3 && new_tiles[pos_k + i] == tile + count)
                {
                    count++;
                }
                else
                {
                    new_tiles[i - count] = new_tiles[pos_k + i];
                }
            }

            if (count == 3)
            {
                if (eat_num == 9)
                {
                    out_hupai[3] = tile;
                    out_hupai[4] = tile + 1;
                    out_hupai[5] = tile + 2;
                }
                else
                {
                    out_hupai[6] = tile;
                    out_hupai[7] = tile + 1;
                    out_hupai[8] = tile + 2;
                }

                new_num -= pos_k + 3;
                eat_num -= 3;
            }
            else
            {
                pos_k++;
                set_k = SET_KEZI;
                continue;
            }
        }

        if (set_k == SET_INVALID)
        {
            pos_k++;
            set_k = SET_KEZI;
            continue;
        }

        // 第四组
        if (pos_l + eat_num > new_num)
        {
            set_k++;
            pos_l = 0;
            continue;
        }

        tile = new_tiles[pos_l];

        if (eat_num >= 5 &&
            pos_l > 0 &&
            new_tiles[pos_l - 1] == tile)
        {
            pos_l++;
            continue;
        }

        if (eat_num >= 5 &&
            set_l == SET_KEZI)
        {
            if (new_tiles[pos_l + 2] == tile)
            {
                if (eat_num == 6)
                {
                    out_hupai[6] = tile;
                    out_hupai[7] = tile;
                    out_hupai[8] = tile;
                }
                else
                {
                    out_hupai[9 ] = tile;
                    out_hupai[10] = tile;
                    out_hupai[11] = tile;
                }

                for (uint i = 0; pos_l + 3 + i < new_num; i++)
                {
                    new_tiles[i] = new_tiles[pos_l + 3 + i];
                }

                new_num -= pos_l + 3;
                eat_num -= 3;
            }
            else
            {
                if (eat_num == 5)
                {
                    set_l = SET_DUIZI;
                }
                else
                {
                    set_l = SET_SHUNZI;
                }
            }
        }

        if (eat_num == 5 &&
            set_l == SET_DUIZI)
        {
            if (new_tiles[pos_l + 1] == tile)
            {
                out_hupai[12] = tile;
                out_hupai[13] = tile;

                for (uint i = 0; pos_l + 2 + i < new_num; i++)
                {
                    new_tiles[i] = new_tiles[pos_l + 2 + i];
                }

                new_num -= pos_l + 2;
                eat_num -= 2;
            }
            else
            {
                set_l = SET_SHUNZI;
            }
        }

        if (eat_num >= 5 &&
            set_l == SET_SHUNZI)
        {
            uint count = 1;

            for (uint i = 1; pos_l + i < new_num; i++)
            {
                if (count < 3 && new_tiles[pos_l + i] == tile + count)
                {
                    count++;
                }
                else
                {
                    new_tiles[i - count] = new_tiles[pos_l + i];
                }
            }

            if (count == 3)
            {
                if (eat_num == 6)
                {
                    out_hupai[6] = tile;
                    out_hupai[7] = tile + 1;
                    out_hupai[8] = tile + 2;
                }
                else
                {
                    out_hupai[9 ] = tile;
                    out_hupai[10] = tile + 1;
                    out_hupai[11] = tile + 2;
                }

                new_num -= pos_l + 3;
                eat_num -= 3;
            }
            else
            {
                pos_l++;
                set_l = SET_KEZI;
                continue;
            }
        }

        if (set_l == SET_INVALID)
        {
            pos_l++;
            set_l = SET_KEZI;
            continue;
        }

        // 第五组
        if (pos_m + eat_num > new_num)
        {
            set_l++;
            pos_m = 0;
            continue;
        }

        tile = new_tiles[pos_m];

        if (eat_num >= 2 &&
            pos_m > 0 &&
            new_tiles[pos_m - 1] == tile)
        {
            pos_m++;
            continue;
        }

        if (eat_num == 3 &&
            set_m == SET_KEZI)
        {
            if (new_tiles[pos_m + 2] == tile)
            {
                out_hupai[9 ] = tile;
                out_hupai[10] = tile;
                out_hupai[11] = tile;

                new_num -= pos_m + 3;
                eat_num -= 3;
            }
            else
            {
                set_m = SET_SHUNZI;
            }
        }

        if (eat_num == 2 &&
            set_m == SET_KEZI)
        {
            set_m = SET_DUIZI;
        }

        if (eat_num == 2 &&
            set_m == SET_DUIZI)
        {
            if (new_tiles[pos_m + 1] == tile)
            {
                out_hupai[12] = tile;
                out_hupai[13] = tile;

                new_num -= pos_m + 2;
                eat_num -= 2;
            }
            else
            {
                pos_m++;
                continue;
            }
        }

        if (eat_num == 3 &&
            set_m == SET_SHUNZI)
        {
            uint count = 1;

            for (uint i = 1; pos_m + i < new_num; i++)
            {
                if (count < 3 && new_tiles[pos_m + i] == tile + count)
                {
                    count++;
                }
                else
                {
                    new_tiles[i - count] = new_tiles[pos_m + i];
                }
            }

            if (count == 3)
            {
                out_hupai[9 ] = tile;
                out_hupai[10] = tile + 1;
                out_hupai[11] = tile + 2;

                new_num -= pos_m + 3;
                eat_num -= 3;
            }
            else
            {
                pos_m++;
                set_m = SET_KEZI;
                continue;
            }
        }


        // 完成一般形
        if (eat_num == 0)
        {
            out_paixing[0] = PAIXING_YIBAN;
            return 1;
        }

        set_m++;
        if (set_m == SET_INVALID)
        {
            pos_m++;
            set_m = SET_KEZI;
            continue;
        }
    }


    // 有副露或者暗杠，无法完成特殊牌型
    if (fulu_n > 0)
    {
        return 0;
    }


    // 判断七对子，使用临时牌做判断
    tile = NO_TILE;
    uint duizi_n = 0;
    for (uint i = 0; i < tmp_num - 1; i++)
    {
        if (tmp_tiles[i + 1] != tmp_tiles[i]) // 每两张一组，这一组不是对子
        {
            continue;
        }

        if (tmp_tiles[i] == tile) // 这张牌和上一组对子是重复的牌
        {
            continue;
        }

        tile = tmp_tiles[i];
        out_hupai[2 * duizi_n + 0] = tile;
        out_hupai[2 * duizi_n + 1] = tile;
        duizi_n++;

        // 完成七对子
        if (duizi_n == 7)
        {
            out_paixing[0] = PAIXING_QIDUI;
            return 1;
        }
    }



    // 判断国士无双，使用原始牌做判断
    uint full_mask = 0;
    for (uint i = 0; i < tile_n; i++)
    {
        tile = tiles[i];

        // 不是幺九牌
        if ((tile & 0x07) != 0x01)
        {
            continue;
        }

        // TILE_1M -> 0x11 ->  0x2 -> 0x1000 🀇 一萬
        // TILE_9M -> 0x19 ->  0x3 -> 0x0800 🀏 九萬
        // TILE_1S -> 0x21 ->  0x4 -> 0x0400 🀐 一索
        // TILE_9S -> 0x29 ->  0x5 -> 0x0200 🀘 九索
        // TILE_1P -> 0x31 ->  0x6 -> 0x0100 🀙 一筒
        // TILE_9P -> 0x39 ->  0x7 -> 0x0080 🀡 九筒
        // TILE_1Z -> 0x41 ->  0x8 -> 0x0040 🀀 東
        // TILE_2Z -> 0x49 ->  0x9 -> 0x0020 🀁 南
        // TILE_3Z -> 0x51 ->  0xA -> 0x0010 🀂 西
        // TILE_4Z -> 0x59 ->  0xB -> 0x0008 🀃 北
        // TILE_5Z -> 0x61 ->  0xC -> 0x0004 🀆 白
        // TILE_6Z -> 0x69 ->  0xD -> 0x0002 🀅 發
        // TILE_7Z -> 0x71 ->  0xE -> 0x0001 🀄 中
        uint mask = 0x4000 >> (tile >> 3);

        if ((full_mask & mask) == 0)
        {
            full_mask |= mask;
            out_hupai[(tile >> 3) - 2] = tile;
        }
        else if ((full_mask & 0x2000) == 0)
        {
            full_mask |= 0x2000;
            out_hupai[13] = tile;
        }


        // 完成国士无双
        if (full_mask == 0x3FFF)
        {
            out_paixing[0] = PAIXING_GUOSHI;
            return 1;
        }
    }


    // 没有完成任何牌型
    return 0;
}

void main()
{
    out_hule = has_hule (tile_num, fulu_num, shoupai, out_paixing, out_hupai);

    return;
}

// end of file
