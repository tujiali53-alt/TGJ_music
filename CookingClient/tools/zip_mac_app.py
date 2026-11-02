"""把 Windows 上打出的 .app 压成 zip，并写上 Mac 能识别的可执行权限。

对方用访达双击解压后即可双击打开，不必再在终端里 chmod。
不要再用资源管理器或微信重新压缩这个包，否则权限会再次丢掉。

用法：
    python Tools/zip_mac_app.py "D:\\Build\\CookingClient.app"
"""

import os
import sys
import zipfile

DIR_MODE = 0o040755
FILE_MODE = 0o100755


def add(zf, arcname, data, mode):
    info = zipfile.ZipInfo(arcname)
    info.create_system = 3
    info.external_attr = mode << 16
    info.compress_type = zipfile.ZIP_DEFLATED
    zf.writestr(info, data)


def zip_app(app_path, zip_path):
    app_path = os.path.abspath(app_path)
    parent = os.path.dirname(app_path)
    app_name = os.path.basename(app_path)
    with zipfile.ZipFile(zip_path, "w") as zf:
        add(zf, app_name + "/", b"", DIR_MODE)
        for root, dirs, files in os.walk(app_path):
            dirs.sort()
            files.sort()
            for name in dirs:
                full = os.path.join(root, name)
                rel = os.path.relpath(full, parent).replace("\\", "/") + "/"
                add(zf, rel, b"", DIR_MODE)
            for name in files:
                full = os.path.join(root, name)
                rel = os.path.relpath(full, parent).replace("\\", "/")
                with open(full, "rb") as handle:
                    add(zf, rel, handle.read(), FILE_MODE)


def main():
    if len(sys.argv) != 2:
        print("用法: python zip_mac_app.py <某某.app 的路径>")
        return 1

    app_path = sys.argv[1]
    if not app_path.lower().endswith(".app") or not os.path.isdir(app_path):
        print("需要一个已经打好的 .app 文件夹。")
        return 1

    zip_path = app_path[:-4] + "_mac.zip"
    zip_app(app_path, zip_path)
    print(zip_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
