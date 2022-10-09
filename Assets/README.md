#SuperLoadConfig
* 在根目录会生成一个CONFIG，然后里面可以设置某个类型资源的加载方式AB|Source

#AssetPreference
* 负责管理整个资源系统的配置

#宏定义
* SIM_PLAY
	让当前Editor下模拟真机加载，会加载下载目录下的资源

#加载器
* LoaderService
	加载器服务器，用来提供加载器的选用，初始化服务在AssetManager初始化和重置里面


