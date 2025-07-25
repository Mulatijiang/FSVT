//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
// Copyright by the contributors to the Dafny Project
// SPDX-License-Identifier: MIT
//
//-----------------------------------------------------------------------------
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using Microsoft.Boogie;
using System.Threading.Tasks;

namespace Microsoft.Dafny {
  public enum Result {
    Unknown = 0,
    CorrectProof = 1,
    CorrectProofByTimeout = 2,
    IncorrectProof = 3,
    FalsePredicate = 4,
    InvalidExpr = 5,
    NoMatchingTrigger = 6,
    vacousProofPass = 7
  }
  public class MutationEvaluator {
    private string UnderscoreStr = "";
    private static Random random = new Random();
    private Cloner cloner = new Cloner();
    public static string RandomString(int length) {
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
      return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    public ExpressionFinder expressionFinder = null;
        // private Cloner cloner = new Cloner();
    private List<BitArray> bitArrayList = new List<BitArray>();
    private List<float> executionTimes = new List<float>();
    private Dictionary<String,float> verboseExecTimes = new Dictionary<string, float>();
    private Dictionary<int,float> verboseExecTimesTotal = new Dictionary<int, float>();
    private List<float> startTimes = new List<float>();
    private ExpressionFinder.ExpressionDepth constraintExpr = null;

    private Dictionary<int,List<int>> classheirarchy = new Dictionary<int, List<int>>();
    private List<int> passingIndecies = new List<int>();

    private Dictionary<int,int> requestNumCnts = new Dictionary<int, int>();
    public static bool IsGoodResult(Result result) {
      return (result == Result.CorrectProof ||
              result == Result.CorrectProofByTimeout ||
              result == Result.IncorrectProof ||
              result == Result.Unknown);
    }
    private Dictionary<int, Result> combinationResults = new Dictionary<int, Result>();
    private Dictionary<int, int> negateOfExpressionIndex = new Dictionary<int, int>();
    // private DafnyExecutor dafnyMainExecutor = new DafnyExecutor();
    private DafnyExecutor dafnyImpliesExecutor = new DafnyExecutor();
    public DafnyVerifierClient dafnyVerifier;

    private TasksList tasksList = null;
    private Dictionary<string, VerificationTaskArgs> tasksListDictionary = new Dictionary<string, VerificationTaskArgs>();
    private IncludeParser includeParser = null;
    private List<string> affectedFiles = new List<string>();

    public static int validityLemmaNameStartCol = 0;
    public static string lemmaForExprValidityString = "";
    private static int lemmaForExprValidityLineCount = 0;


    private void UpdateCombinationResult(int index) {
      var requestList = dafnyVerifier.requestsList[index];
      for (int i = 0; i < requestList.Count; i++) {
        var request = requestList[i];
        var position = dafnyVerifier.requestToPostConditionPosition[request];
        var lemmaStartPosition = dafnyVerifier.requestToLemmaStartPosition[request];
        var output = dafnyVerifier.dafnyOutput[request];
        var response = output.Response;
        var filePath = output.FileName;
        // var startTime = output.StartTime;
        var execTime = output.ExecutionTimeInMs;
        executionTimes.Add(execTime);
        // startTimes.Add(startTime);
        Result res;
        if (position != -1) {
          var expectedOutput =
            $"{filePath}({position},0): Error: A postcondition might not hold on this return path.";
          var expectedInconclusiveOutputStart =
            $"{filePath}({lemmaStartPosition},{validityLemmaNameStartCol}): Verification inconclusive";
          // Console.WriteLine($"{index} => {output}");
          // Console.WriteLine($"{output.EndsWith("0 errors\n")} {output.EndsWith($"resolution/type errors detected in {fileName}.dfy\n")}");
          // Console.WriteLine($"----------------------------------------------------------------");
          res = DafnyVerifierClient.IsCorrectOutputForValidityCheck(response, expectedOutput, expectedInconclusiveOutputStart);
        } else {
          res = DafnyVerifierClient.IsCorrectOutputForNoErrors(response);
        }
        if (res != Result.IncorrectProof) {
          // correctExpressions.Add(dafnyMainExecutor.processToExpr[p]);
          // Console.WriteLine(output);
          combinationResults[index] = res;
          // Console.WriteLine(p.StartInfo.Arguments);
          // Console.WriteLine(Printer.ExprToString(dafnyVerifier.requestToExpr[request]));
          Console.WriteLine(Printer.ExprToString(dafnyVerifier.requestToExpr[request]));
        } else if (response.Contains(" 0 errors\n")) {
          combinationResults[index] = Result.FalsePredicate;
          Console.WriteLine("Mutation that Passes = " + index);
          break;
        } else if (response.EndsWith($"resolution/type errors detected in {Path.GetFileName(filePath)}\n")) {
          combinationResults[index] = Result.InvalidExpr;
          break;
        } else {
          combinationResults[index] = Result.IncorrectProof;
          break;
        }
      }
      expressionFinder.combinationResults[index] = combinationResults[index];
    }

public async Task<bool>writeOutputs(int index)
{
    var requestList = dafnyVerifier.requestsList[index];
    for(int i = 1; i<requestList.Count;i++){
      var request = requestList[i];
      var position = dafnyVerifier.requestToPostConditionPosition[request];
      var lemmaStartPosition = dafnyVerifier.requestToLemmaStartPosition[request];
      var output = dafnyVerifier.dafnyOutput[request];
      var response = output.Response;
      if (DafnyOptions.O.HoleEvaluatorCreateAuxFiles){
              if(i == 1){
                await File.AppendAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}dafnyOutput_{index}.txt", "--VAC TEST--\n"+response + "\n------\n");
              }else{
                await File.AppendAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}dafnyOutput_{index}.txt", response);
              }
      }
    }
    return true;
}

public async Task<bool>writeFailedOutputs(int index)
{
    var requestList = dafnyVerifier.requestsList[index];
    for(int i = 0; i<requestList.Count;i++){
      var request = requestList[i];
      var position = dafnyVerifier.requestToPostConditionPosition[request];
      var lemmaStartPosition = dafnyVerifier.requestToLemmaStartPosition[request];
      var output = dafnyVerifier.dafnyOutput[request];
      var response = output.Response;
      if (DafnyOptions.O.HoleEvaluatorCreateAuxFiles){
              if(i == 1){
                File.AppendAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}dafnyOutput_{index}.txt", "--VAC TEST--\n"+response + "\n------\n");
              }else{
                File.AppendAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}dafnyOutput_{index}.txt", response);
              }
      }
    }
    return true;
}

    private void classifyMutationsDag(int index,int passingIndex,bool vac)
    {
      var requestList = dafnyVerifier.requestsList[index];
      var passingIndexRef = 0;
      for(int i = requestNumCnts[index]; i<requestList.Count;i++){
          var request = requestList[i];
          var position = dafnyVerifier.requestToPostConditionPosition[request];
          var lemmaStartPosition = dafnyVerifier.requestToLemmaStartPosition[request];
          var output = dafnyVerifier.dafnyOutput[request];
          var response = output.Response;
          var filePath = output.FileName;
          var execTime = output.ExecutionTimeInMs;
          // var passingIndexRef = 0;
          if(passingIndecies[passingIndexRef] == index){
            passingIndexRef++;
          }
          // else{
          //   passingIndexRef = i-3;
          // }
          var expectedOutput =
            $"{filePath}({position},0): Error: A postcondition might not hold on this return path.";
          var expectedInconclusiveOutputStart =
            $"{filePath}({lemmaStartPosition},{validityLemmaNameStartCol}): Verification inconclusive";
          var res = DafnyVerifierClient.IsCorrectOutput(response, expectedOutput, expectedInconclusiveOutputStart);
        if (response.Contains(" 0 errors\n")){
          // Console.WriteLine("Passes " + index + " :: " + passingIndecies[passingIndexRef]);
          if(!classheirarchy.ContainsKey(index)){
              classheirarchy.Add(index,new List<int>());
              classheirarchy[index].Add(passingIndecies[passingIndexRef]);
          }else{
            classheirarchy[index].Add(passingIndecies[passingIndexRef]);
          }
        }else{
          // Console.WriteLine("FAILS " + index + " :: " + passingIndecies[passingIndexRef]);
        }
        passingIndexRef++;
      }
      
    }
    private void UpdateCombinationResultVacAwareList(int index,bool vac) {
      var requestList = dafnyVerifier.requestsList[index];
      requestNumCnts.Add(index,requestList.Count);
      for(int i = 2; i<requestList.Count;i++){
          var request = requestList[i];
          var position = dafnyVerifier.requestToPostConditionPosition[request];
          var lemmaStartPosition = dafnyVerifier.requestToLemmaStartPosition[request];
          var output = dafnyVerifier.dafnyOutput[request];
          var response = output.Response;
          var filePath = output.FileName;
          var execTime = output.ExecutionTimeInMs;
          executionTimes.Add(execTime);
          // verboseExecTimes.Add("(" + index + ") "+ filePath,execTime);
          if(verboseExecTimes.ContainsKey("(" + index + ") "+ filePath))
          {
            verboseExecTimes["(" + index + ") "+ filePath] += execTime;
          }else{
            verboseExecTimes.Add("(" + index + ") "+ filePath,execTime);    
          }
          if(verboseExecTimesTotal.ContainsKey(index)){
            verboseExecTimesTotal[index] = verboseExecTimesTotal[index] + execTime;
          }else{
            verboseExecTimesTotal.Add(index,execTime);
          }
          var expectedOutput =
            $"{filePath}({position},0): Error: A postcondition might not hold on this return path.";
          var expectedInconclusiveOutputStart =
            $"{filePath}({lemmaStartPosition},{validityLemmaNameStartCol}): Verification inconclusive";
                   var res = DafnyVerifierClient.IsCorrectOutput(response, expectedOutput, expectedInconclusiveOutputStart);

        if (res != Result.IncorrectProof) {
          // correctExpressions.Add(dafnyMainExecutor.processToExpr[p]);
          // Console.WriteLine(output);
          combinationResults[index] = res;
          // Console.WriteLine(p.StartInfo.Arguments);
          // Console.WriteLine(Printer.ExprToString(dafnyVerifier.requestToExpr[request]));
          Console.WriteLine(Printer.ExprToString(dafnyVerifier.requestToExpr[request]));
        } else if (response.Contains(" 0 errors\n")) {
        
        if(vac){
          // Console.WriteLine("Mutation that Passes = " + index + " ** is vacous!");
          combinationResults[index] = Result.vacousProofPass;
        }else{
          // Console.WriteLine("Mutation that Passes = " + index);
          combinationResults[index] = Result.FalsePredicate;
        }
        // break;
      }else if (response.EndsWith($"resolution/type errors detected in {Path.GetFileName(filePath)}\n")) {
          combinationResults[index] = Result.InvalidExpr;
          // break;
        } else {
          combinationResults[index] = Result.IncorrectProof;
          break;
        }
      }
      if(requestList.Count < 2)
      {
          combinationResults[index] = Result.IncorrectProof;
      }else if (requestList.Count == 2)
      {
                  combinationResults[index] = Result.vacousProofPass;

      }
 
      // }
      expressionFinder.combinationResults[index] = combinationResults[index];
    }


    private void UpdateCombinationResultVacAware(int index,bool vac) {
      var requestList = dafnyVerifier.requestsList[index];
      var request = requestList.Last();
      // foreach (var request in requestList) {
        var position = dafnyVerifier.requestToPostConditionPosition[request];
        var lemmaStartPosition = dafnyVerifier.requestToLemmaStartPosition[request];
        var output = dafnyVerifier.dafnyOutput[request];
        var response = output.Response;
        var filePath = output.FileName;
        // var startTime = output.StartTime;
        var execTime = output.ExecutionTimeInMs;
        executionTimes.Add(execTime);
        // startTimes.Add(startTime);
        
        // Result res;
        // if (position != -1) {
          var expectedOutput =
            $"{filePath}({position},0): Error: A postcondition might not hold on this return path.";
          var expectedInconclusiveOutputStart =
            $"{filePath}({lemmaStartPosition},{validityLemmaNameStartCol}): Verification inconclusive";
        //   // Console.WriteLine($"{index} => {output}");
        //   // Console.WriteLine($"{output.EndsWith("0 errors\n")} {output.EndsWith($"resolution/type errors detected in {fileName}.dfy\n")}");
        //   // Console.WriteLine($"----------------------------------------------------------------");
        //   res = DafnyVerifierClient.IsCorrectOutputForValidityCheck(response, expectedOutput, expectedInconclusiveOutputStart);
        // } else {
        //   res = DafnyVerifierClient.IsCorrectOutputForNoErrors(response);
        // }
        var res = DafnyVerifierClient.IsCorrectOutput(response, expectedOutput, expectedInconclusiveOutputStart);

        if (res != Result.IncorrectProof) {
          // correctExpressions.Add(dafnyMainExecutor.processToExpr[p]);
          // Console.WriteLine(output);
          combinationResults[index] = res;
          // Console.WriteLine(p.StartInfo.Arguments);
          // Console.WriteLine(Printer.ExprToString(dafnyVerifier.requestToExpr[request]));
          Console.WriteLine(Printer.ExprToString(dafnyVerifier.requestToExpr[request]));
        } else if (response.Contains(" 0 errors\n")) {
        
        if(vac){
          Console.WriteLine("Mutation that Passes = " + index + " ** is vacous!");
          combinationResults[index] = Result.vacousProofPass;
        }else{
          Console.WriteLine("Mutation that Passes = " + index);
          combinationResults[index] = Result.FalsePredicate;
        }
        // break;
      }else if (response.EndsWith($"resolution/type errors detected in {Path.GetFileName(filePath)}\n")) {
          combinationResults[index] = Result.InvalidExpr;
          // break;
        } else {
          combinationResults[index] = Result.IncorrectProof;
          // break;
        }
      // }
      expressionFinder.combinationResults[index] = combinationResults[index];
    }

    

    public Dictionary<string, List<string>> GetEqualExpressionList(Expression expr) {
      // The first element of each value's list in the result is the type of list
      Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
      HoleEvalGraph<string> G = new HoleEvalGraph<string>();
      foreach (var e in Expression.Conjuncts(expr)) {
        if (e is BinaryExpr && (e as BinaryExpr).ResolvedOp == BinaryExpr.ResolvedOpcode.EqCommon) {
          var be = e as BinaryExpr;
          var e0 = Printer.ExprToString(be.E0);
          var e1 = Printer.ExprToString(be.E1);
          if (e0 == e1) {
            continue;
          }
          if (!G.AdjacencyList.ContainsKey(e0)) {
            G.AddVertex(e0);
          }
          if (!G.AdjacencyList.ContainsKey(e1)) {
            G.AddVertex(e1);
          }
          G.AddEdge(new Tuple<string, string>(e0, e1));
        }
      }
      HashSet<string> visited = new HashSet<string>();
      foreach (var e in Expression.Conjuncts(expr)) {
        if (e is BinaryExpr && (e as BinaryExpr).ResolvedOp == BinaryExpr.ResolvedOpcode.EqCommon) {
          var be = e as BinaryExpr;
          var e0 = Printer.ExprToString(be.E0);
          var e1 = Printer.ExprToString(be.E1);
          if (e0 == e1) {
            continue;
          }
          if (visited.Contains(e0) || visited.Contains(e1)) {
            Debug.Assert(visited.Contains(e0));
            Debug.Assert(visited.Contains(e1));
            continue;
          }
          HashSet<string> newVisits = G.DFS(e0);
          visited.UnionWith(newVisits);
          result[e0] = new List<string>();
          // The first element of each value's list in the result is the type of list
          result[e0].Add(be.E0.Type.ToString());
          newVisits.Remove(e0);
          foreach (string s in newVisits) {
            result[e0].Add(s);
          }
        }
      }
      return result;
    }

    public static DirectedCallGraph<Function, FunctionCallExpr, Expression> GetCallGraph(Function baseFunc) {
      Contract.Requires(baseFunc != null);
      DirectedCallGraph<Function, FunctionCallExpr, Expression> G = new DirectedCallGraph<Function, FunctionCallExpr, Expression>();
      // Tuple of SubExpression that is going to be parsed, pre-condition to reach this SubExpression, containing Function
      Queue<Tuple<Expression, Expression, Function>> queue = new Queue<Tuple<Expression, Expression, Function>>();
      queue.Enqueue(new Tuple<Expression, Expression, Function>(baseFunc.Body, null, baseFunc));
      G.AddVertex(baseFunc);
      HashSet<string> seenFunctionCalls = new HashSet<string>();
      // keys.Add(Printer.ExprToString(baseF.Body) + ":" + baseF.Body);
      // TODO: Check an example in which a function is called in another function with two different pre-conditions
      while (queue.Count > 0) {
        Tuple<Expression, Expression, Function> currExprCondParentTuple = queue.Dequeue();
        if (currExprCondParentTuple.Item1 == null) continue;
        // Console.WriteLine("Processing " + currExprParentTuple.Item1 + ": " + Printer.ExprToString(currExprParentTuple.Item1));
        if (currExprCondParentTuple.Item1 is FunctionCallExpr /*&& (currExpr as FunctionCallExpr).Function.Body != null*/) {
          // if (!keys.Contains(Printer.ExprToString((currExprParentTuple.Item1 as FunctionCallExpr).Function.Body) + ":" + (currExprParentTuple.Item1 as FunctionCallExpr).Function.Body)) {
          // Console.WriteLine("Adding " + (currExprParentTuple.Item1 as FunctionCallExpr).Function.Body + ": " + Printer.ExprToString((currExprParentTuple.Item1 as FunctionCallExpr).Function.Body));
          var funcCallCondAsString = $"{currExprCondParentTuple.Item3.Name} -> " +
                                     $"{(currExprCondParentTuple.Item1 as FunctionCallExpr).Function.Name} -> ";
          if (currExprCondParentTuple.Item2 != null) {
            funcCallCondAsString += $"{Printer.ExprToString(currExprCondParentTuple.Item2)}";
          } else {
            funcCallCondAsString += "NULL";
          }
          if (!seenFunctionCalls.Contains(funcCallCondAsString)) {
            seenFunctionCalls.Add(funcCallCondAsString);
            // if (currExprCondParentTuple.Item2 != null) {
            //   Console.WriteLine($"condition : {Printer.ExprToString(currExprCondParentTuple.Item2)}");
            // } else {
            //   Console.WriteLine($"condition : null");
            // }
            // if ((currExprCondParentTuple.Item1 as FunctionCallExpr).Function.ToString() == ) {

            // }
            if (currExprCondParentTuple.Item3 != (currExprCondParentTuple.Item1 as FunctionCallExpr).Function) {
              // Console.WriteLine("Here => "+ Printer.ExprToString((currExprCondParentTuple.Item1 as FunctionCallExpr).Function.Body));
              queue.Enqueue(new Tuple<Expression, Expression, Function>(
                (currExprCondParentTuple.Item1 as FunctionCallExpr).Function.Body,
                null,
                (currExprCondParentTuple.Item1 as FunctionCallExpr).Function));
              G.AddVertex((currExprCondParentTuple.Item1 as FunctionCallExpr).Function);
              G.AddEdge(
                currExprCondParentTuple.Item3,
                (currExprCondParentTuple.Item1 as FunctionCallExpr).Function,
                currExprCondParentTuple.Item1 as FunctionCallExpr,
                currExprCondParentTuple.Item2);
              // Console.WriteLine("-------------------------------------");
              // keys.Add(Printer.ExprToString((currExprParentTuple.Item1 as FunctionCallExpr).Function.Body) + ":" + (currExprParentTuple.Item1 as FunctionCallExpr).Function.Body);
            }
          }
        }
        if (currExprCondParentTuple.Item1 is ITEExpr) {
          var iteExpr = currExprCondParentTuple.Item1 as ITEExpr;

          // add Condition
          queue.Enqueue(new Tuple<Expression, Expression, Function>(
            iteExpr.Test, currExprCondParentTuple.Item2, currExprCondParentTuple.Item3));

          // add then path
          Expression thenCond;
          if (currExprCondParentTuple.Item2 != null && currExprCondParentTuple.Item2 is LetExpr) {
            var prevLet = currExprCondParentTuple.Item2 as LetExpr;
            thenCond = Expression.CreateLet(prevLet.tok, prevLet.LHSs, prevLet.RHSs, 
              Expression.CreateAnd(prevLet.Body, iteExpr.Test), prevLet.Exact);
          }
          else {
            thenCond = currExprCondParentTuple.Item2 != null ?
              Expression.CreateAnd(currExprCondParentTuple.Item2, iteExpr.Test) :
              iteExpr.Test;
          }
          queue.Enqueue(new Tuple<Expression, Expression, Function>(
            iteExpr.Thn, thenCond, currExprCondParentTuple.Item3));
          // add else path
          Expression elseCond;
          if (currExprCondParentTuple.Item2 != null && currExprCondParentTuple.Item2 is LetExpr) {
            var prevLet = currExprCondParentTuple.Item2 as LetExpr;
            elseCond = Expression.CreateLet(prevLet.tok, prevLet.LHSs, prevLet.RHSs, 
              Expression.CreateAnd(prevLet.Body, 
                Expression.CreateNot(iteExpr.Test.tok, iteExpr.Test)
              ), prevLet.Exact);
          }
          else {
            elseCond = currExprCondParentTuple.Item2 != null ?
              Expression.CreateAnd(currExprCondParentTuple.Item2,
                                   Expression.CreateNot(iteExpr.Test.tok, iteExpr.Test)) :
              Expression.CreateNot(iteExpr.Test.tok, iteExpr.Test);
          }
          queue.Enqueue(new Tuple<Expression, Expression, Function>(
            iteExpr.Els, elseCond, currExprCondParentTuple.Item3));

          G.AddVertex(currExprCondParentTuple.Item3);
        } else if (currExprCondParentTuple.Item1 is MatchExpr) {
          var matchExpr = currExprCondParentTuple.Item1 as MatchExpr;
          // add source
          queue.Enqueue(new Tuple<Expression, Expression, Function>(
            matchExpr.Source, currExprCondParentTuple.Item2, currExprCondParentTuple.Item3));

          // add cases
          // Console.WriteLine(Printer.ExprToString(matchExpr));
          foreach (var c in matchExpr.Cases) {
            // Console.WriteLine($"{c.Ctor} -> {c.Ctor.Name}");
            Expression cond;
            if (currExprCondParentTuple.Item2 != null && currExprCondParentTuple.Item2 is LetExpr) {
              var prevLet = currExprCondParentTuple.Item2 as LetExpr;
              cond = new LetExpr(prevLet.tok, prevLet.LHSs, prevLet.RHSs, 
                Expression.CreateAnd(prevLet.Body, new MemberSelectExpr(c.Ctor.tok, matchExpr.Source, c.Ctor.Name)), prevLet.Exact);
            }
            else {
              cond = currExprCondParentTuple.Item2 != null ?
                Expression.CreateAnd(currExprCondParentTuple.Item2, new MemberSelectExpr(c.Ctor.tok, matchExpr.Source, c.Ctor.Name)) :
                new MemberSelectExpr(c.Ctor.tok, matchExpr.Source, c.Ctor.Name + "?");
          }
            queue.Enqueue(new Tuple<Expression, Expression, Function>(
              c.Body, cond, currExprCondParentTuple.Item3));
          }
          G.AddVertex(currExprCondParentTuple.Item3);
          // Console.WriteLine("----------------------------------------------------------------");
        } else if (currExprCondParentTuple.Item1 is ExistsExpr) {
          var existsExpr = currExprCondParentTuple.Item1 as ExistsExpr;
          var lhss = new List<CasePattern<BoundVar>>();
          var rhss = new List<Expression>();
          foreach (var bv in existsExpr.BoundVars) {
            lhss.Add(new CasePattern<BoundVar>(bv.Tok,
              new BoundVar(bv.Tok, bv.Name, bv.Type)));
          }
          rhss.Add(existsExpr.Term);
        Expression prevCond;
          if (currExprCondParentTuple.Item2 != null && currExprCondParentTuple.Item2 is LetExpr) {
            var prevLet = currExprCondParentTuple.Item2 as LetExpr;
            prevCond = Expression.CreateLet(prevLet.tok, prevLet.LHSs, prevLet.RHSs, 
              Expression.CreateAnd(prevLet.Body, existsExpr), prevLet.Exact);
          }
          else {
            prevCond = currExprCondParentTuple.Item2 != null ?
              Expression.CreateAnd(currExprCondParentTuple.Item2, existsExpr) :
              existsExpr;
          }
          var cond = Expression.CreateAnd(prevCond, Expression.CreateLet(existsExpr.BodyStartTok, lhss, rhss,
            Expression.CreateBoolLiteral(existsExpr.BodyStartTok, true), false));

          queue.Enqueue(new Tuple<Expression, Expression, Function>(existsExpr.Term, cond, currExprCondParentTuple.Item3));
          G.AddVertex(currExprCondParentTuple.Item3);
        }
        else if (currExprCondParentTuple.Item1 is LetExpr) {
          var letExpr = currExprCondParentTuple.Item1 as LetExpr;
          var currLetExpr = letExpr;
          var lhss = new List<CasePattern<BoundVar>>();
          var rhss = new List<Expression>();
          while (true) {
            for (int i = 0; i < currLetExpr.LHSs.Count; i++) {
              var lhs = currLetExpr.LHSs[i];
              var rhs = currLetExpr.RHSs[0];
              // lhss.Add(new CasePattern<BoundVar>(bv.Tok,
              //   new BoundVar(bv.Tok, currExprCondParentTuple.Item3.Name + "_" + bv.Name, bv.Type)));
              lhss.Add(new CasePattern<BoundVar>(lhs.tok,
                new BoundVar(lhs.tok, lhs.Id, lhs.Expr != null ? lhs.Expr.Type : lhs.Var.Type)));
              rhss.Add(rhs);
            }
            if (currLetExpr.Body is LetExpr) {
              currLetExpr = currLetExpr.Body as LetExpr;
            }
            else {
              break;
            }
          }
          // var cond = currExprCondParentTuple.Item2 != null ?
          //   Expression.CreateAnd(currExprCondParentTuple.Item2, letExpr) :
          //   letExpr;
          var cond = Expression.CreateLet(letExpr.Body.tok, lhss, rhss,
            Expression.CreateBoolLiteral(letExpr.BodyStartTok, true), letExpr.Exact);
          queue.Enqueue(new Tuple<Expression, Expression, Function>(letExpr.Body, cond, currExprCondParentTuple.Item3));
          G.AddVertex(currExprCondParentTuple.Item3);
        }
        else if(currExprCondParentTuple.Item1 is ForallExpr) {
          var forallExpr = currExprCondParentTuple.Item1 as ForallExpr;
          var lhss = new List<CasePattern<BoundVar>>();
          var rhss = new List<Expression>();
           foreach (var bv in forallExpr.BoundVars) {
            lhss.Add(new CasePattern<BoundVar>(bv.Tok,
              new BoundVar(bv.Tok, bv.Name, bv.Type)));
          }
          // rhss.Add(forallExpr.Term);
          if(forallExpr.Range == null){
            rhss.Add(Expression.CreateBoolLiteral(forallExpr.BodyStartTok, true));
          }else{
            rhss.Add(forallExpr.Range);
          }
           var prevCond= currExprCondParentTuple.Item2 != null ?
            Expression.CreateAnd(currExprCondParentTuple.Item2, forallExpr) :
            forallExpr;
           var cond = Expression.CreateAnd(prevCond, Expression.CreateLet(forallExpr.BodyStartTok, lhss, rhss,
            forallExpr.Term, false));
          queue.Enqueue(new Tuple<Expression, Expression, Function>(forallExpr.Term, cond, currExprCondParentTuple.Item3));
          G.AddVertex(currExprCondParentTuple.Item3);
        } else {
          foreach (var e in currExprCondParentTuple.Item1.SubExpressions) {
            // if (!keys.Contains(Printer.ExprToString(e) + ":" + e)) {
            // Console.WriteLine("Adding " + e + ": " + Printer.ExprToString(e));
            // if (e is MatchExpr) {
            //   // Console.WriteLine($"MatchExpr : {Printer.ExprToString(e)}");
            //   queue.Enqueue(new Tuple<Expression, Expression, Function>(e, e, currExprCondParentTuple.Item3));
            //   G.AddVertex(currExprCondParentTuple.Item3);
            // } else if (e is ITEExpr) {
            //   // Console.WriteLine($"ITEExpr : {Printer.ExprToString(e)}");
            //   queue.Enqueue(new Tuple<Expression, Expression, Function>(e, e, currExprCondParentTuple.Item3));
            //   G.AddVertex(currExprCondParentTuple.Item3);
            // } else {
            // Console.WriteLine($"else : {e} -> {Printer.ExprToString(e)}");
            queue.Enqueue(new Tuple<Expression, Expression, Function>(e, currExprCondParentTuple.Item2, currExprCondParentTuple.Item3));
            G.AddVertex(currExprCondParentTuple.Item3);
            // }
          }
        }
      }
      return G;
    }

    DirectedCallGraph<Function, FunctionCallExpr, Expression> CG;
    List<List<Tuple<Function, FunctionCallExpr, Expression>>> Paths =
      new List<List<Tuple<Function, FunctionCallExpr, Expression>>>();
    List<Tuple<Function, FunctionCallExpr, Expression>> CurrentPath =
      new List<Tuple<Function, FunctionCallExpr, Expression>>();

    List<List<Tuple<Function, FunctionCallExpr, Expression>>> MutationsPaths =
      new List<List<Tuple<Function, FunctionCallExpr, Expression>>>();
    DirectedCallGraph<Function, FunctionCallExpr, Expression> MCG;
      List<Tuple<Function, FunctionCallExpr, Expression>> CurrentPathMutations =
      new List<Tuple<Function, FunctionCallExpr, Expression>>();

    public void GetAllPaths(Function source, Function destination) {
      if (source.FullDafnyName == destination.FullDafnyName) {
        Paths.Add(new List<Tuple<Function, FunctionCallExpr, Expression>>(CurrentPath));
        return;
      }
      foreach (var nwPair in CG.AdjacencyWeightList[source]) {
        CurrentPath.Add(new Tuple<Function, FunctionCallExpr, Expression>(
          nwPair.Item1, nwPair.Item2, nwPair.Item3));
        GetAllPaths(nwPair.Item1, destination);
        CurrentPath.RemoveAt(CurrentPath.Count - 1);
      }
    }

    public void GetAllMutationsPaths(Function source, Function destination) {
      if (source.FullDafnyName == destination.FullDafnyName) {
        MutationsPaths.Add(new List<Tuple<Function, FunctionCallExpr, Expression>>(CurrentPathMutations));
        return;
      }
      foreach (var nwPair in MCG.AdjacencyWeightList[source]) {
        CurrentPathMutations.Add(new Tuple<Function, FunctionCallExpr, Expression>(
          nwPair.Item1, nwPair.Item2, nwPair.Item3));
        GetAllMutationsPaths(nwPair.Item1, destination);
        CurrentPathMutations.RemoveAt(CurrentPathMutations.Count - 1);
      }
    }

    public static string GetPrefixedString(string prefix, Expression expr, ModuleDefinition currentModuleDef) {
      using (var wr = new System.IO.StringWriter()) {
        var pr = new Printer(wr);
        pr.Prefix = prefix;
        pr.ModuleForTypes = currentModuleDef;
        pr.PrintExpression(expr, false);
        return wr.ToString();
      }
    }
    public static string GetNonPrefixedString(Expression expr, ModuleDefinition currentModuleDef) {
      using (var wr = new System.IO.StringWriter()) {
        var pr = new Printer(wr);
        pr.ModuleForTypes = currentModuleDef;
        pr.PrintExpression(expr, false);
        return wr.ToString();
      }
    }

