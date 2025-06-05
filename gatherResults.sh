
#!/usr/bin/env bash

echo "正在收集结果..."

rm -f $(pwd)/experimentOutput/finalResults.txt
touch $(pwd)/experimentOutput/finalResults.txt
outputfile=$(pwd)/experimentOutput/finalResults.txt

# grep "Total Alive Mutations" ./experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt  | awk '{print $5}'


# ---- ASC -----

echo -e "表1 - ASC \n" >> $outputfile

echo -e "--- ASC实验输出 --- \n" >> $outputfile
echo "-----------------------------------------------------------------------------------------------------------------------------------" >> $outputfile
echo "|    规范名称     | 方法名称     | 标志                                                                                             | " >> $outputfile
echo "-----------------------------------------------------------------------------------------------------------------------------------" >> $outputfile
echo "|    TrueSat     | Formula CTor |  $(grep "警告" $(pwd)/experimentOutput/truesat/fmulaCtor/truesat_fmulaCtor_asc_output.txt) |" >> $outputfile
echo "-----------------------------------------------------------------------------------------------------------------------------------" >> $outputfile
echo "|    TrueSat     |     Start    |  $(grep "警告" $(pwd)/experimentOutput/truesat/Start/truesat_start_asc_output.txt) |" >> $outputfile
echo "-----------------------------------------------------------------------------------------------------------------------------------" >> $outputfile
echo "|    Eth2.0      |   on_block   |  $(grep "警告" $(pwd)/experimentOutput/eth2/forkChoice/eth2_forkChoice_asc_output.txt) |" >> $outputfile
echo "-----------------------------------------------------------------------------------------------------------------------------------" >> $outputfile
echo "|   AWS ESDK     |   Encrypt    |  $(grep "警告" $(pwd)/experimentOutput/awsESDK/encrypt/aws_esdk_encrypt_asc_output.txt) |" >> $outputfile
echo "-----------------------------------------------------------------------------------------------------------------------------------" >> $outputfile
echo "|   AWS ESDK     |   Decrypt    |  $(grep "警告" $(pwd)/experimentOutput/awsESDK/decrypt/aws_esdk_decrypt_asc_output.txt) |" >> $outputfile
echo -e "-----------------------------------------------------------------------------------------------------------------------------------\n\n" >> $outputfile

# --- 变异测试 ----

