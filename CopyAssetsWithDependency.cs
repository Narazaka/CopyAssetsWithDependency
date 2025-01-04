/*
Forked from https://qiita.com/k7a/items/eb5a3ee4ed6448343543 by k7a

Copyright (c) 2020 Narazaka

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

   3. This notice may not be removed or altered from any source
   distribution.
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.VersionControl;

namespace Narazaka.Unity.CopyAssetsWithDependency
{
    public class CopyAssetsWithDependency
    {
        /// <summary>
        /// YAML形式のEncoding
        /// </summary>
        private static Encoding Encoding { get { return Encoding.GetEncoding("UTF-8"); } }

        /// <summary>
        /// コピー後のファイル名やAssetBundle名につける接尾辞
        /// </summary>
        private const string Suffix = " copy";

        [MenuItem("Assets/CopyAssetsWithDependency")]
        public static void Execute()
        {
            if (Selection.assetGUIDs.Count() == 0)
            {
                Debug.LogError("コピーしたいアセットを選択してください");
                return;
            }

            DeepCopy(Selection.assetGUIDs.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToList());
        }

        public static Dictionary<string, string> GetAllAssetAndCopyPaths(IEnumerable<string> baseAssetPaths)
        {
            var assetPathMap = new Dictionary<string, string>();
            foreach (var baseAssetPath in baseAssetPaths)
            {
                if (Directory.Exists(baseAssetPath))
                {
                    var baseAssetFullPath = Path.GetFullPath(baseAssetPath) + "\\";
                    foreach (var filePath in Directory.EnumerateFiles(baseAssetPath, "*", SearchOption.AllDirectories))
                    {
                        if (Path.GetExtension(filePath) == ".meta")
                            continue;
                        assetPathMap[filePath] = Path.Combine($"{baseAssetPath}{Suffix}", GetRelativePath(baseAssetFullPath, Path.GetFullPath(filePath)));
                    }
                }
                else
                {
                    assetPathMap[baseAssetPath] = $"{Path.GetDirectoryName(baseAssetPath)}/{Path.GetFileNameWithoutExtension(baseAssetPath)}{Suffix}{Path.GetExtension(baseAssetPath)}";
                }
            }
            return assetPathMap;
        }

        public static string GetRelativePath(string fromPath, string toPath)
        {
            return Uri.UnescapeDataString(new Uri(fromPath).MakeRelativeUri(new Uri(toPath)).ToString());
        }

        /// <summary>
        /// コピー後のAssetBundle名を返すメソッド
        /// </summary>
        public static string GetCopyAssetBundleName(string assetBundleName, string suffix)
        {
            return Regex.Replace(assetBundleName, @"(?<path>.+)(?<extension>\..+)", string.Format("${{path}}{0}${{extension}}", Suffix));
        }

        /// <summary>
        /// 指定したアセット達をAssetDatabase.ImportAssetする
        /// </summary>
        public static void ImportAssets(IEnumerable<string> assetPaths)
        {
            foreach (var assetPath in assetPaths)
                AssetDatabase.ImportAsset(assetPath);
        }

        /// <summary>
        /// 対象のアセットと依存関係にある全てのアセットをコピーし、それぞれの参照先をコピー先のアセットにする
        /// </summary>
        public static void DeepCopy(IEnumerable<string> baseAssetPaths)
        {
            // コピー前後のパスの対応関係
            var assetPathMap = GetAllAssetAndCopyPaths(baseAssetPaths);

            // 確認ダイアログを出す
            var assetsCount = assetPathMap.Count();

            bool confirm = EditorUtility.DisplayDialog(
                "確認",
                string.Format("{0}個のアセットをコピーします。よろしいですか？\n{1}", assetsCount, string.Join("\n", baseAssetPaths)),
                "OK",
                "キャンセル"
            );

            if (confirm)
                Debug.Log(string.Format("[Deep Copy Assets] Start...target assets count: {0}", assetsCount));
            else
                return;

            // コピー前のパスとGUIDの対応関係
            var assetPathGUIDMap = assetPathMap.Keys.ToDictionary(path => path, path => AssetDatabase.AssetPathToGUID(path));

            // コピー前後のGUIDの対応関係
            var GUIDMap = new Dictionary<string, string>();

            // アセットのコピーを行い、コピー先のGUIDを生成する
            foreach (var kvp in assetPathMap)
            {
                var assetPath = kvp.Key;
                var copyPath = kvp.Value;
                var originalGUID = assetPathGUIDMap[assetPath];

                // アセットのコピーを行う
                var copyDir = Path.GetDirectoryName(copyPath);
                if (!Directory.Exists(copyDir))
                    Directory.CreateDirectory(copyDir);
                System.IO.File.Copy(assetPath, copyPath);

                // 一旦ImportしてUnityにGUIDを生成させる
                AssetDatabase.ImportAsset(copyPath);
                var copyGUID = AssetDatabase.AssetPathToGUID(copyPath);
                GUIDMap.Add(originalGUID, copyGUID);

                // メタファイルをGUIDを書き換えてコピーする
                using (StreamReader sr = new StreamReader(string.Format("{0}.meta", assetPath), Encoding))
                {
                    string s = sr.ReadToEnd();
                    // GUIDを置換
                    s = s.Replace(originalGUID, copyGUID);

                    using (FileStream fs = new FileStream(string.Format("{0}.meta", copyPath), System.IO.FileMode.Truncate, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.Write(s);
                        }
                    }
                }
                Debug.Log(string.Format("[Copy Meta] from:{0}, to:{1}", assetPath, copyPath));

                Debug.Log(string.Format("[Copy Asset] from:{0}, to:{1}", assetPath, copyPath));
            }
            foreach (var kvp in assetPathMap)
            {
                var assetPath = kvp.Key;
                var copyPath = kvp.Value;
                var originalGUID = assetPathGUIDMap[assetPath];
                var copyGUID = GUIDMap[originalGUID];
            }

            // アセットの再読み込みを行う
            // 書き換え直後にImportすると上手く読み込んでくれないことがあるので一通り処理した後に回している
            ImportAssets(assetPathMap.Values);

            foreach (var kvp in assetPathMap)
            {
                var assetPath = kvp.Key;
                var copyPath = kvp.Value;

                // prefabやanimationが参照するGUIDの書き換えを行う
                using (StreamReader sr = new StreamReader(assetPath, Encoding))
                {
                    string s = sr.ReadToEnd();
                    // YAML形式の場合のみ参照先のGUIDの書き換え処理
                    if (s.StartsWith("%YAML"))
                    {
                        foreach (var originalAssetPath in assetPathMap.Keys)
                        {
                            var originalAssetGUID = assetPathGUIDMap[originalAssetPath];
                            var copyAssetGUID = GUIDMap[originalAssetGUID];
                            s = s.Replace(originalAssetGUID, copyAssetGUID);
                        }

                        Debug.Log(string.Format("[Replace Dependencies] {0}", copyPath));
                        using (StreamWriter sw = new StreamWriter(copyPath, false, Encoding))
                        {
                            sw.Write(s);
                        }
                    }
                }
            }

            // 再読み込みを走らせる（ImportAssetだと上手くいかない）
            AssetDatabase.Refresh();
        }
    }
}
