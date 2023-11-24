using Compiler.IO;
using Compiler.Nodes;
using Compiler.Tokenization;
using System.Collections.Generic;
using static Compiler.Tokenization.TokenType;

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
        /// Peeks the next token without consuming it
        /// </summary>
        /// <returns> The next token </returns>
        private Token PeekNext()
        {
            if (currentIndex < tokens.Count - 1)
            {
                return tokens[currentIndex + 1];
            } else
            {
                return null;
            }
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
        /// <returns>An abstract syntax tree representing the program</returns>
        private ProgramNode ParseProgram()
        {
            Debugger.Write("Parsing program");
            ICommandNode command = ParseCommand();
            ProgramNode program = new ProgramNode(command);
            return program;
        }

        /// <summary>
        /// Parses a Command
        /// </summary>
        /// <returns>An abstract syntax tree representing the Command</returns>
        private ICommandNode ParseCommand()
        {
            Debugger.Write("Parsing Command");
            List<ICommandNode> commands = new List<ICommandNode>();
            commands.Add(ParseSingleCommand());
            while (CurrentToken.Type == Semicolon)
            {
                Accept(Semicolon);
                commands.Add(ParseSingleCommand());
            }
            if (commands.Count == 1)
                return commands[0];
            else
                return new SequentialCommandNode(commands);
        }

        /// <summary>
        /// Parses a single Command
        /// </summary>
        /// <returns>An abstract syntax tree representing the single Command</returns>
        private ICommandNode ParseSingleCommand()
        {
            Debugger.Write("Parsing Single Command");
            switch (CurrentToken.Type)
            {
                case Identifier:
                    return ParseAssignmentOrCallCommand();
                case OpeningBrace:
                    return ParseBeginCommand();
                case Let:
                    return ParseLetCommand();
                case If:
                    return ParseIfCommand();
                case While:
                    return ParseWhileCommand();
                case With:
                    return ParseWithCommand();
                default:
                    return ParseSkipCommand();
            }
        }

        /// <summary>
        /// Parses an assignment or call Command
        /// </summary>
        /// <returns>An abstract syntax tree representing the assignment or call Command</returns>
        private ICommandNode ParseAssignmentOrCallCommand()
        {
            Debugger.Write("Parsing Assignment Command or Call Command");
            Position startPosition = CurrentToken.Position;

            IdentifierNode identifier = ParseIdentifier();
            
            if (CurrentToken.Type == Becomes)
            {
                Debugger.Write("Parsing Assignment Command");

                Accept(Becomes);

                IExpressionNode expression = ParseExpression();
                return new AssignCommandNode(identifier, expression);
            } else if (CurrentToken.Type == OpeningParenthesis)
            {
                Debugger.Write("Parsing Call Command");

                Accept(OpeningParenthesis);
                IParameterNode parameter = ParseParameter();
                Accept(ClosingParenthesis);

                return new CallCommandNode(identifier, parameter);
            }
            else
            {
                return new ErrorNode(startPosition);
            }
        }

        /// <summary>
        /// Parses a skip Command
        /// </summary>
        /// <returns>An abstract syntax tree representing the skip Command</returns>
        private ICommandNode ParseSkipCommand()
        {
            Debugger.Write("Parsing Skip Command");
            Position startPosition = CurrentToken.Position;
            return new BlankCommandNode(startPosition);
        }

        /// <summary>
        /// Parses a while Command
        /// </summary>
        /// <returns>An abstract syntax tree representing the while Command</returns>
        private ICommandNode ParseWhileCommand()
        {
            Debugger.Write("Parsing While Command");
            Position startPosition = CurrentToken.Position;

            Accept(While);
            IExpressionNode expression = ParseParenthesesExpression();

            Accept(Do);
            ICommandNode command = ParseCommand();
            return new WhileCommandNode(expression, command, startPosition);
        }

        /// <summary>
        /// Parses an ifElseCommand or an ifUnlessCommand
        /// </summary>
        /// <returns></returns>
        private ICommandNode ParseIfCommand()
        {
            // Check if it's an "if" or "if unless" unlessCommand
            Position startPosition = CurrentToken.Position;
            Accept(If);

            IExpressionNode ifCondition = ParseParenthesesExpression();

            // Check for "unless" and parse accordingly
            if (CurrentToken.Type == Unless)
            {
                Accept(Unless);
                IExpressionNode unlessCondition = ParseParenthesesExpression();
                ICommandNode ifCommand = ParseCommand();

                Accept(Else);
                ICommandNode elseCommand = ParseSingleCommand();
                return new IfUnlessCommandNode(ifCondition, unlessCondition, ifCommand, elseCommand, startPosition);
            }
            else
            {
                ICommandNode ifCommand = ParseCommand();
                Accept(Else);
                ICommandNode elseCommand = ParseSingleCommand();
                return new IfCommandNode(ifCondition, ifCommand, elseCommand, startPosition);
            }
        }

        /// <summary>
        /// Parses a let in Command
        /// </summary>
        /// <returns>An abstract syntax tree representing the let in Command</returns>
        private ICommandNode ParseLetCommand()
        {
            Debugger.Write("Parsing Let Command");
            Position startPosition = CurrentToken.Position;
            Accept(Let);
            IDeclarationNode declaration = ParseDeclaration();
            Accept(In);
            ICommandNode command = ParseSingleCommand();
            return new LetCommandNode(declaration, command, startPosition);
        }

        /// <summary>
        /// Parses a with Command
        /// </summary>
        /// <returns>An abstract syntax tree representing the with Command</returns>
        private ICommandNode ParseWithCommand()
        {
            Debugger.Write("Parsing With Command");

            Position startPosition = CurrentToken.Position;
            Accept(With);

            IDeclarationNode declaration = ParseDeclaration();
            Accept(Do);

            ICommandNode command = ParseCommand();
            Accept(Done);

            return new WithCommandNode(declaration, command, startPosition);
        }

        /// <summary>
        /// Parses a begin Command
        /// </summary>
        /// <returns>An abstract syntax tree representing the begin Command</returns>
        private ICommandNode ParseBeginCommand()
        {
            Debugger.Write("Parsing Begin Command");
            Accept(OpeningBrace);
            ICommandNode command = ParseCommand();
            Accept(ClosingBrace);
            return command;
        }

        /// <summary>
        /// Parses a declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the declaration</returns>
        private IDeclarationNode ParseDeclaration()
        {
            Debugger.Write("Parsing Declaration");

            List<IDeclarationNode> declarations = new List<IDeclarationNode>();
            declarations.Add(ParseSingleDeclaration());

            while (CurrentToken.Type == Semicolon)
            {
                Accept(Semicolon);
                Token nextToken = PeekNext();
                Debugger.Write($"Parsed declaration of {CurrentToken} \n Next token is {nextToken}");

                if (nextToken.Type == Identifier) {
                    declarations.Add(ParseSingleDeclaration());
                }
                else { break; }
            }
            if (declarations.Count == 1)
                return declarations[0];
            else
                return new SequentialDeclarationNode(declarations);
        }

        /// <summary>
        /// Parses a single declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the single declaration</returns>
        private IDeclarationNode ParseSingleDeclaration()
        {
            Position StartingPosition = CurrentToken.Position;
            Debugger.Write("Parsing Single Declaration");
            IdentifierNode identifier = ParseIdentifier();

            switch (CurrentToken.Type)
            {
                case Is:
                    // It's a constant declaration
                    Accept(Is);
                    IExpressionNode expression = ParseExpression();
                    return new ConstDeclarationNode(identifier, expression, StartingPosition);

                case Colon:
                    // It's a variable declaration
                    Accept(Colon);
                    TypeDenoterNode typeDenoter = ParseTypeDenoter();
                    return new VarDeclarationNode(identifier, typeDenoter, StartingPosition);

                default:
                    // If it's neither '=', nor '~', it's an error
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses a type denoter
        /// </summary>
        /// <returns>An abstract syntax tree representing the type denoter</returns>
        private TypeDenoterNode ParseTypeDenoter()
        {
            Debugger.Write("Parsing Type Denoter");
            IdentifierNode identifier = ParseIdentifier();
            return new TypeDenoterNode(identifier);
        }

        /// <summary>
        /// Parses an expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the expression</returns>
        private IExpressionNode ParseExpression()
        {
            Debugger.Write("Parsing Expression");
            IExpressionNode leftExpression = ParsePrimaryExpression();
            while (CurrentToken.Type == Operator)
            {
                OperatorNode operation = ParseOperator();
                IExpressionNode rightExpression = ParsePrimaryExpression();
                leftExpression = new BinaryExpressionNode(leftExpression, operation, rightExpression);
            }
            return leftExpression;
        }

        /// <summary>
        /// Parses a primary expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the primary expression</returns>
        private IExpressionNode ParsePrimaryExpression()
        {
            Debugger.Write("Parsing Primary Expression");
            switch (CurrentToken.Type)
            {
                case IntLiteral:
                    return ParseIntExpression();
                case CharLiteral:
                    return ParseCharExpression();
                case Identifier:
                    return ParseIdentifierExpression();
                case Operator:
                    return ParseUnaryExpression();
                case OpeningParenthesis:
                    return ParseParenthesesExpression();
                default:
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses an int expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the int expression</returns>
        private IExpressionNode ParseIntExpression()
        {
            Debugger.Write("Parsing Int Expression");
            IntegerLiteralNode intLit = ParseIntegerLiteral();
            return new IntegerExpressionNode(intLit);
        }

        /// <summary>
        /// Parses a char expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the char expression</returns>
        private IExpressionNode ParseCharExpression()
        {
            Debugger.Write("Parsing Char Expression");
            CharacterLiteralNode charLit = ParseCharacterLiteral();
            return new CharacterExpressionNode(charLit);
        }

        /// <summary>
        /// Parses an ID expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the expression</returns>
        private IExpressionNode ParseIdentifierExpression()
        {
            Debugger.Write("Parsing Identifier Expression");
            IdentifierNode identifier = ParseIdentifier();
            return new IdentifierExpressionNode(identifier);
        }

        /// <summary>
        /// Parses a unary expresion
        /// </summary>
        /// <returns>An abstract syntax tree representing the unary expression</returns>
        private IExpressionNode ParseUnaryExpression()
        {
            Debugger.Write("Parsing Unary Expression");
            OperatorNode operation = ParseOperator();
            IExpressionNode expression = ParsePrimaryExpression();
            return new UnaryExpressionNode(operation, expression);
        }

        /// <summary>
        /// Parses a arenthesis expression
        /// </summary>
        /// <returns>An abstract syntax tree representing an expression in parentheses</returns>
        private IExpressionNode ParseParenthesesExpression()
        {
            Debugger.Write("Parsing Bracket Expression");
            Accept(OpeningParenthesis);
            IExpressionNode expression = ParseExpression();
            Accept(ClosingParenthesis);
            return expression;
        }

        /// <summary>
        /// Parses a parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the parameter</returns>
        private IParameterNode ParseParameter()
        {
            Debugger.Write("Parsing Parameter");
            switch (CurrentToken.Type)
            {
                case Identifier:
                case IntLiteral:
                case CharLiteral:
                case Operator:
                case In:
                    return ParseValueParameter();
                case Out:
                    return ParseVarParameter();
                case ClosingParenthesis:
                    return new BlankParameterNode(CurrentToken.Position);
                default:
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses an expression parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the expression parameter</returns>
        private IParameterNode ParseValueParameter()
        {
            Debugger.Write("Parsing Value Parameter");
            Accept(In);
            IExpressionNode expression = ParseExpression();
            return new ValueParameterNode(expression);
        }

        /// <summary>
        /// Parses a variable parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the variable parameter</returns>
        private IParameterNode ParseVarParameter()
        {
            Debugger.Write("Parsing Variable Parameter");
            Accept(Out);
            IdentifierNode identifierNode = ParseIdentifier();
            return new VarParameterNode(identifierNode, CurrentToken.Position);
        }

        /// <summary>
        /// Parses an integer literal
        /// </summary>
        /// <returns>An abstract syntax tree representing the integer literal</returns>
        private IntegerLiteralNode ParseIntegerLiteral()
        {
            Debugger.Write("Parsing integer literal");
            Token integerLiteralToken = CurrentToken;
            Accept(IntLiteral);
            return new IntegerLiteralNode(integerLiteralToken);
        }

        /// <summary>
        /// Parses a character literal
        /// </summary>
        /// <returns>An abstract syntax tree representing the character literal</returns>
        private CharacterLiteralNode ParseCharacterLiteral()
        {
            Debugger.Write("Parsing character literal");
            Token CharacterLiteralToken = CurrentToken;
            Accept(CharLiteral);
            return new CharacterLiteralNode(CharacterLiteralToken);
        }

        /// <summary>
        /// Parses an identifier
        /// </summary>
        /// <returns>An abstract syntax tree representing the identifier</returns>
        private IdentifierNode ParseIdentifier()
        {
            Debugger.Write("Parsing identifier");
            Token IdentifierToken = CurrentToken;
            Accept(Identifier);
            return new IdentifierNode(IdentifierToken);
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
    }
}