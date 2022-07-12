using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VRVis.Utilities;

namespace VRVis.IO.Features {

    /// <summary>
    /// Influence function as in SPLConqueror - but only for numeric options:
    /// https://github.com/se-passau/SPLConqueror/blob/master/SPLConqueror/SPLConqueror/InfluenceFunction.cs <para/>
    /// It represents a term like 2.1 * x... mainly used as step functions in this context.<para/>
    /// As I understood from the original code, "log10(...)" is replaced by "[...]" for evaluation.
    /// At least thats how this code handles the case.<para/>
    /// Created by Leon H. (2019)
    /// </summary>
    public class InfluenceFunction {

        private Feature_Range numericOption = null;

        // contains the whole expression (each entry is an operand or operator)
        protected string[] expressionArray = null;

        // holds the expression after formatting it
        protected string formatedExpression = "";



        // CONSTRUCTOR

        /// <summary>
        /// Creates an instance of the InfluenceFunction used as a step function in this context.<para/>
        /// Name of the option, numbers, operators and "n" are supported in the expression.<para/>
        /// Each occurence of "n" will be replaced with the name of the numeric option!
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="option"></param>
        public InfluenceFunction(string expression, Feature_Range option) {

            if (option == null) { return; }
            numericOption = option;

            // replace occurrences of "n" with the name of this option
            List<string> split = new List<string>(FormatExpression(expression).Split(' '));
            if (!option.GetName().Equals("n") && split.Contains("n")) {
                expression = expression.Replace("n", option.GetName());
            }

            // convert the expression to reverse polish notation and store in expressionArray
            ParseExpressionRPN(expression);

            // DEBUG
            /*
            StringBuilder strb = new StringBuilder("Influence function: " + expression + ", RPN:");
            foreach (string s in expressionArray) {
                strb.Append(' ');
                strb.Append(s);
            }
            Debug.LogWarning(strb.ToString());
            */
        }



        // GETTER AND SETTER

        /// <summary>
        /// Get a copy of the expression array in reverse polish notation.<para/>
        /// See <a href="https://en.wikipedia.org/wiki/Reverse_Polish_notation">"Wikipedia Reverse Polish Notation"</a> for more information.
        /// </summary>
        public string[] GetExpressionTree() {
            string[] copy = new string[expressionArray.Length];
            Array.Copy(expressionArray, copy, copy.Length);
            return copy;
        }



        // FUNCTIONALITY

