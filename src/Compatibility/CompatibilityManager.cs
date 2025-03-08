using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CowBoySlug.Compatibility
{
    /// <summary>
    /// 管理与其他mod的兼容性
    /// </summary>
    public static class CompatibilityManager
    {
        private static bool _initialized = false;

        /// <summary>
        /// 初始化所有兼容性模块
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 初始化各个兼容性模块
                Meadow.MeadowCompat.Initialize();
                
                // 在这里添加其他兼容性模块的初始化
                // OtherMod.OtherModCompat.Initialize();
                
                Debug.Log("[CowBoySlug] 兼容性模块初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 初始化兼容性模块时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查指定的程序集是否存在
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns>程序集是否存在</returns>
        public static bool DoesAssemblyExist(string assemblyName)
        {
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => a.GetName().Name == assemblyName);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 尝试获取指定的程序集
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns>程序集对象，如果不存在则返回null</returns>
        public static Assembly GetAssembly(string assemblyName)
        {
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);
            }
            catch
            {
                return null;
            }
        }
    }
} 