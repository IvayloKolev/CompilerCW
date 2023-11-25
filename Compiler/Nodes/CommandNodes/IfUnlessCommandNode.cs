namespace Compiler.Nodes
{
    /// <summary>
    /// A node corresponding to an if command with an unless condition
    /// </summary>
    public class IfUnlessCommandNode : ICommandNode
    {
        /// <summary>
        /// The condition expression
        /// </summary>
        public IExpressionNode IfCondition { get; }

        /// <summary>
        /// The unless condition expression
        /// </summary>
        public IExpressionNode UnlessCondition { get; }

        /// <summary>
        /// The if branch command
        /// </summary>
        public ICommandNode IfCommand { get; }

        /// <summary>
        /// The else branch command
        /// </summary>
        public ICommandNode ElseCommand { get; }

        /// <summary>
        /// The position in the code where the content associated with the node begins
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Creates a new if command node with an unless condition
        /// </summary>
        /// <param name="ifcondition">The condition expression</param>
        /// <param name="unlesscondition">The unless condition expression</param>
        /// <param name="elseCommand">The else branch command</param>
        /// <param name="position">The position in the code where the content associated with the node begins</param>
        public IfUnlessCommandNode(IExpressionNode ifCondition, 
                                   IExpressionNode unlessCondition, 
                                   ICommandNode command, 
                                   ICommandNode elseCommand, 
                                   Position position)
        {
            IfCondition = ifCondition;
            UnlessCondition = unlessCondition;
            IfCommand = command;
            ElseCommand = elseCommand;
            Position = position;
        }
    }
}
