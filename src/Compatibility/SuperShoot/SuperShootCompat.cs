using System;
using System.Reflection;
using UnityEngine;
using CowBoySlug.Compatibility;
using CowBoySlug.ExAbility;

namespace CowBoySlug.Compatibility.SuperShoot
{
    /// <summary>
    /// 管理超级射击功能与其他模组的兼容性
    /// </summary>
    public static class SuperShootCompat
    {
        private static bool _initialized = false;
        private static bool _meadowExists = false;
        private static Assembly _meadowAssembly = null;
        
        /// <summary>
        /// Rain-Meadow是否存在
        /// </summary>
        public static bool MeadowExists => _meadowExists;
        
        /// <summary>
        /// 初始化SuperShoot兼容性模块
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
                    Debug.Log("[CowBoySlug] Rain-Meadow已检测到，启用超级射击功能兼容模式");
                    // 初始化兼容性功能
                    InitializeCompatibility();
                }
                else
                {
                    Debug.Log("[CowBoySlug] 未检测到Rain-Meadow，跳过超级射击功能兼容性初始化");
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
                // 注册超级射击同步事件
                RegisterSuperShootSyncEvents();
                
                // 注册网络消息
                SuperShootNetworkMessages.RegisterMessages();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 初始化超级射击功能兼容性时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 注册超级射击同步事件
        /// </summary>
        private static void RegisterSuperShootSyncEvents()
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
                            MethodInfo handlerMethod = typeof(SuperShootCompat).GetMethod("OnPlayerJoined", 
                                BindingFlags.NonPublic | BindingFlags.Static);
                            
                            if (handlerMethod != null && delegateType != null)
                            {
                                Delegate handler = Delegate.CreateDelegate(delegateType, handlerMethod);
                                playerJoinedEvent.AddEventHandler(networkManager, handler);
                                
                                Debug.Log("[CowBoySlug] 成功注册Rain-Meadow玩家加入事件（超级射击功能）");
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
                Debug.Log("[CowBoySlug] 检测到新玩家加入，同步超级射击信息");
                // 当新玩家加入时，同步超级射击信息
                SyncSuperShootToNewPlayer(eventArgs);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 处理玩家加入事件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 同步超级射击信息给新玩家
        /// </summary>
        private static void SyncSuperShootToNewPlayer(object playerEventArgs)
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
                        // 获取所有本地玩家的超级射击信息并发送
                        foreach (var player in PlayerManager.GetPlayers())
                        {
                            // 查找玩家持有的石头
                            Rock rock = null;
                            for (int i = 0; i < player.grasps.Length; i++)
                            {
                                if (player.grasps[i]?.grabbed is Rock r)
                                {
                                    rock = r;
                                    break;
                                }
                            }
                            
                            // 如果找到了石头，检查是否是超级石头
                            if (rock != null && rock.IsSuperRock(out var superRock))
                            {
                                // 发送超级射击信息给新玩家
                                SendSuperShootUpdateToSpecificPlayer(superRock, player, remotePlayer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 同步超级射击信息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送超级射击更新到网络
        /// </summary>
        public static void SendSuperShootUpdate(SuperShootModule superShoot, Player player)
        {
            if (!_meadowExists) return;
            
            try
            {
                // 创建超级射击同步消息
                object message = SuperShootNetworkMessages.CreateSuperShootSyncMessage(superShoot, player);
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
                                    Debug.Log("[CowBoySlug] 成功发送超级射击更新到网络");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 发送超级射击更新时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送超级射击更新给特定玩家
        /// </summary>
        private static void SendSuperShootUpdateToSpecificPlayer(SuperShootModule superShoot, Player localPlayer, object remotePlayer)
        {
            if (!_meadowExists) return;
            
            try
            {
                // 创建超级射击同步消息
                object message = SuperShootNetworkMessages.CreateSuperShootSyncMessage(superShoot, localPlayer);
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
                                    Debug.Log("[CowBoySlug] 成功发送超级射击更新给特定玩家");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 发送超级射击更新给特定玩家时出错: {ex.Message}");
            }
        }
    }
} 