﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Talifun.Web.Crusher
{
    public class CssFileProcessor
    {
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        protected readonly IRetryableFileOpener RetryableFileOpener;
        protected readonly IPathProvider PathProvider;
        protected readonly ICssPathRewriter CssPathRewriter;
        protected readonly FileInfo FileInfo;
        protected readonly Uri CssRootUri;
        protected readonly Uri RelativeRootUri;
        protected readonly bool AppendHashToAssets;

        public CssFileProcessor(IRetryableFileOpener retryableFileOpener, IPathProvider pathProvider, ICssPathRewriter cssPathRewriter, string filePath, CssCompressionType compressionType, Uri cssRootUri, bool appendHashToAssets)
        {
            RetryableFileOpener = retryableFileOpener;
            PathProvider = pathProvider;
            CssPathRewriter = cssPathRewriter;
            CompressionType = compressionType;

            var resolvedFilePath = PathProvider.MapPath(filePath);
            FileInfo = new FileInfo(resolvedFilePath);
            CssRootUri = cssRootUri;
            RelativeRootUri = PathProvider.GetRelativeRootUri(filePath);

            AppendHashToAssets = appendHashToAssets;
        }

        public CssCompressionType CompressionType { get; protected set; }

        private string _contents;
        /// <summary>
        /// Get the processed css of a file.
        /// 
        /// Files with extension .less or .less.css will be generated with dotless.
        /// If appentHashToAssets = true, hashes of local existing files will be appending to processed css.
        /// </summary>
        /// <returns>The processed contents of a file.</returns>
        public string GetContents()
        {
            _lock.EnterUpgradeableReadLock();

            try
            {
                if (_contents == null)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _contents = RetryableFileOpener.ReadAllText(FileInfo);

                        var fileName = FileInfo.Name.ToLower();

                        if (fileName.EndsWith(".less") || fileName.EndsWith(".less.css"))
                        {
                            _contents = ProcessDotLess(_contents);
                        }

                        var cssRootPathUri = PathProvider.GetRootPathUri(CssRootUri);
                        var distinctRelativePaths = CssPathRewriter.FindDistinctRelativePaths(_contents);
                        _contents = CssPathRewriter.RewriteCssPathsToBeRelativeToPath(distinctRelativePaths,
                                                                                      cssRootPathUri,
                                                                                      RelativeRootUri, _contents);

                        if (AppendHashToAssets)
                        {
                            _contents = ProcessAppendHash(cssRootPathUri, _contents);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }

                return _contents;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        private DateTime? _lastModified;
        /// <summary>
        /// The time the file was last modified in Utc time.
        /// </summary>
        /// <returns>Last modifed time.</returns>
        public DateTime GetLastModified()
        {
            if (!_lastModified.HasValue)
            {
                _lastModified = FileInfo.LastWriteTimeUtc;
            }
            return _lastModified.Value;
        }

        private List<FileInfo> _localCssAssetFilesThatExist;
        /// <summary>
        /// Gets local css assets that exist.
        /// </summary>
        /// <returns>A list of FileInfo of css assets that exist.</returns>
        public List<FileInfo> GetLocalCssAssetFilesThatExist()
        {
            if (_localCssAssetFilesThatExist == null)
            {
                GetContents();

                if (_localCssAssetFilesThatExist == null)
                {
                    _localCssAssetFilesThatExist = new List<FileInfo>();
                }
            }

            return _localCssAssetFilesThatExist;
        }

        /// <summary>
        /// Compiles dot less css file contents.
        /// </summary>
        /// <param name="fileContents">Uncompiled css file contents.</param>
        /// <returns>Compiled css file contents.</returns>
        protected virtual string ProcessDotLess(string fileContents)
        {
            return dotless.Core.Less.Parse(fileContents);
        }

        protected string ProcessAppendHash(Uri cssRootPathUri, string fileContents)
        {
            if (_localCssAssetFilesThatExist == null)
            {
                _localCssAssetFilesThatExist = new List<FileInfo>();
            }

            var distinctLocalPaths = CssPathRewriter.FindDistinctLocalPaths(fileContents);
            var distinctLocalPathsThatExist = new List<Uri>();

            foreach (var distinctLocalPath in distinctLocalPaths)
            {
                var cssAssetFileInfo = new FileInfo(PathProvider.MapPath(cssRootPathUri, distinctLocalPath));

                if (!cssAssetFileInfo.Exists)
                {
                    continue;
                }

                distinctLocalPathsThatExist.Add(distinctLocalPath);
                _localCssAssetFilesThatExist.Add(cssAssetFileInfo);
            }

            return CssPathRewriter.RewriteCssPathsToAppendHash(distinctLocalPathsThatExist, cssRootPathUri, fileContents);
        }
    }
}
