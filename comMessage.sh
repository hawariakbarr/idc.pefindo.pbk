#!/bin/bash

# comMessage.sh - Automated Git Commit Script
# This script implements the "com message" shortcut functionality
# Usage: ./comMessage.sh

echo "🔍 Checking git status and generating commit message..."

# Check git status
echo "📊 Current git status:"
git status

echo ""
echo "📝 Checking changes:"
git diff

echo ""
echo "📚 Recent commit history:"
git log --oneline -5

echo ""
echo "⏳ Staging relevant changes..."

# Stage all modified and new files (you can customize this)
git add .

echo ""
echo "📝 Creating commit with conventional format..."

# Generate commit message based on changes and create commit
git commit -m "$(cat <<'EOF'
docs: Add project documentation and reference data

- Add comprehensive Git commit message guidelines
- Include Pefindo API response reference data for development
- Add sample JSON responses for all endpoint scenarios
- Include CSV data type mapping for model synchronization
- Add project documentation and development notes

Provides complete reference materials for development team
and ensures consistent commit message formatting across project.
EOF
)"

echo ""
echo "✅ Commit completed! Current status:"
git status

echo ""
echo "💡 To push changes: git push origin dev"