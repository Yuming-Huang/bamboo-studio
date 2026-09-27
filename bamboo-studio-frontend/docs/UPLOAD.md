# 将本仓库上传到 GitHub

这是独立源码仓库，不包含 node_modules、编译产物、本机登录信息或商业 SDK。上传不会改动已安装的竹拱工作室。

## 方式一：GitHub 网页上传

1. 登录 GitHub，新建一个空仓库。前端命名为 `bamboo-studio-frontend`，后端命名为 `bamboo-studio-backend`。
2. 自己选择仓库可见性。创建时不要另行生成 README、`.gitignore` 或许可证，避免与本包已有内容冲突。
3. 将对应 ZIP 解压，在解压后的仓库根目录能看到 `README.md`。
4. 进入 GitHub 仓库的上传文件入口，上传该根目录内的文件和子目录，不是上传 ZIP 本身，也不要再套一层同名目录。
5. 确认 `.gitignore`、`.gitattributes`、锁文件也在上传列表中；Windows 资源管理器可能未明显显示点号文件。
6. 填写提交说明并提交。仓库首页应直接显示本 README。
7. 对另一个仓库重复以上操作。

如果网页上传遇到文件数量或浏览器限制，使用下面的 Git 方式。不要上传本地 `.git` 文件夹。

## 方式二：Git 命令上传

需要先安装 Git，并在 GitHub 创建对应的空仓库。下面以当前仓库为例，在其根目录打开 PowerShell：

```powershell
git init -b main
git add .
git status --short
git commit -m "Import Bamboo Studio 0.8.0 source"
```

首次提交若提示没有作者身份，只为这个仓库设置你的真实姓名及 GitHub 账户可用邮箱，然后再次执行 commit：

```powershell
git config user.name "你的名字"
git config user.email "你的邮箱"
```

复制 GitHub 空仓库页面给出的 HTTPS 地址，替换以下占位内容：

```powershell
git remote add origin https://github.com/YOUR_ACCOUNT/YOUR_REPOSITORY.git
git push -u origin main
```

`YOUR_REPOSITORY` 分别使用前端、后端仓库名，`YOUR_ACCOUNT` 替换为自己的账户。按 Git 的登录提示完成认证，不要把密码或访问令牌写进源文件。两仓库各执行一次。

本交付包未配置远程地址，也未替你创建远程仓库或执行上传。

## 后续更新

在对应仓库修改源码并通过测试后：

```powershell
git add .
git diff --cached --stat
git commit -m "Describe this change"
git push
```

前端外观和交互更新主要提交前端；计算、参数约束与搜索算法提交后端。接口字段变化需同步两仓库的 API 文档与测试。

需要分发安装文件时，将前端完整发布目录压缩为 Release 附件；后端发布自有 `.rhp`。不要把第三方商业 SDK 放进源码仓库或 Release。
