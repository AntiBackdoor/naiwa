# Naiwa 视频播放器 Spec

## Why
需要一个基于 WPF 的全屏视频播放器应用程序，能够加载并播放 "Naiwa.mp4" 视频文件，提供直观的播放控制和全屏显示体验。

## What Changes
- 重构 MainWindow 为全屏视频播放器界面
- 使用 MediaElement 实现视频加载与播放
- 实现播放/暂停控制
- 实现进度条显示与拖动跳转
- 实现音量控制滑块
- 实现全屏/窗口模式切换
- 实现播放状态显示（当前时间/总时长）
- 实现控制栏自动隐藏（鼠标静止时隐藏，移动时显示）
- 将 Naiwa.mp4 文件复制到项目输出目录

## Impact
- Affected code: MainWindow.xaml, MainWindow.xaml.cs, naiwa.csproj
- 新增文件: 无（在现有文件基础上修改）

## ADDED Requirements

### Requirement: 视频加载与播放
系统 SHALL 在启动时自动定位并加载与可执行文件同目录下的 Naiwa.mp4 文件，并自动开始播放。

#### Scenario: 正常加载
- **WHEN** 应用程序启动
- **THEN** 系统定位到 Naiwa.mp4 文件并自动开始播放

#### Scenario: 文件不存在
- **WHEN** Naiwa.mp4 文件不存在
- **THEN** 界面显示友好的错误提示信息

### Requirement: 播放/暂停控制
系统 SHALL 提供播放/暂停按钮，允许用户切换视频的播放状态。同时支持空格键快捷键切换。

#### Scenario: 点击播放/暂停按钮
- **WHEN** 用户点击播放/暂停按钮
- **THEN** 视频在播放与暂停状态之间切换，按钮图标相应更新

#### Scenario: 空格键快捷键
- **WHEN** 用户按下空格键
- **THEN** 视频在播放与暂停状态之间切换

### Requirement: 进度条显示与跳转
系统 SHALL 显示视频播放进度条，包含当前播放时间和总时长，并允许用户拖动进度条跳转到指定位置。

#### Scenario: 进度条实时更新
- **WHEN** 视频正在播放
- **THEN** 进度条实时更新，显示当前播放位置和时间

#### Scenario: 拖动进度条跳转
- **WHEN** 用户拖动进度条到指定位置
- **THEN** 视频跳转到对应位置继续播放

### Requirement: 音量控制
系统 SHALL 提供音量控制滑块，允许用户调节视频播放音量（0-100%），并支持静音切换。

#### Scenario: 调节音量
- **WHEN** 用户拖动音量滑块
- **THEN** 视频播放音量相应变化

#### Scenario: 静音切换
- **WHEN** 用户点击静音按钮
- **THEN** 音量切换为静音/恢复，图标相应更新

### Requirement: 全屏模式切换
系统 SHALL 支持全屏与窗口模式之间的切换，支持双击视频区域和 F11 快捷键切换。默认以全屏模式启动。

#### Scenario: 双击切换全屏
- **WHEN** 用户双击视频区域
- **THEN** 在全屏与窗口模式之间切换

#### Scenario: F11 快捷键切换全屏
- **WHEN** 用户按下 F11 键
- **THEN** 在全屏与窗口模式之间切换

#### Scenario: ESC 退出全屏
- **WHEN** 用户在全屏模式下按下 ESC 键
- **THEN** 退出全屏，切换到窗口模式

### Requirement: 控制栏自动隐藏
系统 SHALL 在鼠标静止 3 秒后自动隐藏控制栏，鼠标移动时重新显示控制栏。

#### Scenario: 鼠标静止隐藏
- **WHEN** 鼠标在视频区域静止超过 3 秒
- **THEN** 控制栏自动隐藏

#### Scenario: 鼠标移动显示
- **WHEN** 鼠标在视频区域移动
- **THEN** 控制栏重新显示

### Requirement: 视频文件部署
系统 SHALL 在项目构建时将 Naiwa.mp4 复制到输出目录，确保运行时可正确访问。

#### Scenario: 构建后文件可用
- **WHEN** 项目构建完成
- **THEN** Naiwa.mp4 存在于输出目录中
