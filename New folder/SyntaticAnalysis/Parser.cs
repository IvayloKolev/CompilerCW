using Compiler.IO;
using Compiler.Tokenization;
using System.Collections.Generic;
using static Compiler.Tokenization.TokenType;
using Compiler.Nodes;

namespace Compiler.SyntacticAnalysis
{
    /// <summary>
    /// A recursive descent parser
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// The error reporter
        /// </summary>
        public ErrorReporter Reporter { get; }

        /// <summary>
        /// The tokens to be parsed
        /// </summary>
        private List<Token> tokens;

        /// <summary>
        /// The index of the current token in tokens
        /// </summary>
        private int currentIndex;

        /// <summary>
        /// The current token
        /// </summary>
        private Token CurrentToken { get { return tokens[currentIndex]; } }

        /// <summary>
        /// Advances the current token to the next one to be parsed
        /// </summary>
        private void MoveNext()
        {
            if (currentIndex < tokens.Count - 1)
                currentIndex += 1;
        }

        /// <summary>
        /// Creates a new parser
        /// </summary>
        /// <param name="reporter">The error reporter to use</param>
        public Parser(ErrorReporter reporter)
        {
            Reporter = reporter;
        }

        /// <summary>
        /// Checks the current token is the expected kind and moves to the next token
        /// </summary>
        /// <param name="expectedType">The expected token type</param>
        private void Accept(TokenType expectedType)
        {
            if (CurrentToken.Type == expectedType)
            {
                Debugger.Write($"Accepted {CurrentToken}");
                MoveNext();
            }
            else
            {
                Debugger.Write($"Type mismatch - Current token {CurrentToken} isn't of type {expectedType}.  {CurrentToken.Position}");
                MoveNext();
            }
        }

        /// <summary>
        /// Parses a program
        /// </summary>
        /// <param name="tokens">The tokens to parse</param>
        /// <returns>The abstract syntax tree resulting from the parse</returns>
        public ProgramNode Parse(List<Token> tokens)
        {
            this.tokens = tokens;
            ProgramNode program = ParseProgram();
            return program;
        }


        /// <summary>
        /// Parses a program
        /// </summary>
        private ProgramNode ParseProgram()
        {
            Debugger.Write("Parsing program");
            ICommandNode command = ParseCommand();
            return new ProgramNode(command);
        }

        /// <summary>
        /// Parses a command
        /// </summary>
        /// <returns>An abstract syntax tree representing the command</returns>
        private ICommandNode ParseCommand()
        {
            Debugger.Write("Parsing command");
            List<ICommandNode> commands = new List<ICommandNode>();
            ICommandNode lastCommand = ParseSingleCommand();

            while (CurrentToken.Type == Semicolon)
            {
                Accept(Semicolon);
                ICommandNode nextCommand = ParseSingleCommand();
                if (nextCommand != null)
                    commands.Add(nextCommand);
            }

            // Return the last command directly if it is LetCommandNode
            if (lastCommand != null && lastCommand is LetCommandNode)
                return lastCommand;

            // Only create a SequentialCommandNode if there are multiple commands
            if (commands.Count > 0)
                commands.Add(lastCommand);

            return new SequentialCommandNode(commands);
        }


        /// <summary>
        /// Parses a single command
        /// </summary>
        /// <returns>An abstract syntax tree representing the single command</returns>
        private ICommandNode ParseSingleCommand()
        {
            Debugger.Write("Parsing Single Command");

            ICommandNode command = null;

            switch (CurrentToken.Type)
            {
                case Identifier:
                    command = ParseAssignmentOrCallCommand();
                    break;
                case If:
                    command = ParseIfCommand();
                    break;
                case While:
                    command = ParseWhileCommand();
                    break;
                case Let: 
                    command = ParseLetCommand();
                    break;
                case With:
                    command = ParseWithCommand();
                    break;
                case LeftBrace:
                    command = ParseBeginCommand();
                    break;
                default:
                    Debugger.Write($"Error: Command {CurrentToken} at line {CurrentToken.Position} not recognised");
                    command = ParseSkipCommand();
                    MoveNext();
                    break;
            }

            return command;
        }

