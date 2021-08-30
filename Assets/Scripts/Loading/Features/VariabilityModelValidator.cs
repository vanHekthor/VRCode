using System.Collections;
using System.Collections.Generic;
using Microsoft.Z3;
using System;
using UnityEngine;

namespace VRVis.IO.Features {

    /// <summary>
    /// Validate the variability model with its current configuration.
    /// </summary>
    public class VariabilityModelValidator {

        private readonly VariabilityModel model;

        /// <summary>Stores the initialized solver system for the variability model.</summary>
        private static Dictionary<VariabilityModel, Tuple<Context, BoolExpr>> solverSystems = new Dictionary<VariabilityModel, Tuple<Context, BoolExpr>>();



        // CONSTRUCTOR

        public VariabilityModelValidator(VariabilityModel model) {
            this.model = model;
        }



        // FUNCTIONALITY

        /// <summary>Returns true if the configuration of the model is valid.</summary>
        public bool IsConfigurationValid(bool partialConfiguration = false) {

            model.SetCurrentlyBeingValidated(true);
            bool valid = CheckConfigurationSAT(model, partialConfiguration);
            model.SetCurrentlyBeingValidated(false);
            model.JustValidatedNotification(valid);
            return valid;
        }

        /// <summary>Clear the stored solver system of this variability model.</summary>
        public bool ClearStoredSolverSystem(VariabilityModel model) {
            return solverSystems.Remove(model);
        }

        /// <summary>Clear all the stored solver systems.</summary>
        public void ClearSolverSystems() {
            solverSystems.Clear();
        }


        /// <summary>Get an enumerable of currently selected binary options.</summary>
        private List<Feature_Boolean> GetSelectedBinaryOptions(VariabilityModel vm) {

            // add all selected bin. options to the list considering its parent
            List<Feature_Boolean> selected = new List<Feature_Boolean>();
            vm.GetBinaryOptions().ForEach(binOpt => {
                if (binOpt.IsSelected(true)) { selected.Add(binOpt); }
            });

            return selected;
        }


        /// <summary>Get the numeric options with their current value as a dictionary.</summary>
        private Dictionary<Feature_Range, float> GetNumericOptionDict(VariabilityModel vm) {

            Dictionary<Feature_Range, float> dict = new Dictionary<Feature_Range, float>();
            vm.GetNumericOptions().ForEach(numOpt => { dict.Add(numOpt, numOpt.GetValue()); });
            return dict;
        }


        // ======================================================================================================== //
        // ADJUSTED SPL CONQUEROR CODE (MachineLearning/Solver/z3/)
        // Z3 C# API - Context: https://github.com/Z3Prover/z3/blob/master/src/api/dotnet/Context.cs
        // ======================================================================================================== //
        
        // ToDo: maybe add configuration class and use it instead of vm options directly
        private bool CheckConfigurationSAT(VariabilityModel vm, bool partialConfiguration = false) {

            List<Expr> variables;
            Dictionary<Expr, AFeature> termToOption;
            Dictionary<AFeature, Expr> optionToTerm;
            Tuple<Context, BoolExpr> z3Tuple = GetInitializedSolverSystem(out variables, out optionToTerm, out termToOption, vm);
            Context z3Context = z3Tuple.Item1;
            BoolExpr z3Constraints = z3Tuple.Item2;

            //List<Expr> constraints = new List<Expr>(); // WHY? not used at all
            Solver solver = z3Context.MkSolver();
            solver.Assert(z3Constraints);

            List<Feature_Boolean> selectedBinaryOptions = GetSelectedBinaryOptions(vm);
            Dictionary<Feature_Range, float> numOptionsDictionary = GetNumericOptionDict(vm);
            solver.Assert(ConvertConfiguration(z3Context, selectedBinaryOptions, optionToTerm, vm, partialConfiguration, numOptionsDictionary));

            Status solverStatus = solver.Check();
            Debug.LogWarning("Solver status: " + solverStatus);
            //Debug.LogWarning("Solver unsat core length: " + solver.UnsatCore.Length);
            if (solverStatus == Status.UNKNOWN) { Debug.LogWarning("Solver reason unknown: " + solver.ReasonUnknown); }
            //Debug.LogWarning("Solver proof: " + solver.Proof);
            return solverStatus == Status.SATISFIABLE;
        }

