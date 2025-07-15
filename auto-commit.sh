#!/bin/bash

# Generic Git Commit Script - IDC Pefindo PBK
# Automatically analyzes changes and generates appropriate commit messages

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== IDC Pefindo PBK Auto Commit Script ===${NC}"

# Check if we're in a git repository
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}Error: Not in a git repository${NC}"
    exit 1
fi

# Check for unstaged changes
if git diff --quiet && git diff --staged --quiet; then
    echo -e "${YELLOW}No changes to commit${NC}"
    exit 0
fi

echo -e "${BLUE}Analyzing current changes...${NC}"

# Get list of modified files
MODIFIED_FILES=$(git diff --name-only HEAD)
STAGED_FILES=$(git diff --staged --name-only)
ALL_CHANGED_FILES=$(echo -e "$MODIFIED_FILES\n$STAGED_FILES" | sort -u | grep -v '^$')

echo -e "${CYAN}Changed files:${NC}"
echo "$ALL_CHANGED_FILES" | sed 's/^/  - /'

# Analyze file types and changes to determine commit type
COMMIT_TYPE=""
SCOPE=""
SUBJECT=""
BODY_POINTS=()

# Check for specific patterns
if echo "$ALL_CHANGED_FILES" | grep -q "\.cs$"; then
    # C# files changed
    if git diff HEAD | grep -q "class.*Service" || git diff --staged | grep -q "class.*Service"; then
        COMMIT_TYPE="feat"
        SCOPE="service"
    elif git diff HEAD | grep -q "Controller" || git diff --staged | grep -q "Controller"; then
        COMMIT_TYPE="feat"
        SCOPE="api"
    elif git diff HEAD | grep -q "Repository" || git diff --staged | grep -q "Repository"; then
        COMMIT_TYPE="feat"
        SCOPE="data"
    else
        COMMIT_TYPE="feat"
        SCOPE="core"
    fi
fi

if echo "$ALL_CHANGED_FILES" | grep -q "\.md$"; then
    if [ "$COMMIT_TYPE" = "" ]; then
        COMMIT_TYPE="docs"
    fi
fi

if echo "$ALL_CHANGED_FILES" | grep -q "Test"; then
    if [ "$COMMIT_TYPE" = "" ]; then
        COMMIT_TYPE="test"
    fi
fi

# Default to feat if we can't determine
if [ "$COMMIT_TYPE" = "" ]; then
    COMMIT_TYPE="feat"
fi

# Interactive input for commit details
echo -e "${YELLOW}Detected commit type: ${COMMIT_TYPE}${NC}"
echo -e "${YELLOW}Detected scope: ${SCOPE}${NC}"
echo

read -p "$(echo -e ${GREEN}Enter commit type [${COMMIT_TYPE}]: ${NC})" INPUT_TYPE
if [ ! -z "$INPUT_TYPE" ]; then
    COMMIT_TYPE="$INPUT_TYPE"
fi

read -p "$(echo -e ${GREEN}Enter scope [${SCOPE}]: ${NC})" INPUT_SCOPE
if [ ! -z "$INPUT_SCOPE" ]; then
    SCOPE="$INPUT_SCOPE"
fi

read -p "$(echo -e ${GREEN}Enter short description: ${NC})" SUBJECT

if [ -z "$SUBJECT" ]; then
    echo -e "${RED}Subject is required${NC}"
    exit 1
fi

# Build commit message
if [ ! -z "$SCOPE" ]; then
    COMMIT_HEADER="${COMMIT_TYPE}(${SCOPE}): ${SUBJECT}"
else
    COMMIT_HEADER="${COMMIT_TYPE}: ${SUBJECT}"
fi

# Ask for detailed description
echo
echo -e "${BLUE}Enter detailed description (optional):${NC}"
echo -e "${BLUE}Press Enter on empty line to finish${NC}"

BODY_LINES=()
while IFS= read -r line; do
    if [ -z "$line" ]; then
        break
    fi
    BODY_LINES+=("$line")
done

# Build full commit message
COMMIT_MESSAGE="$COMMIT_HEADER"

if [ ${#BODY_LINES[@]} -gt 0 ]; then
    COMMIT_MESSAGE+="\n\n"
    for line in "${BODY_LINES[@]}"; do
        COMMIT_MESSAGE+="$line\n"
    done
fi

# Ask about staging files
echo
echo -e "${BLUE}Files to stage:${NC}"
if [ ! -z "$MODIFIED_FILES" ]; then
    echo -e "${YELLOW}Modified files:${NC}"
    echo "$MODIFIED_FILES" | sed 's/^/  - /'
fi

if [ ! -z "$STAGED_FILES" ]; then
    echo -e "${YELLOW}Already staged files:${NC}"
    echo "$STAGED_FILES" | sed 's/^/  - /'
fi

read -p "$(echo -e ${GREEN}Stage all modified files? [Y/n]: ${NC})" -n 1 -r
echo
if [[ ! $REPLY =~ ^[Nn]$ ]]; then
    echo -e "${BLUE}Staging modified files...${NC}"
    if [ ! -z "$MODIFIED_FILES" ]; then
        echo "$MODIFIED_FILES" | xargs git add
    fi
fi

# Show what will be committed
echo -e "${BLUE}Files to be committed:${NC}"
git diff --staged --name-only | sed 's/^/  - /'

# Show commit message preview
echo
echo -e "${YELLOW}Commit message preview:${NC}"
echo "----------------------------------------"
echo -e "$COMMIT_MESSAGE"
echo "----------------------------------------"

# Ask for confirmation
read -p "$(echo -e ${GREEN}Proceed with this commit? [y/N]: ${NC})" -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Commit cancelled${NC}"
    exit 0
fi

# Perform the commit
echo -e "${BLUE}Committing changes...${NC}"
git commit -m "$(echo -e "$COMMIT_MESSAGE")"

# Success message
echo -e "${GREEN}✅ Commit successful!${NC}"
echo -e "${BLUE}Latest commit:${NC}"
git log -1 --oneline

# Optional: Ask if user wants to push
read -p "$(echo -e ${GREEN}Push to remote? [y/N]: ${NC})" -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${BLUE}Pushing to remote...${NC}"
    git push
    echo -e "${GREEN}✅ Push successful!${NC}"
else
    echo -e "${YELLOW}Remember to push when ready: git push${NC}"
fi

echo -e "${GREEN}Done!${NC}"