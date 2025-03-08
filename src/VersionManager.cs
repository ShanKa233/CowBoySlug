using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CowBoySLug
{
    /// <summary>
    /// 版本管理器，用于自动更新版本文件
    /// </summary>
    public static class VersionManager
    {
        // 获取当前程序集所在目录
        private static readonly string BaseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        // 版本文件路径
        private static readonly string VersionFilePath = Path.Combine(BaseDirectory, "version.txt");
        
        // mod信息文件路径
        private static readonly string ModInfoPath = Path.Combine(BaseDirectory, "mod", "modinfo.json");
        
        // workshop数据文件路径
        private static readonly string WorkshopDataPath = Path.Combine(BaseDirectory, "mod", "workshopdata.json");
        
        // 当前版本
        private static Version _currentVersion;
        
        /// <summary>
        /// 当前版本
        /// </summary>
        public static Version CurrentVersion
        {
            get
            {
                if (_currentVersion == null)
                {
                    LoadVersion();
                }
                return _currentVersion;
            }
        }
        
        /// <summary>
        /// 初始化版本管理器
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadVersion();
                UnityEngine.Debug.Log($"[CowBoySlug] 当前版本: {_currentVersion}");
                
                // 确保版本文件与 modinfo.json 和 workshopdata.json 同步
                SyncVersionFiles();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] 初始化版本管理器时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载版本信息
        /// </summary>
        private static void LoadVersion()
        {
            try
            {
                UnityEngine.Debug.Log($"[CowBoySlug] 尝试从以下路径加载版本信息:");
                UnityEngine.Debug.Log($"[CowBoySlug] - 版本文件: {VersionFilePath}");
                UnityEngine.Debug.Log($"[CowBoySlug] - ModInfo文件: {ModInfoPath}");
                UnityEngine.Debug.Log($"[CowBoySlug] - Workshop数据文件: {WorkshopDataPath}");
                
                // 首先尝试从 modinfo.json 加载版本
                if (File.Exists(ModInfoPath))
                {
                    string modInfoContent = File.ReadAllText(ModInfoPath);
                    Match versionMatch = Regex.Match(modInfoContent, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                    if (versionMatch.Success)
                    {
                        string versionText = versionMatch.Groups[1].Value;
                        if (Version.TryParse(versionText, out Version modVersion))
                        {
                            _currentVersion = modVersion;
                            UnityEngine.Debug.Log($"[CowBoySlug] 从 modinfo.json 加载版本: {_currentVersion}");
                            SaveVersion(); // 保存到版本文件
                            return;
                        }
                    }
                }
                
                // 如果从 modinfo.json 加载失败，尝试从版本文件加载
                if (File.Exists(VersionFilePath))
                {
                    string versionText = File.ReadAllText(VersionFilePath).Trim();
                    if (Version.TryParse(versionText, out Version fileVersion))
                    {
                        _currentVersion = fileVersion;
                        UnityEngine.Debug.Log($"[CowBoySlug] 从版本文件加载版本: {_currentVersion}");
                        return;
                    }
                }
                
                // 如果都失败，使用默认版本 0.2.50
                _currentVersion = new Version(0, 2, 50);
                UnityEngine.Debug.Log($"[CowBoySlug] 使用默认版本: {_currentVersion}");
                SaveVersion();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] 加载版本信息时出错: {ex.Message}");
                _currentVersion = new Version(0, 2, 50);
            }
        }
        
        /// <summary>
        /// 保存版本信息
        /// </summary>
        private static void SaveVersion()
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(VersionFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(VersionFilePath, _currentVersion.ToString());
                UnityEngine.Debug.Log($"[CowBoySlug] 版本信息已保存到: {VersionFilePath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] 保存版本信息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 同步所有版本文件
        /// </summary>
        private static void SyncVersionFiles()
        {
            try
            {
                string versionString = _currentVersion.ToString();
                
                // 更新 modinfo.json
                if (File.Exists(ModInfoPath))
                {
                    string modInfoContent = File.ReadAllText(ModInfoPath);
                    modInfoContent = Regex.Replace(modInfoContent, "(\"version\"\\s*:\\s*\")[^\"]+(\",)", $"$1{versionString}$2");
                    File.WriteAllText(ModInfoPath, modInfoContent);
                    UnityEngine.Debug.Log($"[CowBoySlug] 已更新 modinfo.json 中的版本号: {versionString}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[CowBoySlug] 找不到 modinfo.json 文件: {ModInfoPath}");
                }
                
                // 更新 workshopdata.json
                if (File.Exists(WorkshopDataPath))
                {
                    string workshopDataContent = File.ReadAllText(WorkshopDataPath);
                    workshopDataContent = Regex.Replace(workshopDataContent, "(\"Version\"\\s*:\\s*\")[^\"]+(\",)", $"$1{versionString}$2");
                    File.WriteAllText(WorkshopDataPath, workshopDataContent);
                    UnityEngine.Debug.Log($"[CowBoySlug] 已更新 workshopdata.json 中的版本号: {versionString}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[CowBoySlug] 找不到 workshopdata.json 文件: {WorkshopDataPath}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] 同步版本文件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新版本号
        /// </summary>
        /// <param name="updateType">更新类型：0=补丁版本，1=次要版本，2=主要版本</param>
        public static void UpdateVersion(int updateType = 0)
        {
            try
            {
                if (_currentVersion == null)
                {
                    LoadVersion();
                }
                
                int major = _currentVersion.Major;
                int minor = _currentVersion.Minor;
                int build = _currentVersion.Build;
                
                switch (updateType)
                {
                    case 0: // 补丁版本
                        build++;
                        break;
                    case 1: // 次要版本
                        minor++;
                        build = 0;
                        break;
                    case 2: // 主要版本
                        major++;
                        minor = 0;
                        build = 0;
                        break;
                }
                
                _currentVersion = new Version(major, minor, build);
                SaveVersion();
                
                // 同步所有版本文件
                SyncVersionFiles();
                
                UnityEngine.Debug.Log($"[CowBoySlug] 版本已更新: {_currentVersion}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] 更新版本号时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取版本字符串
        /// </summary>
        public static string GetVersionString()
        {
            return CurrentVersion.ToString();
        }
    }
} 