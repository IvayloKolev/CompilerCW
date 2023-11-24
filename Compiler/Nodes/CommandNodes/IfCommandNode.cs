namespace Compiler.Nodes
{
    /// <summary>
    /// A node corresponding to an if command
    /// </summary>
    public class IfCommandNode : ICommandNode
    {
        /// <summary>
        /// The condition expression
        /// </summary>
        public IExpressionNode IfCondition { get; }

        /// <summary>
        /// The command in the if branch
        /// </summary>
        public ICommandNode IfCommand { get; }

        /// <summary>
        /// The command in the else branch
        /// </summary>
        public ICommandNode ElseCommand { get; }

        /// <summary>
        /// The position in the code where the content associated with the node begins
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Creates a new if command node
        /// </summary>
        /// <param name="condition">The condition expression</param>
        /// <param name="elseCommand">The command in the else branch</param>
        /// <param name="position">The position in the code where the content associated with the node begins</param>
        public IfCommandNode(IExpressionNode ifcondition,
                             ICommandNode ifCommand,
                             ICommandNode elseCommand, 
                             Position position)
        {
            IfCondition = ifcondition;
            IfCommand = ifCommand;
            ElseCommand = elseCommand;
            Position = position;
        }
    }
}
