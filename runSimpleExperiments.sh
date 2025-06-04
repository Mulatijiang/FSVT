#!/bin/bash

# 执行清理脚本，清除之前的实验输出
./cleanExperimentOutput.sh

# 输出：开始对Max规格（正确版本）进行变异测试
echo -e "开始对Max规格（正确版本）进行变异测试"

# 启动grpc服务
../FSVT-dafny-grpc-server/bazel-bin/src/server -v -d ./Binaries/Dafny &

# 创建实验输出目录
mkdir experimentOutput/MaxSpecCorrect

# 执行Dafny变异测试工具
./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:maxExample.maxSpec /proofName:maxTest.maxT  /proofLocation:"$(pwd)/specs/max/lemmaMaxTestCorrect.dfy" /serverIpPortList:ipPorts.txt $(pwd)/specs/max/maxSpec.dfy 2>&1 | tee experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt

# 创建日志输出目录，复制当前日志并清空
mkdir experimentOutput/MaxSpecCorrect/outputLogs
cp ./outputLogs/* experimentOutput/MaxSpecCorrect/outputLogs
rm ./outputLogs/*

# 输出：Max规格（正确版本）变异测试完成
echo -e "Max规格（正确版本）变异测试完成 - 输出位于 ./experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt \n"

####

# 输出：开始对Sort进行自动健全性检查器(ASC)测试
echo -e "开始对Sort进行自动健全性检查器(ASC)测试"

# 创建实验输出目录
mkdir experimentOutput/sortASC

# 执行Dafny的ASC测试
./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:sort.SortSpec /proofName:sort.merge_sort /mutationRootName:sort.SortSpec /proofLocation:"$(pwd)/specs/sort/sortMethod.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/sort/sortMethod.dfy 2>&1 | tee experimentOutput/sortASC/sortASC_output.txt

# 输出：Sort的ASC测试完成
echo -e "Sort的ASC测试完成 - 输出位于 ./experimentOutput/sortASC/sortASC_output.txt \n"