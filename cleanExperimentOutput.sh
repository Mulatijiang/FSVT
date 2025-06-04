#!/bin/bash

# 输出提示信息（-e 启用转义字符，\n 表示换行）
echo -e "正在清理之前的实验输出 \n"

# 清理之前实验产生的输出目录
if [ -d "$(pwd)/experimentOutput" ]; then rm -Rf $(pwd)/experimentOutput; fi

mkdir experimentOutput

# 清理输出日志目录中的残留文件
if [ -d "$(pwd)/outputLogs" ]; then rm -Rf $(pwd)/outputLogs/* ; fi
