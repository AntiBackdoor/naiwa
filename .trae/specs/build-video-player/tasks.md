# Tasks

- [x] Task 1: 配置项目文件部署，将 Naiwa.mp4 包含到项目输出中
  - [x] SubTask 1.1: 修改 naiwa.csproj，添加 Naiwa.mp4 的 Content 项，设置 CopyToOutputDirectory

- [x] Task 2: 实现 MainWindow.xaml 视频播放器界面布局
  - [x] SubTask 2.1: 设计全屏窗口属性（WindowStyle=None, ResizeMode=NoResize, WindowState=Maximized）
  - [x] SubTask 2.2: 添加 MediaElement 作为视频播放区域
  - [x] SubTask 2.3: 添加底部控制栏（StackPanel/Grid），包含：播放/暂停按钮、进度条、时间显示、音量按钮、音量滑块、全屏按钮
  - [x] SubTask 2.4: 添加控制栏自动隐藏的动画/样式

- [x] Task 3: 实现 MainWindow.xaml.cs 播放器核心逻辑
  - [x] SubTask 3.1: 实现视频文件加载逻辑（定位 Naiwa.mp4 并设置 MediaElement.Source）
  - [x] SubTask 3.2: 实现播放/暂停切换逻辑及按钮图标更新
  - [x] SubTask 3.3: 实现进度条更新与拖动跳转逻辑
  - [x] SubTask 3.4: 实现音量控制与静音切换逻辑
  - [x] SubTask 3.5: 实现全屏/窗口模式切换逻辑（双击、F11、ESC）
  - [x] SubTask 3.6: 实现控制栏自动隐藏逻辑（DispatcherTimer，3秒无操作隐藏）
  - [x] SubTask 3.7: 实现文件不存在的错误提示

# Task Dependencies
- Task 2 依赖 Task 1（需要确认文件路径策略）
- Task 3 依赖 Task 2（需要界面元素已定义）