        /// <summary>
        /// Parses an assignment or call command
        /// </summary>
        private ICommandNode ParseAssignmentOrCallCommand()
        {
            Debugger.Write("Parsing Assignment Command or Call Command");

            IdentifierNode identifier = ParseIdentifier();

            if (CurrentToken.Type == LeftBracket)
            {
                Debugger.Write("Parsing Call Command");
                Accept(LeftBracket);
                IParameterNode parameter = ParseParameter();
                Accept(RightBracket);
                return new CallCommandNode(identifier, parameter);
            }
            else if (CurrentToken.Type == Becomes)
            {
                Debugger.Write("Parsing Assignment Command");
                Accept(Becomes);
                IExpressionNode expression = ParseExpression();
                return new AssignCommandNode(identifier, expression);
            }

            // Handle error or unexpected token
            Debugger.Write($"Error: Unexpected token {CurrentToken} in assignment or call command at position {CurrentToken.Position}");
            MoveNext(); // Move to the next token to avoid an infinite loop

            return null; // Return null or handle the error as appropriate in your implementation
        }

        /// <summary>
        /// Parses a begin command
        /// </summary>
        /// <returns>An abstract syntax tree representing the begin command</returns>
        // Add this method to your existing Parser class

        /// <summary>
        /// Parses a begin command
        /// </summary>
        /// <returns>An abstract syntax tree representing the begin command</returns>
        private ICommandNode ParseBeginCommand()
        {
            Debugger.Write("Parsing Begin Command");
            Accept(LeftBrace);

            List<ICommandNode> commands = new List<ICommandNode>();

            // Parse the sequence of commands within the braces
            while (CurrentToken.Type != RightBrace)
            {
                ICommandNode command = ParseCommand();
                commands.Add(command);

                // Check for optional semicolon between commands
                if (CurrentToken.Type == Semicolon)
                {
                    Accept(Semicolon);
                }
            }

            // Make sure to accept the closing brace
            Accept(RightBrace);

            // If there's only one command, return it directly
            if (commands.Count == 1)
                return commands[0];
            else
                return new SequentialCommandNode(commands);
        }


        /// <summary>
        /// Parses a while command
        /// </summary>
        private ICommandNode ParseWhileCommand()
        {
            Debugger.Write("Parsing While Command");
            Accept(While);
            IExpressionNode condition = ParseExpression(); // Assuming ParseExpression returns IExpressionNode
            Accept(Do);
            ICommandNode body = ParseSingleCommand(); // Assuming ParseSingleCommand returns ICommandNode
            return new WhileCommandNode(condition, body, CurrentToken.Position);
        }

