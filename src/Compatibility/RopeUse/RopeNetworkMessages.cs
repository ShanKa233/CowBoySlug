using System;
using System.Reflection;
using UnityEngine;
using CowBoySlug.Compatibility;
using CowBoySlug.CowBoy.Ability.RopeUse;

namespace CowBoySlug.Compatibility.RopeUse
{
    /// <summary>
    /// 处理绳索的网络消息
    /// </summary>
    public static class RopeNetworkMessages
    {
        private static bool _initialized = false;
        private static Type _ropeSyncMessageType = null;
        
        /// <summary>
        /// 注册网络消息类型
        /// </summary>
        public static void RegisterMessages()
        {
            if (!RopeUseCompat.MeadowExists || _initialized) return;
            _initialized = true;
            
            try
            {
                // 获取Rain-Meadow程序集
                Assembly meadowAssembly = CompatibilityManager.GetAssembly("Rain Meadow");
                if (meadowAssembly == null) return;
                
                // 获取网络消息注册器类型
                Type messageRegistryType = meadowAssembly.GetType("RainMeadow.Messaging.MessageRegistry");
                if (messageRegistryType == null) return;
                
                // 创建绳索同步消息类型
                _ropeSyncMessageType = CreateRopeSyncMessageType(meadowAssembly);
                if (_ropeSyncMessageType == null) return;
                
                // 注册消息类型
                MethodInfo registerMethod = messageRegistryType.GetMethod("RegisterMessage", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (registerMethod != null)
                {
                    registerMethod.Invoke(null, new object[] { _ropeSyncMessageType });
                    Debug.Log($"[CowBoySlug] 成功注册绳索同步消息类型: {_ropeSyncMessageType.Name}");
                    
                    // 注册消息处理器
                    RegisterMessageHandler(meadowAssembly, _ropeSyncMessageType);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 注册绳索网络消息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建绳索同步消息类型
        /// </summary>
        private static Type CreateRopeSyncMessageType(Assembly meadowAssembly)
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
                Debug.LogError($"[CowBoySlug] 创建绳索同步消息类型时出错: {ex.Message}");
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
                    MethodInfo handlerMethod = typeof(RopeNetworkMessages).GetMethod("HandleRopeSyncMessage", 
                        BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (handlerMethod != null)
                    {
                        // 获取委托类型
                        Type actionType = meadowAssembly.GetType("System.Action`1").MakeGenericType(messageType);
                        
                        // 创建委托
                        Delegate handler = Delegate.CreateDelegate(actionType, handlerMethod);
                        
                        // 注册处理器
                        registerHandlerMethod.Invoke(null, new object[] { messageType, handler });
                        Debug.Log("[CowBoySlug] 成功注册绳索同步消息处理器");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 注册绳索消息处理器时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理绳索同步消息
        /// </summary>
        private static void HandleRopeSyncMessage(object message)
        {
            try
            {
                Debug.Log("[CowBoySlug] 收到绳索同步消息");
                
                // 解析消息内容
                PropertyInfo playerIdProperty = message.GetType().GetProperty("PlayerId");
                PropertyInfo ropeDataProperty = message.GetType().GetProperty("RopeData");
                
                if (playerIdProperty != null && ropeDataProperty != null)
                {
                    string playerId = (string)playerIdProperty.GetValue(message);
                    string ropeData = (string)ropeDataProperty.GetValue(message);
                    
                    // 查找对应的玩家
                    Player player = FindPlayerById(playerId);
                    if (player != null)
                    {
                        // 应用绳索数据
                        ApplyRopeData(player, ropeData);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 处理绳索同步消息时出错: {ex.Message}");
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
        /// 应用绳索数据
        /// </summary>
        private static void ApplyRopeData(Player player, string ropeData)
        {
            try
            {
                // 解析绳索数据
                string[] parts = ropeData.Split('|');
                if (parts.Length < 2) return;
                
                string ropeType = parts[0];
                string ropeState = parts[1];
                
                // 获取或创建绳索
                var ropeMaster = RopeMaster.GetRopeMasterData(player);
                if (ropeMaster != null)
                {
                    // 应用绳索状态
                    ApplyRopeState(ropeMaster, ropeType, ropeState);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 应用绳索数据时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 应用绳索状态
        /// </summary>
        private static void ApplyRopeState(RopeMaster ropeMaster, string ropeType, string ropeState)
        {
            try
            {
                // 根据绳索类型和状态更新绳索
                // 这里需要根据实际的绳索系统实现具体的逻辑
                
                // 示例：
                if (ropeType == "CowBoyRope")
                {
                    // 更新牛仔绳索状态
                    if (ropeState == "Deployed")
                    {
                        // 部署绳索
                        ropeMaster.DeployRope();
                    }
                    else if (ropeState == "Retracted")
                    {
                        // 收回绳索
                        ropeMaster.RetractRope();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 应用绳索状态时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建绳索同步消息
        /// </summary>
        public static object CreateRopeSyncMessage(RopeMaster ropeMaster, Player player)
        {
            if (!RopeUseCompat.MeadowExists || _ropeSyncMessageType == null) return null;
            
            try
            {
                // 创建消息实例
                object message = Activator.CreateInstance(_ropeSyncMessageType);
                
                // 设置消息属性
                PropertyInfo playerIdProperty = _ropeSyncMessageType.GetProperty("PlayerId");
                PropertyInfo ropeDataProperty = _ropeSyncMessageType.GetProperty("RopeData");
                
                if (playerIdProperty != null && ropeDataProperty != null)
                {
                    // 获取玩家ID
                    string playerId = GetPlayerId(player);
                    
                    // 获取绳索数据
                    string ropeData = GetRopeData(ropeMaster);
                    
                    // 设置属性值
                    playerIdProperty.SetValue(message, playerId);
                    ropeDataProperty.SetValue(message, ropeData);
                    
                    return message;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 创建绳索同步消息时出错: {ex.Message}");
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
        /// 获取绳索数据
        /// </summary>
        private static string GetRopeData(RopeMaster ropeMaster)
        {
            // 获取绳索类型和状态
            string ropeType = "CowBoyRope"; // 默认类型
            string ropeState = ropeMaster.HaveRope ? "Deployed" : "Retracted"; // 默认状态
            
            // 返回格式化的绳索数据
            return $"{ropeType}|{ropeState}";
        }
    }
} 