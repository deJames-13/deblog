#!/usr/bin/env bash
# ==============================================================================
# Helper Script for Tailwind File-First VSA
# Scaffolds VSA stylesheet slices, shared elements, and wires aggregator imports.
# ==============================================================================
set -euo pipefail

usage() {
  cat << 'EOF'
Usage:
  ./scripts/run.sh [options] <feature_or_element> [element_name]

Arguments:
  <feature_or_element>   Name of the global element (e.g. 'button') or feature slice (e.g. 'dashboard')
  [element_name]         Optional sub-element for feature slices (e.g. 'header', 'sidebar')

Options:
  -d, --dir <path>       Target styles directory (default: looks for 'src/styles' or 'styles')
  -h, --help             Show this help message

Examples:
  # Scaffold a global shared button element:
  ./scripts/run.sh button

  # Scaffold a dashboard feature slice element (sidebar):
  ./scripts/run.sh dashboard sidebar

  # Target a custom directory:
  ./scripts/run.sh -d frontend/src/styles auth login-card
EOF
}

STYLES_DIR=""
TARGET_ARG1=""
TARGET_ARG2=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    -d|--dir)
      STYLES_DIR="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    -*)
      echo "Error: Unknown option: $1" >&2
      usage
      exit 1
      ;;
    *)
      if [[ -z "$TARGET_ARG1" ]]; then
        TARGET_ARG1="$1"
      elif [[ -z "$TARGET_ARG2" ]]; then
        TARGET_ARG2="$1"
      else
        echo "Error: Unexpected argument: $1" >&2
        usage
        exit 1
      fi
      shift
      ;;
  esac
done

if [[ -z "$TARGET_ARG1" ]]; then
  echo "Error: Please specify an element or feature slice name." >&2
  usage
  exit 1
fi

# Auto-detect styles directory if not specified
if [[ -z "$STYLES_DIR" ]]; then
  if [[ -d "src/styles" ]]; then
    STYLES_DIR="src/styles"
  elif [[ -d "styles" ]]; then
    STYLES_DIR="styles"
  else
    STYLES_DIR="src/styles"
  fi
fi

mkdir -p "$STYLES_DIR"

# Ensure root styles.css exists
ROOT_CSS="$STYLES_DIR/styles.css"
if [[ ! -f "$ROOT_CSS" ]]; then
  cat << 'EOF' > "$ROOT_CSS"
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Global Aggregator */
@import "./main.css";
EOF
  echo "==> Initialized root stylesheet: $ROOT_CSS"
fi

# Ensure global main.css exists
GLOBAL_MAIN_CSS="$STYLES_DIR/main.css"
if [[ ! -f "$GLOBAL_MAIN_CSS" ]]; then
  cat << 'EOF' > "$GLOBAL_MAIN_CSS"
/* Global Shared Elements Aggregator */
EOF
  echo "==> Initialized global aggregator: $GLOBAL_MAIN_CSS"
fi

if [[ -z "$TARGET_ARG2" ]]; then
  # Single argument: Global shared element (e.g. 'button')
  ELEMENT="$TARGET_ARG1"
  TARGET_CSS="$STYLES_DIR/${ELEMENT}.css"
  IMPORT_LINE="@import \"./${ELEMENT}.css\";"

  if [[ ! -f "$TARGET_CSS" ]]; then
    cat << EOF > "$TARGET_CSS"
@layer components {
  /* Global ${ELEMENT^} Component */
  .${ELEMENT} {
    @apply transition-all duration-150;
  }
}
EOF
    echo "==> Created global element stylesheet: $TARGET_CSS"
  else
    echo "==> File already exists: $TARGET_CSS"
  fi

  # Append import to global main.css if not present
  if ! grep -qF "$IMPORT_LINE" "$GLOBAL_MAIN_CSS"; then
    echo "$IMPORT_LINE" >> "$GLOBAL_MAIN_CSS"
    echo "==> Added import to $GLOBAL_MAIN_CSS"
  fi

else
  # Two arguments: Feature slice context (e.g. 'dashboard' 'sidebar')
  SLICE="$TARGET_ARG1"
  ELEMENT="$TARGET_ARG2"
  SLICE_DIR="$STYLES_DIR/$SLICE"
  SLICE_MAIN_CSS="$SLICE_DIR/main.css"
  TARGET_CSS="$SLICE_DIR/${ELEMENT}.css"

  mkdir -p "$SLICE_DIR"

  # 1. Create slice main.css aggregator if needed
  if [[ ! -f "$SLICE_MAIN_CSS" ]]; then
    cat << EOF > "$SLICE_MAIN_CSS"
/* Feature Slice Aggregator: ${SLICE^} */
EOF
    echo "==> Initialized slice aggregator: $SLICE_MAIN_CSS"
  fi

  # 2. Create element css inside slice
  IMPORT_LINE="@import \"./${ELEMENT}.css\";"
  CLASS_PREFIX="${SLICE}-${ELEMENT}"

  if [[ ! -f "$TARGET_CSS" ]]; then
    cat << EOF > "$TARGET_CSS"
@layer components {
  /* ${SLICE^} ${ELEMENT^} Component */
  .${CLASS_PREFIX} {
    @apply relative transition-all duration-150;
  }
}
EOF
    echo "==> Created feature slice stylesheet: $TARGET_CSS"
  else
    echo "==> File already exists: $TARGET_CSS"
  fi

  # 3. Add import to slice main.css
  if ! grep -qF "$IMPORT_LINE" "$SLICE_MAIN_CSS"; then
    echo "$IMPORT_LINE" >> "$SLICE_MAIN_CSS"
    echo "==> Added import to $SLICE_MAIN_CSS"
  fi

  # 4. Add slice aggregator to root styles.css if not present
  ROOT_SLICE_IMPORT="@import \"./${SLICE}/main.css\";"
  if ! grep -qF "$ROOT_SLICE_IMPORT" "$ROOT_CSS"; then
    echo "$ROOT_SLICE_IMPORT" >> "$ROOT_CSS"
    echo "==> Registered slice in root: $ROOT_CSS"
  fi
fi

echo "==> Successfully configured Tailwind File-First VSA slice!"