        /// <summary>
        /// Parse the given expression from algebraic notation 
        /// to reverse polish notation (RPN) using the "shunting yard algorithm".
        /// (e.g. 23 * 2 - 4 => 23 2 * 4 -)<para/>
        /// [State before 22.01.2019] This current version is not working 100% as the algorithm should.
        /// (e.g. pass "(3+5)*2+4-(2*(10-2))" => "3 5 + 2 * 4 2 10 2 - * - +" but should be "3 5 + 2 * 4 + 2 10 2 - * -"<para/>
        /// [State 22.01.2019] Better, but still not 100% corect.
        /// (e.g. pass "(5+3)*2-4+8*(5-14)" => "5 3 + 2 * 4 8 5 14 - * + -" but should be "5 3 + 2 * 4 - 8 5 14 - * +"<para/>
        /// [State 22.01.2019 - NOW]: Works now that I am using "precedence(stack-top) >= precedence(token)"
        /// </summary>
        /// <seealso cref="https://en.wikipedia.org/wiki/Shunting-yard_algorithm"/>
        /// <param name="expression">The expression to parse in algebraic notation</param>
        private void ParseExpressionRPN(string expression) {

            // stack to hold tokens temporarily and queue to hold final result
            Queue<string> queue = new Queue<string>();
            Stack<string> stack = new Stack<string>(); // operator stack

            // create "well formed expression" (ensure we can split using whitespace and so on)
            expression = FormatExpression(expression);
            formatedExpression = expression;

            //List<string> justAdd = new List<string>(new string[]{"(", "[", "{", "<"}); // in original code but closing not used?!
            List<string> opening = new List<string>(new string[] { "(", "[" });
            List<string> closing = new List<string>(new string[] { ")", "]" });

            // split expression at whitespaces and process each token
            string[] eSplit = expression.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < eSplit.Length; i++) {

                string token = eSplit[i];
                if (IsOperator(token)) {

                    // add operators from the stack to queue that have same or higher precedence
                    while (stack.Count > 0 && IsOperator(stack.Peek()) &&
                        OperatorPrecedence(stack.Peek()) >= OperatorPrecedence(token)) {
                        queue.Enqueue(stack.Pop());
                    }

                    // add the operator to the stack
                    stack.Push(token);
                    continue;
                }
                else if (opening.Contains(token)) {

                    // add e.g. opening brackets to the stack
                    stack.Push(token);
                }
                else if (closing.Contains(token)) {

                    // find according opening token (can be improved by additional method - but not required for now)
                    string tokenOpen = "";
                    if (token.Equals("]")) { tokenOpen = "["; }
                    else if (token.Equals(")")) { tokenOpen = "("; }

                    // add items from stack to queue until another opening bracket of the same kind is found
                    while (stack.Count > 0 && !stack.Peek().Equals(tokenOpen)) { queue.Enqueue(stack.Pop()); }

                    // add "]" as "log10" operator (required for evaluation)
                    if (token.Equals("]")) { queue.Enqueue(token); }

                    // remove closing bracket from top of stack
                    if (stack.Count > 0) { stack.Pop(); }
                    continue;
                }
                else {

                    // add token if it's a number or this option
                    // (in original code also: "or a feature with a value" - but this is not done for numeric options, so we ignore it)
                    double value = 0;
                    bool isNumber = double.TryParse(token, out value);
                    //if (isNumber || token.Equals(numericOption.GetName())) {
                    //    queue.Enqueue(token);
                    //}
                    queue.Enqueue(token);
                }
            }

            // add rest of the stack to queue and store queue in expression array
            while (stack.Count > 0) { queue.Enqueue(stack.Pop()); }
            expressionArray = queue.ToArray();
        }


        /// <summary>
        /// Tells if the passed string is a mathematical operator.
        /// </summary>
        private bool IsOperator(string token) {

            // remove possible leading and trailing whitespaces
            token = token.Trim();
            if (token.Equals("+") || token.Equals("-") || token.Equals("*") || token.Equals("/")) { return true; }
            return false;
        }


        /// <summary> Get the precedence of an operator. </summary>
        private static int OperatorPrecedence(string op) {

            // remove possible leading and trailing whitespaces
            op = op.Trim();

            switch (op) {
                case "*":
                case "/":
                    return 3;
                case "+":
                case "-":
                    return 2;
                case "(":
                    return 1;
            }

            return 0;
        }


        /// <summary>
        /// Adjusts the expression so that we can use it correctly in later parsing.<para/>
        /// Adds whitespaces before and after each allowed special character so that splitting works.
        /// It also replaces double whitespace occurrences with a single whitespace.<para/>
        /// Name in original code: "createWellFormedExpression".
        /// </summary>
        private string FormatExpression(string expression) {

            // remove all whitespaces (yields same result as original while loop - I tested it)
            if (expression.Contains(" ")) { expression = expression.Replace(" ", ""); }

            // [ToDo] if required: replace different log parts

            expression = expression.Replace("\n", " ");
            expression = expression.Replace("\t", " ");

            string[] addWhitespace = new string[] { "+", "-", "*", "/", "(", ")", "[", "]" };
            for (int i = 0; i < addWhitespace.Length; i++) {
                expression = expression.Replace(addWhitespace[i], " " + addWhitespace[i] + " ");
            }

            // replace all double whitespace occurrences with single ones
            if (expression.Contains("  ")) { expression = expression.Replace("  ", " "); }

            // [ToDo] if required: remove whitespaces exponential part
            // (couldn't find out what original code is doing with it - it did never change anything testing it)

            return expression;
        }