echo -e "表2 - 变异测试 \n"  >> $outputfile
echo -e "--- 变异测试实验输出 ---\n" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|                                 内部规范                                                        |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|     规范名称           | 谓词名称       | 变异总数       | 存活变异总数                | 耗时     | " >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      Max(C)            |     maxSpec    | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt | awk '{print $10 "\t\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/MaxSpecCorrect/maxSpecCorrect_output.txt |awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      Max(I)            |     maxSpec    | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/MaxSpecIncorrect/maxSpecIncorrect_output.txt | awk '{print $10 "\t\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/MaxSpecIncorrect/maxSpecIncorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/MaxSpecIncorrect/maxSpecIncorrect_output.txt | awk '{print $5 " " $6 "\t"}')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      Sort(C)           |     sortSpec   | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/SortSpecCorrect/sortSpecCorrect_output.txt | awk '{print $10 "\t\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/SortSpecCorrect/sortSpecCorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/SortSpecCorrect/sortSpecCorrect_output.txt | awk '{print $5 " " $6 "\t"}')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      Sort(I)           |     sortSpec   | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/SortSpecIncorrect/sortSpecIncorrect_output.txt | awk '{print $10 "\t\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/SortSpecIncorrect/sortSpecIncorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/SortSpecIncorrect/sortSpecIncorrect_output.txt | awk '{print $5 " " $6 "\t"}')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|     Binary Search(C)   |    searchSpec  | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/BSearchSpecCorrect/bsearchSpecCorrect_output.txt | awk '{print $10 "\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/BSearchSpecCorrect/bsearchSpecCorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/BSearchSpecCorrect/bsearchSpecCorrect_output.txt |awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|     Binary Search(I)   |    searchSpec  | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/BSearchSpecIncorrect/bsearchSpecIncorrect_output.txt | awk '{print $10 "\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/BSearchSpecIncorrect/bsearchSpecIncorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/BSearchSpecIncorrect/bsearchSpecIncorrect_output.txt |awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|     KV SM(C)           |    Query Op    | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/KVSMSpecCorrect/kvmsmSpecCorrect_output.txt | awk '{print $10 "\t\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/KVSMSpecCorrect/kvmsmSpecCorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/KVSMSpecCorrect/kvmsmSpecCorrect_output.txt |awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|     KV SM(I)           |    Query Op    | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/KVSMSpecIncorrect/kvmsmSpecIncorrect_output.txt | awk '{print $10 "\t\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/KVSMSpecIncorrect/kvmsmSpecIncorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/KVSMSpecIncorrect/kvmsmSpecIncorrect_output.txt |awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|     Token-wre(C)       |    GInv        | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/Token-wreSpecCorrect/token-wreCorrect_output.txt | awk '{print $10 "\t\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/Token-wreSpecCorrect/token-wreCorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/Token-wreSpecCorrect/token-wreCorrect_output.txt | awk '{print $5 " " $6 "\t"}')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|     Token-wre(I)       |    GInv        | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/Token-wreSpecIncorrect/token-wreIncorrect_output.txt | awk '{print $10 "\t\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/Token-wreSpecIncorrect/token-wreIncorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/Token-wreSpecIncorrect/token-wreIncorrect_output.txt | awk '{print $5 " " $6 "\t"}')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|   SimpleAuction-wre(C) |    GInv        | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/SimpleAuction-wreSpecCorrect/SimpleAuction-wreCorrect_output.txt | awk '{print $10 "\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/SimpleAuction-wreSpecCorrect/SimpleAuction-wreCorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/SimpleAuction-wreSpecCorrect/SimpleAuction-wreCorrect_output.txt | awk '{print $5 " " $6}')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|   SimpleAuction-wre(I) |    GInv        | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/SimpleAuction-wreSpecIncorrect/SimpleAuction-wreIncorrect_output.txt | awk '{print $10 "\t  "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/SimpleAuction-wreSpecIncorrect/SimpleAuction-wreIncorrect_output.txt | awk '{print $5 "\t\t\t\t"}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/SimpleAuction-wreSpecIncorrect/SimpleAuction-wreIncorrect_output.txt | awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile

echo -e "\n"
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|                                  开源规范                                                       |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|     规范名称           | 谓词名称       | 变异总数      | 存活变异总数                   | 耗时     | " >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      Div               |  Div           | \
 $(tail -1 $(pwd)/experimentOutput/Div/Div_output.txt | awk '{print $6"\t\t "}') | \
 $(tail -1 $(pwd)/experimentOutput/Div/Div_output.txt | awk '{print $3"\t\t\t\t  "}') | \
 $(grep "Elapsed Time" $(pwd)/experimentOutput/Div/Div_output.txt |awk '{print $4 " " $5 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      NthHarmonic       |  NthHarmonic   | \
 $(tail -1 $(pwd)/experimentOutput/NthHarmonic/NthHarmonic_output.txt | awk '{print $6"\t\t "}') | \
 $(tail -1 $(pwd)/experimentOutput/NthHarmonic/NthHarmonic_output.txt | awk '{print $3"\t\t\t\t  "}') | \
 $(grep "Elapsed Time" $(pwd)/experimentOutput/NthHarmonic/NthHarmonic_output.txt |awk '{print $4 " " $5 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      QBFT              |  NetworkInit   | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/qbft/NetworkInit/qbft-networkInit_output.txt | awk '{print $10 "\t\t "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/qbft/NetworkInit/qbft-networkInit_output.txt | awk '{print $5 "\t\t\t\t  "}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/qbft/NetworkInit/qbft-networkInit_output.txt | awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      QBFT              |  AdversaryNext | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/qbft/AdversaryNext/qbft-adversaryNext_output.txt | awk '{print $10 "\t "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/qbft/AdversaryNext/qbft-adversaryNext_output.txt | awk '{print $5 "\t\t\t\t  "}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/qbft/AdversaryNext/qbft-adversaryNext_output.txt | awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      QBFT              |  AdversaryInit | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/qbft/AdversaryInit/qbft-adversaryInit_output.txt | awk '{print $10 "\t\t "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/qbft/AdversaryInit/qbft-adversaryInit_output.txt | awk '{print $5 "\t\t\t\t  "}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/qbft/AdversaryInit/qbft-adversaryInit_output.txt | awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      DVT               |  AdversaryNext | \
 $(grep "Begin Is At Least As Weak Pass" $(pwd)/experimentOutput/dvt/AdversaryNext/dvt-adversaryNext_output.txt | awk '{print $10 "\t "}') | \
 $(grep "Total Alive Mutations" $(pwd)/experimentOutput/dvt/AdversaryNext/dvt-adversaryNext_output.txt | awk '{print $5 "\t\t\t\t  "}') | \
 $(grep "TOTAL Elapsed Time" $(pwd)/experimentOutput/dvt/AdversaryNext/dvt-adversaryNext_output.txt | awk '{print $5 " " $6 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      daisy-nfsd        |  GETATTR       | \
 $(tail -1 $(pwd)/experimentOutput/daisy-nfsd/GETATTR/daisy-nfsd-GETATTR_output.txt | awk '{print $6"\t\t "}') | \
 $(tail -1 $(pwd)/experimentOutput/daisy-nfsd/GETATTR/daisy-nfsd-GETATTR_output.txt | awk '{print $3"\t\t\t\t  "}') | \
 $(grep "Elapsed Time" $(pwd)/experimentOutput/daisy-nfsd/GETATTR/daisy-nfsd-GETATTR_output.txt |awk '{print $4 " " $5 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile
echo "|      daisy-nfsd        |  WRITE         | \
 $(tail -1 $(pwd)/experimentOutput/daisy-nfsd/WRTIE/daisy-nfsd-WRTIE_output.txt | awk '{print $6"\t "}') | \
 $(tail -1 $(pwd)/experimentOutput/daisy-nfsd/WRTIE/daisy-nfsd-WRTIE_output.txt | awk '{print $3"\t\t\t\t  "}') | \
 $(grep "Elapsed Time" $(pwd)/experimentOutput/daisy-nfsd/WRTIE/daisy-nfsd-WRTIE_output.txt |awk '{print $4 " " $5 }')  |" >> $outputfile
echo "--------------------------------------------------------------------------------------------------" >> $outputfile

echo -e "完成"


