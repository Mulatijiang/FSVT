#!/bin/bash
echo -e "正在运行FSVT ASC实验 \n"

# --------------
# TrueSat
# --------------

# 克隆truesat仓库并重置到初始实验使用的提交哈希
echo -e "正在克隆truesat仓库到./specs/truesat"

cd specs/truesat
git clone https://github.com/andricicezar/truesat.git

cd truesat
git reset --hard 62f52fd82709b888fa604f20297f83572c8592ae
patch truesat_src/solver/formula.dfy ../formula.patch 
patch truesat_src/solver/solver.dfy ../solver.patch
cd ../../..
mkdir experimentOutput/truesat/
# --------------

echo -e "开始对truesat Start进行ASC测试"

mkdir experimentOutput/truesat/Start

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:testWrapper.test /proofName:testWrapper.SATSolver.start /mutationRootName:testWrapper.test   /proofLocation:"$(pwd)/specs/truesat/truesat/truesat_src/solver/solver.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/truesat/truesat/truesat_src/solver/solver.dfy &> experimentOutput/truesat/Start/truesat_start_asc_output.txt

echo -e "truesat Start的ASC测试完成 - 输出位于./experimentOutput/truesat/Start/truesat_start_asc_output.txt \n"

# --------------

echo -e "开始对truesat公式构造函数进行ASC测试"

mkdir experimentOutput/truesat/fmulaCtor

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:formula.DataStructures.valid /proofName:formula.Formula._ctor /mutationRootName:formula.DataStructures.valid   /proofLocation:"$(pwd)/specs/truesat/truesat/truesat_src/solver/formula.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/truesat/truesat/truesat_src/solver/formula.dfy &> experimentOutput/truesat/fmulaCtor/truesat_fmulaCtor_asc_output.txt

echo -e "truesat公式构造函数的ASC测试完成 - 输出位于./experimentOutput/truesat/fmulaCtor/truesat_fmulaCtor_asc_output.txt \n"

# --------------
# Eth2.0
# --------------

# 克隆eth2.0仓库并重置到初始实验使用的提交哈希
echo -e "正在克隆eth2.0仓库到./specs/eth2"

cd specs/eth2
git clone https://github.com/Consensys/eth2.0-dafny.git

cd eth2.0-dafny
git reset --hard 4e41de2866c8d017ccf4aaf2154471ffa722b308
patch src/dafny/beacon/forkchoice/ForkChoice.dfy ../forkChoice.patch 
cd ../../..
mkdir experimentOutput/eth2/
# --------------

echo -e "开始对eth2.0 ForkChoice onBlock进行ASC测试"

mkdir experimentOutput/eth2/forkChoice

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:ForkChoice.Env.storeIsValid /proofName:ForkChoice.Env.on_block /mutationRootName:ForkChoice.Env.storeIsValid   /proofLocation:"$(pwd)/specs/eth2/eth2.0-dafny/src/dafny/beacon/forkchoice/ForkChoice.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/eth2/eth2.0-dafny/src/dafny/beacon/forkchoice/ForkChoice.dfy &> experimentOutput/eth2/forkChoice/eth2_forkChoice_asc_output.txt

echo -e "eth2.0 ForkChoice onBlock的ASC测试完成 - 输出位于./experimentOutput/eth2/forkChoice/eth2_forkChoice_asc_output.txt \n"

# --------------
# AWS ESDK
# --------------

# 克隆AWS Dafny ESDK仓库并重置到初始实验使用的提交哈希
echo -e "正在克隆AWS Dafny ESDK仓库到./specs/awsESDK"

cd specs/awsESDK
git clone --recurse-submodules https://github.com/aws/aws-encryption-sdk-dafny.git

cd aws-encryption-sdk-dafny
git reset --hard eaa30b377be9c0c17aeae9fce11387b0fbccafba
patch mpl/AwsCryptographicMaterialProviders/dafny/AwsCryptographyKeyStore/src/Structure.dfy ../structure.patch 
patch AwsEncryptionSDK/dafny/AwsEncryptionSdk/src/Serialize/SerializeFunctions.dfy ../serializeFuncs.patch 
patch AwsEncryptionSDK/dafny/AwsEncryptionSdk/src/Serialize/EncryptionContext.dfy ../encryptionContext.patch 
patch AwsEncryptionSDK/dafny/AwsEncryptionSdk/src/Serialize/V1HeaderBody.dfy ../v1Header.patch 
patch AwsEncryptionSDK/dafny/AwsEncryptionSdk/src/Serialize/EncryptedDataKeys.dfy ../encryptedDataKeys.patch 

# 重新初始化所有子模块
git submodule update --init --recursive --force

cd ../../..
mkdir experimentOutput/awsESDK/

# --------------

echo -e "开始对AWS ESDK加密功能进行ASC测试"

mkdir experimentOutput/awsESDK/encrypt

./Binaries/Dafny /compile:0 /noCheating:1 /trace /timeLimit:10000 /mutationTarget:AwsEncryptionSdkOperations.EncryptEnsuresPublicly /proofName:AwsEncryptionSdkOperations.Encrypt      /mutationRootName:AwsEncryptionSdkOperations.EncryptEnsuresPublicly  /proofLocation:"$(pwd)/specs/awsESDK/aws-encryption-sdk-dafny/AwsEncryptionSDK/dafny/AwsEncryptionSdk/src/AwsEncryptionSdkOperations.dfy"  /serverIpPortList:ipPorts.txt  /checkInputAndOutputSpecified  $(pwd)/specs/awsESDK/aws-encryption-sdk-dafny/AwsEncryptionSDK/dafny/AwsEncryptionSdk/src/AwsEncryptionSdkOperations.dfy &> experimentOutput/awsESDK/encrypt/aws_esdk_encrypt_asc_output.txt

echo -e "AWS ESDK加密功能的ASC测试完成 - 输出位于./experimentOutput/awsESDK/encrypt/aws_esdk_encrypt_asc_output.txt \n"

# --------------

echo -e "开始对AWS ESDK解密功能进行ASC测试"

mkdir experimentOutput/awsESDK/decrypt

./Binaries/Dafny /compile:0 /noCheating:1 /trace /timeLimit:10000 /mutationTarget:AwsEncryptionSdkOperations.EncryptEnsuresPublicly /proofName:AwsEncryptionSdkOperations.Decrypt      /mutationRootName:AwsEncryptionSdkOperations.EncryptEnsuresPublicly  /proofLocation:"$(pwd)/specs/awsESDK/aws-encryption-sdk-dafny/AwsEncryptionSDK/dafny/AwsEncryptionSdk/src/AwsEncryptionSdkOperations.dfy"  /serverIpPortList:ipPorts.txt  /checkInputAndOutputSpecified  $(pwd)/specs/awsESDK/aws-encryption-sdk-dafny/AwsEncryptionSDK/dafny/AwsEncryptionSdk/src/AwsEncryptionSdkOperations.dfy &> experimentOutput/awsESDK/decrypt/aws_esdk_decrypt_asc_output.txt

echo -e "AWS ESDK解密功能的ASC测试完成 - 输出位于./experimentOutput/awsESDK/decrypt/aws_esdk_decrypt_asc_output.txt \n"