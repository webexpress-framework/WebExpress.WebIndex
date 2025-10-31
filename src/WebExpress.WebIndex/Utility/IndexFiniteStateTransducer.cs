using System;
using System.Collections.Generic;
using System.Text;

namespace WebExpress.WebIndex.Utility
{
    /// <summary>
    /// Represents a finite state transducer (FST) that transforms input strings into 
    /// output strings based on defined transitions. The transducer validates configuration 
    /// (start state and accept states) and processes input in a single pass using a 
    /// StringBuilder for efficiency.
    /// </summary>
    public class IndexFiniteStateTransducer
    {
        private readonly Dictionary<(int, char), (int ToState, char Output)> _transitions = [];
        private readonly HashSet<int> _acceptStates = [];
        private bool _startStateSet;
        private int _startState;

        /// <summary>
        /// Initializes a new instance of the finite state transducer.
        /// </summary>
        public IndexFiniteStateTransducer()
        {
        }

        /// <summary>
        /// Adds a transition to the transducer.
        /// </summary>
        /// <param name="fromState">The state from which the transition originates.</param>
        /// <param name="inputChar">The input character that triggers the transition.</param>
        /// <param name="toState">The state to which the transition leads.</param>
        /// <param name="outputChar">The output character produced by the transition.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when a transition for the same (fromState, inputChar) already exists.
        /// </exception>
        public void AddTransition(int fromState, char inputChar, int toState, char outputChar)
        {
            var key = (fromState, inputChar);

            if (_transitions.ContainsKey(key))
            {
                // do not silently overwrite configuration
                throw new ArgumentException($"Transition for state '{fromState}' with input '{inputChar}' already exists.", nameof(inputChar));
            }

            _transitions[key] = (toState, outputChar);
        }

        /// <summary>
        /// Sets the start state of the transducer.
        /// </summary>
        /// <param name="state">The start state.</param>
        public void SetStartState(int state)
        {
            _startState = state;
            _startStateSet = true;
        }

        /// <summary>
        /// Adds an accept state to the transducer.
        /// </summary>
        /// <param name="state">The accept state.</param>
        public void AddAcceptState(int state)
        {
            _acceptStates.Add(state);
        }

        /// <summary>
        /// Processes the input string and returns the transformed output string.
        /// </summary>
        /// <param name="input">The input string to be processed.</param>
        /// <returns>The output string resulting from the transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when start state is not set, no accept states are configured, a transition 
        /// is missing, or the final state is not accepting.
        /// </exception>
        public string ProcessInput(string input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (!_startStateSet)
            {
                // ensure configuration was provided before processing
                throw new InvalidOperationException("Start state is not set.");
            }

            if (_acceptStates.Count == 0)
            {
                // ensure there is at least one accepting state
                throw new InvalidOperationException("No accept states are configured.");
            }

            var currentState = _startState;
            var sb = new StringBuilder(input.Length);

            foreach (char c in input)
            {
                if (_transitions.TryGetValue((currentState, c), out var transition))
                {
                    currentState = transition.ToState;
                    sb.Append(transition.Output);
                }
                else
                {
                    // no valid transition found for the current state and character
                    throw new InvalidOperationException($"No transition defined for state '{currentState}' with input '{c}'.");
                }
            }

            if (_acceptStates.Contains(currentState))
            {
                return sb.ToString();
            }
            else
            {
                // ended in a non-accepting state
                throw new InvalidOperationException($"Final state '{currentState}' is not an accepting state.");
            }
        }

        /// <summary>
        /// Tries to process the input string and returns whether the input was accepted.
        /// </summary>
        /// <param name="input">The input string to be processed.</param>
        /// <param name="output">
        /// When this method returns, contains the transformed output if accepted; otherwise 
        /// an empty string.
        /// </param>
        /// <returns>True if the input was accepted; otherwise false.</returns>
        public bool TryProcessInput(string input, out string output)
        {
            output = string.Empty;

            if (input is null || !_startStateSet || _acceptStates.Count == 0)
            {
                // invalid configuration or input
                return false;
            }

            var currentState = _startState;
            var sb = new StringBuilder(input.Length);

            foreach (char c in input)
            {
                if (_transitions.TryGetValue((currentState, c), out var transition))
                {
                    currentState = transition.ToState;
                    sb.Append(transition.Output);
                }
                else
                {
                    // missing transition -> not accepted
                    return false;
                }
            }

            if (_acceptStates.Contains(currentState))
            {
                output = sb.ToString();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all transitions and accept states and resets the start state flag.
        /// </summary>
        public void Reset()
        {
            _transitions.Clear();
            _acceptStates.Clear();
            _startStateSet = false;
            _startState = default;
        }
    }
}