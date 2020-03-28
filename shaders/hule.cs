// compute shader
// 判断和了

#version 460 core

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout (binding = 0) readonly buffer in_buffer
{
    uint shoupai[14]; // 参数：手牌，可以是14、11、8、5、2张，其余补0
};

layout (binding = 1) writeonly buffer out_buffer
{
    uint hule; // 返回值：0=没和, 1=和了
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

const uint TILE_1P = 0x21; // 一筒 🀙
const uint TILE_2P = 0x22; // 二筒 🀚
const uint TILE_3P = 0x23; // 三筒 🀛
const uint TILE_4P = 0x24; // 四筒 🀜
const uint TILE_5P = 0x25; // 五筒 🀝
const uint TILE_6P = 0x26; // 六筒 🀞
const uint TILE_7P = 0x27; // 七筒 🀟
const uint TILE_8P = 0x28; // 八筒 🀠
const uint TILE_9P = 0x29; // 九筒 🀡

const uint TILE_1S = 0x31; // 一索 🀐
const uint TILE_2S = 0x32; // 二索 🀑
const uint TILE_3S = 0x33; // 三索 🀒
const uint TILE_4S = 0x34; // 四索 🀓
const uint TILE_5S = 0x35; // 五索 🀔
const uint TILE_6S = 0x36; // 六索 🀕
const uint TILE_7S = 0x37; // 七索 🀖
const uint TILE_8S = 0x38; // 八索 🀗
const uint TILE_9S = 0x39; // 九索 🀘

const uint TILE_1Z = 0x41; // 東 🀀
const uint TILE_2Z = 0x49; // 南 🀁
const uint TILE_3Z = 0x51; // 西 🀂
const uint TILE_4Z = 0x59; // 北 🀃
const uint TILE_5Z = 0x61; // 白 🀆
const uint TILE_6Z = 0x69; // 發 🀅
const uint TILE_7Z = 0x71; // 中 🀄

const uint TILE_0M = 0x95; // 赤五萬 🀋
const uint TILE_0P = 0xA5; // 赤五筒 🀝
const uint TILE_0S = 0xB5; // 赤五索 🀔

// 一萬 🀇 -> 0x1000 
// 九萬 🀏 -> 0x0800
// 一筒 🀙 -> 0x0400
// 九筒 🀡 -> 0x0200
// 一索 🀐 -> 0x0100
// 九索 🀘 -> 0x0080
// 東 🀀 -> 0x0040
// 南 🀁 -> 0x0020
// 西 🀂 -> 0x0010
// 北 🀃 -> 0x0008
// 白 🀆 -> 0x0004
// 發 🀅 -> 0x0002
// 中 🀄 -> 0x0001
uint mask_yaojiu(uint tile) // 么九牌掩码，便于判断国士无双
{
    if ((tile & 0x07) != 0x01)
    {
        return 0;
    }

    return 0x4000 >> (tile >> 3);
}

bool is_2(uint[2] tiles) // 1个雀头
{
    if (tiles[0] == tiles[1]) // 是对子
    {
        return true;
    }

    return false;
}

bool is_3(uint[3] tiles) // 1个面子
{
    if (tiles[2] == tiles[0]) // 是刻子
    {
        return true;
    }

    if (tiles[1] == tiles[0] + 1 &&
        tiles[2] == tiles[1] + 1) // 是顺子
    {
        return true;
    }

    return false;
}

bool is_3_2(uint[5] tiles) // 1个面子和1个雀头
{
    if (tiles[2] == tiles[0])
    {
        uint[2] new_tiles = 
        {
            tiles[3], tiles[4],
        };
        if (is_2 (new_tiles)) // 移除一组刻子，剩下1个雀头
        {
            return true;
        }
    }

    if (tiles[1] == tiles[0])
    {
        uint[3] new_tiles = 
        {
            tiles[2], tiles[3], tiles[4],
        };
        if (is_3 (new_tiles)) // 移除一个雀头，剩下1个面子
        {
            return true;
        }
    }

    uint count = 0;
    for (uint i = 1; i < 5; i++)
    {
        if (count < 2 && tiles[i] == tiles[0] + 1 + count)
        {
            count++;
        }
        else if (count > 0)
        {
            tiles[i - count] = tiles[i];
        }
    }

    if (count < 2)
    {
        return false;
    }

    uint[2] new_tiles = 
    {
        tiles[1], tiles[2],
    };
    if (is_2 (new_tiles)) // 移除一组顺子，剩下1个雀头
    {
        return true;
    }

    return false;
}

bool is_3_3(uint[6] tiles) // 2个面子
{
    if (tiles[2] == tiles[0])
    {
        uint[3] new_tiles = 
        {
            tiles[3], tiles[4], tiles[5],
        };
        if (is_3 (new_tiles)) // 移除一组刻子，剩下1个面子
        {
            return true;
        }
    }

    uint count = 0;
    for (uint i = 1; i < 6; i++)
    {
        if (count < 2 && tiles[i] == tiles[0] + 1 + count)
        {
            count++;
        }
        else if (count > 0)
        {
            tiles[i - count] = tiles[i];
        }
    }

    if (count < 2)
    {
        return false;
    }

    uint[3] new_tiles = 
    {
        tiles[1], tiles[2], tiles[3],
    };
    if (is_3 (new_tiles)) // 移除一组顺子，剩下1个面子
    {
        return true;
    }

    return false;
}

bool is_3_3_2(uint[8] tiles) // 2个面子和1个雀头
{
    if (tiles[2] == tiles[0])
    {
        uint[5] new_tiles = 
        {
            tiles[3], tiles[4], tiles[5],
            tiles[6], tiles[7],
        };
        if (is_3_2 (new_tiles)) // 移除一组刻子，剩下1个面子和1个雀头
        {
            return true;
        }
    }

    if (tiles[1] == tiles[0])
    {
        uint[6] new_tiles = 
        {
            tiles[2], tiles[3], tiles[4],
            tiles[5], tiles[6], tiles[7],
        };
        if (is_3_3 (new_tiles)) // 移除一个雀头，剩下2个面子
        {
            return true;
        }
    }

    uint count = 0;
    for (uint i = 1; i < 8; i++)
    {
        if (count < 2 && tiles[i] == tiles[0] + 1 + count)
        {
            count++;
        }
        else if (count > 0)
        {
            tiles[i - count] = tiles[i];
        }
    }

    if (count < 2)
    {
        return false;
    }

    uint[5] new_tiles = 
    {
        tiles[1], tiles[2], tiles[3],
        tiles[4], tiles[5],
    };
    if (is_3_2 (new_tiles)) // 移除一组顺子，剩下1个面子和1个雀头
    {
        return true;
    }

    return false;
}

bool is_3_3_3(uint[9] tiles) // 3个面子
{
    if (tiles[2] == tiles[0])
    {
        uint[6] new_tiles = 
        {
            tiles[3], tiles[4], tiles[5],
            tiles[6], tiles[7], tiles[8],
        };
        if (is_3_3 (new_tiles)) // 移除一组刻子，剩下2个面子
        {
            return true;
        }
    }

    uint count = 0;
    for (uint i = 1; i < 9; i++)
    {
        if (count < 2 && tiles[i] == tiles[0] + 1 + count)
        {
            count++;
        }
        else if (count > 0)
        {
            tiles[i - count] = tiles[i];
        }
    }

    if (count < 2)
    {
        return false;
    }

    uint[6] new_tiles = 
    {
        tiles[1], tiles[2], tiles[3],
        tiles[4], tiles[5], tiles[6],
    };
    if (is_3_3 (new_tiles)) // 移除一组顺子，剩下2个面子
    {
        return true;
    }

    return false;
}

bool is_3_3_3_2(uint[11] tiles) // 3个面子和1个雀头
{
    if (tiles[2] == tiles[0])
    {
        uint[8] new_tiles = 
        {
            tiles[3], tiles[4], tiles[5],
            tiles[6], tiles[7], tiles[8],
            tiles[9], tiles[10],
        };
        if (is_3_3_2 (new_tiles)) // 移除一组刻子，剩下2个面子和1个雀头
        {
            return true;
        }
    }

    if (tiles[1] == tiles[0])
    {
        uint[9] new_tiles = 
        {
            tiles[2], tiles[3], tiles[4],
            tiles[5], tiles[6], tiles[7],
            tiles[8], tiles[9], tiles[10],
        };
        if (is_3_3_3 (new_tiles)) // 移除一个雀头，剩下3个面子
        {
            return true;
        }
    }

    uint count = 0;
    for (uint i = 1; i < 11; i++)
    {
        if (count < 2 && tiles[i] == tiles[0] + 1 + count)
        {
            count++;
        }
        else if (count > 0)
        {
            tiles[i - count] = tiles[i];
        }
    }

    if (count < 2)
    {
        return false;
    }

    uint[8] new_tiles = 
    {
        tiles[1], tiles[2], tiles[3],
        tiles[4], tiles[5], tiles[6],
        tiles[7], tiles[8],
    };
    if (is_3_3_2 (new_tiles)) // 移除一组顺子，剩下2个面子和1个雀头
    {
        return true;
    }

    return false;
}

bool is_3_3_3_3(uint[12] tiles) // 4个面子
{
    if (tiles[2] == tiles[0])
    {
        uint[9] new_tiles = 
        {
            tiles[3], tiles[4], tiles[5],
            tiles[6], tiles[7], tiles[8],
            tiles[9], tiles[10], tiles[11],
        };
        if (is_3_3_3 (new_tiles)) // 移除一组刻子，剩下3个面子
        {
            return true;
        }
    }

    uint count = 0;
    for (uint i = 1; i < 12; i++)
    {
        if (count < 2 && tiles[i] == tiles[0] + 1 + count)
        {
            count++;
        }
        else if (count > 0)
        {
            tiles[i - count] = tiles[i];
        }
    }

    if (count < 2)
    {
        return false;
    }

    uint[9] new_tiles = 
    {
        tiles[1], tiles[2], tiles[3],
        tiles[4], tiles[5], tiles[6],
        tiles[7], tiles[8], tiles[9],
    };
    if (is_3_3_3 (new_tiles)) // 移除一组顺子，剩下3个面子
    {
        return true;
    }

    return false;
}

bool is_3_3_3_3_2(uint[14] tiles) // 4个面子和1个雀头
{
    if (tiles[2] == tiles[0])
    {
        uint[11] new_tiles = 
        {
            tiles[3], tiles[4], tiles[5],
            tiles[6], tiles[7], tiles[8],
            tiles[9], tiles[10], tiles[11],
            tiles[12], tiles[13],
        };
        if (is_3_3_3_2 (new_tiles)) // 移除一组刻子，剩下3个面子和1个雀头
        {
            return true;
        }
    }

    if (tiles[1] == tiles[0])
    {
        uint[12] new_tiles = 
        {
            tiles[2], tiles[3], tiles[4],
            tiles[5], tiles[6], tiles[7],
            tiles[8], tiles[9], tiles[10],
            tiles[11], tiles[12], tiles[13],
        };
        if (is_3_3_3_3 (new_tiles)) // 移除一个雀头，剩下4个面子
        {
            return true;
        }
    }

    uint count = 0;
    for (uint i = 1; i < 14; i++)
    {
        if (count < 2 && tiles[i] == tiles[0] + 1 + count)
        {
            count++;
        }
        else if (count > 0)
        {
            tiles[i - count] = tiles[i];
        }
    }

    if (count < 2)
    {
        return false;
    }

    uint[11] new_tiles = 
    {
        tiles[1], tiles[2], tiles[3],
        tiles[4], tiles[5], tiles[6],
        tiles[7], tiles[8], tiles[9],
        tiles[10], tiles[11],
    };
    if (is_3_3_3_2 (new_tiles)) // 移除一组顺子，剩下3个面子和1个雀头
    {
        return true;
    }

    return false;
}

bool is_qidui(uint[14] tiles) // 七对子
{
    uint pre_tile = NO_TILE;
    for (uint i = 0; i < 14; i += 2)
    {
        if (tiles[i] != tiles[i + 1]) // 每两张一组，这一组不是对子
        {
            return false;
        }

        if (tiles[i] == pre_tile) // 这组对子和上一组是重复的对子
        {
            return false;
        }

        pre_tile = tiles[i];
    }

    return true;
}

bool is_guoshi(uint[14] tiles) // 国士无双
{
    uint full_mask = 0;
    for (uint i = 0; i < 14; i++)
    {
        uint mask = mask_yaojiu (tiles[i]);
        if (mask == 0)
        {
            return false;
        }

        full_mask |= mask;
    }

    if (full_mask != 0x1FFF)
    {
        return false;
    }

    return true;
}

uint is_hule(uint tile_num, uint[14] tiles)
{
    if (tile_num % 3 != 2)
    {
        return 0;
    }

    // 清理赤宝
    for (uint i = 0; i < tile_num; i++)
    {
        if (tiles[i] == TILE_0M)
        {
            tiles[i] = TILE_5M;
        }
        else if (tiles[i] == TILE_0P)
        {
            tiles[i] = TILE_5P;
        }
        else if (tiles[i] == TILE_0S)
        {
            tiles[i] = TILE_5S;
        }
    }

    // 冒泡排序
    for (uint i = 0; i < tile_num - 1; i++)
    {
        bool dirty = false;
        for (uint j = tile_num - 1; j > i; j--)
        {
            if (tiles[j] < tiles[j - 1])
            {
                uint tmp = tiles[j];
                tiles[j] = tiles[j - 1];
                tiles[j - 1] = tmp;
                dirty = true;
            }
        }

        if (!dirty)
        {
            break;
        }
    }

    // 判断一般形
    if (tile_num == 14)
    {
        if (is_3_3_3_3_2 (tiles))
        {
            return 1;
        }
    }

    if (tile_num == 11)
    {
        uint[11] new_tiles =
        {
            tiles[0], tiles[1], tiles[2],
            tiles[3], tiles[4], tiles[5],
            tiles[6], tiles[7], tiles[8],
            tiles[9], tiles[10],
        };
        if (is_3_3_3_2 (new_tiles))
        {
            return 1;
        }
    }

    if (tile_num == 8)
    {
        uint[8] new_tiles =
        {
            tiles[0], tiles[1], tiles[2],
            tiles[3], tiles[4], tiles[5],
            tiles[6], tiles[7],
        };
        if (is_3_3_2 (new_tiles))
        {
            return 1;
        }
    }

    if (tile_num == 5)
    {
        uint[5] new_tiles =
        {
            tiles[0], tiles[1], tiles[2],
            tiles[3], tiles[4],
        };
        if (is_3_2 (new_tiles))
        {
            return 1;
        }
    }

    if (tile_num == 2)
    {
        uint[2] new_tiles =
        {
            tiles[0], tiles[1],
        };
        if (is_2 (new_tiles))
        {
            return 1;
        }
    }

    // 判断七对子和国士无双
    if (tile_num != 14)
    {
        return 0;
    }

    if (is_qidui (tiles))
    {
        return 1;
    }

    if (is_guoshi (tiles))
    {
        return 1;
    }

    return 0;
}

void main()
{
    uint tile_num = 0;
    while (tile_num < 14 && shoupai[tile_num] != NO_TILE)
    {
        tile_num++;
    }

    hule = is_hule (tile_num, shoupai);

    return;
}

// end of file
