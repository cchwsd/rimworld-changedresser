using ChangeDresser.UI.DTO;
using RimWorld;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ChangeDresser
{
    public enum ColorPresetType
    {
        Apparel,
        Hair,
        Skin
    };

    static class IOUtil
    {
        private static bool TryGetFileName(ColorPresetType type, out string fileName)
        {
            if (TryGetDirectoryPath(type, out fileName))
            {
                fileName = Path.Combine(fileName, type.ToString() + ".xml");
                return true;
            }

            return false;
        }

        private static bool TryGetDirectoryPath(ColorPresetType type, out string path)
        {
            if (TryGetDirectoryName(out path))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }

                return true;
            }

            return false;
        }

        private static bool TryGetDirectoryName(out string path)
        {
            try
            {
                path = (string)typeof(GenFilePaths)
                    .GetMethod("FolderUnderSaveData", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                        new object[]
                        {
                            "ChangeDresser"
                        });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("ChangeDresser: Failed to get folder name - " + ex);
                path = null;
                return false;
            }
        }
    }
}