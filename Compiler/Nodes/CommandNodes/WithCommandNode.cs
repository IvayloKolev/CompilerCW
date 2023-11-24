namespace Compiler.Nodes
{
    /// <summary>
    /// A node corresponding to a with command
    /// </summary>
    public class WithCommandNode : ICommandNode
    {
        /// <summary>
        /// The declaration in the with command
        /// </summary>
        public IDeclarationNode Declaration { get; }

        /// <summary>
        /// The command in the with command
        /// </summary>
        public ICommandNode Command { get; }

        /// <summary>
        /// The position in the code where the content associated with the node begins
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Creates a new with command node
        /// </summary>
        /// <param name="declaration">The declaration in the with command</param>
        /// <param name="command">The command in the with command</param>
        /// <param name="position">The position in the code where the content associated with the node begins</param>
        public WithCommandNode(IDeclarationNode declaration, ICommandNode command, Position position)
        {
            Declaration = declaration;
            Command = command;
            Position = position;
        }
    }
}
