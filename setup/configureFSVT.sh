#!/bin/bash
echo "正在配置其他脚本... "
echo "使用 [用户名] [节点ID列表] 个性化脚本"

# 配置变量
output_dir=grpc_nodes_output;
output_logs=outputLogs;
mkdir -p ${output_dir}
mkdir -p ${output_logs}
Username="$1"
ListOfNodeIds=""
ListOfIps=""
RootIP=""
declare -i numberOfNodes=0
for i in `seq 2 $#`
do
    ListOfNodeIds+="${!i} "
    numberOfNodes+=1
done
echo 节点ID列表 = $ListOfNodeIds
echo 用户名 = $Username

# 计算节点的IP地址并创建ipPorts.txt
rm -f ipPorts.txt
touch ipPorts.txt
# > ipPorts.txt
for id in $ListOfNodeIds; do
    echo "正在获取节点ID = $id 的IP地址"
    nodeId="$(ssh $Username@clnode${id}.clemson.cloudlab.us ifconfig | grep "130.127" | awk '{print $2}')"
    echo "$nodeId:50051" >> ipPorts.txt
    ListOfIps+="$nodeId"
    ListOfIps+=" "
done


for ip in $ListOfIps; do
    echo "测试到ID = $ip 的ssh连接"
    ssh $Username@${ip} ls;
done

echo "---- 安装和构建依赖 ----"

cd ..
ROOTPWD=$(pwd)
echo "当前目录是 $ROOTPWD"
cd -
for ip in $ListOfIps; do
    echo "-- 为节点 $ip 克隆仓库并安装依赖 -- "
    # 克隆FSVT仓库
    ssh $Username@${ip} "(cd $ROOTPWD; git clone https://github.com/Mulatijiang/FSVT.git)"
    # 克隆grpc服务器仓库
    ssh $Username@${ip} "(cd $ROOTPWD; git clone https://github.com/Mulatijiang/FSVT-dafny-grpc-server.git)";

    # 安装全局依赖
    ssh $Username@${ip} "(cd $ROOTPWD/FSVT; ./setup/node_prep.sh)";
    ssh $Username@${ip} "(cd $ROOTPWD/FSVT; ./setup/install_dotnet_ubuntu_20.04.sh)";

    # 添加用户特定元素
    ssh $Username@${ip} "(sed "s/username/$Username/" $ROOTPWD/FSVT/Source/Dafny/DafnyVerifier.cs > ./tmp.cs && mv ./tmp.cs $ROOTPWD/FSVT/Source/Dafny/DafnyVerifier.cs)";

    # 构建FSVT
    ssh $Username@${ip} "(cd $ROOTPWD/FSVT; make exe)"

    # 编译z3
    ssh $Username@${ip} "(cd $ROOTPWD/FSVT; make z3-ubuntu)" 

    # 构建dafny grpc服务器
    ssh $Username@${ip} "(cd $ROOTPWD/FSVT-dafny-grpc-server; bazel-4.0.0 build --cxxopt="-g" --cxxopt="--std=c++17" //src:server)";

done

echo "---- 依赖安装完成 ----"


# echo "---- 启动Dafny GRPC服务器(s) ----"

for ip in $ListOfIps; do
    # 将dafny二进制文件复制到grpc服务器
    echo "grpc服务器在 $ip 端口:50051 启动" &
    # ssh $Username@${ip} "(cd $ROOTPWD/FSVT-dafny-grpc-server; ls)"
    ssh $Username@${ip} "(cd $ROOTPWD/FSVT-dafny-grpc-server; ./bazel-bin/src/server -v -d $ROOTPWD/FSVT/Binaries/Dafny & disown -a)" &> ${output_dir}/node_${ip}.txt &  
done

for((cnt=0;cnt<$numberOfNodes;cnt=cnt+1))
do
    wait -n
    echo "一个进程(进程ID=$!)结束，退出状态: $?"
done

echo "FSVT设置完成!"