public static int[] AllIndexesOf(string str, string substr, bool ignoreCase = false)
{
    if (string.IsNullOrWhiteSpace(str) ||
        string.IsNullOrWhiteSpace(substr))
    {
        throw new ArgumentException("String or substring is not specified.");
    }

    var indexes = new List<int>();
    int index = 0;

    while ((index = str.IndexOf(substr, index, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) != -1)
    {
        indexes.Add(index++);
    }

    return indexes.ToArray();
}



   public static string GetLocalMutationList(Program program,Function fn, ModuleDefinition currentModuleDef,List<Tuple<Function, FunctionCallExpr, Expression>> path) {
      string res = "";
      // add all predicates in path
      for(int i = 0; i<path.Count-1; i++)
      {
        var nwPair = path[i];
      // }
      // foreach (var nwPair in path)
      // {
          using (var wr = new System.IO.StringWriter()) {
          var pr = new Printer(wr);
          pr.ModuleForTypes = currentModuleDef;
          pr.PrintFunction(nwPair.Item1, 0,false);
          res += wr.ToString();
        }
      res += "\n\n";
      }
    if(path.Count > 1){
    // annotate with "BASE_"
      foreach (var nwPair in path)
      {
        var indecies = AllIndexesOf(res,nwPair.Item1.Name+"(");
        foreach (var index in indecies.Reverse())
        {
          res = res.Insert(index, "MUTATED_");
        }
      }
    }
   
      return res;
    }

   public static string GetBaseLemmaList(Function fn, ModuleDefinition currentModuleDef) {
      string res = "";
      // res += RemoveWhitespace(Printer.FunctionSignatureToString(fn));
      res += "\n{\n";
      res += Printer.ExprToString(fn.Body);
      res += "\n}";
      // return res;
      // Console.WriteLine(fn.Name);
        using (var wr = new System.IO.StringWriter()) {
        var pr = new Printer(wr);
        pr.ModuleForTypes = currentModuleDef;
        pr.PrintFunction(fn, 0,false);
        res = wr.ToString();
      }
      int i = res.IndexOf(fn.Name);
      String modStr1 = res.Insert(i, "BASE_");
      return modStr1;
    }

    public static string getVacuityLemma(Lemma l)
    {
      string res = "";
      // res += Printer.ExprToString(baseLemma.Body);
      using (var wr1 = new System.IO.StringWriter()) {
        var pr1 = new Printer(wr1);
        pr1.PrintMethod(l, 0,false);
        // Console.WriteLine(wr1.ToString());
        res = wr1.ToString();
      }
      int d = res.IndexOf("decreases");
     foreach (var e in l.Ens) {
        Console.WriteLine( Printer.ExprToString(e.E));
     }
      
      if(d == -1){
        int i = res.IndexOf("{");
        res = res.Insert(i-1, "ensures false;\n");
      }else{
        res = res.Insert(d-1, "ensures false;\n");
      }
      return res;
    }

    public static string GetIsSameLemmaList(Function fn,List<Tuple<Function, FunctionCallExpr, Expression>> path, ModuleDefinition currentModuleDef, Expression constraintExpr,Boolean isQuantifier) {
      string res = "lemma isSame";
       foreach (var nwPair in path) {
        res += "_" + nwPair.Item1.Name;
      }
      res += "(";
      var sep = "";
      var p = "";
       Tuple<string, string> x = new Tuple<string, string>(null, null);
      foreach (var nwPair in path) {
        res += sep + " ";
        sep = "";
        x = GetFunctionParamList(nwPair.Item1);
        res += x.Item2;
        p += "" + x.Item1; 
        sep = ", ";
      }
      res += ")\n";
      foreach (var req in path[0].Item1.Req) {
       if(isQuantifier){
          res += "  requires forall " + x.Item2 + " :: "+ GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }else{
          res += "  requires " + GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }
      }
       if(p != ""){
        res += "  ensures forall " + p + " :: "+ fn.Name+ "_BASE("+p+") <==> " + fn.Name+"("+p+")\n{}";
       }else{
              res += "  ensures " + fn.Name+ "_BASE() <==> " + fn.Name+"()\n{}";
       }
      return res;
    }

    public static string GetIsWeaker(Function fn,List<Tuple<Function, FunctionCallExpr, Expression>> path, ModuleDefinition currentModuleDef, Expression constraintExpr, Boolean isQuantifier) {
      string res = "lemma isAtLeastAsWeak";
      //  foreach (var nwPair in path) {
      //   res += "_" + nwPair.Item1.Name;
      // }
       res += "_" + path.Last().Item1.Name;
      foreach(var t in fn.TypeArgs)
      {
        res += "<"+t+"(0,!new)>";
      }
      res += "(";
      var sep = "";
      var p = "";
      Tuple<string, string> x = new Tuple<string, string>(null, null);

      // foreach (var nwPair in path) {
      //   res += sep + " ";
      //   sep = "";
      //   x = GetFunctionParamList(nwPair.Item1);
      //   res += x.Item2;
      //   p += "" + x.Item1; 
      //   sep = ", ";
      // }
        res += sep + " ";
        sep = "";
        x = GetFunctionParamListSpec(path.Last().Item1);
        
        res += x.Item2;
        p += "" + x.Item1; 
        sep = ", ";
      res += ")\n";
      // if(p != ""){
      //   res += "requires " + fn.Name+"_BASE("+p+")\n";
      // }
      foreach (var req in path.Last().Item1.Req) {
        // res += "  requires " + GetPrefixedString(path[0].Item1.Name + "_", req.E, currentModuleDef) + "\n";
        
        if(isQuantifier){
          res += "  requires forall " + x.Item2 + " :: "+ GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }else{
          res += "  requires " + GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }
      }
      if(p != ""){
        res += "requires BASE_" + fn.Name+"("+p+")\n";
      }
      if(p != ""){
        // res += "  ensures forall " + p + " :: "+ fn.Name+"_BASE("+p+") ==> " + fn.Name+"("+p+")\n{}";
        res += "ensures " + fn.Name+"("+p+")\n{}\n";
      }else{
        // res += "  ensures BASE_" + fn.Name+"() ==> " + fn.Name+"()\n{}";
        res += "  ensures " + fn.Name+"() ==> BASE_" + fn.Name+"()\n{}";


      }
      return res;
    }

    

    public static string getVacuityLemmaRevised(Function fn,List<Tuple<Function, FunctionCallExpr, Expression>> path, ModuleDefinition currentModuleDef, Expression constraintExpr, Boolean isQuantifier) {
      string res = "lemma isVac";
      //  foreach (var nwPair in path) {
      //   res += "_" + nwPair.Item1.Name;
      
      // }
       res += "_" + path.Last().Item1.Name;
      foreach(var t in fn.TypeArgs)
      {
        res += "<"+t+"(0,!new)>";
      }
      res += "(";
      var sep = "";
      var p = "";
      Tuple<string, string> x = new Tuple<string, string>(null, null);

      // foreach (var nwPair in path) {
      //   res += sep + " ";
      //   sep = "";
      //   x = GetFunctionParamList(nwPair.Item1);
      //   res += x.Item2;
      //   p += "" + x.Item1; 
      //   sep = ", ";
      // }
        res += sep + " ";
        sep = "";
        x = GetFunctionParamList(path.Last().Item1);
        res += x.Item2;
        p += "" + x.Item1; 
        sep = ", ";
      res += ")\n";
      // if(p != ""){
      //   res += "requires " + fn.Name+"_BASE("+p+")\n";
      // }
      foreach (var req in path.Last().Item1.Req) {
        // res += "  requires " + GetPrefixedString(path[0].Item1.Name + "_", req.E, currentModuleDef) + "\n";
        
        if(isQuantifier){
          res += "  requires forall " + x.Item2 + " :: "+ GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }else{
          res += "  requires " + GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }
      }
      if(p != ""){
        res += "\t requires " + fn.Name+"("+p+")\n";
      }
      res += "\t ensures false;\n{}";
      return res;
    }

    public static string GetIsStronger(Function fn,List<Tuple<Function, FunctionCallExpr, Expression>> path, ModuleDefinition currentModuleDef, Expression constraintExpr, Boolean isQuantifier) {
      string res = "lemma isStronger";
       foreach (var nwPair in path) {
        res += "_" + nwPair.Item1.Name;
      }
      res += "(";
      var sep = "";
      var p = "";
      Tuple<string, string> x = new Tuple<string, string>(null, null);

      foreach (var nwPair in path) {
        res += sep + " ";
        sep = "";
        x = GetFunctionParamList(nwPair.Item1);
        res += x.Item2;
        p += "" + x.Item1; 
        sep = ", ";
      }
      res += ")\n";
      foreach (var req in path[0].Item1.Req) {
        // Console.WriteLine("  requires forall " + x.Item2 + " :: "+ GetNonPrefixedString(req.E, currentModuleDef) + "\n");
        // res += "  requires " + GetPrefixedString(path[0].Item1.Name + "_", req.E, currentModuleDef) + "\n";
        if(isQuantifier){
          res += "  requires forall " + x.Item2 + " :: "+ GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }else{
          res += "  requires " + GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }
      }
      if(p != ""){
        res += "  ensures forall " + p + " :: "+ fn.Name+ "("+p+") ==> " + fn.Name+"_BASE("+p+")\n{}";
      }else{
                res += "  ensures " + fn.Name+ "_BASE() ==> " + fn.Name+"()\n{}";

        // res += "  ensures " + fn.Name+ "() ==> " + fn.Name+"_BASE()\n{}";

      }
      return res;
    }

  public static string GetBaseLemmaListMutationList(Program program,Function fn, ModuleDefinition currentModuleDef,List<Tuple<Function, FunctionCallExpr, Expression>> path) {
      string res = "";
      // add all predicates in path
      foreach (var nwPair in path)
      {
          using (var wr = new System.IO.StringWriter()) {
          var pr = new Printer(wr);
          pr.ModuleForTypes = currentModuleDef;
          pr.PrintFunction(nwPair.Item1, 0,false);
          res += wr.ToString();
        }
      res += "\n\n";
      }
    // annotate with "BASE_"
    foreach (var nwPair in path)
    {
      var indeciesNormal = AllIndexesOf(res,nwPair.Item1.Name+"(");
      var indeciesGeneric = AllIndexesOf(res,nwPair.Item1.Name+"<");
      List<int> indeciesTotal = new List<int>();
      indeciesTotal.AddRange(indeciesNormal);
      indeciesTotal.AddRange(indeciesGeneric);
      // if(indecies.Length == 0){
      //   indecies = AllIndexesOf(res,nwPair.Item1.Name+"<");
      // }
      foreach (var index in indeciesTotal.ToArray().Reverse())
      {
        res = res.Insert(index, "BASE_");
      }
    }
    // add "mutated" intermediate predicates
    // for (int i = 0; i < path.Count - 1; i++)
    // {
    //   var nwPair = path[i];
    //   using (var wr = new System.IO.StringWriter()) {
    //       var pr = new Printer(wr);
    //       pr.ModuleForTypes = currentModuleDef;
    //       pr.PrintFunction(nwPair.Item1, 0,false);
    //       res += wr.ToString();
    //     }
    //   res += "\n\n";
    // }

      return res;
    }

      public static string GetBaseLemmaListArbitMutation(Program program,Function fn, ModuleDefinition currentModuleDef,List<Tuple<Function, FunctionCallExpr, Expression>> path,ExpressionFinder.ExpressionDepth expr,string prefixName) {
      string res = "";
      // add all predicates in path
      foreach (var nwPair in path)
      {
        var orig = nwPair.Item1.Body;
        Function fnTemp = nwPair.Item1;
        fnTemp.Body = expr.expr;
        // nwPair.Item1.Body = orig;
        // fn.body = expr.expr;
          using (var wr = new System.IO.StringWriter()) {
          var pr = new Printer(wr);
          pr.ModuleForTypes = currentModuleDef;
          pr.PrintFunction(nwPair.Item1, 0,false);
          res += wr.ToString();
        }
      res += "\n\n";
      }
    // annotate with "BASE_"
    foreach (var nwPair in path)
    {
      var indeciesNormal = AllIndexesOf(res,nwPair.Item1.Name+"(");
      var indeciesGeneric = AllIndexesOf(res,nwPair.Item1.Name+"<");
      List<int> indeciesTotal = new List<int>();
      indeciesTotal.AddRange(indeciesNormal);
      indeciesTotal.AddRange(indeciesGeneric);
      // if(indecies.Length == 0){
      //   indecies = AllIndexesOf(res,nwPair.Item1.Name+"<");
      // }
      foreach (var index in indeciesTotal.ToArray().Reverse())
      {
        res = res.Insert(index, prefixName+"_");
      }
    }

      return res;
    }

    public static string GetIsWeakerMutationsRoot(Function fn,List<Tuple<Function, FunctionCallExpr, Expression>> path, ModuleDefinition currentModuleDef, Expression constraintExpr, Boolean isQuantifier) {
      string res = "lemma isAtLeastAsWeak";
      //  foreach (var nwPair in path) {
      //   res += "_" + nwPair.Item1.Name;
      // }
       res += "_" + path[0].Item1.Name;
      foreach(var t in fn.TypeArgs)
      {
        res += "<"+t+"(0,!new)>";
      }
      res += "(";
      var sep = "";
      var p = "";
      Tuple<string, string> x = new Tuple<string, string>(null, null);

      // foreach (var nwPair in path) {
      //   res += sep + " ";
      //   sep = "";
      //   x = GetFunctionParamList(nwPair.Item1);
      //   res += x.Item2;
      //   p += "" + x.Item1; 
      //   sep = ", ";
      // }
        res += sep + " ";
        sep = "";
        x = GetFunctionParamListSpec(path[0].Item1);
        
        res += x.Item2;
        p += "" + x.Item1; 
        sep = ", ";
      res += ")\n";
      // if(p != ""){
      //   res += "requires " + fn.Name+"_BASE("+p+")\n";
      // }
      foreach (var req in path[0].Item1.Req) {
        // res += "  requires " + GetPrefixedString(path[0].Item1.Name + "_", req.E, currentModuleDef) + "\n";
        
        if(isQuantifier){
          res += "  requires forall " + x.Item2 + " :: "+ GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }else{
          res += "  requires " + GetNonPrefixedString(req.E, currentModuleDef) + "\n";
        }
      }
      if(p != ""){
        if(DafnyOptions.O.IsRequires)
        {
          res += "requires " + fn.Name+"("+p+")\n";
          res += "ensures BASE_" + fn.Name+"("+p+")\n{}\n";
        }else{
          res += "requires BASE_" + fn.Name+"("+p+")\n";
          res += "ensures " + fn.Name+"("+p+")\n{}\n";
        }        
      }else{
        res += "  ensures BASE_" + fn.Name+"() ==> " + fn.Name+"()\n{}";
        // res += "  ensures " + fn.Name+"() ==> BASE_" + fn.Name+"()\n{}";

      }
      return res;
    }

    public static string GetIsWeakerClassificationLemma(Function fn,List<Tuple<Function, FunctionCallExpr, Expression>> path, ModuleDefinition currentModuleDef, Expression constraintExpr, string firstName, string secondName) {
      string res = "lemma isAtLeastAsWeak";
      //  foreach (var nwPair in path) {
      //   res += "_" + nwPair.Item1.Name;
      // }
       res += "_" + path[0].Item1.Name;
      foreach(var t in fn.TypeArgs)
      {
        res += "<"+t+"(0,!new)>";
      }
      res += "(";
      var sep = "";
      var p = "";
      Tuple<string, string> x = new Tuple<string, string>(null, null);
      var temp = "";
      foreach (var nwPair in path) {
        res += sep + "\n    ";
        sep = "";
        // res += GetFunctionParamList(nwPair.Item1, nwPair.Item1.Name + "_").Item2;
        res += GetFunctionParamListSpec(nwPair.Item1,nwPair.Item1.Name + "_").Item2;
        // p += "" + nwPair.Item1;  
        sep = ", ";
      }
      // foreach (var nwPair in path) {
      //   res += sep + " ";
      //   sep = "";
      //   x = GetFunctionParamList(nwPair.Item1);
      //   res += x.Item2;
      //   p += "" + x.Item1; 
      //   sep = ", ";
      // }
        // res += sep + " ";
        // sep = "";
        // x = GetFunctionParamListSpec(path[0].Item1);
        
        // res += x.Item2;
        // p += "" + x.Item1; 
        // sep = ", ";
      res += ")\n";
    var bdyAssert = "";
      foreach (var req in path[0].Item1.Req) {
        currentModuleDef = path[0].Item1.EnclosingClass.EnclosingModuleDefinition;
        res += "  requires " + GetPrefixedString(path[0].Item1.Name + "_", req.E, currentModuleDef) + "\n";
      }
      res += "  requires " +firstName + fn.Name + "(";
      sep = "";
      foreach (var formal in path[0].Item1.Formals) {
        res += sep + path[0].Item1.Name + "_" + formal.Name;
        sep = ", ";
      }
      res += ")\n";

            for (int i = 0; i < path.Count - 1; i++) {
        var callExpr = path[i + 1].Item2;
        var condExpr = path[i + 1].Item3;
        var requiresOrAndSep = "requires";
        if (condExpr != null) {
          if (condExpr is BinaryExpr && (condExpr as BinaryExpr).E1 is LetExpr) {
            requiresOrAndSep = "  &&";
          }
          currentModuleDef = path[i].Item1.EnclosingClass.EnclosingModuleDefinition;
          var tempStr = $"  {requiresOrAndSep} " + GetPrefixedString(path[i].Item1.Name + "_", condExpr, currentModuleDef) ;
          var callExprStr = Printer.ExprToString(callExpr);
          // tempStr = ReplaceFirst()
          var indecies = AllIndexesOf(tempStr,callExprStr.Substring(0,callExprStr.IndexOf("(")));
          foreach (var index in indecies.Reverse())
          {
            tempStr = tempStr.Insert(index, firstName);
          }
          bdyAssert = tempStr + " && " +GetPrefixedString(path[i].Item1.Name + "_", callExpr, currentModuleDef) +";";
          bdyAssert = "assert " + bdyAssert.Substring(2);
          res += tempStr+ "\n";
          // res += $"  {requiresOrAndSep} " + GetPrefixedString(path[i].Item1.Name + "_", condExpr, currentModuleDef) + "\n";
        }
        for (int j = 0; j < callExpr.Args.Count; j++) {
          res += $"  {requiresOrAndSep} ";
          res += GetPrefixedString(path[i].Item1.Name + "_", callExpr.Args[j], currentModuleDef);
          res += " == ";
          res += path[i + 1].Item1.Name + "_" + path[i + 1].Item1.Formals[j].Name + "\n";
        }
        foreach (var req in callExpr.Function.Req) {
          res += $"  {requiresOrAndSep} " + GetPrefixedString(path[i + 1].Item1.Name + "_", req.E, currentModuleDef) + "\n";
        }
        res += $"  {requiresOrAndSep} " + firstName +callExpr.Function.Name + "(";
        sep = "";
        foreach (var arg in callExpr.Args) {
          res += sep + GetPrefixedString(path[i].Item1.Name + "_", arg, currentModuleDef);
          sep = ", ";
        }
        res += ")\n";
      }
      
      
      // if(p != ""){
      //   res += "requires " + fn.Name+"_BASE("+p+")\n";
      // }
      res += "  ensures " + secondName + fn.Name + "(";
      sep = "";
      foreach (var formal in path[0].Item1.Formals) {
        res += sep + path[0].Item1.Name + "_" + formal.Name;
        sep = ", ";
      }
      res += ")\n";
      // res += "ensures " + fn.Name+"("+GetFunctionParamList(fn);+")\n{}\n";
      if(bdyAssert.Contains("requires"))
      {
        bdyAssert = "assert " + bdyAssert.Substring(bdyAssert.IndexOf("requires")+8);
      }
      res +=  " {\n" + bdyAssert + "\n}";
      // Console.WriteLine(res);
      return res;
    }

    public static string GetIsWeakerMutationsRootFull(Function fn,List<Tuple<Function, FunctionCallExpr, Expression>> path, ModuleDefinition currentModuleDef, Expression constraintExpr, Boolean isQuantifier) {
      string res = "lemma isAtLeastAsWeak";
      //  foreach (var nwPair in path) {
      //   res += "_" + nwPair.Item1.Name;
      // }
       res += "_" + path[0].Item1.Name;
      foreach(var t in fn.TypeArgs)
      {
        res += "<"+t+"(0,!new)>";
        // Console.WriteLine("a = " + t);
      }
      res += "(";
      var sep = "";
      var p = "";
      Tuple<string, string> x = new Tuple<string, string>(null, null);
      var temp = "";
      foreach (var nwPair in path) {
        res += sep + "\n    ";
        sep = "";
        // res += GetFunctionParamList(nwPair.Item1, nwPair.Item1.Name + "_").Item2;
        res += GetFunctionParamListSpec(nwPair.Item1,nwPair.Item1.Name + "_").Item2;
        // p += "" + nwPair.Item1;  
        sep = ", ";
      }
      res += ")\n";
    var bdyAssert = "";
      foreach (var req in path[0].Item1.Req) {
        currentModuleDef = path[0].Item1.EnclosingClass.EnclosingModuleDefinition;
        res += "  requires " + GetPrefixedString(path[0].Item1.Name + "_", req.E, currentModuleDef) + "\n";
      }
      res += "  requires BASE_" + fn.Name + "(";
      sep = "";
      foreach (var formal in path[0].Item1.Formals) {
        res += sep + path[0].Item1.Name + "_" + formal.Name;
        sep = ", ";
      }
      res += ")\n";

            for (int i = 0; i < path.Count - 1; i++) {
        var callExpr = path[i + 1].Item2;
        var condExpr = path[i + 1].Item3;
        var requiresOrAndSep = "requires";
        if (condExpr != null) {
          if (condExpr is BinaryExpr && (condExpr as BinaryExpr).E1 is LetExpr) {
            requiresOrAndSep = "  &&";
          }
          currentModuleDef = path[i].Item1.EnclosingClass.EnclosingModuleDefinition;
          var tempStr = $"  {requiresOrAndSep} " + GetPrefixedString(path[i].Item1.Name + "_", condExpr, currentModuleDef) ;
          var callExprStr = Printer.ExprToString(callExpr);
          // tempStr = ReplaceFirst()
          var indecies = AllIndexesOf(tempStr,callExprStr.Substring(0,callExprStr.IndexOf("(")));
          foreach (var index in indecies.Reverse())
          {
            tempStr = tempStr.Insert(index, "BASE_");
          }
          bdyAssert = tempStr + " && " +GetPrefixedString(path[i].Item1.Name + "_", callExpr, currentModuleDef) +";";
          bdyAssert = "assert " + bdyAssert.Substring(2);
          res += tempStr+ "\n";
          // res += $"  {requiresOrAndSep} " + GetPrefixedString(path[i].Item1.Name + "_", condExpr, currentModuleDef) + "\n";
        }
        for (int j = 0; j < callExpr.Args.Count; j++) {
          res += $"  {requiresOrAndSep} ";
          res += GetPrefixedString(path[i].Item1.Name + "_", callExpr.Args[j], currentModuleDef);
          res += " == ";
          res += path[i + 1].Item1.Name + "_" + path[i + 1].Item1.Formals[j].Name + "\n";
        }
        foreach (var req in callExpr.Function.Req) {
          res += $"  {requiresOrAndSep} " + GetPrefixedString(path[i + 1].Item1.Name + "_", req.E, currentModuleDef) + "\n";
        }
        res += $"  {requiresOrAndSep} " + "BASE_"+callExpr.Function.Name + "(";
        sep = "";
        foreach (var arg in callExpr.Args) {
          res += sep + GetPrefixedString(path[i].Item1.Name + "_", arg, currentModuleDef);
          sep = ", ";
        }
        res += ")\n";
      }
      
      
      // if(p != ""){
      //   res += "requires " + fn.Name+"_BASE("+p+")\n";
      // }
      res += "  ensures " + fn.Name + "(";
      sep = "";
      foreach (var formal in path[0].Item1.Formals) {
        res += sep + path[0].Item1.Name + "_" + formal.Name;
        sep = ", ";
      }
      res += ")\n";
      // res += "ensures " + fn.Name+"("+GetFunctionParamList(fn);+")\n{}\n";
      if(bdyAssert.Contains("requires"))
      {
        bdyAssert = "assert " + bdyAssert.Substring(bdyAssert.IndexOf("requires")+8);
      }
      res +=  " {\n" + bdyAssert + "\n}";
      return res;
    }




//GetValidityLemma 
 public static string GetValidityLemma(List<Tuple<Function, FunctionCallExpr, Expression>> path, ModuleDefinition currentModuleDef, Expression constraintExpr, int cnt, int id) {
      string res = "lemma {:timeLimitMultiplier 2} validityCheck";
      if (cnt != -1) {
        res += "_" + cnt.ToString();
      }
      foreach (var nwPair in path) {
        res += "_" + nwPair.Item1.Name;
      }
      res += "_" + id;
            foreach(var t in path.Last().Item1.TypeArgs)
      {
        res += "<"+t+"(0,!new)>";
        // Console.WriteLine("a = " + t);
      }
      res += "(";
      var sep = "";
      foreach (var nwPair in path) {
        res += sep + "\n    ";
        sep = "";
        res += GetFunctionParamList(nwPair.Item1, nwPair.Item1.Name + "_").Item2;
        sep = ", ";
      }
      res += ")\n";
      foreach (var req in path[0].Item1.Req) {
        res += "  requires " + GetPrefixedString(path[0].Item1.Name + "_", req.E, currentModuleDef) + "\n";
      }
      res += "  requires " + path[0].Item1.FullDafnyName + "(";
      sep = "";
      foreach (var formal in path[0].Item1.Formals) {
        res += sep + path[0].Item1.Name + "_" + formal.Name;
        sep = ", ";
      }
      res += ")\n";
      for (int i = 0; i < path.Count - 1; i++) {
        var callExpr = path[i + 1].Item2;
        var condExpr = path[i + 1].Item3;
        var requiresOrAndSep = "requires";
        if (condExpr != null) {
          if (condExpr is BinaryExpr && 
                ((condExpr as BinaryExpr).E0 is LetExpr ||
                 (condExpr as BinaryExpr).E1 is LetExpr) )
          {
            requiresOrAndSep = "  &&";
          }
          currentModuleDef = path[i].Item1.EnclosingClass.EnclosingModuleDefinition;
          res += $"  {requiresOrAndSep} " + GetPrefixedString(path[i].Item1.Name + "_", condExpr, null) + "\n";//          res += $"  {requiresOrAndSep} " + GetPrefixedString(path[i].Item1.Name + "_", condExpr, currentModuleDef) + "\n";

           if (condExpr is LetExpr) {
            requiresOrAndSep = "  &&";
          }
        }
        for (int j = 0; j < callExpr.Args.Count; j++) {
          res += $"  {requiresOrAndSep} ";
          res += GetPrefixedString(path[i].Item1.Name + "_", callExpr.Args[j], currentModuleDef);
          res += " == ";
          res += path[i + 1].Item1.Name + "_" + path[i + 1].Item1.Formals[j].Name + "\n";
        }
        foreach (var req in callExpr.Function.Req) {
          res += $"  {requiresOrAndSep} " + GetPrefixedString(path[i + 1].Item1.Name + "_", req.E, currentModuleDef) + "\n";
        }
        res += $"  {requiresOrAndSep} " + callExpr.Function.FullDafnyName + "(";
        sep = "";
        foreach (var arg in callExpr.Args) {
          res += sep + GetPrefixedString(path[i].Item1.Name + "_", arg, currentModuleDef);
          sep = ", ";
        }
        res += ")\n";
      }
      if (constraintExpr != null) {
        currentModuleDef = path[0].Item1.EnclosingClass.EnclosingModuleDefinition;
        res += "  requires " + GetPrefixedString(path[0].Item1.Name + "_", constraintExpr, currentModuleDef) + "\n";
      }
      res += "  ensures false\n{}";
      return res;
    }
    public async Task<bool> EvaluateAfterRemoveFileLine(Program program, Program unresolvedProgram, string removeFileLine, string baseFuncName, int depth) {
      var funcName = CodeModifier.Erase(program, removeFileLine);
      return await Evaluate(program, unresolvedProgram, funcName, baseFuncName, depth);
    }

        public void PrintAllLemmas(Program program, string lemmaName) {
      foreach (var kvp in program.ModuleSigs) {
        foreach (var d in kvp.Value.ModuleDef.TopLevelDecls) {
          var cl = d as TopLevelDeclWithMembers;
          if (cl != null) {
            foreach (var member in cl.Members) {
              var m = member as Lemma;
              if (m != null) {
                Console.WriteLine("LEMMA NAME = " + m.FullDafnyName);
              }
            }
          }
        }
      }
    }
    public static Lemma GetLemma(Program program, string lemmaName) {
      foreach (var kvp in program.ModuleSigs) {
        foreach (var d in kvp.Value.ModuleDef.TopLevelDecls) {
          var cl = d as TopLevelDeclWithMembers;
          if (cl != null) {
            foreach (var member in cl.Members) {
              var m = member as Lemma;
              if (m != null) {
                if (m.FullDafnyName == lemmaName) {
                  return m;
                }
              }
            }
          }
        }
      }
      return null;
    }

        public static Method GetMethod(Program program, string methodName) {
      foreach (var kvp in program.ModuleSigs) {
        foreach (var d in kvp.Value.ModuleDef.TopLevelDecls) {
          var cl = d as TopLevelDeclWithMembers;
          if (cl != null) {
            foreach (var member in cl.Members) {
              var m = member as Method;
              if (m != null) {
                if (m.FullDafnyName == methodName) {
                  return m;
                }
              }
            }
          }
        }
      }
      return null;
    }
        public void PrintAllMethods(Program program, string methodName) {
      foreach (var kvp in program.ModuleSigs) {
        foreach (var d in kvp.Value.ModuleDef.TopLevelDecls) {
          var cl = d as TopLevelDeclWithMembers;
          if (cl != null) {
            foreach (var member in cl.Members) {
              var m = member as Method;
              if (m != null) {
                Console.WriteLine("Method NAME = " + m.FullDafnyName);
              }
            }
          }
        }
      }
    }



    public async Task<bool> EvaluateFilterStrongerAndSame(Program program, Program unresolvedProgram, string funcName, string lemmaName, string proofModuleName, string baseFuncName, int depth, bool mutationsFromParams,Program proofProg, Program unresolvedProofProgram) {
      if(DafnyOptions.O.MutationRootName == null)
      {
        DafnyOptions.O.MutationRootName = DafnyOptions.O.MutationTarget;
      }
      Console.WriteLine("mutationsFromParams = " + mutationsFromParams);
      if (DafnyOptions.O.ServerIpPortList == null) {
        Console.WriteLine("ip port list is not given. Please specify with /serverIpPortList");
        return false;
      }
      Console.WriteLine(DafnyOptions.O.ServerIpPortList);
      if (DafnyOptions.O.HoleEvaluatorCommands != null) {
        var input = File.ReadAllText(DafnyOptions.O.HoleEvaluatorCommands);
        tasksList = Google.Protobuf.JsonParser.Default.Parse<TasksList>(input);
      }
      dafnyVerifier = new DafnyVerifierClient(DafnyOptions.O.ServerIpPortList, $"output_{funcName}");
      expressionFinder = new ExpressionFinder(this);
      bool runOnce = DafnyOptions.O.HoleEvaluatorRunOnce;
      int timeLimitMultiplier = 2;
      int timeLimitMultiplierLength = 0;
      while (timeLimitMultiplier >= 1) {
        timeLimitMultiplierLength++;
        timeLimitMultiplier /= 10;
      }
      validityLemmaNameStartCol = 30 + timeLimitMultiplierLength;

      // Collect all paths from baseFunc to func mutation to mutation root
      Console.WriteLine($"{funcName} {baseFuncName} {depth}");
      if (baseFuncName == null) {
        baseFuncName = funcName;
      }
      Function baseFunc = null;
      if(proofProg != null){
       baseFunc = GetFunction(proofProg, baseFuncName);
      }else{
       baseFunc = GetFunction(program, baseFuncName);
      }

      if (baseFunc == null) {
        Console.WriteLine($"couldn't find function {baseFuncName}. List of all functions:");
        foreach (var kvp in program.ModuleSigs) {
          foreach (var topLevelDecl in ModuleDefinition.AllFunctions(kvp.Value.ModuleDef.TopLevelDecls)) {
            Console.WriteLine(topLevelDecl.FullDafnyName);
          }
        }
        return false;
      }


      Function constraintFunc = null;
      if (DafnyOptions.O.HoleEvaluatorConstraint != null) {
        constraintFunc = GetFunction(proofProg, DafnyOptions.O.HoleEvaluatorConstraint);
        if (constraintFunc == null) {
          Console.WriteLine($"constraint function {DafnyOptions.O.HoleEvaluatorConstraint} not found!");
          return false;
        }
      }
      CG = GetCallGraph(baseFunc);
      Function func = GetFunction(program, funcName);
      Function mutationRootFunc = GetFunction(program, DafnyOptions.O.MutationRootName);
      CurrentPath.Add(new Tuple<Function, FunctionCallExpr, Expression>(baseFunc, null, null));
      GetAllPaths(baseFunc, func);
      MCG = GetCallGraph(mutationRootFunc);
      CurrentPathMutations.Add(new Tuple<Function, FunctionCallExpr, Expression>(mutationRootFunc, null, null));
      GetAllMutationsPaths(mutationRootFunc,func);
      if (Paths.Count == 0)
        Paths.Add(new List<Tuple<Function, FunctionCallExpr, Expression>>(CurrentPath));
      // foreach (var p in Paths) {
      //   Console.WriteLine(GetValidityLemma(p));
      //   Console.WriteLine("\n----------------------------------------------------------------\n");
      // }
      // return true;

      UnderscoreStr = RandomString(8);
      dafnyVerifier.sw = Stopwatch.StartNew();
      Console.WriteLine($"mutation evaluation begins for func {funcName}");
      Function desiredFunction = null;
      Function desiredFunctionUnresolved = null;
      Method desiredMethod = null;
      // Method desiredMethodUnresolved = null;
      Function desiredFunctionMutationRoot = null;
      Function desiredFunctionMutationRootResolved = null;
      Function topLevelDeclCopy = null;
      desiredFunction = GetFunction(program, funcName);
      // Console.WriteLine("--Function to Mutate--");
      // using (var wr = new System.IO.StringWriter()) {
      //   var pr = new Printer(wr);
      //   pr.PrintFunction(desiredFunction, 0,false);
      //   Console.WriteLine(wr.ToString());
      // }
      // Console.WriteLine("--------------");

      if (desiredFunction != null) {
        includeParser = new IncludeParser(program);
        var filename = "";
        if(desiredFunction.BodyStartTok.Filename == null)
        {
          filename = includeParser.Normalized(program.FullName);
        }else{
          filename = includeParser.Normalized(desiredFunction.BodyStartTok.Filename);
        }
        foreach (var file in includeParser.GetListOfAffectedFilesBy(filename)) {
          affectedFiles.Add(file);
          affectedFiles = affectedFiles.Distinct().ToList();
        }

      if(proofProg != null){
        // dafnyVerifier.InitializeBaseFoldersInRemoteServers(proofProg, includeParser.commonPrefix);
        affectedFiles.Add(filename);
        affectedFiles = affectedFiles.Distinct().ToList();
        desiredMethod = GetMethod(proofProg, lemmaName);
        Lemma desiredLemmm = GetLemma(proofProg, lemmaName);
      if (desiredLemmm == null && desiredMethod == null) {
        Console.WriteLine($"couldn't find function {desiredLemmm}. List of all lemmas:");
        PrintAllLemmas(proofProg, lemmaName);
        Console.WriteLine($"couldn't find function {desiredMethod}. List of all methods:");
        PrintAllMethods(proofProg, lemmaName);
        return false;
      }
      var filenameProof = "";
      if(desiredLemmm != null){
      Console.WriteLine(getVacuityLemma(desiredLemmm));
      Console.WriteLine("-----\n------\n");
              includeParser = new IncludeParser(proofProg);
        // var filenameProof = "";
        if(desiredLemmm.BodyStartTok.Filename == null)
        {
          filenameProof = includeParser.Normalized(proofProg.FullName);
        }else{
          filenameProof = includeParser.Normalized(desiredLemmm.BodyStartTok.Filename);
        }
        foreach (var file in includeParser.GetListOfAffectedFilesBy(filenameProof)) {
          Console.WriteLine("file = " + filenameProof);
          affectedFiles.Add(file);
        }
      }else{
        Console.WriteLine("-----\n------\n");
        includeParser = new IncludeParser(proofProg);
        
        if(desiredMethod.BodyStartTok.Filename == null)
        {
          filenameProof = includeParser.Normalized(proofProg.FullName);
        }else{
          filenameProof = includeParser.Normalized(desiredMethod.BodyStartTok.Filename);
        }
        foreach (var file in includeParser.GetListOfAffectedFilesBy(filenameProof)) {
          Console.WriteLine("file = " + filenameProof);
          affectedFiles.Add(file);
        }
      }

        foreach (var file in includeParser.GetListOfAffectedFiles(filenameProof)) {
          Console.WriteLine("file = " + filenameProof);
          affectedFiles.Add(file);
        }
        // create appropriate fs in grpc servers
        dafnyVerifier.InitializeBaseFoldersInRemoteServers(proofProg, includeParser.commonPrefix);

      }else{
        dafnyVerifier.InitializeBaseFoldersInRemoteServers(program, includeParser.commonPrefix);
        affectedFiles.Add(filename);
        affectedFiles = affectedFiles.Distinct().ToList();
      }

      
        if (constraintFunc != null) {
          Dictionary<string, HashSet<ExpressionFinder.ExpressionDepth>> typeToExpressionDictForInputs = new Dictionary<string, HashSet<ExpressionFinder.ExpressionDepth>>();
          foreach (var formal in baseFunc.Formals) {
            var identExpr = new ExpressionFinder.ExpressionDepth(Expression.CreateIdentExpr(formal), 1);
            var typeString = formal.Type.ToString();
            if (typeToExpressionDictForInputs.ContainsKey(typeString)) {
              typeToExpressionDictForInputs[typeString].Add(identExpr);
            } else {
              var lst = new HashSet<ExpressionFinder.ExpressionDepth>();
              lst.Add(identExpr);
              typeToExpressionDictForInputs.Add(typeString, lst);
            }
          }
          var funcCalls = ExpressionFinder.GetAllPossibleFunctionInvocations(proofProg, constraintFunc, typeToExpressionDictForInputs);
          foreach (var funcCall in funcCalls) {
            if (constraintExpr == null) {
              constraintExpr = new ExpressionFinder.ExpressionDepth(funcCall.expr, 1);
            } else {
              constraintExpr.expr = Expression.CreateAnd(constraintExpr.expr, funcCall.expr);
            }
          }
          if (funcCalls.Count > 0) {
          Console.WriteLine($"constraint expr to be added : {Printer.ExprToString(constraintExpr.expr)}");
          }
        }
        //MERGE
        desiredFunctionUnresolved = GetFunctionFromUnresolved(unresolvedProgram, funcName);
        // expressionFinder.CalcDepthOneAvailableExpresssionsFromFunctionBody(program, desiredFunctionUnresolved);
        expressionFinder.CalcDepthOneAvailableExpresssionsFromFunctionBody(program, desiredFunction);
        desiredFunctionMutationRoot = GetFunctionFromUnresolved(unresolvedProgram,DafnyOptions.O.MutationRootName);
        // desiredMethodUnresolved = GetMethodFromUnresolved(unresolvedProofProgram,lemmaName);
        desiredFunctionMutationRootResolved = GetFunction(program, DafnyOptions.O.MutationRootName);

        if (DafnyOptions.O.HoleEvaluatorRemoveFileLine != null) {
          var fileLineList = DafnyOptions.O.HoleEvaluatorRemoveFileLine.Split(',');
          foreach (var fileLineString in fileLineList) {
            var fileLineArray = fileLineString.Split(':');
            var file = fileLineArray[0];
            var line = Int32.Parse(fileLineArray[1]);
            CodeModifier.EraseFromPredicate(desiredFunctionUnresolved as Predicate, line);
          }
        }
        Contract.Assert(desiredFunctionUnresolved != null);
        topLevelDeclCopy = new Function(
          desiredFunctionUnresolved.tok, desiredFunctionUnresolved.Name, desiredFunctionUnresolved.HasStaticKeyword,
          desiredFunctionUnresolved.IsGhost, desiredFunctionUnresolved.TypeArgs, desiredFunctionUnresolved.Formals,
          desiredFunctionUnresolved.Result, desiredFunctionUnresolved.ResultType, desiredFunctionUnresolved.Req,
          desiredFunctionUnresolved.Reads, desiredFunctionUnresolved.Ens, desiredFunctionUnresolved.Decreases,
          desiredFunctionUnresolved.Body, desiredFunctionUnresolved.ByMethodTok, desiredFunctionUnresolved.ByMethodBody,
          desiredFunctionUnresolved.Attributes, desiredFunctionUnresolved.SignatureEllipsis);
      } else {
        Console.WriteLine($"{funcName} was not found!");
        return false;
      }
       Lemma baseLemma;
       if(proofProg == null){
        baseLemma = GetLemma(program, lemmaName);
      if (baseLemma == null) {
        Console.WriteLine($"couldn't find function {lemmaName}. List of all lemmas:");
        PrintAllLemmas(program, lemmaName);
        return false;
      }
      // Console.WriteLine(getVacuityLemma(baseLemma));
      }else{
         baseLemma = GetLemma(proofProg, lemmaName);
      if (baseLemma == null) {
        Console.WriteLine($"couldn't find function {lemmaName}. List of all lemmas:");
        PrintAllLemmas(program, lemmaName);
        return false;
      }
      var id = 0;
      foreach (var p in Paths)
      {
        var singleValidityLemma = GetValidityLemma(p, null, constraintExpr == null ? null : constraintExpr.expr, -1,id);
        id++;
      }

      }
      // generate mutations
      int initialCount = expressionFinder.availableExpressions.Count;
      ExpressionFinder expressionFindeTest = new ExpressionFinder(this);
      if(mutationsFromParams){
        expressionFindeTest.CalcDepthOneAvailableExpresssionsFromFunction(program, desiredFunction);
        for (int i = 0; i < expressionFindeTest.availableExpressions.Count; i++) {
          Expression simpleAdditiveMutation = Expression.CreateAnd(expressionFindeTest.availableExpressions[i].expr, desiredFunctionUnresolved.Body);
          Console.WriteLine("mutated (from params) = " +  Printer.ExprToString(simpleAdditiveMutation));
          expressionFinder.availableExpressions.Add(new ExpressionFinder.ExpressionDepth(simpleAdditiveMutation,1));
        }

        int comboCount = 0;

        if(depth > 1 ){
        for(int i = 1; i < expressionFindeTest.availableExpressions.Count;i++){
          for(int j = 2; j < expressionFindeTest.availableExpressions.Count;j++){
            if(i != j){
              var exprA = expressionFindeTest.availableExpressions[i];
              var exprB = expressionFindeTest.availableExpressions[j];
              var conjunctExpr = Expression.CreateAnd(exprA.expr, exprB.expr);
              var disjunctExpr = Expression.CreateOr(exprA.expr, exprB.expr);
              // Console.WriteLine("Combo ("+ i +","+ j + ") " + Printer.ExprToString(conjunctExpr));
              // Console.WriteLine("Combo ("+ i +","+ j + ") " + Printer.ExprToString(disjunctExpr));
              
              Expression disjunctDepth2 = Expression.CreateAnd(disjunctExpr, desiredFunctionUnresolved.Body);
              Console.WriteLine("mutated (from params d > 1) = " +  Printer.ExprToString(disjunctDepth2));
              expressionFinder.availableExpressions.Add(new ExpressionFinder.ExpressionDepth(disjunctDepth2,1));
              Expression conjunctDepth2 = Expression.CreateAnd(conjunctExpr, desiredFunctionUnresolved.Body);
              Console.WriteLine("mutated (from params d > 1) = " +  Printer.ExprToString(conjunctDepth2));
              expressionFinder.availableExpressions.Add(new ExpressionFinder.ExpressionDepth(conjunctDepth2,1));
              comboCount = comboCount + 2;
            }
          }
        }
      }
      }
            
      Console.WriteLine($"expressionFinder.availableExpressions.Count == {expressionFinder.availableExpressions.Count}");

      // 第一步：去除完全重复的mutations
      Hashtable duplicateMutations = new Hashtable();
      foreach (var ed in expressionFinder.availableExpressions)
      {
        if(!duplicateMutations.ContainsKey(Printer.ExprToString(ed.expr)))
        {
          duplicateMutations.Add(Printer.ExprToString(ed.expr),ed);
        }else{
          Console.WriteLine("Skipping duplicate = " + Printer.ExprToString(ed.expr));
        }
      }
      var dedupExpressions = duplicateMutations.Values.Cast<Microsoft.Dafny.ExpressionFinder.ExpressionDepth>().ToList();

      // 第二步：质量筛选和优先级排序
      var prioritizedMutations = new List<ExpressionFinder.ExpressionDepth>();
      var simpleCount = 0;
      var complexCount = 0;
      var skippedSimple = 0;

      foreach (var ed in dedupExpressions) {
          string exprStr = Printer.ExprToString(ed.expr);
          
          // 跳过无意义的简单常量
          if (exprStr.Trim() == "true" || exprStr.Trim() == "false" || 
              exprStr.Trim() == "0" || exprStr.Trim() == "1") {
              Console.WriteLine("Skipping trivial = " + exprStr);
              continue;
          }
          
          // 优先保留复杂表达式
          if (exprStr.Contains("forall") || exprStr.Contains("exists") || 
              exprStr.Contains("&&") || exprStr.Contains("||") || 
              exprStr.Contains("==>") || exprStr.Length > 50) {
              prioritizedMutations.Add(ed);
              complexCount++;
          } 
          // 限制简单表达式数量
          else if (simpleCount < 15) {
              prioritizedMutations.Add(ed);
              simpleCount++;
          } else {
              skippedSimple++;
          }
      }

      expressionFinder.availableExpressions = prioritizedMutations;
      Console.WriteLine($"Optimization results: {duplicateMutations.Count} after dedup -> {complexCount} complex + {simpleCount} simple = {prioritizedMutations.Count} total (skipped {skippedSimple} simple)");

      expressionFinder.availableExpressions = duplicateMutations.Values.Cast<Microsoft.Dafny.ExpressionFinder.ExpressionDepth>().ToList();
      int numFailiedAfter1stPass = 0;
      int remainingVal = expressionFinder.availableExpressions.Count;
      int mutationCap = expressionFinder.availableExpressions.Count < 50 ? expressionFinder.availableExpressions.Count : expressionFinder.availableExpressions.Count; 

      // === 核心优化：预筛选去重 ===
      var originalCount = expressionFinder.availableExpressions.Count;
      var filteredExpressions = new List<ExpressionFinder.ExpressionDepth>();
      var exprSignatures = new HashSet<string>();

      for (int i = 0; i < expressionFinder.availableExpressions.Count; i++) {
          var expr = expressionFinder.availableExpressions[i];
          string signature = Printer.ExprToString(expr.expr).Replace(" ", "").ToLower();
          
          // 跳过重复表达式
          if (exprSignatures.Contains(signature)) continue;
          
          // 跳过简单常量mutations
          if (signature == "true" || signature == "false" || signature == "0" || signature == "1") continue;
          
          exprSignatures.Add(signature);
          filteredExpressions.Add(expr);
      }

      expressionFinder.availableExpressions = filteredExpressions;
      mutationCap = Math.Min(filteredExpressions.Count, 30); // 限制最大数量
      Console.WriteLine($"Optimized: {originalCount} -> {mutationCap} mutations");


      Console.WriteLine("--- Begin Is At Least As Weak Pass -- " + remainingVal);
      for (int i = 0; i < mutationCap; i++) {
        desiredFunctionUnresolved.Body = topLevelDeclCopy.Body;
        if(unresolvedProofProgram != null){
            PrintExprAndCreateProcessLemmaSeperateProof(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved, baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,false,true,false,desiredFunctionMutationRoot);
        }else{
          PrintExprAndCreateProcessLemma(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved, baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,false,true,false,desiredFunctionMutationRoot);
        }
      }
      await dafnyVerifier.startProofTasksAccordingToPriority();
       dafnyVerifier.clearTasks();
      Console.WriteLine("--- END Is At Least As Weak Pass  -- ");
      Console.WriteLine("--- START Vacuity Pass -- ");
        Stopwatch VacStopwatch = new Stopwatch();
        VacStopwatch.Start();
        for (int i = 0; i < mutationCap; i++) {
          var isWeaker = isDafnyVerifySuccessful(i);  
          var resolutionError = isResolutionError(i);
          if(!isWeaker && !resolutionError){
            desiredFunctionUnresolved.Body = topLevelDeclCopy.Body;
            if(unresolvedProofProgram != null){
                PrintExprAndCreateProcessLemmaSeperateProof(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved,baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,true,false,true,desiredFunctionMutationRoot);
            }else{
              PrintExprAndCreateProcessLemma(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved,baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,false,false,true,desiredFunctionMutationRoot);
            }
          }else{
          Console.WriteLine("Failed Afer 1st PASS:  Index(" + i + ") :: IsWeaker = " + isWeaker + " :: ResolutionError= " + resolutionError);
          numFailiedAfter1stPass++;
            remainingVal = remainingVal - 1;
            var requestList = dafnyVerifier.requestsList[i];
            writeFailedOutputs(i);
            foreach (var request in requestList){
            dafnyVerifier.dafnyOutput[request].Response = "isAtLeastAsWeak";
            }
            
          }
        }

        await dafnyVerifier.startProofTasksAccordingToPriority();
        dafnyVerifier.clearTasks();

        Console.WriteLine("--- END Vacuity Pass --");
        VacStopwatch.Stop();
        Console.WriteLine("Elapsed Time is {0} ms", VacStopwatch.ElapsedMilliseconds);

        Console.WriteLine("--- START Full Proof Pass -- ");
        Stopwatch FullStopWatch = new Stopwatch();
        FullStopWatch.Start();
        List<int> vacIndex = new List<int>();
        // if(unresolvedProofProgram == null){
        
        for (int i = 0; i < mutationCap; i++) {
            var isVacuous = isDafnyVerifySuccessful(i);
            var prevPassRes = dafnyVerifier.dafnyOutput[dafnyVerifier.requestsList[i].First()].Response; //isAtLeastAsWeak
            if(prevPassRes == "isAtLeastAsWeak"){
              Console.WriteLine("Failed Afer 1st PASS:  Index(" + i + ")");
            }else if(isVacuous){
              vacIndex.Add(i);
              Console.WriteLine("Failed Afer 2nd PASS:  Index(" + i + ") :: isVacuous"); // Note that it is vacous, but still check
                desiredFunctionUnresolved.Body = topLevelDeclCopy.Body;
                if(unresolvedProofProgram != null){
                PrintExprAndCreateProcessLemmaSeperateProof(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved,baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,true,false,false,desiredFunctionMutationRoot);
              }else{
              PrintExprAndCreateProcessLemma(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved,baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,true,false,false,desiredFunctionMutationRoot);
              }

            }else{
              // if(Printer.ExprToString(expressionFinder.availableExpressions[i].expr) == "pred_axiom_is_my_attestation_2(dv, process, block) && forall a1: Attestation, a2: Attestation | ((a1 in block.body.attestations && DVC_Spec_NonInstr.isMyAttestation(a1, process.bn, block, bn_get_validator_index(process.bn, block.body.state_root, process.dv_pubkey))) || !(a2 in block.body.attestations)) && DVC_Spec_NonInstr.isMyAttestation(a2, process.bn, block, bn_get_validator_index(process.bn, block.body.state_root, process.dv_pubkey)) :: a1.data.slot == a2.data.slot ==> a1 == a2")
              // {              
               desiredFunctionUnresolved.Body = topLevelDeclCopy.Body;
                if(unresolvedProofProgram != null){
                PrintExprAndCreateProcessLemmaSeperateProof(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved,baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,true,false,false,desiredFunctionMutationRootResolved);
              }else{
              // PrintExprAndCreateProcessLemma(unresolvedProgram, desiredFunctionUnresolved,baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,true,false,true);
              PrintExprAndCreateProcessLemma(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved,baseLemma,proofModuleName,expressionFinder.availableExpressions[i], i,true,false,false,desiredFunctionMutationRoot);
              }
              // } 
            }
           var isWeaker = isDafnyVerifySuccessful(i);  
        }
        await dafnyVerifier.startProofTasksAccordingToPriority();
        dafnyVerifier.clearTasks();
        Console.WriteLine("--- END Full Proof Pass -- ");
     
        FullStopWatch.Stop();
        Console.WriteLine("Elapsed Time is {0} ms", FullStopWatch.ElapsedMilliseconds);
        
        var fullProofElapsedTime = FullStopWatch.ElapsedMilliseconds;
      bool foundCorrectExpr = false;

        for (int i = 0; i < mutationCap; i++) {
          if(DafnyOptions.O.EvaluateAllAtOnce || DafnyOptions.O.ProofOnly){
                // UpdateCombinationResultVacAware(i,vacIndex.Contains(i));
                //  writeOutputs(i);
                UpdateCombinationResultVacAwareList(i,vacIndex.Contains(i));
          
                writeOutputs(i);
                foundCorrectExpr = false;
                foundCorrectExpr |= combinationResults[i] == Result.FalsePredicate;
                // Console.WriteLine(foundCorrectExpr);
                var t = isDafnyVerifySuccessful(i);
                if(foundCorrectExpr)
                {
                  Console.WriteLine("Mutation that Passes = " + i);
                  Console.WriteLine("\t ==> " + Printer.ExprToString(expressionFinder.availableExpressions[i].expr));

                }else if (combinationResults[i] == Result.vacousProofPass)
                {
                  Console.WriteLine("Mutation that Passes = " + i + " ** is vacous!");
                  Console.WriteLine("\t ==> " +  Printer.ExprToString(expressionFinder.availableExpressions[i].expr));

                }
          }else{
            UpdateCombinationResultVacAwareList(i,vacIndex.Contains(i));
          
            writeOutputs(i);
            foundCorrectExpr = false;
            foundCorrectExpr |= combinationResults[i] == Result.FalsePredicate;
            // Console.WriteLine(foundCorrectExpr);
            var t = isDafnyVerifySuccessful(i);
            if(foundCorrectExpr)
            {
              Console.WriteLine("Mutation that Passes = " + i);
              Console.WriteLine("\t ==> " +  Printer.ExprToString(expressionFinder.availableExpressions[i].expr));
              passingIndecies.Add(i);
            }else if (combinationResults[i] == Result.vacousProofPass)
            {
              Console.WriteLine("Mutation that Passes = " + i + " ** is vacous!");
              Console.WriteLine("\t ==> " +  Printer.ExprToString(expressionFinder.availableExpressions[i].expr));

            }
          }
        }
      Console.WriteLine("--- Finished Mutation Testing -- ");

      Console.WriteLine($"{dafnyVerifier.sw.ElapsedMilliseconds / 1000} :: finished exploring");
      int correctProofCount = 0;
      int correctProofByTimeoutCount = 0;
      int incorrectProofCount = 0;
      int invalidExprCount = 0;
      int falsePredicateCount = 0;
      int vacousProofPass = 0;
      int noMatchingTriggerCount = 0;
      for (int i = 0; i < mutationCap; i++) {
        switch (combinationResults[i]) {
          case Result.InvalidExpr: invalidExprCount++; break;
          case Result.FalsePredicate: falsePredicateCount++; break;
          case Result.CorrectProof: correctProofCount++; break;
          case Result.CorrectProofByTimeout: correctProofByTimeoutCount++; break;
          case Result.IncorrectProof: incorrectProofCount++; break;
          case Result.NoMatchingTrigger: noMatchingTriggerCount++; break;
          case Result.vacousProofPass: vacousProofPass++; break;
          case Result.Unknown: throw new NotSupportedException();
        }
      }
      Console.WriteLine("{0,-15} {1,-15} {2,-15} {3,-15} {4, -25} {5, -15}",
        "InvalidExpr", "IncorrectProof", "ProofPasses", "1st pass","vacousPasses","Total");
      Console.WriteLine("{0,-15} {1,-15} {2,-15} {3,-15} {4, -25}  {5, -15}",
        invalidExprCount, incorrectProofCount, falsePredicateCount, numFailiedAfter1stPass, vacousProofPass , expressionFinder.availableExpressions.Count);
      string executionTimesSummary = "";
      string verboseExecTimesSummary = "";

      foreach (var pair in verboseExecTimes)
        {
            string key = pair.Key;
            string value = pair.Value.ToString();
            verboseExecTimesSummary += key + " :: " + value + "\n";
        }
      foreach (var pair in verboseExecTimesTotal)
      {
            string key = pair.Key.ToString();
            string value = pair.Value.ToString();
            verboseExecTimesSummary +="( " + key + ") :: " + value + "\n";
      }
      await File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}/executionTimeSummaryVerbose.txt",
      verboseExecTimesSummary);
      for (int i = 0; i < executionTimes.Count; i++) {
        executionTimesSummary += $"{i}, {executionTimes[i].ToString()}\n";
      }
      await File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}/executionTimeSummary.txt",
            executionTimesSummary);

      string startTimesSummary = "";
      for (int i = 0; i < startTimes.Count; i++) {
        startTimesSummary += $"{i}, {(startTimes[i] - startTimes[0]).ToString()}\n";
      }
      await File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}/startTimeSummary.txt",
            startTimesSummary);

      Console.WriteLine("--- START Mutation Classifications -- ");
      Stopwatch ClassificationsStopWatch = new Stopwatch();
      ClassificationsStopWatch.Start();
      Console.WriteLine("Elapsed Time (Classifications) is {0} ms", ClassificationsStopWatch.ElapsedMilliseconds);

      for (int i = 0; i<passingIndecies.Count;i++)
      {
      // for(int j = i+1; j<passingIndecies.Count;j++){
      //   desiredFunctionUnresolved.Body = topLevelDeclCopy.Body;
      //   MutationClassification(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved,baseLemma,proofModuleName,passingIndecies[i],passingIndecies[j]);
      // }
        for(int j = 0; j<passingIndecies.Count;j++){
          if(i != j){
          desiredFunctionUnresolved.Body = topLevelDeclCopy.Body;
          MutationClassification(unresolvedProgram, unresolvedProofProgram,desiredFunctionUnresolved,baseLemma,proofModuleName,passingIndecies[i],passingIndecies[j]);
          }
        }
      }
      await dafnyVerifier.startProofTasksAccordingToPriority();
      dafnyVerifier.clearTasks();
      //
      for (int i = 0; i<passingIndecies.Count;i++){
        classheirarchy.Add(passingIndecies[i],new List<int>());
        classifyMutationsDag(passingIndecies[i],i,vacIndex.Contains(i));

      }
      dafnyVerifier.Cleanup();

    // process classifications
    foreach (var k in classheirarchy.Keys)
    {
      foreach (var v in classheirarchy[k])
      {
        if(classheirarchy.ContainsKey(v))
        {
          //merge b/c transitive
          foreach (var otherV in classheirarchy[v])
          {
            if(otherV == k)
            {
              Console.WriteLine("Mutation " + v + " is equivalent to = " + otherV );
            }
            else if(!classheirarchy[k].Contains(otherV))
            {
              // adding (probably not used)
              // Console.WriteLine("adding == " + otherV);
              classheirarchy[k].Add(otherV);
            }
          }
          classheirarchy.Remove(v);
        }
      }
    }
    Console.WriteLine("--- END Mutation Classifications -- \n");
    FullStopWatch.Stop();
    Console.WriteLine("Elapsed Time (Classifications) is {0} ms", ClassificationsStopWatch.ElapsedMilliseconds);
    Console.WriteLine("TOTAL Elapsed Time is {0} ms", ClassificationsStopWatch.ElapsedMilliseconds+fullProofElapsedTime);

    // print classifications

    foreach (var kvp in classheirarchy)
    {
      string weakList = "";
      foreach(var w in kvp.Value)
      {
        weakList += w +" :: ";
      }
      if(weakList.Length > 0){
        weakList = weakList.Substring(0,weakList.Length-3);
      }
      Console.WriteLine("root = (" + kvp.Key + "):" +Printer.ExprToString(expressionFinder.availableExpressions[kvp.Key].expr)+ "\n"+ weakList + "\n---");
    }
    
    Console.WriteLine("Total Alive Mutations = " + classheirarchy.Keys.Count);

    if (combinationResults[0] == Result.CorrectProof || combinationResults[0] == Result.CorrectProofByTimeout) {
      Console.WriteLine("proof already goes through and no additional conjunction is needed!");
      return true;
    }
    await dafnyVerifier.FinalizeCleanup();
    return true;

  
    }



    public static Function GetFunctionFromUnresolvedClass(Program program, string funcName) {
      int index = funcName.IndexOf('.');
      string moduleName = funcName.Remove(index);
      foreach (var topLevelDecl in program.DefaultModuleDef.TopLevelDecls) {
        if(topLevelDecl.FullDafnyName == moduleName) {
          var lmd = topLevelDecl as LiteralModuleDecl;
          var func = GetFunctionFromModuleDef(lmd.ModuleDef, funcName);
          if (func != null) {
            return func;
          }
        }
      }
      return null;
    }
        public static Method GetMethodFromUnresolved(Program program, string methodName) {
      int index = methodName.IndexOf('.');
      string moduleName = methodName.Remove(index);
      foreach (var topLevelDecl in program.DefaultModuleDef.TopLevelDecls) {
        if (topLevelDecl.FullDafnyName == moduleName) {
          var lmd = topLevelDecl as LiteralModuleDecl;
          var func = GetMethodFromModuleDef(lmd.ModuleDef, methodName);
          if (func != null) {
            return func;
          }
        }
      }
      return null;
    }