        /// <summary>
        /// Parses an if command
        /// </summary>
        private ICommandNode ParseIfCommand()
        {
            Debugger.Write("Parsing If Command");
            Accept(If);
            IExpressionNode condition = ParseExpression();

            if (CurrentToken.Type == Unless)
            {
                Accept(Unless);
                IExpressionNode unlessCondition = ParseExpression();
                ICommandNode elseCommand = ParseSingleCommand(); // Parse the else command
                return new IfUnlessCommandNode(condition, unlessCondition, elseCommand, CurrentToken.Position);
            }
            else
            {
                Accept(Else);
                ICommandNode elseCommand = ParseSingleCommand();

                return new IfCommandNode(condition, elseCommand, CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses a let command
        /// </summary>
        /// <returns>An abstract syntax tree representing the let command</returns>
        private ICommandNode ParseLetCommand()
        {
            Debugger.Write("Parsing Let Command");
            Position startPosition = CurrentToken.Position;
            Accept(Let);

            // Parse the declaration
            IDeclarationNode declaration = ParseDeclaration();
            if (declaration == null)
            {
                // Handle the error or unexpected token for the declaration
                Debugger.Write($"Error: Unexpected token {CurrentToken} at position {CurrentToken.Position} in the declaration of Let command");
                return null;
            }

            Accept(In);
            // Skip irrelevant tokens until the start of the command block
            while (CurrentToken.Type != LeftBrace)
            {
                MoveNext();
            }

            // Parse the subsequent command
            ICommandNode command = ParseCommand();
            if (command == null)
            {
                // Handle the error or unexpected token for the subsequent command
                Debugger.Write($"Error: Unexpected token {CurrentToken} at position {CurrentToken.Position} command of Let command");
                return null;
            }

            return new LetCommandNode(declaration, command, startPosition);
        }

        /// <summary>
        /// Parses a with command
        /// </summary>
        private ICommandNode ParseWithCommand()
        {
            Debugger.Write("Parsing With Command");

            Accept(With);
            IDeclarationNode declaration = ParseSingleDeclaration();
            Accept(Do);
            ICommandNode command = ParseCommand();
            Accept(Done);

            return new WithCommandNode(declaration, command, CurrentToken.Position);
        }

        /// <summary>
        /// Parses a parameter
        /// </summary>
        private IParameterNode ParseParameter()
        {
            Debugger.Write("Parsing Parameter");

            switch (CurrentToken.Type)
            {
                case RightBracket:
                    // Case: Empty parameter list
                    Accept(RightBracket);
                    return new BlankParameterNode(CurrentToken.Position);

                case In:
                    // Case: Value parameter
                    return ParseValueParameter();

                case Out:
                    // Case: Variable parameter
                    return ParseOutParameter();

                default:
                    // Handle error for unexpected token
                    Debugger.Write($"Error: Unexpected token in parameter at position: {CurrentToken.Position}");
                    MoveNext(); // Move to the next token to avoid an infinite loop
                    return null;
            }
        }

        /// <summary>
        /// Parses a value parameter
        /// </summary>
        private IParameterNode ParseValueParameter()
        {
            Debugger.Write("Parsing Value Parameter");
            Accept(In);
            IExpressionNode expressionNode = ParseExpression();
            return new ValueParameterNode(expressionNode);
        }

        /// <summary>
        /// Parses a variable parameter
        /// </summary>
        private IParameterNode ParseOutParameter()
        {
            Debugger.Write("Parsing Variable Parameter");
            Accept(Out);
            IdentifierNode identifierNode = ParseIdentifier();
            return new VarParameterNode(identifierNode, CurrentToken.Position);
        }

        /// <summary>
        /// Parses an expression
        /// </summary>
        private IExpressionNode ParseExpression()
        {
            Debugger.Write("Parsing Expression");

            IExpressionNode expressionNode = ParsePrimaryExpression();

            while (CurrentToken.Type == Operator)
            {
                OperatorNode operatorNode = ParseOperator();
                IExpressionNode rightExpressionNode = ParsePrimaryExpression();
                expressionNode = new BinaryExpressionNode(expressionNode, operatorNode, rightExpressionNode);
            }

            return expressionNode;
        }

        /// <summary>
        /// Parses a primary expression
        /// </summary>
        private IExpressionNode ParsePrimaryExpression()
        {
            Debugger.Write("Parsing Primary Expression");

            IExpressionNode expressionNode = null;

            switch (CurrentToken.Type)
            {
                case IntLiteral:
                    expressionNode = ParseIntExpression();
                    break;
                case CharLiteral:
                    expressionNode = ParseCharExpression();
                    break;
                case Identifier:
                    expressionNode = ParseIdExpression();
                    if (CurrentToken.Type == LeftBracket)
                    {
                        // Handle optional parameter list
                        ParseParameter();
                    }
                    break;
                case Operator:
                    expressionNode = ParseUnaryExpression();
                    break;
                case LeftBracket:
                    Accept(LeftBracket);
                    expressionNode = ParseExpression();
                    Accept(RightBracket);
                    break;
            }
            return expressionNode;
        }

        /// <summary>
        /// Parses an int expression
        /// </summary>
        private IExpressionNode ParseIntExpression()
        {
            Debugger.Write("Parsing Int Expression");
            IntegerLiteralNode intLiteralNode = ParseIntegerLiteral();
            return new IntegerExpressionNode(intLiteralNode);
        }

        /// <summary>
        /// Parses a char expression
        /// </summary>
        private IExpressionNode ParseCharExpression()
        {
            Debugger.Write("Parsing Char Expression");
            CharacterLiteralNode charLiteralNode = ParseCharacterLiteral();
            return new CharacterExpressionNode(charLiteralNode);
        }

        /// <summary>
        /// Parses an ID expression
        /// </summary>
        private IExpressionNode ParseIdExpression()
        {
            Debugger.Write("Parsing Call Expression or Identifier Expression");
            IdentifierNode identifierNode = ParseIdentifier();
            return new IdExpressionNode(identifierNode);
        }

        /// <summary>
        /// Parses a unary expresion
        /// </summary>
        private IExpressionNode ParseUnaryExpression()
        {
            Debugger.Write("Parsing Unary Expression");
            OperatorNode operatorNode = ParseOperator();
            IExpressionNode primaryExpression = ParsePrimaryExpression();

            return new UnaryExpressionNode(operatorNode, primaryExpression);
        }

        /// <summary>
        /// Parses a declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the declaration</returns>
        private IDeclarationNode ParseDeclaration()
        {
            Debugger.Write("Parsing Declaration");
            List<IDeclarationNode> declarations = new List<IDeclarationNode>();

            // Parse the first declaration
            IDeclarationNode firstDeclaration = ParseSingleDeclaration();
            if (firstDeclaration == null)
            {
                // Handle the error or unexpected token for the first declaration
                Debugger.Write($"Error: Unexpected token {CurrentToken} at position {CurrentToken.Position} in the first declaration");
                return null;
            }

            declarations.Add(firstDeclaration);

            // Continue parsing additional declarations with semicolons
            while (CurrentToken.Type == Semicolon && CurrentToken.Type != In)
            {
                Accept(Semicolon);

                // Parse the next declaration
                IDeclarationNode nextDeclaration = ParseSingleDeclaration();

                if (nextDeclaration == null)
                {
                    // Handle the error or unexpected token for the next declaration
                    Debugger.Write($"Error: Unexpected token {CurrentToken} at position {CurrentToken.Position} in a subsequent declaration");
                    break; // Exit the loop to avoid an infinite loop
                }

                declarations.Add(nextDeclaration);
            }

            if (declarations.Count == 1)
                return declarations[0];
            else
                return new SequentialDeclarationNode(declarations);
        }

        /// <summary>
        /// Parses a single declaration
        /// </summary>
        private IEntityDeclarationNode ParseSingleDeclaration()
        {
            Debugger.Write("Parsing Single Declaration");
            IdentifierNode identifier = ParseIdentifier();

            if (CurrentToken.Type == Is)
            {
                Debugger.Write("Parsing Constant Declaration");
                Accept(Is);
                IExpressionNode expression = ParseExpression();
                return new ConstDeclarationNode(identifier, expression, CurrentToken.Position);
            }
            else if (CurrentToken.Type == Colon)
            {
                Debugger.Write("Parsing Variable Declaration");
                Accept(Colon);
                TypeDenoterNode typeDenoter = ParseTypeDenoter();
                return new VarDeclarationNode(identifier, typeDenoter, CurrentToken.Position);
            }
            else
            {
                // Handle error or unexpected token
                Debugger.Write($"Error: Unexpected token {CurrentToken} at position {CurrentToken.Position} in single declaration");
                MoveNext(); // Move to the next token to avoid an infinite loop
                return null;
            }
        }

        /// <summary>
        /// Parses a type denoter
        /// </summary>
        /// <returns>An abstract syntax tree representing the type denoter</returns>
        private TypeDenoterNode ParseTypeDenoter()
        {
            Debugger.Write("Parsing Type Denoter");
            return new TypeDenoterNode(ParseIdentifier());
        }

        /// <summary>
        /// Parses an integer literal
        /// </summary>
        /// <returns>An abstract syntax tree representing the integer literal</returns>
        private IntegerLiteralNode ParseIntegerLiteral()
        {
            Debugger.Write("Parsing integer literal");
            Token intLiteralToken = CurrentToken;
            Accept(IntLiteral);
            return new IntegerLiteralNode(intLiteralToken);
        }

        /// <summary>
        /// Parses a character literal
        /// </summary>
        /// <returns>An abstract syntax tree representing the character literal</returns>
        private CharacterLiteralNode ParseCharacterLiteral()
        {
            Debugger.Write("Parsing character literal");
            Token charLiteralToken = CurrentToken;
            Accept(CharLiteral);
            return new CharacterLiteralNode(charLiteralToken);
        }

        /// <summary>
        /// Parses an identifier
        /// </summary>
        /// <returns>An abstract syntax tree representing the identifier</returns>
        private IdentifierNode ParseIdentifier()
        {
            Debugger.Write("Parsing identifier");
            Token identifierToken = CurrentToken;
            Accept(Identifier);
            return new IdentifierNode(identifierToken);
        }

        /// <summary>
        /// Parses an operator
        /// </summary>
        /// <returns>An abstract syntax tree representing the operator</returns>
        private OperatorNode ParseOperator()
        {
            Debugger.Write("Parsing operator");
            Token OperatorToken = CurrentToken;
            Accept(Operator);
            return new OperatorNode(OperatorToken);
        }

        /// <summary>
        /// Parses a skip command
        /// </summary>
        /// <returns>An abstract syntax tree representing the skip command</returns>
        private ICommandNode ParseSkipCommand()
        {
            Debugger.Write($"Parsing Skip Command at position {CurrentToken.Position}");
            Position startPosition = CurrentToken.Position;
            return new BlankCommandNode(startPosition);
        }
    }
}