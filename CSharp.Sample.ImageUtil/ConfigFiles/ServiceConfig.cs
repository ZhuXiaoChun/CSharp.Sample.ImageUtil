﻿using BaoXia.Utils;
using BaoXia.Utils.Extensions;
using CSharp.Sample.ImageUtil.Controllers;
using CSharp.Sample.ImageUtil.Models;

namespace CSharp.Sample.ImageUtil.ConfigFiles
{
        public class ServiceConfig : ConfigFile
        {
                ////////////////////////////////////////////////
                // @静态常量
                ////////////////////////////////////////////////

                #region 静态常量

                public const double FileInfoStorageIntervalSecondsDefault = 1.0;

                #endregion



                ////////////////////////////////////////////////
                // @自身属性
                ////////////////////////////////////////////////


                ////////////////////////////////////////////////
                // @通用文件相关
                ////////////////////////////////////////////////

                #region 自身属性

                /// <summary>
                /// 文件上传大小的最大值，单位为“MB”。
                /// </summary>
                public long FileUploadSizeInMBMax { get; set; }

                public long FileUploadSizeInBytesMax => FileUploadSizeInMBMax * 1024 * 1024;

                /// <summary>
                /// 更新文件时是否自动备份旧文件。
                /// </summary>
                public bool IsFileBackupFirstUpdateSecondEnable { get; set; }

                /// <summary>
                /// 是否文件标签是必须的。
                /// </summary>
                public bool IsFileTagNecessary { get; set; }

                private FilePathRule[]? _filePathRules;

                /// <summary>
                /// 文件路径规则。
                /// </summary>
                public FilePathRule[]? FilePathRules
                {
                        get => _filePathRules;
                        set
                        {
                                var filePathRules = value;
                                if (filePathRules?.Length > 0)
                                {
                                        // !!! 按标签数量由少到多排序 !!!
                                        Array.Sort(
                                                filePathRules,
                                                (filePathRuleA, filePathRuleB) =>
                                                {
                                                        if (filePathRuleA.FileTagsCount < filePathRuleB.FileTagsCount)
                                                        {
                                                                return -1;
                                                        }
                                                        else if (filePathRuleA.FileTagsCount > filePathRuleB.FileTagsCount)
                                                        {
                                                                return 1;
                                                        }
                                                        return 0;
                                                });
                                }
                                _filePathRules = filePathRules;
                        }
                }

                ////////////////////////////////////////////////
                // @安全相关
                ////////////////////////////////////////////////

                /// <summary>
                /// 会话Token验证间隔秒数。
                /// </summary>
                public double AuthorizationTokenVerifyIntervalSeconds { get; set; }

                /// <summary>
                /// 文件信息持久化间隔秒数。
                /// </summary>
                public double FileInfoStorageIntervalSeconds { get; set; }

                #endregion


                ////////////////////////////////////////////////
                // @自身实现
                ////////////////////////////////////////////////

                #region 自身实现

                public FilePathRule? GetFilePathRuleWithFileExtensionName(
                        string? fileExtensionName,
                        string[]? fileTags)
                {
                        if (fileExtensionName == null
                            || fileExtensionName.Length < 1)
                        {
                                return null;
                        }
                        if (fileTags == null
                                || fileTags.Length < 1)
                        {
                                if (Config.Service.IsFileTagNecessary == true)
                                {
                                        return null;
                                }
                        }

                        var filePathRules = Config.Service.FilePathRules;
                        if (filePathRules == null
                                || filePathRules.Length < 1)
                        {
                                return null;
                        }

                        FilePathRule? filePathRuleMatched = null;
                        foreach (var filePathRule in filePathRules)
                        {
                                var isFileExtensionNameMatched = false;
                                var filePathRuleFileExtensionNames
                                    = filePathRule.FileExtensionNames;
                                if (filePathRuleFileExtensionNames
                                    ?.Contains(fileExtensionName) == true)
                                {
                                        isFileExtensionNameMatched = true;
                                }

                                if (isFileExtensionNameMatched)
                                {
                                        var isFileTagsMatched = false;
                                        var filePathRuleFileTags = filePathRule.FileTags;
                                        if (filePathRuleFileTags?.Length > 0)
                                        {
                                                if (filePathRuleFileTags?.IsContains(fileTags) == true)
                                                {
                                                        isFileTagsMatched = true;
                                                }
                                        }
                                        else
                                        {
                                                isFileTagsMatched = true;
                                        }

                                        if (isFileTagsMatched)
                                        {
                                                // !!!
                                                filePathRuleMatched = filePathRule;
                                                break;
                                                // !!!
                                        }
                                }
                        }
                        return filePathRuleMatched;
                }

                #endregion



                ////////////////////////////////////////////////
                // @重载
                ////////////////////////////////////////////////

                #region 重载
                protected override void DidLoadConfigFileCompletedFromFilePath(
                        string filePath,
                        ConfigFile configFile)
                {
                        base.DidLoadConfigFileCompletedFromFilePath(
                                filePath,
                                configFile);

                        Task.Run(
                            () =>
                            {
                                    // !!!
                                    UploadController.DidServiceConfigChanged(this);
                                    // !!!
                            });
                }

                #endregion
        }
}