public async Task<bool> Evaluate(Program program, Program unresolvedProgram, string funcName, string baseFuncName, int depth) {
      if (DafnyOptions.O.ServerIpPortList == null) {
        Console.WriteLine("ip port list is not given. Please specify with /serverIpPortList");
        return false;
      }
      if (DafnyOptions.O.HoleEvaluatorCommands != null) {
        var input = File.ReadAllText(DafnyOptions.O.HoleEvaluatorCommands);
        tasksList = Google.Protobuf.JsonParser.Default.Parse<TasksList>(input);
        foreach (var task in tasksList.Tasks) {
          tasksListDictionary.Add(task.Path, task);
        }
      }
      dafnyVerifier = new DafnyVerifierClient(DafnyOptions.O.ServerIpPortList, $"output_{funcName}");
      expressionFinder = new ExpressionFinder(this);
      bool runOnce = DafnyOptions.O.HoleEvaluatorRunOnce;
      int timeLimitMultiplier = 2;
      int timeLimitMultiplierLength = 0;
      while (timeLimitMultiplier >= 1) {
        timeLimitMultiplierLength++;
        timeLimitMultiplier /= 10;
      }
      validityLemmaNameStartCol = 30 + timeLimitMultiplierLength;

      // Collect all paths from baseFunc to func
      Console.WriteLine($"{funcName} {baseFuncName} {depth}");
      if (baseFuncName == null) {
        baseFuncName = funcName;
      }
      Function baseFunc = GetFunction(program, baseFuncName);
      if (baseFunc == null) {
        Console.WriteLine($"couldn't find function {baseFuncName}. List of all functions:");
        foreach (var kvp in program.ModuleSigs) {
          foreach (var topLevelDecl in ModuleDefinition.AllFunctions(kvp.Value.ModuleDef.TopLevelDecls)) {
            Console.WriteLine(topLevelDecl.FullDafnyName);
          }
        }
        return false;
      }
      Function constraintFunc = null;
      if (DafnyOptions.O.HoleEvaluatorConstraint != null) {
        constraintFunc = GetFunction(program, DafnyOptions.O.HoleEvaluatorConstraint);
        if (constraintFunc == null) {
          Console.WriteLine($"constraint function {DafnyOptions.O.HoleEvaluatorConstraint} not found!");
          return false;
        }
      }
      CG = GetCallGraph(baseFunc);
      Function func = GetFunction(program, funcName);
      CurrentPath.Add(new Tuple<Function, FunctionCallExpr, Expression>(baseFunc, null, null));
      GetAllPaths(baseFunc, func);
      if (Paths.Count == 0)
        Paths.Add(new List<Tuple<Function, FunctionCallExpr, Expression>>(CurrentPath));
      // foreach (var p in Paths) {
      //   Console.WriteLine(GetValidityLemma(p));
      //   Console.WriteLine("\n----------------------------------------------------------------\n");
      // }
      // return true;

      UnderscoreStr = RandomString(8);
      dafnyVerifier.sw = Stopwatch.StartNew();
      Console.WriteLine($"mutation evaluation begins for func {funcName}");
      Function desiredFunction = null;
      Function desiredFunctionUnresolved = null;
      Function topLevelDeclCopy = null;
      desiredFunction = GetFunction(program, funcName);
      if (desiredFunction != null) {
        includeParser = new IncludeParser(program);
        var filename = includeParser.Normalized(desiredFunction.BodyStartTok.filename);
        foreach (var file in includeParser.GetListOfAffectedFilesBy(filename)) {
          affectedFiles.Add(file);
        }
        dafnyVerifier.InitializeBaseFoldersInRemoteServers(program, includeParser.commonPrefix);
        affectedFiles.Add(filename);
        affectedFiles = affectedFiles.Distinct().ToList();
        if (constraintFunc != null) {
          Dictionary<string, HashSet<ExpressionFinder.ExpressionDepth>> typeToExpressionDictForInputs = new Dictionary<string, HashSet<ExpressionFinder.ExpressionDepth>>();
          foreach (var formal in baseFunc.Formals) {
            var identExpr = new ExpressionFinder.ExpressionDepth(Expression.CreateIdentExpr(formal), 1);
            var typeString = formal.Type.ToString();
            if (typeToExpressionDictForInputs.ContainsKey(typeString)) {
              typeToExpressionDictForInputs[typeString].Add(identExpr);
            } else {
              var lst = new HashSet<ExpressionFinder.ExpressionDepth>();
              lst.Add(identExpr);
              typeToExpressionDictForInputs.Add(typeString, lst);
            }
          }
          var funcCalls = ExpressionFinder.GetAllPossibleFunctionInvocations(program, constraintFunc, typeToExpressionDictForInputs);
          foreach (var funcCall in funcCalls) {
            if (constraintExpr == null) {
              constraintExpr = new ExpressionFinder.ExpressionDepth(funcCall.expr, 1);
            } else {
              constraintExpr.expr = Expression.CreateAnd(constraintExpr.expr, funcCall.expr);
            }
          }
          Console.WriteLine($"constraint expr to be added : {Printer.ExprToString(constraintExpr.expr)}");
        }
        expressionFinder.CalcDepthOneAvailableExpresssionsFromFunction(program, desiredFunction);
        desiredFunctionUnresolved = GetFunctionFromUnresolved(unresolvedProgram, funcName);
        if (DafnyOptions.O.HoleEvaluatorRemoveFileLine != null) {
          var fileLineList = DafnyOptions.O.HoleEvaluatorRemoveFileLine.Split(',');
          foreach (var fileLineString in fileLineList) {
            var fileLineArray = fileLineString.Split(':');
            var file = fileLineArray[0];
            var line = Int32.Parse(fileLineArray[1]);
            CodeModifier.EraseFromPredicate(desiredFunctionUnresolved as Predicate, line);
          }
        }
        Contract.Assert(desiredFunctionUnresolved != null);
        topLevelDeclCopy = new Function(
          desiredFunctionUnresolved.tok, desiredFunctionUnresolved.Name, desiredFunctionUnresolved.HasStaticKeyword,
          desiredFunctionUnresolved.IsGhost, desiredFunctionUnresolved.TypeArgs, desiredFunctionUnresolved.Formals,
          desiredFunctionUnresolved.Result, desiredFunctionUnresolved.ResultType, desiredFunctionUnresolved.Req,
          desiredFunctionUnresolved.Reads, desiredFunctionUnresolved.Ens, desiredFunctionUnresolved.Decreases,
          desiredFunctionUnresolved.Body, desiredFunctionUnresolved.ByMethodTok, desiredFunctionUnresolved.ByMethodBody,
          desiredFunctionUnresolved.Attributes, desiredFunctionUnresolved.SignatureEllipsis);
      } else {
        Console.WriteLine($"{funcName} was not found!");
        return false;
      }
      Console.WriteLine($"expressionFinder.availableExpressions.Count == {expressionFinder.availableExpressions.Count}");

      workingFunc = desiredFunctionUnresolved;
      workingConstraintFunc = constraintFunc;
      workingFuncCode = File.ReadAllLines(workingFunc.BodyStartTok.filename);
      mergedCode.Add(String.Join('\n', workingFuncCode.Take(workingFunc.tok.line - 1)));
      // placeholder for workingLemma
      mergedCode.Add("");
      mergedCode.Add(String.Join('\n', workingFuncCode.Skip(workingFunc.BodyEndTok.line)));

      if (constraintFunc != null && constraintFunc.BodyStartTok.filename != workingFunc.BodyStartTok.filename) {
        constraintFuncCode = File.ReadAllText(constraintFunc.BodyStartTok.filename);
        constraintFuncLineCount = constraintFuncCode.Count(f => (f == '\n'));
      }
      
      lemmaForExprValidityString = GetValidityLemma(Paths[0], null, constraintExpr == null ? null : constraintExpr.expr, -1,0);
      lemmaForExprValidityLineCount = lemmaForExprValidityString.Count(f => (f == '\n'));
      Console.WriteLine("VALIDITYLEMMA \n" + lemmaForExprValidityString + " \n --");
      for (int i = 0; i < expressionFinder.availableExpressions.Count; i++) {
      // for (int i = 0; i < 100; i++) {
        PrintExprAndCreateProcess(unresolvedProgram, expressionFinder.availableExpressions[i], i);
        desiredFunctionUnresolved.Body = topLevelDeclCopy.Body;
      }
      Console.WriteLine($"{dafnyVerifier.sw.ElapsedMilliseconds / 1000}:: finish generatinng expressions and lemmas");
      await dafnyVerifier.startProofTasksAccordingToPriority();
      Console.WriteLine($"{dafnyVerifier.sw.ElapsedMilliseconds / 1000}:: finish exploring");

      dafnyVerifier.Cleanup();
      // bool foundCorrectExpr = false;
      
      for (int i = 0; i < expressionFinder.availableExpressions.Count; i++) {
      // for (int i = 0; i < 100; i++) {
        UpdateCombinationResult(i);
        // foundCorrectExpr |= combinationResults[i] == Result.CorrectProof;
      }

      // Until here, we only check depth 1 of expressions.
      // Now we try to check next depths
      int numberOfSingleExpr = expressionFinder.availableExpressions.Count;
      for (int dd = 2; dd <= depth; dd++) {
        var prevDepthExprStartIndex = expressionFinder.availableExpressions.Count;
        expressionFinder.CalcNextDepthAvailableExpressions();
        for (int i = prevDepthExprStartIndex; i < expressionFinder.availableExpressions.Count; i++) {
          var expr = expressionFinder.availableExpressions[i];
          PrintExprAndCreateProcess(program, expr, i);
          desiredFunction.Body = topLevelDeclCopy.Body;
        }
        await dafnyVerifier.startAndWaitUntilAllProcessesFinishAndDumpTheirOutputs();
        for (int i = prevDepthExprStartIndex; i < expressionFinder.availableExpressions.Count; i++) {
          UpdateCombinationResult(i);
        }
      }
      Console.WriteLine($"{dafnyVerifier.sw.ElapsedMilliseconds / 1000}:: finish exploring, try to calculate implies graph");
      int correctProofCount = 0;
      int correctProofByTimeoutCount = 0;
      int incorrectProofCount = 0;
      int invalidExprCount = 0;
      int falsePredicateCount = 0;
      int noMatchingTriggerCount = 0;
      for (int i = 0; i < expressionFinder.availableExpressions.Count; i++) {
      // for (int i = 0; i < 100; i++) {
        switch (combinationResults[i]) {
          case Result.InvalidExpr: invalidExprCount++; break;
          case Result.FalsePredicate: falsePredicateCount++; break;
          case Result.CorrectProof:
            Console.WriteLine($"correct answer: {i} {Printer.ExprToString(expressionFinder.availableExpressions[i].expr)}");
            correctProofCount++; 
            break;
          case Result.CorrectProofByTimeout: 
            Console.WriteLine($"correct answer by timeout: {i}");
            correctProofByTimeoutCount++; 
            break;
          case Result.IncorrectProof: incorrectProofCount++; break;
          case Result.NoMatchingTrigger: noMatchingTriggerCount++; break;
          case Result.Unknown: throw new NotSupportedException();
        }
      }
      Console.WriteLine("{0,-15} {1,-15} {2,-15} {3,-15} {4, -25} {5, -15} {6, -15}",
        "InvalidExpr", "IncorrectProof", "FalsePredicate", "CorrectProof", "CorrectProofByTimeout", "NoMatchingTrigger", "Total");
      Console.WriteLine("{0,-15} {1,-15} {2,-15} {3,-15} {4, -25} {5, -15} {6, -15}",
        invalidExprCount, incorrectProofCount, falsePredicateCount, correctProofCount, correctProofByTimeoutCount,
        noMatchingTriggerCount, expressionFinder.availableExpressions.Count);
      string executionTimesSummary = "";
      // executionTimes.Sort();
      for (int i = 0; i < executionTimes.Count; i++) {
        executionTimesSummary += $"{i}, {executionTimes[i]}\n";
      }
      await File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}/executionTimeSummary.txt",
            executionTimesSummary);

      // for (int i = 0; i < bitArrayList.Count; i++) {
      //   var ba = bitArrayList[i];
      //   Console.WriteLine("------------------------------");
      //   Console.WriteLine(i + " : " +
      //                     Printer.ExprToString(availableExpressions[i]) + " : " +
      //                     combinationResults[i].ToString());
      //   Console.WriteLine(ToBitString(ba));
      //   Console.WriteLine("------------------------------");
      // }
      // return true;

      // int correctExpressionCount = combinationResults.Count(elem => elem.Value == Result.CorrectProof);
      // if (correctExpressionCount == 0) {
      //   Console.WriteLine("Couldn't find any correct answer. Printing 0 error ones");
      //   for (int i = 0; i < availableExpressions.Count; i++) {
      //     if (combinationResults[i] == Result.InvalidExpr) {
      //       var p = dafnyMainExecutor.dafnyProcesses[i];
      //       Console.WriteLine(p.StartInfo.Arguments);
      //       Console.WriteLine(Printer.ExprToString(dafnyMainExecutor.processToExpr[p]));
      //     }
      //   }
      //   return false;
      // }
      // The 0th process represents no change to the predicate, and
      // if that is a correct predicate, it means the proof already 
      // goes through and no additional conjunction is needed.
      if (combinationResults[0] == Result.CorrectProof || combinationResults[0] == Result.CorrectProofByTimeout) {
        Console.WriteLine("proof already goes through and no additional conjunction is needed!");
        return true;
      }
      await dafnyVerifier.FinalizeCleanup();
      return true;
      List<int> correctExpressionsIndex = new List<int>();
      for (int i = 0; i < expressionFinder.availableExpressions.Count; i++) {
        if (combinationResults[i] == Result.CorrectProof || combinationResults[i] == Result.CorrectProofByTimeout)
          correctExpressionsIndex.Add(i);
      }
      // for (int i = 0; i < correctExpressionsIndex.Count; i++) {
      //   Console.WriteLine($"correct Expr #{correctExpressionsIndex[i],3}: {Printer.ExprToString(availableExpressions[correctExpressionsIndex[i]])}");
      // }
      for (int i = 0; i < correctExpressionsIndex.Count; i++) {
        for (int j = i + 1; j < correctExpressionsIndex.Count; j++) {
          {
            PrintImplies(program, desiredFunction, correctExpressionsIndex[i], correctExpressionsIndex[j]);
            PrintImplies(program, desiredFunction, correctExpressionsIndex[j], correctExpressionsIndex[i]);
          }
        }
      }
      dafnyImpliesExecutor.startAndWaitUntilAllProcessesFinishAndDumpTheirOutputs(true);
      Console.WriteLine($"{dafnyVerifier.sw.ElapsedMilliseconds / 1000}:: finish calculating implies, printing the dot graph");
      string graphVizOutput = $"digraph \"{funcName}_implies_graph\" {{\n";
      graphVizOutput += "  // The list of correct expressions\n";
      for (int i = 0; i < correctExpressionsIndex.Count; i++) {
        graphVizOutput += $"  {correctExpressionsIndex[i]} [label=\"{Printer.ExprToString(expressionFinder.availableExpressions[correctExpressionsIndex[i]].expr)}\"];\n";
      }
      graphVizOutput += "\n  // The list of edges:\n";
      foreach (var p in dafnyImpliesExecutor.dafnyProcesses) {
        var availableExprAIndex = dafnyImpliesExecutor.processToAvailableExprAIndex[p];
        var availableExprBIndex = dafnyImpliesExecutor.processToAvailableExprBIndex[p];
        // skip connecting all nodes to true
        if (Printer.ExprToString(expressionFinder.availableExpressions[availableExprAIndex].expr) == "true" ||
            Printer.ExprToString(expressionFinder.availableExpressions[availableExprBIndex].expr) == "true")
          continue;
        var output = dafnyImpliesExecutor.dafnyOutput[p];
        if (output.EndsWith("0 errors\n")) {
          Console.WriteLine($"edge from {availableExprAIndex} to {availableExprBIndex}");
          graphVizOutput += $"  {availableExprAIndex} -> {availableExprBIndex};\n";
        }
      }
      graphVizOutput += "}\n";
      await File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}graph_{funcName}_implies.dot", graphVizOutput);
      Console.WriteLine($"{dafnyVerifier.sw.ElapsedMilliseconds / 1000}:: end");
      return true;
    }

    public static string GetFullModuleName(ModuleDefinition moduleDef) {
      if (moduleDef.Name == "_module") {
        return "";
      } else if (moduleDef.EnclosingModule.Name == "_module") {
        return moduleDef.Name;
      } else {
        return GetFullModuleName(moduleDef.EnclosingModule) + "." + moduleDef.Name;
      }
    }

    public static string GetFullLemmaNameString(ModuleDefinition moduleDef, string name) {
      if (moduleDef is null) {
        return name;
      }
      foreach (var decl in ModuleDefinition.AllFunctions(moduleDef.TopLevelDecls)) {
        if (decl.ToString() == name) {
          var moduleName = GetFullModuleName(moduleDef);
          return (moduleName == "") ? name : (moduleName + "." + name);
        }
      }
      foreach (var imp in ModuleDefinition.AllDeclarationsAndNonNullTypeDecls(moduleDef.TopLevelDecls)) {
        if (imp is ModuleDecl) {
          var result = GetFullLemmaNameString((imp as ModuleDecl).Signature.ModuleDef, name);
          if (result != "") {
            return result;
          }
        }
      }
      // couldn't find the type definition here, so we should search the parent
      return "";
    }

  // public static string GetFullTypeString(ModuleDefinition moduleDef, Type type) {
  //     if (moduleDef is null) {
  //       return type.ToString();
  //     }
  //     if (type is UserDefinedType) {
  //       var udt = type as UserDefinedType;
  //       if (udt.Name == "nat" || udt.Name == "object?")
  //         return udt.ToString();
  //       foreach (var decl in moduleDef.TopLevelDecls) {
  //         if (decl.ToString() == type.ToString()) {
  //           var moduleName = GetFullModuleName(moduleDef);
  //           return (moduleName == "") ? type.ToString() : (moduleName + "." + type.ToString());
  //         }else{
  //           return udt.ToString();
  //         }
  //       }
  //       if (moduleDef.Name != "_module") {
  //         foreach (var imp in ModuleDefinition.AllDeclarationsAndNonNullTypeDecls(moduleDef.TopLevelDecls)) {
  //           if (imp is ModuleDecl) {
  //             var result = GetFullTypeString((imp as ModuleDecl).Signature.ModuleDef, type);
  //             if (result != "") {
  //               return result;
  //             }
  //           }
  //         }
  //       }
  //       // couldn't find the type definition here, so we should search the parent
  //       if (moduleDef.EnclosingModule != moduleDef) {
  //         return GetFullTypeString(moduleDef.EnclosingModule, type);
  //       } else {
  //         return type.ToString();
  //       }
  //     } else if (type is CollectionType) {
  //       var ct = type as CollectionType;
  //       var result = ct.CollectionTypeName + "<";
  //       var sep = "";
  //       foreach (var typeArg in ct.TypeArgs) {
  //         result += sep + GetFullTypeString(moduleDef, typeArg);
  //         sep = ",";
  //       }
  //       result += ">";
  //       return result;
  //     } else {
  //       return type.ToString();
  //     }
  //   }

    public static Tuple<string, string> GetFunctionParamListSpec(Function func, string namePrefix = "") {
      var funcName = func.FullDafnyName;
      string parameterNameTypes = "";
      string paramNames = "";
      var sep = "";
      foreach (var param in func.Formals) {
          parameterNameTypes += sep + namePrefix + param.Name + ":" + param.Type.ToString();

        // parameterNameTypes += sep + namePrefix + param.Name + ":" + Printer.GetFullTypeString(func.EnclosingClass.EnclosingModuleDefinition, param.Type, new HashSet<ModuleDefinition>(),true);
            // parameterNameTypes += sep + namePrefix + param.Name + ":" + Printer.GetFullTypeString(func.EnclosingClass.EnclosingModuleDefinition, param.Type, new HashSet<ModuleDefinition>());
        paramNames += sep + namePrefix + param.Name;
        sep = ", ";
      }
      return new Tuple<string, string>(paramNames, parameterNameTypes);
    }

    public static Tuple<string, string> GetFunctionParamList(Function func, string namePrefix = "") {
      var funcName = func.FullDafnyName;
      string parameterNameTypes = "";
      string paramNames = "";
      var sep = "";
      foreach (var param in func.Formals) {
        // parameterNameTypes += sep + namePrefix + param.Name + ":" + Printer.GetFullTypeString(func.EnclosingClass.EnclosingModuleDefinition, param.Type, new HashSet<ModuleDefinition>(),true);
            parameterNameTypes += sep + namePrefix + param.Name + ":" + Printer.GetFullTypeString(func.EnclosingClass.EnclosingModuleDefinition, param.Type, new HashSet<ModuleDefinition>());
        paramNames += sep + namePrefix + param.Name;
        sep = ", ";
      }
      return new Tuple<string, string>(paramNames, parameterNameTypes);
    }

    public static Function GetFunction(Program program, string funcName) {
      foreach (var kvp in program.ModuleSigs) {
        foreach (var topLevelDecl in ModuleDefinition.AllFunctions(kvp.Value.ModuleDef.TopLevelDecls)) {
          if (topLevelDecl.FullDafnyName == funcName) {
            return topLevelDecl;
          }
        }
      }
      return null;
    }

    public static Function GetFunctionFromModuleDef(ModuleDefinition moduleDef, string funcName) {
      foreach (var topLevelDecl in moduleDef.TopLevelDecls) {
        if (topLevelDecl is ClassDecl) {
          var cd = topLevelDecl as ClassDecl;
          foreach (var member in cd.Members) {
            if ($"{cd.FullDafnyName}.{member.Name}" == funcName) {
              return member as Function;
            }
          }
        }
        if(topLevelDecl is DatatypeDecl)
        {
          var cd = topLevelDecl as DatatypeDecl;
          foreach (var member in cd.Members) {
            if ($"{cd.FullDafnyName}.{member.Name}" == funcName) {
              return member as Function;
            }
          }
        }
      }
      return null;
    }
    public static Method GetMethodFromModuleDef(ModuleDefinition moduleDef, string methName) {
      foreach (var topLevelDecl in moduleDef.TopLevelDecls) {
        if (topLevelDecl is ClassDecl) {
          var cd = topLevelDecl as ClassDecl;
          foreach (var member in cd.Members) {
            if ($"{cd.FullDafnyName}.{member.Name}" == methName) {
              return member as Method;
            }
          }
        }
        if(topLevelDecl is DatatypeDecl)
        {
          var cd = topLevelDecl as DatatypeDecl;
          foreach (var member in cd.Members) {
            if ($"{cd.FullDafnyName}.{member.Name}" == methName) {
              return member as Method;
            }
          }
        }
      }
      return null;
    }
    public static Function GetFunctionFromUnresolved(Program program, string funcName) {
      int index = funcName.IndexOf('.');
      string moduleName = funcName.Remove(index);
      foreach (var topLevelDecl in program.DefaultModuleDef.TopLevelDecls) {
        if (topLevelDecl.FullDafnyName == moduleName) {
          var lmd = topLevelDecl as LiteralModuleDecl;
          var func = GetFunctionFromModuleDef(lmd.ModuleDef, funcName);
          if (func != null) {
            return func;
          }
        }
      }
      return null;
    }

    public void DuplicateAllFiles(Program program, string workingDir, int cnt) {
      if (System.IO.Directory.Exists(workingDir)) {
        System.IO.Directory.Delete(workingDir, true);
      }
      System.IO.Directory.CreateDirectory(workingDir);
      var samples = new List<string>();
      samples.Add(includeParser.Normalized(program.FullName));
      System.IO.Directory.CreateDirectory(Path.GetDirectoryName($"{workingDir}/{samples[0]}"));
      File.Copy(program.FullName, $"{workingDir}/{samples[0]}", true);
      foreach (var file in program.DefaultModuleDef.Includes) {
        samples.Add(includeParser.Normalized(file.CanonicalPath));
      }
      for (int i = 1; i < samples.Count; i++) {
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName($"{workingDir}/{samples[i]}"));
        File.Copy(program.DefaultModuleDef.Includes[i - 1].CanonicalPath, $"{workingDir}/{samples[i]}", true);
      }
    }

    public void PrintExprAndCreateProcessLemma(Program program, Program proofProg,Function func, Lemma lemma,string moduleName,ExpressionFinder.ExpressionDepth expr, int cnt, bool includeProof,bool isWeaker, bool vacTest,Function mutationRootFn) {
      bool runOnce = DafnyOptions.O.HoleEvaluatorRunOnce;
      Console.WriteLine("Mutation -> " + $"{cnt}" + ": " + $"{Printer.ExprToString(expr.expr)}");
      var funcName = func.Name;

      string lemmaForExprValidityString = ""; // remove validityCheck
      string basePredicateString = GetBaseLemmaList(func,null);
      string isStrongerLemma = "";
      string isWeakerLemma = "";
      string mutationBaseString = ""; 

        // isWeakerLemma = GetIsWeakerMutationsRoot(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, null,true);
         mutationBaseString = GetBaseLemmaListMutationList(program,func,null,MutationsPaths[0]);

      if(expr.expr is QuantifierExpr){
        isStrongerLemma = GetIsStronger(func,Paths[0], null, null,true);
        isWeakerLemma = GetIsWeakerMutationsRoot(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, null,true);
      }else{
        isStrongerLemma = GetIsStronger(func,Paths[0], null, null,false);
        isWeakerLemma = GetIsWeakerMutationsRoot(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, null,true);
      }

      int lemmaForExprValidityPosition = 0;
      int lemmaForExprValidityStartPosition = 0;
      int basePredicatePosition = 0;
      int basePredicateStartPosition = 0;
      var workingDir = $"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}/{funcName}_{cnt}";
      if (tasksList == null)
      {
        string code = "";
        string proofCode = "";
        using (var wr = new System.IO.StringWriter()) {
          var pr = new Printer(wr, DafnyOptions.PrintModes.NoIncludes);
          pr.UniqueStringBeforeUnderscore = UnderscoreStr;
          // if (expr.HasCardinality) {
          //   func.Body = Expression.CreateAnd(expr, func.Body);
          // } else {
          //   func.Body = Expression.CreateAnd(func.Body, expr);
          // }
          func.Body = expr.expr; // Replace Whole Body
          // lemma.Body = new Block;
      //     Console.WriteLine("--Function is Mutated--");
      // using (var wr1 = new System.IO.StringWriter()) {
      //   var pr1 = new Printer(wr);
      //   pr1.PrintFunction(func, 0,false);
      //   Console.WriteLine(wr1.ToString());
      // }
      var includesList = "";
      foreach (var q in program.DefaultModuleDef.Includes)
      {
        // Console.WriteLine(includeParser.Normalized(q.IncludedFilename));
        // includesList += "include \"" +includeParser.Normalized(q.IncludedFilename) + "\"\n";
        includesList += "include \"" +includeParser.NormalizedTo(program.FullName,q.IncludedFilename) + "\"\n";

      }

          pr.PrintProgram(program, true);
          // Console.WriteLine("----\n" + Printer.ModuleDefinitionToString(program.DefaultModuleDef,DafnyOptions.PrintModes.Everything));
          // pr.PrintModuleDefinition(program.DefaultModuleDef, program.DefaultModuleDef.VisibilityScope, 0, null, null);
          code = $"// #{cnt}\n";
          code += $"// {Printer.ExprToString(expr.expr)}\n" + includesList + Printer.ToStringWithoutNewline(wr) + "\n\n";
          
          int fnIndex = code.IndexOf("predicate " + funcName);
          // code = code.Insert(fnIndex-1,basePredicateString+"\n");
            code = code.Insert(fnIndex-1,mutationBaseString+"\n");

          if(!includeProof){
            if(moduleName != null){
              // comment out entire module "assume this is last module"! 
              int moduleLoc = code.IndexOf("module " +moduleName);
              code = code.Insert(moduleLoc-1,"/*"+"\n");
              code = code + "/*";
            }else{
              
            }
          }
          if(isWeaker){
            fnIndex = code.IndexOf("predicate " + funcName);
            code = code.Insert(fnIndex-1,isWeakerLemma+"\n");
          }
          

            if((vacTest)){
            string revisedVac = getVacuityLemmaRevised(func,Paths[0], null, null,false);
            if(func.WhatKind == "predicate"){
              fnIndex = code.IndexOf("predicate " + funcName);
            }else{
              fnIndex = code.IndexOf("function " + funcName);
            }
            code = code.Insert(fnIndex-1,revisedVac+"\n");

          }
          


          // Console.WriteLine(code.IndexOf("lemma isSame_"+funcName));
          
          // Console.WriteLine(code.IndexOf("lemma isSame_"+funcName));
          if (DafnyOptions.O.HoleEvaluatorCreateAuxFiles){
            if(isWeaker)
            {
              File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{funcName}_weaker_{cnt}.dfy", code);
            }else{
              File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{funcName}_{cnt}.dfy", code);
            }
          }
        }
        string env = DafnyOptions.O.Environment.Remove(0, 22);
        var argList = env.Split(' ');
        List<string> args = new List<string>();

        foreach (var arg in argList) {
          if (!arg.EndsWith(".dfy") && !arg.StartsWith("/mutationTarget") && arg.StartsWith("/")&& !arg.StartsWith("/proofName") && !arg.StartsWith("/mutationsFromParams") ) {
            args.Add(arg);
///mutationsFromParams
          }
        }
         if(isWeaker){
          args.Add("/proc:*isAtLeastAsWeak*");
         }else if(includeProof && moduleName == null){
          // args.Add("/proc:*" + lemma.Name +"*");
         }
        // args.Add("/proc:*" + lemma.CompileName );

        var changingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);

        var constraintFuncChangingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);

        var remoteFolderPath = dafnyVerifier.DuplicateAllFiles(cnt, changingFilePath);

        // var remoteFolderPath = dafnyVerifier.DuplicateAllFiles(cnt, changingFilePath.Split("/").Last());
        // Console.WriteLine("Remote = " + remoteFolderPath);
        args.Add("/compile:0");
      //  List<ProofEvaluator.ExprStmtUnion> exprStmtList = new List<ProofEvaluator.ExprStmtUnion>();
      //   dafnyVerifier.runDafnyProofCheck(code,args,exprStmtList,0);
        dafnyVerifier.runDafny(code, args,
            expr, cnt, lemmaForExprValidityPosition, lemmaForExprValidityStartPosition,$"{remoteFolderPath.Path}/{constraintFuncChangingFilePath}");
       
      }
      else
      {
        DuplicateAllFiles(program, workingDir, cnt);

        Expression newFuncBody = null;
        if (expr.expr.HasCardinality) {
          newFuncBody = Expression.CreateAnd(expr.expr, func.Body);
        } else {
          newFuncBody = Expression.CreateAnd(func.Body, expr.expr);
        }
        var baseCode = File.ReadAllLines(func.BodyStartTok.Filename);
        if (func.BodyStartTok.line == func.BodyEndTok.line) {
          baseCode[func.BodyStartTok.line - 1] = baseCode[func.BodyStartTok.line - 1].Remove(func.BodyStartTok.col, func.BodyEndTok.col - func.BodyStartTok.col);
          baseCode[func.BodyStartTok.line - 1] = baseCode[func.BodyStartTok.line - 1].Insert(func.BodyStartTok.col + 1, Printer.ExprToString(newFuncBody));
        }
        else {
          baseCode[func.BodyStartTok.line - 1] = baseCode[func.BodyStartTok.line - 1].Remove(func.BodyStartTok.col);
          for (int i = func.BodyStartTok.line; i < func.BodyEndTok.line - 1; i++) {
            baseCode[i] = "";
          }
          baseCode[func.BodyEndTok.line - 1] = baseCode[func.BodyEndTok.line - 1].Remove(0, func.BodyEndTok.col - 1);
          baseCode[func.BodyStartTok.line - 1] = baseCode[func.BodyStartTok.line - 1].Insert(func.BodyStartTok.col, Printer.ExprToString(newFuncBody));
        }
        lemmaForExprValidityStartPosition = baseCode.Length;
        baseCode = baseCode.Append(lemmaForExprValidityString).ToArray();
        lemmaForExprValidityPosition = baseCode.Length;
        var newCode = String.Join('\n', baseCode);
        File.WriteAllTextAsync($"{workingDir}/{includeParser.Normalized(func.BodyStartTok.Filename)}", newCode);
      }
    }

