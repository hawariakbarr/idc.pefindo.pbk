# idc.pefindo.pbk

Dotnet version 8

# Git Commit Message Guidelines

## Commit Message Structure

```
<type>(<scope>): <subject>

<body>

<footer>
```

## 1. Type Prefix

Always start with a type prefix in lowercase:

* `feat`: New feature
* `fix`: Bug fix
* `docs`: Documentation changes
* `style`: Code style changes (formatting, missing semi-colons, etc.)
* `refactor`: Code refactoring
* `perf`: Performance improvements
* `test`: Adding or modifying tests
* `chore`: Maintenance tasks, dependency updates, etc.
* `ci`: CI/CD related changes
* `revert`: Reverting a previous commit

## 2. Scope (Optional)

Add the scope in parentheses to indicate what part of the codebase is affected:

* `feat(auth)`: Changes to authentication
* `fix(database)`: Database-related fixes
* `style(ui)`: UI styling changes

## 3. Subject Line Rules

* Maximum 50 characters
* Start with a capital letter
* No period at the end
* Use imperative mood ("Add" not "Added" or "Adds")
* Be specific and concise

### Good Examples:

✅ `feat(auth): Add OAuth2 support for Google login`
✅ `fix(cart): Resolve total calculation error`
✅ `docs(api): Update endpoint documentation`

### Bad Examples:

❌ `fixed bug` (too vague)
❌ `Added new feature...` (wrong tense)
❌ `feat: this is a really really really long commit message that goes on forever` (too long)

## 4. Commit Body

* Separate from subject with a blank line
* Wrap at 72 characters
* Explain what and why vs. how
* Use bullet points for multiple points
* Include relevant background information

Example:

```
feat(payment): Implement PayPal integration

- Add PayPal SDK and configuration
- Create payment processing service
- Implement webhook handlers for payment status

This change allows customers to pay using PayPal in addition
to existing credit card options. The implementation follows
PayPal's best practices for secure checkout.

Breaking Change: Payment processor interface has been updated
to accommodate new payment methods.
```

## 5. Footer Rules

Use for:

* Breaking changes: Start with `BREAKING CHANGE:`
* Referencing issues: `Fixes #123` or `Relates to #456`
* Co-authors: `Co-authored-by: Name <email>`

## 6. Revert Commits

When reverting a commit, use:

```
revert: feat(auth): Add OAuth2 support for Google login

This reverts commit abc123def456...
Reason: Security vulnerability found in implementation
```

## 7. Branch Naming Convention

Use this format: `<type>/<ticket-number>-<brief-description>`

Examples:

* `feat/PROJ-123-add-google-auth`
* `fix/PROJ-456-fix-payment-calculation`
* `docs/PROJ-789-update-api-docs`

## 8. Tips for Team Workflow

### Before Committing:

1. Run tests locally
2. Review your changes (git diff)
3. Group related changes
4. Commit often, perfect later, publish once

### Use Commit Message Template

Create `.gitmessage` in your project:

```
<type>(<scope>): <subject>

# Why is this change needed?
# How does it address the issue?
# What side effects does this change have?

# Include a link to the ticket, if any.
# Add co-authors if you worked on this code with others:
# Co-authored-by: Full Name <email@example.com>
```

Set it as template:

```bash
git config --global commit.template ~/.gitmessage
```

### Automation Tools

* Use commitlint for enforcing commit message format
* Set up pre-commit hooks for consistency
* Use tools like commitizen for guided commits

## 9. Examples of Complete Commit Messages

### Feature Addition

```
feat(user): Add password reset functionality

Implement secure password reset flow with email verification:
- Generate secure reset tokens
- Send reset emails using SendGrid
- Add password strength validation
- Create reset password API endpoint

Closes #123
```

### Bug Fix

```
fix(checkout): Resolve cart total miscalculation

When multiple discounts were applied, the cart total was
incorrectly showing negative values due to double-counting
of percentage-based discounts.

Fixed by:
- Adding validation for minimum cart total
- Correcting discount calculation order
- Adding unit tests for edge cases

Fixes #456
```

### Breaking Change

```
feat(api): Upgrade authentication middleware

BREAKING CHANGE: Authentication middleware now requires
API keys to be passed in headers instead of query parameters
for improved security.

Migration guide:
1. Update API key location in requests
2. Regenerate API keys using new format
3. Update client libraries

Related to #789
```
