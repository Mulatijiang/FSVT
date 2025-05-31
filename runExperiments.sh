#!/bin/bash

./cleanExperimentOutput.sh


echo -e "Starting Mutation Testing for Max Spec (Correct)"

mkdir experimentOutput/MaxSpecCorrect

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:maxExample.maxSpec /proofName:maxTest.maxT  /proofLocation:"$(pwd)/specs/max/lemmaMaxTestCorrect.dfy" /serverIpPortList:ipPorts.txt $(pwd)/specs/max/maxSpec.dfy 2>&1 | tee experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt

mkdir experimentOutput/MaxSpecCorrect/outputLogs
cp ./outputLogs/* experimentOutput/MaxSpecCorrect/outputLogs
rm ./outputLogs/*

echo -e "DONE Mutation Testing for Max Spec (Correct) - output found in ./experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt \n"

# --------------
# TrueSat
# --------------

# clone truesat repo and reset to commit hash used in initial experiments. 
echo -e "Cloning truesat Repo into ./specs/truesat"

cd specs/truesat
git clone https://github.com/andricicezar/truesat.git

cd truesat
git reset --hard 62f52fd82709b888fa604f20297f83572c8592ae
patch truesat_src/solver/formula.dfy ../formula.patch 
patch truesat_src/solver/solver.dfy ../solver.patch
cd ../../..
mkdir experimentOutput/truesat/
# --------------

echo -e "Starting ASC Testing for truesat Start"

mkdir experimentOutput/truesat/Start

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:testWrapper.test /proofName:testWrapper.SATSolver.start /mutationRootName:testWrapper.test   /proofLocation:"$(pwd)/specs/truesat/truesat/truesat_src/solver/solver.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/truesat/truesat/truesat_src/solver/solver.dfy &> experimentOutput/truesat/Start/truesat_start_asc_output.txt

echo -e "DONE  ASC Testing for truesat Start - output found in ./experimentOutput/truesat/Start/truesat_start_asc_output.txt \n"

# --------------

echo -e "Starting ASC Testing for truesat Formula Constructor"

mkdir experimentOutput/truesat/fmulaCtor

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:formula.DataStructures.valid /proofName:formula.Formula._ctor /mutationRootName:formula.DataStructures.valid   /proofLocation:"$(pwd)/specs/truesat/truesat/truesat_src/solver/formula.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/truesat/truesat/truesat_src/solver/formula.dfy &> experimentOutput/truesat/fmulaCtor/truesat_fmulaCtor_asc_output.txt

echo -e "DONE ASC Testing for truesat Formula Constructor - output found in ./experimentOutput/truesat/fmulaCtor/truesat_fmulaCtor_asc_output.txt \n"

# --------------
# Eth2.0
# --------------

# clone eth2.0 repo and reset to commit hash used in initial experiments. 
echo -e "Cloning eth2.0 Repo into ./specs/eth2"

cd specs/eth2
git clone https://github.com/Consensys/eth2.0-dafny.git

cd eth2.0-dafny
git reset --hard 4e41de2866c8d017ccf4aaf2154471ffa722b308
patch src/dafny/beacon/forkchoice/ForkChoice.dfy ../forkChoice.patch 
cd ../../..
mkdir experimentOutput/eth2/
# --------------

echo -e "Starting ASC Testing for eth2.0 ForkChoice onBlock"

mkdir experimentOutput/eth2/forkChoice

./Binaries/Dafny /compile:0 /timeLimit:1520 /trace /arith:5 /noCheating:1 /mutationTarget:ForkChoice.Env.storeIsValid /proofName:ForkChoice.Env.on_block /mutationRootName:ForkChoice.Env.storeIsValid   /proofLocation:"$(pwd)/specs/eth2/eth2.0-dafny/src/dafny/beacon/forkchoice/ForkChoice.dfy" /serverIpPortList:ipPorts.txt /checkInputAndOutputSpecified $(pwd)/specs/eth2/eth2.0-dafny/src/dafny/beacon/forkchoice/ForkChoice.dfy &> experimentOutput/eth2/forkChoice/eth2_forkChoice_asc_output.txt

echo -e "DONE ASC Testing for eth2.0 ForkChoice onBlock - output found in ./experimentOutput/eth2/forkChoice/eth2_forkChoice_asc_output.txt \n"


./gatherResults.sh