        //private bool CheckConfigurationSAT(Configuration config, bool partialConfiguration = false) {

        //    List<Expr> variables;
        //    Dictionary<Expr, AFeature> termToOption;
        //    Dictionary<AFeature, Expr> optionToTerm;
        //    Tuple<Context, BoolExpr> z3Tuple = GetInitializedSolverSystem(out variables, out optionToTerm, out termToOption, vm);
        //    Context z3Context = z3Tuple.Item1;
        //    BoolExpr z3Constraints = z3Tuple.Item2;

        //    //List<Expr> constraints = new List<Expr>(); // WHY? not used at all
        //    Solver solver = z3Context.MkSolver();
        //    solver.Assert(z3Constraints);

        //    List<Feature_Boolean> selectedBinaryOptions = GetSelectedBinaryOptions(vm);
        //    Dictionary<Feature_Range, float> numOptionsDictionary = GetNumericOptionDict(vm);
        //    solver.Assert(ConvertConfiguration(z3Context, selectedBinaryOptions, optionToTerm, vm, partialConfiguration, numOptionsDictionary));

        //    Status solverStatus = solver.Check();
        //    Debug.LogWarning("Solver status: " + solverStatus);
        //    //Debug.LogWarning("Solver unsat core length: " + solver.UnsatCore.Length);
        //    if (solverStatus == Status.UNKNOWN) { Debug.LogWarning("Solver reason unknown: " + solver.ReasonUnknown); }
        //    //Debug.LogWarning("Solver proof: " + solver.Proof);
        //    return solverStatus == Status.SATISFIABLE;
        //}


        /// <summary>
        /// Converts the given configuration into a <see cref="BoolExpr"/>.
        /// </summary>
        /// <param name="context">The <see cref="Context"/>-object.</param>
        /// <param name="options">The options that are selected.</param>
        /// <param name="optionToTerm">The mapping from <see cref="Feature_Boolean"/> to the according <see cref="BoolExpr"/>.</param>
        /// <param name="vm">The variability model that contains all configuration options and constraints.</param>
        /// <param name="partial"><code>true</code> if the configuration is a partial configuration; <code>false</code> otherwise.</param>
        /// <returns>The corresponding <see cref="BoolExpr"/>.</returns>
        public static BoolExpr ConvertConfiguration(Context context, List<Feature_Boolean> options, Dictionary<Feature_Boolean, BoolExpr> optionToTerm, VariabilityModel vm, bool partial = false) {
            
            List<BoolExpr> andGroup = new List<BoolExpr>();
            foreach (Feature_Boolean binOpt in vm.GetBinaryOptions()) {
                if (options.Contains(binOpt)) { andGroup.Add(optionToTerm[binOpt]); }
                else if (!partial) { andGroup.Add(context.MkNot(optionToTerm[binOpt])); }
            }

            return context.MkAnd(andGroup.ToArray());
        }


