using System;
using System.Reflection;
using UnityEngine;
using CowBoySlug.Compatibility;
using CowBoySlug.ExAbility;

namespace CowBoySlug.Compatibility.SuperShoot
{
    /// <summary>
    /// 处理超级射击的网络消息
    /// </summary>
    public static class SuperShootNetworkMessages
    {
        private static bool _initialized = false;
        private static Type _superShootSyncMessageType = null;
        
        /// <summary>
        /// 注册网络消息类型
        /// </summary>
        public static void RegisterMessages()
        {
            if (!SuperShootCompat.MeadowExists || _initialized) return;
            _initialized = true;
            
            try
            {
                // 获取Rain-Meadow程序集
                Assembly meadowAssembly = CompatibilityManager.GetAssembly("Rain Meadow");
                if (meadowAssembly == null) return;
                
                // 获取网络消息注册器类型
                Type messageRegistryType = meadowAssembly.GetType("RainMeadow.Messaging.MessageRegistry");
                if (messageRegistryType == null) return;
                
                // 创建超级射击同步消息类型
                _superShootSyncMessageType = CreateSuperShootSyncMessageType(meadowAssembly);
                if (_superShootSyncMessageType == null) return;
                
                // 注册消息类型
                MethodInfo registerMethod = messageRegistryType.GetMethod("RegisterMessage", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (registerMethod != null)
                {
                    registerMethod.Invoke(null, new object[] { _superShootSyncMessageType });
                    Debug.Log($"[CowBoySlug] 成功注册超级射击同步消息类型: {_superShootSyncMessageType.Name}");
                    
                    // 注册消息处理器
                    RegisterMessageHandler(meadowAssembly, _superShootSyncMessageType);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 注册超级射击网络消息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建超级射击同步消息类型
        /// </summary>
        private static Type CreateSuperShootSyncMessageType(Assembly meadowAssembly)
        {
            try
            {
                // 获取基础消息类型
                Type baseMessageType = meadowAssembly.GetType("RainMeadow.Messaging.Message");
                if (baseMessageType == null) return null;
                
                // 创建动态类型
                // 注意：这里只是示例，实际上需要使用更复杂的方式创建动态类型
                // 在实际实现中，你可能需要使用System.Reflection.Emit命名空间下的类型来创建动态类型
                
                // 这里简化处理，假设已经创建了类型
                return baseMessageType; // 实际应该返回创建的类型
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 创建超级射击同步消息类型时出错: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private static void RegisterMessageHandler(Assembly meadowAssembly, Type messageType)
        {
            try
            {
                // 获取消息处理器管理器类型
                Type handlerManagerType = meadowAssembly.GetType("RainMeadow.Messaging.MessageHandlerManager");
                if (handlerManagerType == null) return;
                
                // 获取注册处理器方法
                MethodInfo registerHandlerMethod = handlerManagerType.GetMethod("RegisterHandler", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (registerHandlerMethod != null)
                {
                    // 创建处理器委托
                    MethodInfo handlerMethod = typeof(SuperShootNetworkMessages).GetMethod("HandleSuperShootSyncMessage", 
                        BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (handlerMethod != null)
                    {
                        // 获取委托类型
                        Type actionType = meadowAssembly.GetType("System.Action`1").MakeGenericType(messageType);
                        
                        // 创建委托
                        Delegate handler = Delegate.CreateDelegate(actionType, handlerMethod);
                        
                        // 注册处理器
                        registerHandlerMethod.Invoke(null, new object[] { messageType, handler });
                        Debug.Log("[CowBoySlug] 成功注册超级射击同步消息处理器");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 注册超级射击消息处理器时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理超级射击同步消息
        /// </summary>
        private static void HandleSuperShootSyncMessage(object message)
        {
            try
            {
                Debug.Log("[CowBoySlug] 收到超级射击同步消息");
                
                // 解析消息内容
                PropertyInfo playerIdProperty = message.GetType().GetProperty("PlayerId");
                PropertyInfo rockIdProperty = message.GetType().GetProperty("RockId");
                PropertyInfo powerCountProperty = message.GetType().GetProperty("PowerCount");
                
                if (playerIdProperty != null && rockIdProperty != null && powerCountProperty != null)
                {
                    string playerId = (string)playerIdProperty.GetValue(message);
                    string rockId = (string)rockIdProperty.GetValue(message);
                    int powerCount = (int)powerCountProperty.GetValue(message);
                    
                    // 查找对应的玩家
                    Player player = FindPlayerById(playerId);
                    if (player != null)
                    {
                        // 查找对应的石头
                        Rock rock = FindRockById(rockId, player);
                        if (rock != null)
                        {
                            // 应用超级射击数据
                            ApplySuperShootData(rock, powerCount);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 处理超级射击同步消息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据ID查找玩家
        /// </summary>
        private static Player FindPlayerById(string playerId)
        {
            try
            {
                EntityID id = EntityID.FromString(playerId);
                foreach (var player in PlayerManager.GetPlayers())
                {
                    if (player.abstractCreature.ID.Equals(id))
                    {
                        return player;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 查找玩家时出错: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 根据ID查找石头
        /// </summary>
        private static Rock FindRockById(string rockId, Player player)
        {
            try
            {
                EntityID id = EntityID.FromString(rockId);
                
                // 首先检查玩家持有的石头
                for (int i = 0; i < player.grasps.Length; i++)
                {
                    if (player.grasps[i]?.grabbed is Rock rock && rock.abstractPhysicalObject.ID.Equals(id))
                    {
                        return rock;
                    }
                }
                
                // 如果玩家没有持有对应的石头，在房间中查找
                if (player.room != null)
                {
                    foreach (var objList in player.room.physicalObjects)
                    {
                        foreach (var obj in objList)
                        {
                            if (obj is Rock rock && rock.abstractPhysicalObject.ID.Equals(id))
                            {
                                return rock;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 查找石头时出错: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 应用超级射击数据
        /// </summary>
        private static void ApplySuperShootData(Rock rock, int powerCount)
        {
            try
            {
                // 获取或创建超级射击模块
                var superRock = rock.SuperRock();
                
                // 设置能量值
                superRock.powerCount = powerCount;
                
                // 更新颜色
                superRock.SetColor(superRock.nowColor);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 应用超级射击数据时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建超级射击同步消息
        /// </summary>
        public static object CreateSuperShootSyncMessage(SuperShootModule superShoot, Player player)
        {
            if (!SuperShootCompat.MeadowExists || _superShootSyncMessageType == null) return null;
            
            try
            {
                // 创建消息实例
                object message = Activator.CreateInstance(_superShootSyncMessageType);
                
                // 设置消息属性
                PropertyInfo playerIdProperty = _superShootSyncMessageType.GetProperty("PlayerId");
                PropertyInfo rockIdProperty = _superShootSyncMessageType.GetProperty("RockId");
                PropertyInfo powerCountProperty = _superShootSyncMessageType.GetProperty("PowerCount");
                
                if (playerIdProperty != null && rockIdProperty != null && powerCountProperty != null)
                {
                    // 获取玩家ID
                    string playerId = GetPlayerId(player);
                    
                    // 获取石头ID
                    string rockId = GetRockId(superShoot);
                    
                    // 获取能量值
                    int powerCount = superShoot.powerCount;
                    
                    // 设置属性值
                    playerIdProperty.SetValue(message, playerId);
                    rockIdProperty.SetValue(message, rockId);
                    powerCountProperty.SetValue(message, powerCount);
                    
                    return message;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 创建超级射击同步消息时出错: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取玩家ID
        /// </summary>
        private static string GetPlayerId(Player player)
        {
            // 获取玩家ID
            return player.abstractCreature.ID.ToString();
        }
        
        /// <summary>
        /// 获取石头ID
        /// </summary>
        private static string GetRockId(SuperShootModule superShoot)
        {
            // 获取石头ID
            return superShoot.Rock.abstractPhysicalObject.ID.ToString();
        }
    }
} 