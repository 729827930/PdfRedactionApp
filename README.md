# PDF智能脱敏应用程序

这是一个基于.NET Core的WPF桌面应用程序，用于对PDF文件进行智能脱敏处理。

## 功能特性

- PDF文件解析和文本提取
- 使用DeepSeek AI模型识别敏感信息（姓名、身份证号、电话号码、地址等）
- 基于坐标定位的精确脱敏处理
- 脱敏前后双栏对比预览
- 可配置的脱敏规则
- 实时处理进度显示

## 技术栈

- .NET 9.0
- WPF (Windows Presentation Foundation)
- PdfPig (PDF处理库)
- Newtonsoft.Json (JSON处理)
- DeepSeek API (AI识别)

## 安装和运行

1. 确保已安装 .NET 9.0 SDK
2. 克隆或下载此项目
3. 在项目根目录下运行以下命令：

```bash
dotnet restore
dotnet build
dotnet run
```

## 配置

应用程序的配置文件位于 `Config/redaction.config`，包括：
- DeepSeek API密钥
- 脱敏规则设置

## 使用说明

1. 点击"选择文件"按钮选择要处理的PDF文件
2. 点击"开始脱敏"按钮开始处理
3. 查看脱敏前后的对比预览
4. 点击"保存结果"按钮保存处理后的PDF文件

## 脱敏规则

支持以下类型的敏感信息脱敏：
- 姓名
- 身份证号
- 电话号码
- 地址

每种类型都可以单独启用或禁用。

## 开发说明

### 项目结构

- `Models/`: 数据模型
- `Services/`: 业务服务（PDF处理、AI集成）
- `ViewModels/`: 视图模型（MVVM模式）
- `Views/`: 用户控件
- `Config/`: 配置管理

### 扩展性

可以通过实现 `IRedactionRule` 接口来添加新的脱敏规则。