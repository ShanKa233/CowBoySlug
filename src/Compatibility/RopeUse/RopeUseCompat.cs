using System;
using System.Reflection;
using UnityEngine;
using CowBoySlug.Compatibility;
using CowBoySlug.CowBoy.Ability.RopeUse;

namespace CowBoySlug.Compatibility.RopeUse
{
    /// <summary>
    /// 管理绳索使用功能与其他模组的兼容性
    /// </summary>
    public static class RopeUseCompat
    {
        private static bool _initialized = false;
        private static bool _meadowExists = false;
        private static Assembly _meadowAssembly = null;
        
        /// <summary>
        /// Rain-Meadow是否存在
        /// </summary>
        public static bool MeadowExists => _meadowExists;
        
        /// <summary>
        /// 初始化RopeUse兼容性模块
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            try
            {
                // 尝试获取Rain-Meadow程序集
                _meadowAssembly = CompatibilityManager.GetAssembly("Rain Meadow");
                _meadowExists = _meadowAssembly != null;
                
                if (_meadowExists)
                {
                    Debug.Log("[CowBoySlug] Rain-Meadow已检测到，启用绳索功能兼容模式");
                    // 初始化兼容性功能
                    InitializeCompatibility();
                }
                else
                {
                    Debug.Log("[CowBoySlug] 未检测到Rain-Meadow，跳过绳索功能兼容性初始化");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 检测Rain-Meadow时出错: {ex.Message}");
                _meadowExists = false;
            }
        }
        
        /// <summary>
        /// 初始化与Rain-Meadow的兼容性功能
        /// </summary>
        private static void InitializeCompatibility()
        {
            if (!_meadowExists) return;
            
            try
            {
                // 注册绳索同步事件
                RegisterRopeSyncEvents();
                
                // 注册网络消息
                RopeNetworkMessages.RegisterMessages();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 初始化绳索功能兼容性时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 注册绳索同步事件
        /// </summary>
        private static void RegisterRopeSyncEvents()
        {
            if (!_meadowExists) return;
            
            try
            {
                // 使用反射获取Rain-Meadow的事件
                Type networkManagerType = _meadowAssembly.GetType("RainMeadow.NetworkManager");
                if (networkManagerType != null)
                {
                    // 获取静态实例属性
                    PropertyInfo instanceProperty = networkManagerType.GetProperty("Instance", 
                        BindingFlags.Public | BindingFlags.Static);
                    
                    if (instanceProperty != null)
                    {
                        object networkManager = instanceProperty.GetValue(null);
                        
                        // 获取事件字段
                        EventInfo playerJoinedEvent = networkManagerType.GetEvent("PlayerJoined");
                        if (playerJoinedEvent != null && networkManager != null)
                        {
                            // 创建委托并注册事件
                            Type delegateType = playerJoinedEvent.EventHandlerType;
                            MethodInfo handlerMethod = typeof(RopeUseCompat).GetMethod("OnPlayerJoined", 
                                BindingFlags.NonPublic | BindingFlags.Static);
                            
                            if (handlerMethod != null && delegateType != null)
                            {
                                Delegate handler = Delegate.CreateDelegate(delegateType, handlerMethod);
                                playerJoinedEvent.AddEventHandler(networkManager, handler);
                                
                                Debug.Log("[CowBoySlug] 成功注册Rain-Meadow玩家加入事件（绳索功能）");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 注册Rain-Meadow事件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 当新玩家加入时的处理方法
        /// </summary>
        private static void OnPlayerJoined(object sender, object eventArgs)
        {
            try
            {
                Debug.Log("[CowBoySlug] 检测到新玩家加入，同步绳索信息");
                // 当新玩家加入时，同步绳索信息
                SyncRopeToNewPlayer(eventArgs);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 处理玩家加入事件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 同步绳索信息给新玩家
        /// </summary>
        private static void SyncRopeToNewPlayer(object playerEventArgs)
        {
            try
            {
                // 使用反射获取玩家信息
                PropertyInfo playerProperty = playerEventArgs.GetType().GetProperty("Player");
                if (playerProperty != null)
                {
                    object remotePlayer = playerProperty.GetValue(playerEventArgs);
                    if (remotePlayer != null)
                    {
                        // 获取所有本地玩家的绳索信息并发送
                        foreach (var player in PlayerManager.GetPlayers())
                        {
                            // 获取玩家的绳索信息
                            var ropeMaster = RopeMaster.GetRopeMasterData(player);
                            if (ropeMaster != null && ropeMaster.HaveRope)
                            {
                                // 发送绳索信息给新玩家
                                SendRopeUpdateToSpecificPlayer(ropeMaster, player, remotePlayer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 同步绳索信息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送绳索更新到网络
        /// </summary>
        public static void SendRopeUpdate(RopeMaster ropeMaster, Player player)
        {
            if (!_meadowExists) return;
            
            try
            {
                // 创建绳索同步消息
                object message = RopeNetworkMessages.CreateRopeSyncMessage(ropeMaster, player);
                if (message != null)
                {
                    // 使用反射调用Rain-Meadow的网络发送方法
                    Type networkManagerType = _meadowAssembly.GetType("RainMeadow.NetworkManager");
                    if (networkManagerType != null)
                    {
                        PropertyInfo instanceProperty = networkManagerType.GetProperty("Instance", 
                            BindingFlags.Public | BindingFlags.Static);
                        
                        if (instanceProperty != null)
                        {
                            object networkManager = instanceProperty.GetValue(null);
                            if (networkManager != null)
                            {
                                MethodInfo sendMethod = networkManagerType.GetMethod("Send", 
                                    BindingFlags.Public | BindingFlags.Instance, 
                                    null, 
                                    new Type[] { message.GetType() }, 
                                    null);
                                
                                if (sendMethod != null)
                                {
                                    sendMethod.Invoke(networkManager, new object[] { message });
                                    Debug.Log("[CowBoySlug] 成功发送绳索更新到网络");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 发送绳索更新时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送绳索更新给特定玩家
        /// </summary>
        private static void SendRopeUpdateToSpecificPlayer(RopeMaster ropeMaster, Player localPlayer, object remotePlayer)
        {
            if (!_meadowExists) return;
            
            try
            {
                // 创建绳索同步消息
                object message = RopeNetworkMessages.CreateRopeSyncMessage(ropeMaster, localPlayer);
                if (message != null)
                {
                    // 使用反射调用Rain-Meadow的网络发送方法
                    Type networkManagerType = _meadowAssembly.GetType("RainMeadow.NetworkManager");
                    if (networkManagerType != null)
                    {
                        PropertyInfo instanceProperty = networkManagerType.GetProperty("Instance", 
                            BindingFlags.Public | BindingFlags.Static);
                        
                        if (instanceProperty != null)
                        {
                            object networkManager = instanceProperty.GetValue(null);
                            if (networkManager != null)
                            {
                                MethodInfo sendToMethod = networkManagerType.GetMethod("SendTo", 
                                    BindingFlags.Public | BindingFlags.Instance);
                                
                                if (sendToMethod != null)
                                {
                                    sendToMethod.Invoke(networkManager, new object[] { message, remotePlayer });
                                    Debug.Log("[CowBoySlug] 成功发送绳索更新给特定玩家");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 发送绳索更新给特定玩家时出错: {ex.Message}");
            }
        }
    }
} 