public string ReplaceFirst(int pos, string text, string search, string replace)
{
  // int pos = text.IndexOf(search);
  if (pos < 0)
  {
    return text;
  }
  return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
}


public void MutationClassification(Program program, Program proofProg,Function func, Lemma lemma,string moduleName,int firstCnt,int sndCnt)
{
  string mutatedPredA = "";
  string mutatedPredB = "";
  string lemmaName = "";
   if(DafnyOptions.O.IsStateMachine)
  {
    mutatedPredA = GetBaseLemmaListArbitMutation(program,func,null,MutationsPaths[0].GetRange(MutationsPaths[0].Count-1,1),expressionFinder.availableExpressions[firstCnt],"a");
  mutatedPredB = GetBaseLemmaListArbitMutation(program,func,null,MutationsPaths[0].GetRange(MutationsPaths[0].Count-1,1),expressionFinder.availableExpressions[sndCnt],"b");
  lemmaName = GetIsWeakerClassificationLemma(func,MutationsPaths[0].GetRange(MutationsPaths[0].Count-1,1), null, null,"a_", "b_");
  }else{
  mutatedPredA = GetBaseLemmaListArbitMutation(program,func,null,MutationsPaths[0],expressionFinder.availableExpressions[firstCnt],"a");
  mutatedPredB = GetBaseLemmaListArbitMutation(program,func,null,MutationsPaths[0],expressionFinder.availableExpressions[sndCnt],"b");
  lemmaName = GetIsWeakerClassificationLemma(func,MutationsPaths[0], null, null,"a_", "b_");
  }
// if()MutationsPaths[0].GetRange(MutationsPaths[0].Count-1,1)
  // mutatedPredA = GetBaseLemmaListArbitMutation(program,func,null,MutationsPaths[0],expressionFinder.availableExpressions[firstCnt],"a");
  // mutatedPredB = GetBaseLemmaListArbitMutation(program,func,null,MutationsPaths[0],expressionFinder.availableExpressions[sndCnt],"b");
  // lemmaName = GetIsWeakerClassificationLemma(func,MutationsPaths[0], null, null,"a_", "b_");
  string code = "";

  using (var wr = new System.IO.StringWriter()) {
    var pr = new Printer(wr, DafnyOptions.PrintModes.NoIncludes);
    pr.UniqueStringBeforeUnderscore = UnderscoreStr;
    // func.Body = e xpr.expr; // Replace Whole Body

    var includesList = "";
    foreach (var q in program.DefaultModuleDef.Includes)
    {
      includesList += "include \"" +includeParser.NormalizedTo(program.FullName,q.IncludedFilename) + "\"\n";

    }     
    var funcName = func.Name;

    pr.PrintProgram(program, true);
    code = $"// #{firstCnt}_{sndCnt}\n";
    code += $"// \n" + includesList + Printer.ToStringWithoutNewline(wr) + "\n\n";
      
    int fnIndex = -1;
    if(func.WhatKind == "predicate"){
      fnIndex = code.IndexOf("predicate " + funcName + "(");
      if(fnIndex == -1)
      {
        fnIndex = code.IndexOf("predicate " + funcName);
      }
      if(func.IsGhost && fnIndex == -1)//&& fnIndex == -1
      {
        fnIndex = code.IndexOf("ghost predicate " + funcName);
      }
    }else{
      Console.WriteLine("TYPE = " + func.WhatKind);
      fnIndex = code.IndexOf("function " + funcName + "(");
    }
    // code = code.Insert(fnIndex-1,basePredicateString+"\n");
    code = code.Insert(fnIndex-1,mutatedPredA+"\n" + mutatedPredB +"\n" + lemmaName + "\n");
    if (DafnyOptions.O.HoleEvaluatorCreateAuxFiles)
      File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{funcName}_classify_{firstCnt}_{sndCnt}.dfy", code);
  }

      string env = DafnyOptions.O.Environment.Remove(0, 22);
      var argList = env.Split(' ');
      List<string> args = new List<string>();

      foreach (var arg in argList) {
        if (!arg.EndsWith(".dfy") && !arg.StartsWith("/mutationTarget") && arg.StartsWith("/")&& !arg.StartsWith("/proofName") && !arg.StartsWith("/mutationsFromParams") ) {
          args.Add(arg);
        }
      }

      args.Add("/proc:*isAtLeastAsWeak*");
      var changingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);
      var constraintFuncChangingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);
      var remoteFolderPath = dafnyVerifier.DuplicateAllFiles(firstCnt, changingFilePath);

      args.Add("/compile:0");
      dafnyVerifier.runDafny(code, args,
            expressionFinder.availableExpressions[firstCnt], firstCnt, 0, 0,$"{remoteFolderPath.Path}/{constraintFuncChangingFilePath}");
}   