        /// <summary>
        /// Converts the given configuration into a <see cref="BoolExpr"/>.
        /// </summary>
        /// <param name="context">The <see cref="Context"/>-object.</param>
        /// <param name="options">The options that are selected.</param>
        /// <param name="optionToTerm">The mapping from <see cref="BinaryOption"/> to the according <see cref="Expr"/>.</param>
        /// <param name="vm">The variability model that contains all configuration options and constraints.</param>
        /// <param name="partial"><code>true</code> if the configuration is a partial configuration; <code>false</code> otherwise.</param>
        /// <returns>The corresponding <see cref="ConfigurationOption"/>.</returns>
        public static BoolExpr ConvertConfiguration(Context context, List<Feature_Boolean> options, Dictionary<AFeature, Expr> optionToTerm, VariabilityModel vm, bool partial = false, Dictionary<Feature_Range, float> numericValues = null) {

            List<BoolExpr> andGroup = new List<BoolExpr>();
            foreach (Feature_Boolean binOpt in vm.GetBinaryOptions()) {
                if (options.Contains(binOpt)) { andGroup.Add((BoolExpr) optionToTerm[binOpt]); }
                else if (!partial) { andGroup.Add(context.MkNot((BoolExpr) optionToTerm[binOpt])); }
            }

            // for numeric configuration options
            foreach (Feature_Range numOpt in vm.GetNumericOptions()) {

                Expr numericExpression = optionToTerm[numOpt];

                // Throw an exception if the configuration is not partial and does not contain a numeric option
                if (!partial && !numericValues.ContainsKey(numOpt)) {
                    throw new InvalidOperationException("The numeric option " + numOpt.ToString() + " is missing in the whole configuration.");
                }
                else if (numericValues.ContainsKey(numOpt)) {

                    FPNum second = context.MkFPNumeral(numericValues[numOpt], context.MkFPSort32());
                    andGroup.Add(context.MkEq(numericExpression, second)); //context.MkFPSortDouble())));
                    //Debug.LogWarning("Context MkEq (" + numericExpression.ToString() + ", " + second.ToString() + ")");

                    // ToDo: maybe replace using "all values in OR-expression" by just checking if inside bounds (if value on function is checked by UI already)
                    //BoolExpr inRange = numOpt.IsValueValid() ? context.MkTrue() : context.MkFalse();
                    //andGroup.Add(inRange);
                }
            }

            return context.MkAnd(andGroup.ToArray());
        }


