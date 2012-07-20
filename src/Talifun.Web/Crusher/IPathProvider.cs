﻿using System;

namespace Talifun.Web.Crusher
{
    public interface IPathProvider
    {
        string MapPath(string url);
        string MapPath(Uri url);
        string MapPath(Uri rootPath, string url);
        string MapPath(Uri rootPath, Uri url);
        string ToAbsolute(string virtualPath);
        string ToAbsolute(string virtualPath, string applicationPath);
        
        Uri GetUriDirectory(Uri uri);
        Uri GetRootPathUri(Uri rootUri);
        Uri GetRelativeRootUri(string filePath);
        Uri ToRelative(string filePath);
    }
}
