# MyComputerMonitor 项目说明

## 项目简介

MyComputerMonitor 是一个功能强大的Windows硬件监控软件，专为需要实时监控电脑硬件状态的用户设计。软件支持开机自启动、后台运行、系统托盘常驻，能够实时监控CPU、GPU、内存、主板等硬件的使用率、温度、风扇转速等关键信息。

## 主要特性

- 🖥️ **全面硬件监控**: CPU、GPU、内存、主板、硬盘全方位监控
- 🎯 **系统托盘常驻**: 鼠标悬停即可查看硬件状态
- 🚀 **开机自启动**: 支持开机自动启动，后台静默运行
- 📊 **实时图表显示**: 美观的实时数据图表展示
- 🎨 **现代化界面**: 支持浅色/深色主题切换
- ⚡ **低资源占用**: 优化设计，内存占用小于50MB
- 🔧 **高度可配置**: 丰富的设置选项，满足不同需求

## 技术栈

- **开发语言**: C#
- **框架**: WPF (.NET 9)
- **硬件监控**: LibreHardwareMonitor
- **UI组件**: ModernWpf
- **图表**: LiveCharts
- **系统托盘**: Hardcodet.NotifyIcon.Wpf

## 系统要求

- **操作系统**: Windows 10/11
- **运行时**: .NET 9.0 或更高版本
- **内存**: 至少 100MB 可用内存
- **硬件**: 支持硬件监控的主板芯片组

## 快速开始

### 开发环境搭建

1. 安装 Visual Studio 2022
2. 安装 .NET 9.0 SDK
3. 克隆项目到本地
4. 打开解决方案文件 `MyComputerMonitor.sln`
5. 还原 NuGet 包
6. 编译并运行

### 项目结构

```
MyComputerMonitor/
├── src/                     # 源代码
│   ├── Core/               # 核心业务逻辑
│   ├── UI/                 # 用户界面
│   ├── Infrastructure/     # 基础设施
│   └── App/               # 应用程序入口
├── tests/                  # 测试项目
├── docs/                   # 文档
└── assets/                 # 资源文件
```

## 开发计划

详细的开发计划请参考 [开发计划.md](./开发计划.md) 文档。

## 贡献指南

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 联系方式

如有问题或建议，请通过以下方式联系：

- 项目Issues: [GitHub Issues](https://github.com/username/MyComputerMonitor/issues)
- 邮箱: 646445026@qq.com

## 更新日志

### v1.0.0 (计划中)
- 基础硬件监控功能
- 系统托盘集成
- 开机自启动支持
- 现代化用户界面

---

**开始你的硬件监控之旅！** 🚀
