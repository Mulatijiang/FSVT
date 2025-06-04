#!/bin/bash

# 执行清理实验输出的脚本
./cleanExperimentOutput.sh


echo -e "正在运行FSVT变异测试实验 \n"

# --------------
echo -e "开始对Max规范（正确版本）进行变异测试"

# 创建实验输出目录
mkdir experimentOutput/MaxSpecCorrect

# 运行Dafny进行变异测试（正确规范）
./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:maxExample.maxSpec /proofName:maxTest.maxT   /proofLocation:"$(pwd)/specs/max/lemmaMaxTestCorrect.dfy" /serverIpPortList:ipPorts.txt $(pwd)/specs/max/maxSpec.dfy 2>&1 | tee experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt

# 处理输出日志：复制并清空
mkdir experimentOutput/MaxSpecCorrect/outputLogs
cp ./outputLogs/* experimentOutput/MaxSpecCorrect/outputLogs
rm ./outputLogs/*

echo -e "Max规范（正确版本）变异测试完成 - 输出位于./experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt \n"

# --------------
echo -e "开始对Max规范（错误版本）进行变异测试"

mkdir experimentOutput/MaxSpecIncorrect

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:maxExample.maxSpec /proofName:maxTest.maxT   /proofLocation:"$(pwd)/specs/max/lemmaMaxTest.dfy" /serverIpPortList:ipPorts.txt $(pwd)/specs/max/maxSpec.dfy 2>&1 | tee experimentOutput/MaxSpecIncorrect/maxSpecIncorrect_output.txt

mkdir experimentOutput/MaxSpecIncorrect/outputLogs
cp ./outputLogs/* experimentOutput/MaxSpecIncorrect/outputLogs
rm ./outputLogs/*

echo -e "Max规范（错误版本）变异测试完成 - 输出位于./experimentOutput/MaxSpecIncorrect/maxSpecIncorrect_output.txt \n"

# --------------
echo -e "开始对Sort规范（正确版本）进行变异测试"

mkdir experimentOutput/SortSpecCorrect

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:sortSpec.IsSorted /proofName:sort.merge_sort   /proofLocation:"$(pwd)/specs/sort/sortCorrect.dfy" /serverIpPortList:ipPorts.txt  $(pwd)/specs/sort/sortSpec.dfy 2>&1 | tee experimentOutput/SortSpecCorrect/sortSpecCorrect_output.txt

mkdir experimentOutput/SortSpecCorrect/outputLogs
cp ./outputLogs/* experimentOutput/SortSpecCorrect/outputLogs
rm ./outputLogs/*

echo -e "Sort规范（正确版本）变异测试完成 - 输出位于./experimentOutput/SortSpecCorrect/sortSpecCorrect_output.txt \n"

# --------------
echo -e "开始对Sort规范（错误版本）进行变异测试"

mkdir experimentOutput/SortSpecIncorrect

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:sortSpec.IsSorted /proofName:sort.sortWrong   /proofLocation:"$(pwd)/specs/sort/sortIncorrect.dfy" /serverIpPortList:ipPorts.txt  $(pwd)/specs/sort/sortSpec.dfy 2>&1 | tee experimentOutput/SortSpecIncorrect/sortSpecIncorrect_output.txt

mkdir experimentOutput/SortSpecIncorrect/outputLogs
cp ./outputLogs/* experimentOutput/SortSpecIncorrect/outputLogs
rm ./outputLogs/*

echo -e "Sort规范（错误版本）变异测试完成 - 输出位于./experimentOutput/SortSpecIncorrect/sortSpecIncorrect_output.txt \n"

# --------------
# 开源项目部分
# --------------
echo -e "开始对Div项目进行变异测试"

mkdir experimentOutput/Div

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /noCheating:1 /mutationTarget:div.wrapper /proofName:div.divRem /mutationRootName:div.wrapper    /proofLocation:"$(pwd)/specs/div/div.dfy" /serverIpPortList:ipPorts.txt /inPlaceMutation /functionMethodFlag $(pwd)/specs/div/div.dfy 2>&1 | tee experimentOutput/Div/Div_output.txt

mkdir experimentOutput/Div/outputLogs
cp ./outputLogs/* experimentOutput/Div/outputLogs
rm ./outputLogs/*

echo -e "Div项目变异测试完成 - 输出位于./experimentOutput/Div/Div_output.txt \n"

# --------------
echo -e "开始对NthHarmonic项目进行变异测试"

mkdir experimentOutput/NthHarmonic

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /noCheating:1 /mutationTarget:nHarm.wrapper /proofName:nHarm.NthHarmonic /mutationRootName:nHarm.wrapper    /proofLocation:"$(pwd)/specs/nthHarmonic/nHarmonic.dfy" /serverIpPortList:ipPorts.txt /inPlaceMutation /functionMethodFlag $(pwd)/specs/nthHarmonic/nHarmonic.dfy 2>&1 | tee experimentOutput/NthHarmonic/NthHarmonic_output.txt

mkdir experimentOutput/NthHarmonic/outputLogs
cp ./outputLogs/* experimentOutput/NthHarmonic/outputLogs
rm ./outputLogs/*

echo -e "NthHarmonic项目变异测试完成 - 输出位于./experimentOutput/NthHarmonic/NthHarmonic_output.txt \n"


echo -e "正在运行FSVT ASC实验 \n"

# --------------
# TrueSat项目
# --------------
echo -e "克隆truesat仓库到./specs/truesat"

cd specs/truesat
git clone https://github.com/andricicezar/truesat.git

cd truesat
git reset --hard 62f52fd82709b888fa604f20297f83572c8592ae  # 重置到指定提交哈希
patch truesat_src/solver/formula.dfy ../formula.patch  # 应用补丁文件
patch truesat_src/solver/solver.dfy ../solver.patch
cd ../../..
mkdir experimentOutput/truesat/  # 创建实验输出目录

# --------------
echo -e "开始对truesat Start功能进行ASC测试"

mkdir experimentOutput/truesat/Start  # 创建子目录存储输出

# 运行Dafny进行ASC测试（Start功能）
./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:testWrapper.test /proofName:testWrapper.SATSolver.start /mutationRootName:testWrapper.test   /proofLocation:"$(pwd)/specs/truesat/truesat/truesat_src/solver/solver.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/truesat/truesat/truesat_src/solver/solver.dfy 2>&1 | tee experimentOutput/truesat/Start/truesat_start_asc_output.txt

echo -e "truesat Start功能ASC测试完成 - 输出位于./experimentOutput/truesat/Start/truesat_start_asc_output.txt \n"

# --------------
echo -e "开始对truesat公式构造函数进行ASC测试"

mkdir experimentOutput/truesat/fmulaCtor

# 运行Dafny进行ASC测试（公式构造函数）
./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:formula.DataStructures.valid /proofName:formula.Formula._ctor /mutationRootName:formula.DataStructures.valid   /proofLocation:"$(pwd)/specs/truesat/truesat/truesat_src/solver/formula.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/truesat/truesat/truesat_src/solver/formula.dfy 2>&1 | tee experimentOutput/truesat/fmulaCtor/truesat_fmulaCtor_asc_output.txt

echo -e "truesat公式构造函数ASC测试完成 - 输出位于./experimentOutput/truesat/fmulaCtor/truesat_fmulaCtor_asc_output.txt \n"

# --------------
# Eth2.0项目
# --------------
echo -e "克隆eth2.0仓库到./specs/eth2"

cd specs/eth2
git clone https://github.com/Consensys/eth2.0-dafny.git

cd eth2.0-dafny
git reset --hard 4e41de2866c8d017ccf4aaf2154471ffa722b308  # 重置版本
patch src/dafny/beacon/forkchoice/ForkChoice.dfy ../forkChoice.patch  # 应用补丁
cd ../../..
mkdir experimentOutput/eth2/  # 创建输出目录

# --------------
echo -e "开始对eth2.0 ForkChoice onBlock功能进行ASC测试"

mkdir experimentOutput/eth2/forkChoice

# 运行Dafny进行ASC测试（ForkChoice onBlock）
./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:ForkChoice.Env.storeIsValid /proofName:ForkChoice.Env.on_block /mutationRootName:ForkChoice.Env.storeIsValid   /proofLocation:"$(pwd)/specs/eth2/eth2.0-dafny/src/dafny/beacon/forkchoice/ForkChoice.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/eth2/eth2.0-dafny/src/dafny/beacon/forkchoice/ForkChoice.dfy 2>&1 | tee experimentOutput/eth2/forkChoice/eth2_forkChoice_asc_output.txt

echo -e "eth2.0 ForkChoice onBlock功能ASC测试完成 - 输出位于./experimentOutput/eth2/forkChoice/eth2_forkChoice_asc_output.txt \n"