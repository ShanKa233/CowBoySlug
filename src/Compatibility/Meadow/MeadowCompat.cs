using System;
using System.Reflection;
using UnityEngine;
using CowBoySlug.Compatibility;

namespace CowBoySlug.Compatibility.Meadow
{
    /// <summary>
    /// 管理与Rain-Meadow的兼容性
    /// </summary>
    public static class MeadowCompat
    {
        private static bool _initialized = false;
        private static bool _meadowExists = false;
        private static Assembly _meadowAssembly = null;
        
        /// <summary>
        /// Rain-Meadow是否存在
        /// </summary>
        public static bool MeadowExists => _meadowExists;
        
        /// <summary>
        /// 初始化Rain-Meadow兼容性模块
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
                    Debug.Log("[CowBoySlug] Rain-Meadow已检测到，启用兼容模式");
                    // 初始化兼容性功能
                    InitializeCompatibility();
                }
                else
                {
                    Debug.Log("[CowBoySlug] 未检测到Rain-Meadow，跳过兼容性初始化");
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
                // 注册帽子同步事件
                RegisterHatSyncEvents();
                
                // 注册网络消息
                HatNetworkMessages.RegisterMessages();
                
                // 添加围巾兼容性处理
                PatchScarfForMeadow();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 初始化Rain-Meadow兼容性时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 为 Meadow 修补 Scarf 类
        /// </summary>
        private static void PatchScarfForMeadow()
        {
            try
            {
                Debug.Log("[CowBoySlug] 正在为 Rain-Meadow 应用围巾兼容性补丁");
                
                // 这里我们不需要做任何特殊处理，因为我们已经修改了 Ribbon_ctor 方法
                // 来防止重复添加键到 ConditionalWeakTable 中
                
                Debug.Log("[CowBoySlug] 围巾兼容性补丁应用成功");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 应用围巾兼容性补丁时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 注册帽子同步事件
        /// </summary>
        private static void RegisterHatSyncEvents()
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
                            MethodInfo handlerMethod = typeof(MeadowCompat).GetMethod("OnPlayerJoined", 
                                BindingFlags.NonPublic | BindingFlags.Static);
                            
                            if (handlerMethod != null && delegateType != null)
                            {
                                Delegate handler = Delegate.CreateDelegate(delegateType, handlerMethod);
                                playerJoinedEvent.AddEventHandler(networkManager, handler);
                                
                                Debug.Log("[CowBoySlug] 成功注册Rain-Meadow玩家加入事件");
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
                Debug.Log("[CowBoySlug] 检测到新玩家加入，同步帽子信息");
                // 当新玩家加入时，同步帽子信息
                SyncHatsToNewPlayer(eventArgs);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 处理玩家加入事件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 同步帽子信息给新玩家
        /// </summary>
        private static void SyncHatsToNewPlayer(object playerEventArgs)
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
                        // 获取所有本地玩家的帽子信息并发送
                        foreach (var player in PlayerManager.GetPlayers())
                        {
                            if (player.IsCowBoys(out var exPlayer) && exPlayer.HaveHat)
                            {
                                foreach (var hat in exPlayer.hatList)
                                {
                                    // 发送帽子信息给新玩家
                                    SendHatUpdateToSpecificPlayer(hat, player, remotePlayer);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 同步帽子信息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送帽子更新到网络
        /// </summary>
        public static void SendHatUpdate(CowBoyHat hat, Player player)
        {
            if (!_meadowExists) return;
            
            try
            {
                // 创建帽子同步消息
                object message = HatNetworkMessages.CreateHatSyncMessage(hat, player);
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
                                    Debug.Log("[CowBoySlug] 成功发送帽子更新到网络");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 发送帽子更新时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送帽子更新给特定玩家
        /// </summary>
        private static void SendHatUpdateToSpecificPlayer(CowBoyHat hat, Player localPlayer, object remotePlayer)
        {
            if (!_meadowExists) return;
            
            try
            {
                // 创建帽子同步消息
                object message = HatNetworkMessages.CreateHatSyncMessage(hat, localPlayer);
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
                                    // 调用SendTo方法
                                    sendToMethod.Invoke(networkManager, new object[] { message, remotePlayer });
                                    Debug.Log("[CowBoySlug] 成功发送帽子更新给特定玩家");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 发送帽子更新给特定玩家时出错: {ex.Message}");
            }
        }
    }
} 