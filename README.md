# AssetManager

## 使用说明

### 关于AssetBundle

#### 特殊通用的Bundle

- 如果打包输出的bundle名称为comm.ab时候,将是特殊通用的bundle,例如shader可以添加进去.将不会被释放.

### 编辑器设置

#### 关于发布时候的Build Setting场景导出

- 因为为了兼容加载接口可以直接加载AB场景按名字,在Editor下模拟载入也能正确找到名字的场景载入,增加了导出场景自动记录和运行时把项目中的场景添加进去.
- 所以修改Build Setting好的时候请运行一下刷新保存即可.
或者在根目录Editor_Build_Setting.config文件中进行修改

### 发布apk|ipa程式

#### 关于c#代码版本号

* 代码版本号可以自动检测每次编译从0开始自动递增,记录在根目录的AUTO_VERSION_DONT_MODIFY.config文件.
> AUTO_VERSION_DONT_MODIFY.config文件格式说明:
> <br>code 当前代码版本号
> <br>crc 自动计算备份的改c#代码的md5值
> <br>buildNum 每次发包会自动++,提供给苹果后台上传需要递增 

* 代码版本号可以通过在项目中创建一个c#文件并设置一个静态变量命名为:static int SuperScriptCodeNum = 2;

### 更新模块

#### 关于web页面请求地址及参数

* 例子web + &platform=ios&channel=dyb&bundleid=com.x.n
  参数
      platform : ios , android
      channel : dyb,uc,....在编译程序setting配置
      bundleid : com.xx.xx 这个app具体的包名

#### 关于更新模块记录到PlayerPref的参数

* APP_IS_REVIEW    int 这个app是否审核版本 0 :不是, 1:是
* WEB_EXTENSION    string web端配置的扩展参数存起来
* LOGIN_SERVER  string 登陆服务器
* APP_FULL_VERSION  string 这个app的完整版本号1.0.x.x

## 更新记录

### version 1.3.1

* IBuildBundleBuilder增加BuildMode可以控制相同的api可以multi一批打包,优化unity打包compling script速度

### version 1.3

* 增加android8的安装唤醒,需要增加权限 <uses-permission android:name="android.permission.REQUEST_INSTALL_PACKAGES"/>

### version 1.2.1

* 增加web更新请求增加bundleId参数
* 修复RuntimeInitialize在Editor初始化编译机加载不出来bug

### version 1.2

* add odin inspector cool plugin!

### version 1.1.1

* 更新到unity5.6版本
* 增加兼容unity5.4代码,通过UNITY_5_6_OR_NEWER宏来控制
