# Contributing Guidelines for Students

Welcome! This guide will help you contribute to these educational projects.

## Getting Started

1. **Fork or clone the repository**
2. **Create a branch**: `git checkout -b feature/your-feature-name`
3. **Set up environment**: 
   - Copy `.env.example` to `.env` and fill in your values
   - **Important**: 
     - ✅ **`.env.example`** is committed (template - no secrets)
     - ❌ **`.env`** is ignored (your local config with actual values)

## Development Setup

```bash
# Clone the repository
git clone https://github.com/your-org/project.git
cd project

# Copy .env.example to .env and fill in your values
cp .env.example .env
# Edit .env with your actual configuration values

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the project
dotnet run --project Publisher/Publisher.csproj
```

**Important**: The `.env` file is automatically loaded by the application at startup via the DotNetEnv package. When you build and run the project, the `.env` file is copied to the output directory (bin folder) and will be available at runtime.

## Code Standards

### C# Style
- Use C# 12.0 features
- Follow Microsoft naming conventions
- Use `async`/`await` for I/O operations
- Use dependency injection
- Write clear, readable code with comments

### Project Structure
```
Project/
├── Domain/           # Domain models
├── Application/      # DTOs, validators
├── Infrastructure/   # Data access
├── Publisher/        # API service
├── Subscriber/       # Background service
└── Tests/            # Unit tests
```

### Code Organization
- One class per file (usually)
- Organize using statements alphabetically
- Keep methods focused and simple
- Add comments explaining complex logic
- Use meaningful variable names

## Commit Guidelines

### Format
```
<type>: <description>

<optional details>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `refactor`: Reorganize code
- `test`: Add/fix tests
- `chore`: Dependency updates

### Examples
```
feat: add order validation
fix: handle null reference exception
docs: update README
test: add customer repository tests
```

## Security Reminders

Before committing:
- ❌ No hardcoded passwords or API keys
- ❌ No `.env` file committed
- ✅ Input validation implemented
- ✅ Use parameterized queries (prevent SQL injection)

## Testing Requirements

- Write unit tests for new features
- Verify tests pass locally before pushing: `dotnet test`
- Test both success and error cases

### Running Tests
```bash
dotnet test
dotnet test --filter Category=Integration
```

## Pull Request Process

1. **Write clean code** with comments where needed
2. **Add/update tests** for your changes
3. **Run tests locally**: `dotnet test`
4. **Build solution**: `dotnet build`
5. **Create PR** with a clear description
6. **Respond to feedback** from reviewers

## Code Review Checklist

When submitting code, consider:
- [ ] Does the code work correctly?
- [ ] Are there any obvious bugs?
- [ ] Is the code readable?
- [ ] Are edge cases handled?
- [ ] Are there tests?
- [ ] Is security considered? (no hardcoded secrets)
- [ ] Are comments helpful?

## Tips for Good Code

### Do's
- ✅ Keep functions small and focused
- ✅ Use meaningful names for variables/methods
- ✅ Add comments explaining "why", not "what"
- ✅ Handle errors appropriately
- ✅ Test your code
- ✅ Ask for help if stuck

### Don'ts
- ❌ Large functions doing many things
- ❌ Single-letter variable names (except `i` in loops)
- ❌ Commented-out code
- ❌ Ignoring exceptions
- ❌ Hardcoding values

## Documentation

For new features:
- Update README if needed
- Add XML comments for public methods
- Explain complex logic
- Include code examples for new features

## Questions?

- Create an issue to ask
- Comment on relevant pull requests
- Ask your instructor or peers
- Check Microsoft Learn documentation

---

**Remember**: The goal is to learn! Don't be afraid to ask questions or make mistakes. That's how we grow as developers. 🚀