        /// <summary>
        /// Generates a solver system (in z3: context) based on a variability model. The solver system can be used to check for satisfiability of configurations as well as optimization.
        /// Additionally to <see cref="Z3Solver.GetInitializedBooleanSolverSystem(out List{BoolExpr}, out Dictionary{Feature_Boolean, BoolExpr}, out Dictionary{BoolExpr, Feature_Boolean}, VariabilityModel, bool, int)"/>, this method supports numeric features.
        /// Note that we do not support Henard's randomized approach here, because it is defined only on boolean constraints.
        /// </summary>
        /// <param name="variables">Empty input, outputs a list of CSP terms that correspond to the configuration options of the variability model</param>
        /// <param name="optionToTerm">A map to get for a given configuration option the corresponding CSP term of the constraint system</param>
        /// <param name="termToOption">A map that gives for a given CSP term the corresponding configuration option of the variability model</param>
        /// <param name="vm">The variability model for which we generate a constraint system</param>
        /// <param name="randomSeed">The z3 random seed</param>
        /// <returns>The generated constraint system consisting of logical terms representing configuration options as well as their constraints.</returns>
        internal static Tuple<Context, BoolExpr> GetInitializedSolverSystem(out List<Expr> variables, out Dictionary<AFeature, Expr> optionToTerm, out Dictionary<Expr, AFeature> termToOption, VariabilityModel vm, int randomSeed = 0) {

            // Create a context and turn on model generation
            Context context = new Context(new Dictionary<string, string>() { { "model", "true" } });

            // Assign the out-parameters
            variables = new List<Expr>();
            optionToTerm = new Dictionary<AFeature, Expr>();
            termToOption = new Dictionary<Expr, AFeature>();

            // Create the binary configuration options
            foreach (Feature_Boolean binOpt in vm.GetBinaryOptions()) {
                BoolExpr booleanVariable = GenerateBooleanVariable(context, binOpt.GetName());
                variables.Add(booleanVariable);
                optionToTerm.Add(binOpt, booleanVariable);
                termToOption.Add(booleanVariable, binOpt);
            }

            // Create the numeric configuration options
            foreach (Feature_Range numOpt in vm.GetNumericOptions()) {
                Expr numericVariable = GenerateFloatVariable(context, numOpt.GetName());
                variables.Add(numericVariable);
                optionToTerm.Add(numOpt, numericVariable);
                termToOption.Add(numericVariable, numOpt);
            }


            // return already initialized solver systems
            if (solverSystems.ContainsKey(vm)) { return solverSystems[vm]; }


            // Initialize variables for constraint parsing
            List<List<AFeature>> alreadyHandledAlternativeOptions = new List<List<AFeature>>();

            List<BoolExpr> andGroup = new List<BoolExpr>();

            // Parse the constraints of the binary (boolean) configuration options
            foreach (Feature_Boolean current in vm.GetBinaryOptions()) {

                BoolExpr expr_cur = (BoolExpr) optionToTerm[current];
                if (current.GetParent() == null || current.GetParent() == vm.GetRoot()) {
                    if (current.IsOptional() == false && current.GetExcludedOptionsCount() == 0)
                        andGroup.Add(expr_cur);
                }

                if (current.GetParent() != null && current.GetParent() != vm.GetRoot()) {
                    BoolExpr parent = (BoolExpr) optionToTerm[(Feature_Boolean) current.GetParent()];
                    andGroup.Add(context.MkImplies(expr_cur, parent));
                    if (current.IsOptional() == false && current.GetExcludedOptionsCount() == 0)
                        andGroup.Add(context.MkImplies(parent, expr_cur));
                }

                // Alternative or other exclusion constraints                
                if (current.GetExcludedOptionsCount() > 0) {

                    // get all options excluded by the current option with same parent
                    List<AFeature> alternativeOptions = current.GetAlternativeOptions();
                    
                    // ToDo: cleanup
                    //List<string> names = new List<string>();
                    //alternativeOptions.ForEach(ft => names.Add(ft.GetName()));
                    //Debug.LogWarning("Alternative options for option " + current.GetName() + " are: " + string.Join(", ", names));

                    if (alternativeOptions.Count > 0) {

                        // Check whether we handled this group of alternatives already
                        foreach (List<AFeature> alternativeGroup in alreadyHandledAlternativeOptions) {
                            foreach (AFeature alternative in alternativeGroup) {
                                if (current == alternative) { goto handledAlternative; }
                            }
                        }

                        // It is not allowed that an alternative group has no parent element
                        BoolExpr parent = null;
                        if (current.GetParent() == null) { parent = context.MkTrue(); }
                        else { parent = (BoolExpr) optionToTerm[(Feature_Boolean) current.GetParent()]; }

                        BoolExpr[] terms = new BoolExpr[alternativeOptions.Count + 1];
                        terms[0] = expr_cur;
                        int i = 1;

                        foreach (Feature_Boolean altEle in alternativeOptions) {
                            BoolExpr temp = (BoolExpr) optionToTerm[altEle];
                            terms[i] = temp;
                            i++;
                        }

                        BoolExpr[] exactlyOneOfN = new BoolExpr[] { context.MkAtMost(terms, 1), context.MkOr(terms) };
                        andGroup.Add(context.MkImplies(parent, context.MkAnd(exactlyOneOfN)));
                        alreadyHandledAlternativeOptions.Add(alternativeOptions);

                        // go-to label
                        handledAlternative: {}
                    }

                    // Excluded option(s) as cross-tree constraint(s)
                    List<List<AFeature>> nonAlternative = current.GetNonAlternativeExcludedOptions();
                    if (nonAlternative.Count > 0) {

                        foreach (var excludedOption in nonAlternative) {

                            BoolExpr[] orTerm = new BoolExpr[excludedOption.Count];
                            int i = 0;

                            foreach (var opt in excludedOption) {
                                BoolExpr target = (BoolExpr) optionToTerm[(Feature_Boolean) opt];
                                orTerm[i] = target;
                                i++;
                            }

                            andGroup.Add(context.MkImplies(expr_cur, context.MkNot(context.MkOr(orTerm))));
                        }
                    }
                }

                // Handle implies
                if (current.GetImpliedOptionsCount() > 0) {

                    foreach (List<AFeature> impliedOr in current.GetImpliedOptions()) {

                        BoolExpr[] orTerms = new BoolExpr[impliedOr.Count];

                        // Possible error: if a binary option implies a numeric option
                        for (int i = 0; i < impliedOr.Count; i++) {
                            orTerms[i] = (BoolExpr) optionToTerm[(Feature_Boolean) impliedOr[i]];
                        }

                        andGroup.Add(context.MkImplies(expr_cur, context.MkOr(orTerms)));
                    }
                }
            }

            // Parse the constraints (ranges, step) of the numeric features
            foreach (Feature_Range numOpt in vm.GetNumericOptions()) {

                Expr numExpression = optionToTerm[numOpt];
                List<float> allValues = numOpt.GetAllValues();
                List<BoolExpr> valueExpressions = new List<BoolExpr>();

                // add all possible values to an "OR" expression
                // => the value of this numeric option must be one of them
                foreach (float value in allValues) {
                     
                    FPNum fpNum = context.MkFPNumeral(value, context.MkFPSort32()); //context.MkFPSortDouble());
                    valueExpressions.Add(context.MkEq(numExpression, fpNum));

                    // NOT REQUIRED in VRVis
                    //if (!numericLookUpTable.ContainsKey(fpNum.ToString())) {
                    //    numericLookUpTable.Add(fpNum.ToString(), value);
                    //}
                }

                andGroup.Add(context.MkOr(valueExpressions.ToArray()));
            }

            // TODO: boolean cross-tree constraints
            // Parse the boolean cross-tree constraints
            /*
            foreach (string constraint in vm.BinaryConstraints) {

                bool and = false;
                string[] terms;
                if (constraint.Contains("&")) {
                    and = true;
                    terms = constraint.Split('&');
                }
                else
                    terms = constraint.Split('|');

                BoolExpr[] smtTerms = new BoolExpr[terms.Count()];
                int i = 0;
                foreach (string t in terms)
                {
                    string optName = t.Trim();

                    if (optName.StartsWith("-") || optName.StartsWith("!"))
                    {
                        optName = optName.Substring(1);
                        Feature_Boolean binOpt = vm.getBinaryOption(optName);
                        BoolExpr boolVar = (BoolExpr)optionToTerm[binOpt];
                        boolVar = context.MkNot(boolVar);
                        smtTerms[i] = boolVar;
                    }
                    else
                    {
                        Feature_Boolean binOpt = vm.getBinaryOption(optName);
                        BoolExpr boolVar = (BoolExpr)optionToTerm[binOpt];
                        smtTerms[i] = boolVar;
                    }


                    i++;
                }
                if (and)
                    andGroup.Add(context.MkAnd(smtTerms));
                else
                    andGroup.Add(context.MkOr(smtTerms));
            }
            */
            
            // ToDo: non-boolean constraints
            // Parse the non-boolean constraints
            /*
            Dictionary<Feature_Boolean, Expr> optionMapping = new Dictionary<Feature_Boolean, Expr>();
            if (vm.NonBooleanConstraints.Count > 0)
            {
                foreach (NonBooleanConstraint nonBooleanConstraint in vm.NonBooleanConstraints)
                {
                    andGroup.Add(ProcessMixedConstraint(nonBooleanConstraint, optionMapping, context, optionToTerm));
                }
            }
            */


            // Parse the mixed constraints
            // Note that this step is currently omitted due to critical performance issues.
            // Therefore, we check whether the mixed constraints are satisfied after finding the configuration.
            //if (vm.MixedConstraints.Count > 0)
            //{
                
                //foreach (MixedConstraint constr in vm.MixedConstraints)
                //{
                //    andGroup.Add(ProcessMixedConstraint(constr, optionMapping, context, optionToTerm));
                //}
            //}


            // return the initialized system
            BoolExpr generalConstraint = context.MkAnd(andGroup.ToArray());
            return new Tuple<Context, BoolExpr>(context, generalConstraint);
        }


        /// <summary>Generates a boolean variable with the given name.</summary>
        private static BoolExpr GenerateBooleanVariable(Context context, string name) {
            return context.MkBoolConst(name);
        }
        
        /// <summary>Generates a float variable with the given name.</summary>
        private static Expr GenerateFloatVariable(Context context, string name) {
            return context.MkConst(name, context.MkFPSort32()); // MkFPSortDouble());
        }

    }
}
