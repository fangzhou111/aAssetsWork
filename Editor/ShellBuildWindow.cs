using System.Collections;
using SuperMobs.AssetManager.Editor;
using UnityEditor;
using UnityEngine;

/*
 * 自定义编译测试
 */
using SuperMobs.AssetManager.Core;
using System.IO;
namespace SuperMobs.AssetManager.Editor
{

    public class ShellBuildWindow : EditorWindow
    {
        static ShellBuildWindow window = null;
        static string content = @"
companyName = funtoy
appName = 草地的demo
bundleid = com.supermobs.demo
sdk = default.none
channel = default
version = 1.0
log = enable
web = 
cdn = 
server = 
demo = yes
noUpdate = yes
iosprofile = release=0e1299fa-b278-461b-8ad3-ecead7c4cf25;debug=0e1299fa-b278-461b-8ad3-ecead7c4cf25
priority = set
	";

        [MenuItem("SuperMobs/AssetManager/Player/SimulationWindow")]
        static void ShowWindow()
        {
            // 检测是否在根目录有默认的这个模拟编译的文件
            var contentPath = AssetPath.ProjectRoot + "EDITOR_SHEEL_BUILD_SETTING.config";
            if (File.Exists(contentPath) == false)
            {
                File.WriteAllText(contentPath, content, System.Text.Encoding.UTF8);
            }
            else
            {
                content = File.ReadAllText(contentPath);
            }

            if (window == null)
                window = (ShellBuildWindow) GetWindow(
                    typeof(ShellBuildWindow),
                    true,
                    "模拟编译机编译",
                    true
                );
            window.minSize = new Vector2(750, 300);
            window.maxSize = new Vector2(750, 300);
            window.Show();
        }

        void OnGUI()
        {
            content = EditorGUILayout.TextArea(content, GUILayout.Height(250), GUILayout.Width(745));
            //var old = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("BuildPlayer", GUILayout.Height(40)))
            {
                Debug.Log("开始模拟发布player");
                // push cmd
                ByteReader br = new ByteReader(System.Text.Encoding.UTF8.GetBytes(content));
                var dict = br.ReadDictionary();
                foreach (var item in dict)
                {
                    Debug.Log(item.Key + " = " + item.Value);
                    if (string.IsNullOrEmpty(item.Value) == false)
                    {
                        System.Environment.SetEnvironmentVariable(item.Key, item.Value, System.EnvironmentVariableTarget.Process);
                    }
                }

                System.Environment.SetEnvironmentVariable("SIM_BUILD_PLAYER", "yes", System.EnvironmentVariableTarget.Process);

                //window.Close();
                //window = null;

                ShellBuilder.BuildPlayer();
            }
        }

    }
}