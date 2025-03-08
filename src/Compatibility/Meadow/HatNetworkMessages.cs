using System;
using System.Reflection;
using UnityEngine;
using CowBoySlug.Compatibility; // 添加对PlayerManager所在命名空间的引用

namespace CowBoySlug.Compatibility.Meadow
{
    /// <summary>
    /// 处理帽子的网络消息
    /// </summary>
    public static class HatNetworkMessages
    {
        private static bool _initialized = false;
        private static Type _hatSyncMessageType = null;
        
        /// <summary>
        /// 注册网络消息类型
        /// </summary>
        public static void RegisterMessages()
        {
            if (!MeadowCompat.MeadowExists || _initialized) return;
            _initialized = true;
            
            try
            {
                // 获取Rain-Meadow程序集
                Assembly meadowAssembly = CompatibilityManager.GetAssembly("Rain Meadow");
                if (meadowAssembly == null) return;
                
                // 获取网络消息注册器类型
                Type messageRegistryType = meadowAssembly.GetType("RainMeadow.Messaging.MessageRegistry");
                if (messageRegistryType == null) return;
                
                // 创建帽子同步消息类型
                _hatSyncMessageType = CreateHatSyncMessageType(meadowAssembly);
                if (_hatSyncMessageType == null) return;
                
                // 注册消息类型
                MethodInfo registerMethod = messageRegistryType.GetMethod("RegisterMessage", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (registerMethod != null)
                {
                    registerMethod.Invoke(null, new object[] { _hatSyncMessageType });
                    Debug.Log($"[CowBoySlug] 成功注册帽子同步消息类型: {_hatSyncMessageType.Name}");
                    
                    // 注册消息处理器
                    RegisterMessageHandler(meadowAssembly, _hatSyncMessageType);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 注册网络消息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建帽子同步消息类型
        /// </summary>
        private static Type CreateHatSyncMessageType(Assembly meadowAssembly)
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
                Debug.LogError($"[CowBoySlug] 创建帽子同步消息类型时出错: {ex.Message}");
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
                    MethodInfo handlerMethod = typeof(HatNetworkMessages).GetMethod("HandleHatSyncMessage", 
                        BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (handlerMethod != null)
                    {
                        // 获取委托类型
                        Type actionType = meadowAssembly.GetType("System.Action`1").MakeGenericType(messageType);
                        
                        // 创建委托
                        Delegate handler = Delegate.CreateDelegate(actionType, handlerMethod);
                        
                        // 注册处理器
                        registerHandlerMethod.Invoke(null, new object[] { messageType, handler });
                        Debug.Log("[CowBoySlug] 成功注册帽子同步消息处理器");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 注册消息处理器时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理帽子同步消息
        /// </summary>
        private static void HandleHatSyncMessage(object message)
        {
            try
            {
                Debug.Log("[CowBoySlug] 收到帽子同步消息");
                
                // 解析消息内容
                PropertyInfo playerIdProperty = message.GetType().GetProperty("PlayerId");
                PropertyInfo hatDataProperty = message.GetType().GetProperty("HatData");
                
                if (playerIdProperty != null && hatDataProperty != null)
                {
                    string playerId = (string)playerIdProperty.GetValue(message);
                    string hatData = (string)hatDataProperty.GetValue(message);
                    
                    // 查找对应的玩家
                    Player player = FindPlayerById(playerId);
                    if (player != null)
                    {
                        // 应用帽子数据
                        ApplyHatData(player, hatData);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 处理帽子同步消息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据ID查找玩家
        /// </summary>
        private static Player FindPlayerById(string playerId)
        {
            // 这里需要实现根据ID查找玩家的逻辑
            // 在实际实现中，你需要使用Rain-Meadow提供的方法来查找玩家
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
        /// 应用帽子数据
        /// </summary>
        private static void ApplyHatData(Player player, string hatData)
        {
            try
            {
                // 解析帽子数据
                string[] parts = hatData.Split('|');
                if (parts.Length < 3) return;
                
                string hatId = parts[0];
                string hatType = parts[1];
                string hatColor = parts[2];
                
                // 查找或创建帽子
                CowBoyHat hat = HatFactory.FindOrCreateHat(player, hatId, hatType, hatColor);
                if (hat != null)
                {
                    // 将帽子添加到玩家
                    var exPlayer = player.GetCowBoyData();
                    exPlayer.StackHat(hat);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 应用帽子数据时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建帽子同步消息
        /// </summary>
        public static object CreateHatSyncMessage(CowBoyHat hat, Player player)
        {
            if (!MeadowCompat.MeadowExists || _hatSyncMessageType == null) return null;
            
            try
            {
                // 创建消息实例
                object message = Activator.CreateInstance(_hatSyncMessageType);
                
                // 设置消息属性
                PropertyInfo playerIdProperty = _hatSyncMessageType.GetProperty("PlayerId");
                PropertyInfo hatDataProperty = _hatSyncMessageType.GetProperty("HatData");
                
                if (playerIdProperty != null && hatDataProperty != null)
                {
                    // 获取玩家ID
                    string playerId = GetPlayerId(player);
                    
                    // 获取帽子数据
                    string hatData = GetHatData(hat);
                    
                    // 设置属性值
                    playerIdProperty.SetValue(message, playerId);
                    hatDataProperty.SetValue(message, hatData);
                    
                    return message;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 创建帽子同步消息时出错: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取玩家ID
        /// </summary>
        private static string GetPlayerId(Player player)
        {
            // 这里需要实现获取玩家ID的逻辑
            // 在实际实现中，你需要使用Rain-Meadow提供的方法来获取玩家ID
            return player.abstractCreature.ID.ToString();
        }
        
        /// <summary>
        /// 获取帽子数据
        /// </summary>
        private static string GetHatData(CowBoyHat hat)
        {
            // 这里需要实现获取帽子数据的逻辑
            // 在实际实现中，你需要将帽子的关键信息序列化为字符串
            return $"{hat.abstractPhysicalObject.ID}|{hat.shapeID}|{ColorUtility.ToHtmlStringRGBA(hat.mainColor)}";
        }
    }
} 