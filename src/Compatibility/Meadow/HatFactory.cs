using System;
using System.Collections.Generic;
using UnityEngine;
using CowBoySlug.Compatibility; // 添加对PlayerManager所在命名空间的引用

namespace CowBoySlug.Compatibility.Meadow
{
    /// <summary>
    /// 帽子工厂，用于创建和管理帽子
    /// </summary>
    public static class HatFactory
    {
        // 缓存已创建的帽子，避免重复创建
        private static Dictionary<string, CowBoyHat> _hatCache = new Dictionary<string, CowBoyHat>();
        
        /// <summary>
        /// 根据ID查找帽子
        /// </summary>
        public static CowBoyHat FindHatById(string hatId)
        {
            if (_hatCache.TryGetValue(hatId, out var hat))
            {
                return hat;
            }
            
            return null;
        }
        
        /// <summary>
        /// 创建新帽子
        /// </summary>
        public static CowBoyHat CreateHat(Room room, string hatId, string hatType, string hatColor)
        {
            try
            {
                // 检查缓存中是否已存在
                if (_hatCache.TryGetValue(hatId, out var existingHat))
                {
                    return existingHat;
                }
                
                // 创建帽子的抽象对象
                WorldCoordinate pos = new WorldCoordinate(room.abstractRoom.index, -1, -1, 0);
                EntityID id = EntityID.FromString(hatId);
                
                CowBoyHatAbstract hatAbstract = new CowBoyHatAbstract(room.world, pos, id);
                
                // 设置帽子属性
                hatAbstract.shapeID = hatType;
                
                // 设置帽子颜色
                if (ColorUtility.TryParseHtmlString("#" + hatColor, out Color color))
                {
                    hatAbstract.mainColor = color;
                    hatAbstract.setMainColor = true;
                }
                
                // 实现帽子对象
                hatAbstract.Realize();
                
                // 获取实现的帽子对象
                CowBoyHat hat = hatAbstract.realizedObject as CowBoyHat;
                
                if (hat != null)
                {
                    // 添加到缓存
                    _hatCache[hatId] = hat;
                    
                    // 放置到房间中
                    room.AddObject(hat);
                    
                    return hat;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 创建帽子时出错: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 查找或创建帽子
        /// </summary>
        public static CowBoyHat FindOrCreateHat(Player player, string hatId, string hatType, string hatColor)
        {
            // 先尝试查找
            CowBoyHat hat = FindHatById(hatId);
            
            // 如果不存在，则创建
            if (hat == null && player.room != null)
            {
                hat = CreateHat(player.room, hatId, hatType, hatColor);
            }
            
            return hat;
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _hatCache.Clear();
        }
        
        /// <summary>
        /// 从缓存中移除帽子
        /// </summary>
        public static void RemoveFromCache(CowBoyHat hat)
        {
            if (hat == null || hat.abstractPhysicalObject == null) return;
            
            string hatId = hat.abstractPhysicalObject.ID.ToString();
            if (_hatCache.ContainsKey(hatId))
            {
                _hatCache.Remove(hatId);
            }
        }
    }
} 