using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SuperMobs.AssetManager.Core;
using UnityEditor;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
    /// <summary>
    /// 自动管理和计算脚本的版本号
    /// 当脚本变化自动++
    /// 如果需要人手干预，才自动修改CONFIG文件
    /// </summary>
    public class PackageScriptVersion
    {
        public const string CONST_SCRIPT_CODE_FIELD_NAME = "SuperScriptCodeNum";

        public const string VERSION_RECORD_FILE = "_AUTO_VERSION_DONT_MODIFY.config";

        const string content = @"
            0
            0
		        0
        ";

        // 需要检查的目录如果是"" 代表Assets下都检查
        private static string[] CHECK_FOLDER = new string[] { "" };

        // 忽略所在目录的文件
        private static string[] ignoreTitle = new string[] { "/Editor/", "Plugins/Android/", "Plugins/iOS/" };

        static void CreateVersionFile(string platform)
        {
            string file = AssetPath.ProjectRoot + platform.ToString().ToUpper() + VERSION_RECORD_FILE;

            if (!File.Exists(file))
            {
                FileStream fs = File.Create(file);
                byte[] bytes = Encoding.Default.GetBytes(content);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }
        }

        public static int GenCodeNum(bool cpp, string platform)
        {
            CreateVersionFile(platform);

            string file = AssetPath.ProjectRoot + platform.ToString().ToUpper() + VERSION_RECORD_FILE;
            string[] vers = File.ReadAllLines(file);

            int curr = 0;
            if (vers.Length > 0 && !int.TryParse(vers[0], out curr))
            {
                //Debug.LogError("cant get current code version num = " + vers[0]);
            }

            uint crc = 0;
            if (vers.Length >= 2 && !uint.TryParse(vers[1], out crc))
            {
                //Debug.LogError("cant get current code crc = " + vers[1]);
            }

            int buildNum = 0;
            if (vers.Length >= 3 && !int.TryParse(vers[2], out buildNum))
            {
                //Debug.LogError("cant get current build num = " + vers[2]);
            }

            // check if script code define
            int scriptCode = -1;
            if (CheckConstScriptCode(out scriptCode))
            {
                Debug.Log("SOURCE CODE IS " + scriptCode + " FROME C# SETTING BY YOU!");
                curr = scriptCode;
                crc = 0;

                File.Delete(file);
                FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write);
                var content = Encoding.Default.GetBytes(curr + "\n" + crc + "\n" + buildNum);
                fs.Write(content, 0, content.Length);
                fs.Close();

                return curr;
            }

            uint currCRC = CalculateCurrentCodeCrc();
            if (currCRC != crc || cpp)
            {
                crc = currCRC;
                curr++;
                //Debug.Log("SOURCE CODE IS CHANGED:" + curr);

                // 刷新
                File.Delete(file);
                FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write);
                var content = Encoding.Default.GetBytes(curr + "\n" + crc + "\n" + buildNum);
                fs.Write(content, 0, content.Length);
                fs.Close();
            }

            return curr;
        }

        /// <summary>
        /// build num 每次打包build code会增加.但是不影响code
        /// </summary>
        public static int GenBuildNum(string platform)
        {
            CreateVersionFile(platform);

            string file = AssetPath.ProjectRoot + platform.ToString().ToUpper() + VERSION_RECORD_FILE;
            string[] vers = File.ReadAllLines(file);

            int curr = 0;
            if (!int.TryParse(vers[0], out curr))
            {
                //Debug.LogError("cant get current code version num = " + vers[0]);
            }

            uint crc = 0;
            if (!uint.TryParse(vers[1], out crc))
            {
                //Debug.LogError("cant get current code crc = " + vers[1]);
            }

            int buildNum = 0;
            if (vers.Length >= 3 && !int.TryParse(vers[2], out buildNum))
            {
                //Debug.LogError("cant get current build num = " + vers[2]);
            }

            // build Num 增加
            buildNum++;

            // 刷新
            File.Delete(file);
            FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write);
            var content = Encoding.Default.GetBytes(curr + "\n" + crc + "\n" + buildNum);
            fs.Write(content, 0, content.Length);
            fs.Close();

            return buildNum;
        }

        static bool CheckIfIgnorePath(string path)
        {
            foreach (string ig in ignoreTitle)
            {
                if (path.Contains(ig)) return true;
            }
            return false;
        }

        static uint CalculateCurrentCodeCrc()
        {
            List<string> allFiles = new List<string>();

            foreach (string folder in CHECK_FOLDER)
            {
                if (Directory.Exists(Application.dataPath + "/" + folder) == false) continue;

                List<string> paths = AssetEditorHelper.CollectAllPathDeep("Assets/" + folder, "*.cs").ToList();
                for (int i = 0; i < paths.Count;)
                {
                    FileInfo fi = new FileInfo(paths[i]);

                    if (CheckIfIgnorePath(fi.FullName.Replace(Path.DirectorySeparatorChar, '/')))
                    {
                        //Debug.LogWarning("rem " + paths[i]);
                        paths.RemoveAt(i);
                    }
                    else
                    {
                        //Debug.Log("script " + paths[i]);
                        i++;
                    }
                }
                allFiles.AddRange(paths);
                //Debug.Log("collect cs files = " + paths.Count);

                paths = AssetEditorHelper.CollectAllPathDeep("Assets/" + folder, "*.dll").ToList();
                for (int i = 0; i < paths.Count;)
                {
                    FileInfo fi = new FileInfo(paths[i]);

                    if (CheckIfIgnorePath(fi.FullName.Replace(Path.DirectorySeparatorChar, '/')))
                    {
                        paths.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                allFiles.AddRange(paths);
                //Debug.Log("collect dll files = " + paths.Count);
            }

            string val = string.Empty;
            foreach (string file in allFiles)
            {
                val += Crc32.GetFileCRC32(file).ToString();
            }

            return Crc32.GetStringCRC32(val);
        }

        /// <summary>
        /// 从项目指定的脚本中获取是否有填写固定的代码版本号 
        /// </summary>
        static bool CheckConstScriptCode(out int code)
        {
            code = SearchStaticScriptCodeInAssembly("Assembly-CSharp-firstpass");
            if (code == -1)
            {
                code = SearchStaticScriptCodeInAssembly("Assembly-CSharp");
            }

            return code != -1;
        }

        static int SearchStaticScriptCodeInAssembly(string name)
        {
            int code = -1;
            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(name);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
            }
            finally
            {
                if (assembly != null)
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        try
                        {
                            HashSet<string> assetPaths = new HashSet<string>();
                            FieldInfo[] listFieldInfo = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                            foreach (FieldInfo fieldInfo in listFieldInfo)
                            {
                                if (fieldInfo.Name.Contains(CONST_SCRIPT_CODE_FIELD_NAME) && fieldInfo.GetValue(null) is int)
                                {
                                    var o = fieldInfo.GetValue(null);
                                    code = Convert.ToInt32(o);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning(ex.Message);
                        }
                    }
                }
            }

            return code;
        }
    }
}