public void PrintExprAndCreateProcessLemmaSeperateProof(Program program, Program proofProg,Function func, Lemma lemma,string moduleName,ExpressionFinder.ExpressionDepth expr, int cnt, bool includeProof,bool isWeaker, bool vacTest,Function mutationRootFn) {
      
      if (!includeProof){
      bool runOnce = DafnyOptions.O.HoleEvaluatorRunOnce;
      Console.WriteLine("Mutation -> " + $"{cnt}" + ": " + $"{Printer.ExprToString(expr.expr)}");
      var funcName = func.Name;
      string basePredicateString = "";
      string mutationBaseString = "";    
      string lemmaForExprValidityString = ""; // remove validityCheck
      string isStrongerLemma = "";
      string isWeakerLemma = "";
            // string mutationBaseString = ""; 

      if(constraintExpr == null)
      {
         basePredicateString = GetBaseLemmaList(func,null);
         if(DafnyOptions.O.IsStateMachine)
         {
          mutationBaseString = GetBaseLemmaListMutationList(program,func,null,MutationsPaths[0].GetRange(MutationsPaths[0].Count-1,1));

         }else{
          mutationBaseString = GetBaseLemmaListMutationList(program,func,null,MutationsPaths[0]);

         }
       if(expr.expr is QuantifierExpr){
        isStrongerLemma = GetIsStronger(func,Paths[0], null, null,true);
        isWeakerLemma = GetIsWeakerMutationsRoot(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, null,true);
      }else{
        isStrongerLemma = GetIsStronger(func,Paths[0], null, null,false);
        if(DafnyOptions.O.IsStateMachine || DafnyOptions.O.IsRequires)
         {
          isWeakerLemma = GetIsWeakerMutationsRoot(func,MutationsPaths[0].GetRange(MutationsPaths[0].Count-1,1), null, null,false);//GetIsWeaker(func,Paths[0], null, null,false);
         }else{
                 isWeakerLemma = GetIsWeakerMutationsRootFull(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, constraintExpr.expr,false);

        // isWeakerLemma = GetIsWeakerMutationsRoot(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, null,false);
        }
      }
      }else{
         basePredicateString = GetBaseLemmaList(func,null);
         mutationBaseString = GetBaseLemmaListMutationList(program,func,null,MutationsPaths[0]);
         if(expr.expr is QuantifierExpr){
        isStrongerLemma = GetIsStronger(func,Paths[0], null, constraintExpr.expr,true);
        isWeakerLemma = GetIsWeakerMutationsRoot(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, constraintExpr.expr,true);
      }else{
        //  var ee  = GetIsWeakerMutationsRootFull(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, null,false);
        isStrongerLemma = GetIsStronger(func,Paths[0], null, constraintExpr.expr,false);
        isWeakerLemma = GetIsWeakerMutationsRootFull(mutationRootFn,MutationsPaths[0], null, null,false);//GetIsWeaker(func,Paths[0], null, constraintExpr.expr,false);
      }
      }
    
      // if(cnt == 0)
      // {
      //   Console.WriteLine(getVacuityLemmaRevised(func,Paths[0], null, constraintExpr,false));
      // }

      // if(expr.expr is QuantifierExpr){
      //   isStrongerLemma = GetIsStronger(func,Paths[0], null, constraintExpr.expr,true);
      //   isWeakerLemma = GetIsWeaker(func,Paths[0], null, constraintExpr.expr,true);
      // }else{
      //   isStrongerLemma = GetIsStronger(func,Paths[0], null, constraintExpr.expr,false);
      //   isWeakerLemma = GetIsWeaker(func,Paths[0], null, constraintExpr.expr,false);
      // }

      int lemmaForExprValidityPosition = 0;
      int lemmaForExprValidityStartPosition = 0;
      int basePredicatePosition = 0;
      int basePredicateStartPosition = 0;
      var workingDir = $"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}/{funcName}_{cnt}";
      if (tasksList == null)
      {
        string code = "";
        string proofCode = "";
        using (var wr = new System.IO.StringWriter()) {
          var pr = new Printer(wr, DafnyOptions.PrintModes.NoIncludes);
          pr.UniqueStringBeforeUnderscore = UnderscoreStr;
          // if(DafnyOptions.O.LocalPredicateMutation)
          // {
          //   Console.WriteLine("--Function is Mutated--");
          //   using (var wr1 = new System.IO.StringWriter()) {
          //     var pr1 = new Printer(wr1);
          //     pr1.PrintFunction(func, 0,false);
          //     var origFunc = wr1.ToString();
          //     Console.WriteLine(origFunc);
          //   }
          // }
          func.Body = expr.expr; // Replace Whole Body
          // lemma.Body = new Block;
        //     Console.WriteLine("--Function is Mutated--");
        // using (var wr1 = new System.IO.StringWriter()) {
        //   var pr1 = new Printer(wr);
        //   pr1.PrintFunction(func, 0,false);
        //   Console.WriteLine(wr1.ToString());
        // }
        // Console.WriteLine("--------------");
         var includesList = "";
      foreach (var q in program.DefaultModuleDef.Includes)
      {
        // Console.WriteLine(includeParser.Normalized(q.IncludedFilename));
        includesList += "include \"" +includeParser.NormalizedTo(program.FullName,q.IncludedFilename) + "\"\n";
        // var test = includeParser.NormalizedTo(program.FullName,q.IncludedFilename);

      }

          pr.PrintProgram(program, true);
          code = $"// #{cnt}\n";
          code += $"// {Printer.ExprToString(expr.expr)}\n" + includesList + Printer.ToStringWithoutNewline(wr) + "\n\n";
          
          int fnIndex = -1;
          if(func.WhatKind == "predicate"){
            fnIndex = code.IndexOf("predicate " + funcName + "(");
            // if(DafnyOptions.O.LocalPredicateMutation)
            // {
            // code = ReplaceFirst(code.IndexOf(funcName,fnIndex),code,funcName,funcName+"mutated");

            // }
            if(fnIndex == -1)
            {
              fnIndex = code.IndexOf("predicate " + funcName);
            }
            if(func.IsGhost && fnIndex == -1)//&& fnIndex == -1
            {
              fnIndex = code.IndexOf("ghost predicate " + funcName);
            }
          }else{
            Console.WriteLine("TYPE = " + func.WhatKind);
            fnIndex = code.IndexOf("function " + funcName + "(");
          }
          // code = code.Insert(fnIndex-1,basePredicateString+"\n");
          code = code.Insert(fnIndex-1,mutationBaseString+"\n");
          // if(!includeProof){
          //   if(moduleName != null){
          //     // comment out entire module "assume this is last module"! 
          //     int moduleLoc = code.IndexOf("module " +moduleName);
          //     code = code.Insert(moduleLoc-1,"/*"+"\n");
          //     code = code + "/*";
          //   }else{
          //     //Comment out single 'proof' lemma
          //     Console.WriteLine("=----> " + lemma.Name);
          //     int lemmaLoc = code.IndexOf("lemma " +lemma.Name);
          //     code = code.Insert(lemmaLoc-1,"/*"+"\n");
          //     code = code.Insert(code.IndexOf("}\n\n",lemmaLoc)-1,"*/"+"\n");
          //   }
          // }
          if(isWeaker){
            if(func.WhatKind == "predicate"){
              fnIndex = code.IndexOf("predicate " + funcName);
              if(func.IsGhost && fnIndex == -1)//&& fnIndex == -1
              {
                fnIndex = code.IndexOf("ghost predicate " + funcName);
              }
            }else{
              fnIndex = code.IndexOf("function " + funcName);
            }
            code = code.Insert(fnIndex-1,isWeakerLemma+"\n");
          }
          if((vacTest)){
            // string revisedVac = getVacuityLemmaRevised(func,Paths[0], null, constraintExpr.expr,false);
            string revisedVac = getVacuityLemmaRevised(func,Paths[0], null,null,false);
            if(func.WhatKind == "predicate"){
              fnIndex = code.IndexOf("predicate " + funcName);
            }else{
              fnIndex = code.IndexOf("function " + funcName);
            }
            code = code.Insert(fnIndex-1,revisedVac+"\n");
            // int lemmaLoc = code.IndexOf("lemma " +lemma.Name);
            // int lemmaLocEns = code.IndexOf("{",lemmaLoc);
            // code = code.Insert(lemmaLocEns-1,"ensures false;\n");
          }
          
          if (DafnyOptions.O.HoleEvaluatorCreateAuxFiles)
            File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{funcName}_{cnt}.dfy", code);
        }
        string env = DafnyOptions.O.Environment.Remove(0, 22);
        var argList = env.Split(' ');
        List<string> args = new List<string>();

        foreach (var arg in argList) {
          if (!arg.EndsWith(".dfy") && !arg.StartsWith("/mutationTarget") && arg.StartsWith("/")&& !arg.StartsWith("/proofName") && !arg.StartsWith("/mutationsFromParams") ) {
            args.Add(arg);
          }
        }
         if(isWeaker){
          args.Add("/proc:*isAtLeastAsWeak*");
         }else if(includeProof && moduleName == null){
          // args.Add("/proc:*" + lemma.Name +"*");
         }
        var changingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);

        var constraintFuncChangingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);
        var remoteFolderPath = dafnyVerifier.DuplicateAllFiles(cnt, changingFilePath);

        args.Add("/compile:0");
        dafnyVerifier.runDafny(code, args,
            expr, cnt, lemmaForExprValidityPosition, lemmaForExprValidityStartPosition,$"{remoteFolderPath.Path}/{constraintFuncChangingFilePath}");
       
      }
      else
      {
        DuplicateAllFiles(program, workingDir, cnt);

        Expression newFuncBody = null;
        if (expr.expr.HasCardinality) {
          newFuncBody = Expression.CreateAnd(expr.expr, func.Body);
        } else {
          newFuncBody = Expression.CreateAnd(func.Body, expr.expr);
        }
        var baseCode = File.ReadAllLines(func.BodyStartTok.Filename);
        if (func.BodyStartTok.line == func.BodyEndTok.line) {
          baseCode[func.BodyStartTok.line - 1] = baseCode[func.BodyStartTok.line - 1].Remove(func.BodyStartTok.col, func.BodyEndTok.col - func.BodyStartTok.col);
          baseCode[func.BodyStartTok.line - 1] = baseCode[func.BodyStartTok.line - 1].Insert(func.BodyStartTok.col + 1, Printer.ExprToString(newFuncBody));
        }
        else {
          baseCode[func.BodyStartTok.line - 1] = baseCode[func.BodyStartTok.line - 1].Remove(func.BodyStartTok.col);
          for (int i = func.BodyStartTok.line; i < func.BodyEndTok.line - 1; i++) {
            baseCode[i] = "";
          }
          baseCode[func.BodyEndTok.line - 1] = baseCode[func.BodyEndTok.line - 1].Remove(0, func.BodyEndTok.col - 1);
          baseCode[func.BodyStartTok.line - 1] = baseCode[func.BodyStartTok.line - 1].Insert(func.BodyStartTok.col, Printer.ExprToString(newFuncBody));
        }
        lemmaForExprValidityStartPosition = baseCode.Length;
        baseCode = baseCode.Append(lemmaForExprValidityString).ToArray();
        lemmaForExprValidityPosition = baseCode.Length;
        var newCode = String.Join('\n', baseCode);
        File.WriteAllTextAsync($"{workingDir}/{includeParser.Normalized(func.BodyStartTok.Filename)}", newCode);
      }
    }else{
      Console.WriteLine("Mutation(proof) -> " + $"{cnt}" + ": " + $"{Printer.ExprToString(expr.expr)}");
      string code = "";
      string proofCode = "";
      string validityCheck = "";
      string localMutationStr = GetLocalMutationList(program,func,null,MutationsPaths[0]);   

      if(vacTest)
      {
        validityCheck = GetValidityLemma(Paths[0], null, constraintExpr == null ? null : constraintExpr.expr, -1,0);
        // Console.WriteLine(validityCheck);
      }
      var lemmaName = lemma.Name;
      var funcName = func.Name;

      int lemmaForExprValidityPosition = 0;
      int lemmaForExprValidityStartPosition = 0;
      int basePredicatePosition = 0;
      int basePredicateStartPosition = 0;
      var workingDir = $"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}/{lemmaName}_{cnt}";
      var origFuncStr = "";
      var origFuncName = func.Name;
      if(!vacTest && DafnyOptions.O.LocalPredicateMutation)
          {
            // Console.WriteLine("--Local Mutation is Mutated--");
            using (var wr1 = new System.IO.StringWriter()) {
              var pr1 = new Printer(wr1);
              pr1.PrintFunction(func, 0,false);
              origFuncStr = wr1.ToString();
              // Console.WriteLine(origFuncStr);
              func.Name = "MUTATED_"+func.Name;
            }
          }
      // apply mutation to spec //
      using (var wrSpec = new System.IO.StringWriter()) {
        var prSpec = new Printer(wrSpec, DafnyOptions.PrintModes.NoIncludes);
        prSpec.UniqueStringBeforeUnderscore = UnderscoreStr;
        func.Body = expr.expr;
          
        var includesList = "";
        foreach (var q in program.DefaultModuleDef.Includes)
        {
          var test = includeParser.NormalizedTo(program.FullName,q.IncludedFilename);
          // Console.WriteLine(includeParser.Normalized(q.IncludedFilename));
          includesList += "include \"" +test + "\"\n";

        }

          prSpec.PrintProgram(program, true);
          code += $"// #{cnt}\n";
          code += $"// {Printer.ExprToString(expr.expr)}\n" + Printer.ToStringWithoutNewline(wrSpec) + "\n\n";
           int moduleIndex = code.IndexOf("module ");
          //  code = code.Insert(moduleIndex-1,"*//");
           code = code.Insert(moduleIndex,includesList);
           if(!vacTest && DafnyOptions.O.LocalPredicateMutation)
           {
            // add original function back
            var fnIndex = code.IndexOf("predicate " +func.Name);
            code = code.Insert(fnIndex-1,origFuncStr);
            fnIndex = code.IndexOf("predicate " + funcName);
            code = code.Insert(fnIndex-1,localMutationStr);
            func.Name = origFuncName;
           }
          // Console.WriteLine("code = \n" + code);
          var changingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);
          var constraintFuncChangingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);
          // var remoteFolderPath = dafnyVerifier.DuplicateAllFiles(cnt, changingFilePath);
          // var remoteFilePath = dafnyVerifier.baseFoldersPath[cnt % dafnyVerifier.serversList.Count].Path;
          var serverIndex = cnt % dafnyVerifier.serversList.Count;
          var localizedCntIndex = (cnt-serverIndex)/dafnyVerifier.serversList.Count;
          var remoteFilePath = dafnyVerifier.getTempFilePath()[serverIndex][localizedCntIndex+1].Path;
          dafnyVerifier.WriteToRemoteFile(code, cnt, $"{remoteFilePath}/{constraintFuncChangingFilePath}");
          
          if (DafnyOptions.O.HoleEvaluatorCreateAuxFiles)
            File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{funcName}NotAtLeastAsWeak_{cnt}.dfy", code);
    }
        // end add mutation
       HashSet<string> includesSet = new HashSet<string>();

        using (var wr = new System.IO.StringWriter()) {
          var pr = new Printer(wr, DafnyOptions.PrintModes.NoIncludes);
          pr.UniqueStringBeforeUnderscore = UnderscoreStr;
          // func.Body = expr.expr;
        var constraintFuncChangingFilePathLemmaTest = "";
      if(lemma.BodyStartTok.Filename == null)
      {
         constraintFuncChangingFilePathLemmaTest = includeParser.Normalized(proofProg.FullName);

      }else{
         constraintFuncChangingFilePathLemmaTest = includeParser.Normalized(lemma.BodyStartTok.Filename);
     
     }
      // var constraintFuncChangingFilePathLemmaTestList = path.Split('/').ToList();
        var includesList = "";
      foreach (var q in proofProg.DefaultModuleDef.Includes)
      {
        // Console.WriteLine(includeParser.Normalized(q.IncludedFilename));
        var includesPath = includeParser.Normalized(q.IncludedFilename);
        var normalizedIncludesPath = includeParser.NormalizedTo(proofProg.FullName,q.IncludedFilename);

        var constraintFuncChangingFilePathLemmaTestList = constraintFuncChangingFilePathLemmaTest.Split('/').ToList();

        var includesPathList = includesPath.Split('/').ToList();
        // if(includesPath)
        var minLen = Math.Min(includesPathList.Count,constraintFuncChangingFilePathLemmaTestList.Count);
        var normalizedval = 0;
        for (int i = 0; i<minLen;i++){
          if(includesPathList[i] == constraintFuncChangingFilePathLemmaTestList[i]){
            // includesPathList.RemoveAt(i);
            // constraintFuncChangingFilePathLemmaTestList.RemoveAt(i);
            normalizedval++;
          }
        }
        if(normalizedval > 0){
          for(int i = 0; i < normalizedval;i++)
          {
            includesPathList.RemoveAt(0);
          }
        }
        // includesPath = String.Join('/', includesPathList);
        // includesList += "include \"" +includesPath + "\"\n";
        //only add includes if it is unique
        if(!includesSet.Contains(normalizedIncludesPath)){
          includesList += "include \"" +normalizedIncludesPath + "\"\n";
          includesSet.Add(normalizedIncludesPath);
        }
          // includesList += "include \"" +normalizedIncludesPath + "\"\n";


      }


          pr.PrintProgram(proofProg, true);
          code = $"// #{cnt}\n";
          code += $"// {Printer.ExprToString(expr.expr)}\n" +includesList +"/*"+ Printer.ToStringWithoutNewline(wr) + "\n\n";
          // Console.WriteLine("code = \n" + code);
          int moduleIndex = code.IndexOf("module ");
           code = code.Insert(moduleIndex-1,"*/");

           if(!vacTest && DafnyOptions.O.LocalPredicateMutation)
           {
            //modify lemma to call mutated predicate
            // foreach (var e in lemma.Ens){
            //    Console.WriteLine(e);
            // }
            var localMutatedLemmaStr = "";
            int lemmaLoc = code.IndexOf("lemma " +lemma.Name);
            var origLemmaName = lemma.Name;
            using (var wr1 = new System.IO.StringWriter()) {
              var pr1 = new Printer(wr1);
              lemma.Name = lemma.Name + "_MUTATED";
              pr1.PrintMethod(lemma, 0,false);
              localMutatedLemmaStr = wr1.ToString();
              lemma.Name = origLemmaName;
              // Console.WriteLine(origFuncStr);
              if(!(Printer.ExprToString(expr.expr) == "false") && localMutatedLemmaStr.Contains(mutationRootFn.FullDafnyName)){
                var lhsName = mutationRootFn.FullDafnyName.Substring(0,mutationRootFn.FullDafnyName.LastIndexOf("."));
                localMutatedLemmaStr = localMutatedLemmaStr.Replace(mutationRootFn.FullDafnyName+"(",lhsName +".MUTATED_"+mutationRootFn.Name+"(");
              }else{
                localMutatedLemmaStr = localMutatedLemmaStr.Replace(mutationRootFn.Name+"(","MUTATED_"+mutationRootFn.Name+"(");

              }
            }
            
            code = code.Insert(lemmaLoc-1,localMutatedLemmaStr);

           }
        //   var changingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);
        // var constraintFuncChangingFilePath = includeParser.Normalized(func.BodyStartTok.Filename);
        // var remoteFolderPath = dafnyVerifier.DuplicateAllFiles(cnt, changingFilePath);
        //   dafnyVerifier.WriteToRemoteFile("test", 0, $"{remoteFolderPath.Path}/{constraintFuncChangingFilePath}");
        //   int fnIndex = code.IndexOf("predicate " + funcName);
        //   int fnIndex1 = code.IndexOf("{",fnIndex);

        //   code = code.Insert(fnIndex1+1,Printer.ExprToString(expr.expr)+$"/*\n");
        //             int fnIndex2 = code.IndexOf("}",fnIndex1);

        //   code = code.Insert(fnIndex2-1,$"*/ \n");
            // Console.WriteLine(code);

          if((vacTest && includeProof)){
            int lemmaLoc = code.IndexOf("lemma " +lemma.Name);
            int lemmaLocEns = code.IndexOf("{",lemmaLoc);
            // code = code.Insert(lemmaLocEns-1,"ensures false;\n");
            code = code.Insert(lemmaLoc-1,validityCheck+"\n");
          }

          if (DafnyOptions.O.HoleEvaluatorCreateAuxFiles && vacTest)
            File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{lemmaName}_VAC_{cnt}.dfy", code);
          else if(DafnyOptions.O.HoleEvaluatorCreateAuxFiles)
            File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{lemmaName}_{cnt}.dfy", code);
        }
         string env = DafnyOptions.O.Environment.Remove(0, 22);
        var argList = env.Split(' ');
        List<string> args = new List<string>();

        foreach (var arg in argList) {
          if (!arg.EndsWith(".dfy") && !arg.StartsWith("/mutationTarget") && arg.StartsWith("/")&& !arg.StartsWith("/proofName") && !arg.StartsWith("/mutationsFromParams") ) {
            args.Add(arg);
          }
        }
     
        args.Add("/compile:0");
        // args.Add("/exitAfterFirstError");
         var changingFilePathLemma = "";
         var constraintFuncChangingFilePathLemma = "";
      if(lemma.BodyStartTok.Filename == null)
      {
         changingFilePathLemma = includeParser.Normalized(proofProg.FullName);
         constraintFuncChangingFilePathLemma = includeParser.Normalized(proofProg.FullName);

      }else{
         changingFilePathLemma = includeParser.Normalized(lemma.BodyStartTok.Filename);
          constraintFuncChangingFilePathLemma = includeParser.Normalized(lemma.BodyStartTok.Filename);
     }

          // var changingFilePathLemma = includeParser.Normalized(lemma.BodyStartTok.Filename);
          // var constraintFuncChangingFilePathLemma = includeParser.Normalized(lemma.BodyStartTok.Filename);
          // var remoteFolderPath = dafnyVerifier.DuplicateAllFiles(cnt, changingFilePath);
          var serverIndexLemma = cnt % dafnyVerifier.serversList.Count;
          var localizedCntIndexLemma = (cnt-serverIndexLemma)/dafnyVerifier.serversList.Count;
          var remoteFilePathLemma = dafnyVerifier.getTempFilePath()[serverIndexLemma][localizedCntIndexLemma+1].Path;
          // Console.WriteLine("remote = " + remoteFilePathLemma);
        if(vacTest){
        //  args.Add("/proc:*"+lemmaName+"*");
        var validCheckString = "validityCheck";
        args.Add("/proc:*"+validCheckString+"*");
        }else if(DafnyOptions.O.EvaluateAllAtOnce){
          args.Add("/verifyAllModules");
        }else if(DafnyOptions.O.ProofOnly){ // maybe at least add the spec file? 
          var changingFilePathSpec = includeParser.Normalized(func.BodyStartTok.Filename);
          var constraintFuncChangingFilePathSpec = includeParser.Normalized(func.BodyStartTok.Filename);
          var remoteFilePathSpec = dafnyVerifier.getTempFilePath()[serverIndexLemma][localizedCntIndexLemma+1].Path;
          dafnyVerifier.runDafny("", args,
            expr, cnt, -1, -1,$"{remoteFilePathSpec}/{constraintFuncChangingFilePathSpec}");

        }else{
          foreach (var depn in includesSet)
            {
              var lastDire = constraintFuncChangingFilePathLemma.LastIndexOf("/");
              var normalizedDepn = depn;
              if(lastDire > 0){
                normalizedDepn =  constraintFuncChangingFilePathLemma.Substring(0,constraintFuncChangingFilePathLemma.LastIndexOf("/"))+"/./"+depn;
              
              }else{
                // var test = includeParser.simpleNorm(normalizedDepn);
                normalizedDepn = depn;
              }
              dafnyVerifier.runDafny("", args,
                expr, cnt, -1, -1,$"{remoteFilePathLemma}/{normalizedDepn}");

            }
        }
        dafnyVerifier.runDafny(code, args,
            expr, cnt, lemmaForExprValidityPosition, lemmaForExprValidityStartPosition,$"{remoteFilePathLemma}/{constraintFuncChangingFilePathLemma}");

    }
  }


    private Function workingFunc = null;
    private Function workingConstraintFunc = null;
    private string[] workingFuncCode;
    private string constraintFuncCode = "";
    private int constraintFuncLineCount = 0;
    private List<string> mergedCode = new List<string>();

        public void PrintExprAndCreateProcess(Program program, ExpressionFinder.ExpressionDepth exprDepth, int cnt) {
      bool runOnce = DafnyOptions.O.HoleEvaluatorRunOnce;
      if (cnt % 5000 == 1) {
        Console.WriteLine($"{dafnyVerifier.sw.ElapsedMilliseconds / 1000}:: {cnt}");
      }
      Console.WriteLine($"{cnt} {Printer.ExprToString(exprDepth.expr)}\t\t{exprDepth.depth}");
      var funcName = workingFunc.Name;

      int lemmaForExprValidityPosition = -1;
      int lemmaForExprValidityStartPosition = -1;

      var workingDir = $"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}/{funcName}_{cnt}";
      if (tasksList == null) {
        string code = "";
        using (var wr = new System.IO.StringWriter()) {
          var pr = new Printer(wr, DafnyOptions.PrintModes.DllEmbed);
          pr.UniqueStringBeforeUnderscore = UnderscoreStr;
          if (exprDepth.expr.HasCardinality) {
            workingFunc.Body = Expression.CreateAnd(exprDepth.expr, workingFunc.Body);
          } else {
            workingFunc.Body = Expression.CreateAnd(workingFunc.Body, exprDepth.expr);
          }
          pr.PrintProgram(program, true);
          code = $"// #{cnt}\n";
          code += $"// {Printer.ExprToString(exprDepth.expr)}\n" + Printer.ToStringWithoutNewline(wr) + "\n\n";
          lemmaForExprValidityStartPosition = code.Count(f => f == '\n') + 1;
          code += "" + "\n";
          lemmaForExprValidityPosition = code.Count(f => f == '\n');
          if (DafnyOptions.O.HoleEvaluatorCreateAuxFiles)
            File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{funcName}_{cnt}.dfy", code);
        }
        string env = DafnyOptions.O.Environment.Remove(0, 22);
        var argList = env.Split(' ');
        List<string> args = new List<string>();
        foreach (var arg in argList) {
          if (!arg.EndsWith(".dfy") && !arg.StartsWith("/mutationTarget") && arg.StartsWith("/")) {
            args.Add(arg);
          }
        }
        args.Add("/exitAfterFirstError");
        dafnyVerifier.runDafny(code, args,
            exprDepth, cnt, lemmaForExprValidityPosition, lemmaForExprValidityStartPosition, "");
      } else {
        var changingFilePath = includeParser.Normalized(workingFunc.BodyStartTok.Filename);
        var constraintFuncChangingFilePath = includeParser.Normalized(workingConstraintFunc.BodyStartTok.Filename);
        var remoteFolderPath = dafnyVerifier.DuplicateAllFiles(cnt, changingFilePath);

        var clonedWorkingFunc = cloner.CloneFunction(workingFunc);
        if (exprDepth.expr.HasCardinality) {
          clonedWorkingFunc.Body = Expression.CreateAnd(clonedWorkingFunc.Body, exprDepth.expr);
        } else {
          clonedWorkingFunc.Body = Expression.CreateAnd(exprDepth.expr, clonedWorkingFunc.Body);
        }
        using (var wr = new System.IO.StringWriter()) {
          var pr = new Printer(wr, DafnyOptions.PrintModes.DllEmbed);
          pr.PrintFunction(clonedWorkingFunc, 0, false);
          mergedCode[1] = Printer.ToStringWithoutNewline(wr);
        }
        var newCode = String.Join('\n', mergedCode);

        if (constraintFuncCode != "") {
          lemmaForExprValidityStartPosition = constraintFuncLineCount + 2;
          var newBaseCode = constraintFuncCode + "\n" + lemmaForExprValidityString;
          lemmaForExprValidityPosition = lemmaForExprValidityStartPosition + lemmaForExprValidityLineCount;
          dafnyVerifier.runDafny(newBaseCode, tasksListDictionary[constraintFuncChangingFilePath].Arguments.ToList(),
              exprDepth, cnt, lemmaForExprValidityPosition, lemmaForExprValidityStartPosition,
              $"{remoteFolderPath.Path}/{constraintFuncChangingFilePath}");

          dafnyVerifier.runDafny(newCode, tasksListDictionary[changingFilePath].Arguments.ToList(),
              exprDepth, cnt, -1, -1, $"{remoteFolderPath.Path}/{changingFilePath}");
        } else {
          var newCodeLineCount = newCode.Count(f => (f == '\n'));
          lemmaForExprValidityStartPosition = newCodeLineCount + 2;
          newCode += "\n" + lemmaForExprValidityString;
          lemmaForExprValidityPosition = newCodeLineCount + lemmaForExprValidityLineCount;
          dafnyVerifier.runDafny(newCode, tasksListDictionary[changingFilePath].Arguments.ToList(),
              exprDepth, cnt, lemmaForExprValidityPosition, lemmaForExprValidityStartPosition,
              $"{remoteFolderPath.Path}/{changingFilePath}");
        }

        // File.WriteAllTextAsync($"{workingDir}/{changingFilePath}", newCode);
        foreach (var f in affectedFiles) {
          if (f != changingFilePath && f != constraintFuncChangingFilePath) {
            // var code = File.ReadAllLines($"{workingDir}/{f}");
            dafnyVerifier.runDafny("", tasksListDictionary[f].Arguments.ToList(),
              exprDepth, cnt, -1, -1, $"{remoteFolderPath.Path}/{f}");
          // } else {
            // dafnyVerifier.runDafny(String.Join('\n', code), tasksListDictionary[f].Arguments.ToList(),
            //   expr, cnt, lemmaForExprValidityPosition, lemmaForExprValidityStartPosition);
          }
        }
      }
    }

    private Dictionary<string, bool> verificationCache = new Dictionary<string, bool>(); // 添加到类字段

    public Boolean isDafnyVerifySuccessful(int i)
    {
        // 缓存检查
        var expr = expressionFinder.availableExpressions[i];
        string cacheKey = $"{i}_{Printer.ExprToString(expr.expr).GetHashCode()}";
        
        if (verificationCache.ContainsKey(cacheKey)) {
            return verificationCache[cacheKey];
        }

        var request = dafnyVerifier.requestsList[i].Last();
        var position = dafnyVerifier.requestToPostConditionPosition[request];
        var lemmaStartPosition = dafnyVerifier.requestToLemmaStartPosition[request];
        var output = dafnyVerifier.dafnyOutput[request];
        var response = output.Response;
        var filePath = output.FileName;
        var execTime = output.ExecutionTimeInMs;
        executionTimes.Add(execTime);
        
        var expectedOutput = $"{filePath}({position},0): Error: A postcondition might not hold on this return path.";
        var expectedInconclusiveOutputStart = $"{filePath}({lemmaStartPosition},{validityLemmaNameStartCol}): Verification inconclusive";
        var res = DafnyVerifierClient.IsCorrectOutput(response, expectedOutput, expectedInconclusiveOutputStart);
        
        bool result = response.EndsWith("0 errors\n");
        verificationCache[cacheKey] = result; // 缓存结果
        return result;
    }

    public static Result IsCorrectOutput(string output, string expectedOutput, string expectedInconclusiveOutputStart)
    {
      if (output.EndsWith("1 error\n"))
      {
        var outputList = output.Split('\n');
        return ((outputList.Length >= 7) && (outputList[outputList.Length - 7] == expectedOutput)) ? Result.CorrectProof : Result.IncorrectProof;
      }
      else if (output.EndsWith("1 inconclusive\n"))
      {
        var outputList = output.Split('\n');
        return ((outputList.Length >= 4) && outputList[outputList.Length - 4].StartsWith(expectedInconclusiveOutputStart)) ? Result.CorrectProofByTimeout : Result.IncorrectProof;
      }
      else
      {
        return Result.IncorrectProof;
      }
    }
    
  public Boolean isResolutionError(int i)
  {
    var request = dafnyVerifier.requestsList[i].First();
      var position = dafnyVerifier.requestToPostConditionPosition[request];
      var lemmaStartPosition = dafnyVerifier.requestToLemmaStartPosition[request];
      var output = dafnyVerifier.dafnyOutput[request];
      var response = output.Response;
      var filePath = output.FileName;
      // var startTime = output.StartTime;
      var execTime = output.ExecutionTimeInMs;
        executionTimes.Add(execTime);
      // startTimes.Add(startTime);
      return response.Contains("parse errors") || response.Contains("resolution/type errors") || response.Contains("Error: arguments");
  }

    public void PrintImplies(Program program, Function func, int availableExprAIndex, int availableExprBIndex) {
      // Console.WriteLine($"print implies {availableExprAIndex} {availableExprBIndex}");
      var funcName = func.FullDafnyName;
      var paramList = GetFunctionParamList(func);
      var parameterNameTypes = paramList.Item2;
      string lemmaForCheckingImpliesString = "lemma checkIfExprAImpliesExprB(";
      lemmaForCheckingImpliesString += parameterNameTypes + ")\n";
      Expression A = expressionFinder.availableExpressions[availableExprAIndex].expr;
      Expression B = expressionFinder.availableExpressions[availableExprBIndex].expr;
      lemmaForCheckingImpliesString += "  requires " + Printer.ExprToString(A) + "\n";
      lemmaForCheckingImpliesString += "  ensures " + Printer.ExprToString(B) + "\n";
      lemmaForCheckingImpliesString += "{}";

      int lemmaForCheckingImpliesPosition = 0;

      using (var wr = new System.IO.StringWriter()) {
        var pr = new Printer(wr, DafnyOptions.PrintModes.DllEmbed);
        pr.UniqueStringBeforeUnderscore = UnderscoreStr;
        pr.PrintProgram(program, true);
        var code = $"// Implies {Printer.ExprToString(A)} ==> {Printer.ExprToString(B)}\n" + Printer.ToStringWithoutNewline(wr) + "\n\n";
        code += lemmaForCheckingImpliesString + "\n";
        lemmaForCheckingImpliesPosition = code.Count(f => f == '\n');
        File.WriteAllTextAsync($"{DafnyOptions.O.HoleEvaluatorWorkingDirectory}{funcName}_implies_{availableExprAIndex}_{availableExprBIndex}.dfy", code);
      }

      string dafnyBinaryPath = System.Reflection.Assembly.GetEntryAssembly().Location;
      dafnyBinaryPath = dafnyBinaryPath.Substring(0, dafnyBinaryPath.Length - 4);
      string env = DafnyOptions.O.Environment.Remove(0, 22);
      var argList = env.Split(' ');
      string args = "";
      foreach (var arg in argList) {
        if (!arg.EndsWith(".dfy") && !arg.StartsWith("/mutationTarget") && !arg.StartsWith("/proc:") && args.StartsWith("/")) {
          args += arg + " ";
        }
      }
      dafnyImpliesExecutor.createProcessWithOutput(dafnyBinaryPath,
        $"{args} {DafnyOptions.O.HoleEvaluatorWorkingDirectory}{funcName}_implies_{availableExprAIndex}_{availableExprBIndex}.dfy /proc:Impl*checkIfExprAImpliesExprB*",
        availableExprAIndex, availableExprBIndex, -1, lemmaForCheckingImpliesPosition,
        $"{funcName}_implies_{availableExprAIndex}_{availableExprBIndex}.dfy");
    }

  }
}