        /// <summary>
        /// Replaces occurences of log10(...) with [...] and returns modified expression.
        /// </summary>
        private string ReplaceLog10(string expression) {

            if (!expression.Contains("log10")) { return expression; }

            // split the expression when "log10(" occurs and get the rest of the text starting after "log10("
            string[] eSplit = expression.Split(new string[] { "log10(" }, 2, StringSplitOptions.None);
            if (eSplit.Length == 1) { return expression; } // only one result if not found, so return
            StringBuilder secondPart = new StringBuilder(eSplit[1]);

            // find position of closing bracket and replace it
            int offset = FindClosingBracketOffset(secondPart.ToString());
            if (offset < 0) { return expression; } // failed to find closing bracket
            secondPart[offset] = ']';

            return "[" + secondPart.ToString();
        }

        /// <summary>
        /// Find the next closing bracket its position/offset in the string.
        /// E.g. in "(a+b) - (c*d))" its the third closing bracket we are looking for.
        /// </summary>
        /// <returns>Returns the offset or -1 if not found</returns>
        private int FindClosingBracketOffset(string expression) {

            int offset = -1;
            int opening = 0;
            for (int i = 0; i < expression.Length; i++) {

                char token = expression[i];
                if (token.Equals('(')) { opening++; }
                else if (token.Equals(')')) { opening--; }

                if (opening <= 0) { offset = i; break; }
            }

            return offset;
        }


        /// <summary>
        /// Evaluate the initial expression converted to reverse polish notation
        /// using the numeric option this function is assigned to and the passed feature model
        /// to retrieve values of possible other included features/options.
        /// </summary>
        /// <returns>The calculated value</returns>
        public float EvaluateExpression(VariabilityModel model) {

            if (expressionArray.Length == 0) { return 0; }
            string err_msg = "Influence function expression is faulty!";

            // stack that holds values
            Stack<float> stack = new Stack<float>();

            for (int i = 0; i < expressionArray.Length; i++) {
                string token = expressionArray[i].Trim();
                bool isLogOperator = token.Equals("]");

                // if no operator - add to stack
                if (!IsOperator(token) && !isLogOperator) {
                    stack.Push(GetTokenValue(token, model));
                    continue;
                }
                
                // if operator, check if stack valid and calculate
                if (stack.Count < 2) {
                    Debug.LogError("Expression evaluation failed! - " +
                        "Got operator \"" + token + "\" but not enough operands)");
                    return 0;
                }

                // get operands
                float op1 = stack.Pop();
                float op2 = isLogOperator ? 0 : stack.Pop();

                // apply using the operator
                switch (token) {
                    case "+": stack.Push(op2 + op1); break;
                    case "-": stack.Push(op2 - op1); break;
                    case "*": stack.Push(op2 * op1); break;
                    case "/":
                        if (op2 * op1 == 0) { stack.Push(0); }
                        else { stack.Push(op2 / op1); }
                        break;
                    case "]":
                        if (op1 == 0) {
                            Debug.LogWarning(err_msg + " (compute log10(0))");
                            stack.Push(0);
                        }
                        else { stack.Push(Mathf.Log10(op1)); }
                        break;
                }
            }

            // expression is faulty if more than 1 entry it left
            if (stack.Count > 1) {
                Debug.LogWarning(err_msg + " (" + formatedExpression + ")");
            }

            return stack.Pop();
        }


        /// <summary>
        /// Get the number if it just is one or get the value
        /// of the according feature included in this expression.<para/>
        /// Takes the configuration/hierarchy into account.
        /// As a result, a value will be 0 if its parent is not selected.<para/>
        /// Just returns 0 for invalid tokens.
        /// </summary>
        private float GetTokenValue(string token, VariabilityModel model) {

            // remove leading and trailing whitespaces
            token = token.Trim();

            // try to convert token to number
            float value = 0;
            if (Utility.StrToFloat(token, out value)) { return value; }

            // check if this token even represents a feature
            string optionName = AFeature.RemoveInvalidCharsFromName(token);
            if (!model.HasOption(optionName, false)) { return value; }

            // get the feature and return its influence value 
            AFeature option = model.GetOption(optionName, false);


            // QUESTION: can other options influence the step function of this option?
            // ==> ANSWER: NO


            // only allow that the step function is
            // influenced by the numeric option it is attached to!
            if (option != numericOption) { return value; }

            return option.GetValue();
        }

    }
}
