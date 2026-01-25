#!/bin/bash
# AI Slop Detector - findet typische KI-generierte Phrasen UND Code-Brainfarts
# Exit 1 wenn Slop gefunden wird (kann mit --warn zu warning-only werden)

set -e

RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
GRAY='\033[0;90m'
NC='\033[0m'

SLOP_FOUND=0
TOTAL_MATCHES=0
WARN_ONLY=0

# Parse args
if [[ "$1" == "--warn" ]]; then
    WARN_ONLY=1
fi

# ============================================================================
# IGNORIERTE DATEIEN/PFADE
# ============================================================================

IGNORE_PATHS=(
    "*/bin/*"
    "*/obj/*"
    "*/.git/*"
    "*.legacy.md"
    "*.g.cs"
    "*AssemblyInfo.cs"
    "*GlobalUsings.g.cs"
    "*/Tests/*"           # Test-Code darf "schlampiger" sein
    "*.Tests.cs"
    "*Test.cs"
)

# ============================================================================
# TEIL 1: TEXT SLOP (Markdown, Kommentare)
# ============================================================================

TEXT_SLOP_PATTERNS=(
    # Filler phrases
    "it's important to note"
    "it's worth mentioning"
    "it's worth noting"
    "let me explain"
    "in this section, we will"

    # Marketing-Speak
    "leverage the power"
    "unlock the potential"
    "seamless integration"
    "cutting-edge"
    "game-changing"
    "best-in-class"
    "robust and scalable"

    # Redundante Einleitungen
    "as mentioned earlier"
    "as you can see"
    "needless to say"

    # AI-Selbstreferenz
    "as an ai"
    "language model"

    # Pseudo-Professionalität
    "utilize" # statt "use"
    "in order to" # statt "to"
    "due to the fact that" # statt "because"
    "at this point in time" # statt "now"
)

# ============================================================================
# TEIL 2: CODE SLOP (C# spezifisch)
# ============================================================================

# Pattern -> Beschreibung -> Fix
declare -A CODE_SLOP_PATTERNS
declare -A CODE_SLOP_FIXES

# LINQ Abuse
CODE_SLOP_PATTERNS["\.ToList\(\)\.Count\(\)"]="ToList().Count() - unnötige Allokation"
CODE_SLOP_FIXES["\.ToList\(\)\.Count\(\)"]=".Count()"

CODE_SLOP_PATTERNS["\.ToList\(\)\.Any\(\)"]="ToList().Any() - unnötige Allokation"
CODE_SLOP_FIXES["\.ToList\(\)\.Any\(\)"]=".Any()"

CODE_SLOP_PATTERNS["\.ToList\(\)\.First"]="ToList().First*() - unnötige Allokation"
CODE_SLOP_FIXES["\.ToList\(\)\.First"]=".First*()"

CODE_SLOP_PATTERNS["\.ToArray\(\)\.Length"]="ToArray().Length - unnötige Allokation"
CODE_SLOP_FIXES["\.ToArray\(\)\.Length"]=".Count()"

CODE_SLOP_PATTERNS["\.Where\([^)]+\)\.Count\(\)\s*[><=]+\s*0"]="Where().Count() > 0 - ineffizient"
CODE_SLOP_FIXES["\.Where\([^)]+\)\.Count\(\)\s*[><=]+\s*0"]=".Any(predicate) / !.Any(predicate)"

CODE_SLOP_PATTERNS["\.Select\([^)]+\)\.ToList\(\)\.ForEach"]="Select().ToList().ForEach - unnötig"
CODE_SLOP_FIXES["\.Select\([^)]+\)\.ToList\(\)\.ForEach"]="foreach loop"

# Leere/Sinnlose Blöcke
CODE_SLOP_PATTERNS["catch\s*\([^)]*\)\s*\{\s*\}"]="Leerer catch-Block - Fehler verschluckt!"
CODE_SLOP_FIXES["catch\s*\([^)]*\)\s*\{\s*\}"]="catch { _logger.LogError(...); }"

CODE_SLOP_PATTERNS["catch\s*\{\s*\}"]="Leerer catch-Block - Fehler verschluckt!"
CODE_SLOP_FIXES["catch\s*\{\s*\}"]="catch { _logger.LogError(...); }"

CODE_SLOP_PATTERNS["finally\s*\{\s*\}"]="Leerer finally-Block - sinnlos"
CODE_SLOP_FIXES["finally\s*\{\s*\}"]="[Block entfernen]"

# Redundante Checks
CODE_SLOP_PATTERNS["if\s*\([^)]+\s*!=\s*null\)\s*\{\s*return\s+[^;]+;\s*\}\s*return\s+null"]="Redundante null-Prüfung"
CODE_SLOP_FIXES["if\s*\([^)]+\s*!=\s*null\)\s*\{\s*return\s+[^;]+;\s*\}\s*return\s+null"]="return x?.Property;"

CODE_SLOP_PATTERNS["\.ToString\(\)\s*\+\s*\""]="String concat mit ToString() - use interpolation"
CODE_SLOP_FIXES["\.ToString\(\)\s*\+\s*\""]="\$\"{value}...\""

# Async Brainfarts
CODE_SLOP_PATTERNS["\.Result[;\s]"]="Sync-over-async (.Result) - Deadlock-Gefahr!"
CODE_SLOP_FIXES["\.Result[;\s]"]="await"

CODE_SLOP_PATTERNS["\.Wait\(\)"]="Sync-over-async (.Wait()) - Deadlock-Gefahr!"
CODE_SLOP_FIXES["\.Wait\(\)"]="await"

CODE_SLOP_PATTERNS["Task\.Run\(\s*async"]="Task.Run(async) - meist unnötig"
CODE_SLOP_FIXES["Task\.Run\(\s*async"]="direkt await"

CODE_SLOP_PATTERNS["async.*=>\s*[^{]*;[^}]*$"]="Async lambda ohne await - vergessen?"
CODE_SLOP_FIXES["async.*=>\s*[^{]*;[^}]*$"]="await hinzufügen oder async entfernen"

# String in Loops
CODE_SLOP_PATTERNS["for.*\+=\s*\""]="String += in Loop - O(n²)!"
CODE_SLOP_FIXES["for.*\+=\s*\""]="StringBuilder"

CODE_SLOP_PATTERNS["foreach.*\+=\s*\""]="String += in Loop - O(n²)!"
CODE_SLOP_FIXES["foreach.*\+=\s*\""]="StringBuilder oder string.Join()"

CODE_SLOP_PATTERNS["while.*\+=\s*\""]="String += in Loop - O(n²)!"
CODE_SLOP_FIXES["while.*\+=\s*\""]="StringBuilder"

# Typische Copy-Paste Fehler
CODE_SLOP_PATTERNS["//\s*TODO.*TODO"]="Doppeltes TODO - Copy-Paste?"
CODE_SLOP_FIXES["//\s*TODO.*TODO"]="[Aufräumen]"

CODE_SLOP_PATTERNS["public\s+\w+\s+\w+\s*\{\s*get;\s*set;\s*\}\s*public\s+\w+\s+\w+\s*\{\s*get;\s*set;\s*\}\s*public\s+\w+\s+\w+\s*\{\s*get;\s*set;\s*\}.*//"]="3+ Properties mit gleichem Kommentar"
CODE_SLOP_FIXES["..."]="[Kommentare prüfen]"

# Sinnlose Kommentare
CODE_SLOP_PATTERNS["//\s*[Gg]et[s]?\s+(the\s+)?\w+\s*$"]="Nutzloser Getter-Kommentar"
CODE_SLOP_FIXES["//\s*[Gg]et[s]?\s+(the\s+)?\w+\s*$"]="[Kommentar entfernen oder erweitern]"

CODE_SLOP_PATTERNS["//\s*[Ss]et[s]?\s+(the\s+)?\w+\s*$"]="Nutzloser Setter-Kommentar"
CODE_SLOP_FIXES["//\s*[Ss]et[s]?\s+(the\s+)?\w+\s*$"]="[Kommentar entfernen oder erweitern]"

CODE_SLOP_PATTERNS["//\s*[Cc]onstructor\s*$"]="Nutzloser Constructor-Kommentar"
CODE_SLOP_FIXES["//\s*[Cc]onstructor\s*$"]="[Kommentar entfernen]"

CODE_SLOP_PATTERNS["//\s*[Dd]ispose\s*$"]="Nutzloser Dispose-Kommentar"
CODE_SLOP_FIXES["//\s*[Dd]ispose\s*$"]="[Kommentar entfernen]"

# Magic Numbers/Strings
CODE_SLOP_PATTERNS["Thread\.Sleep\([0-9]+\)"]="Thread.Sleep mit magic number"
CODE_SLOP_FIXES["Thread\.Sleep\([0-9]+\)"]="const oder TimeSpan mit Namen"

CODE_SLOP_PATTERNS["Delay\([0-9]{4,}\)"]="Task.Delay mit großer magic number"
CODE_SLOP_FIXES["Delay\([0-9]{4,}\)"]="TimeSpan.FromSeconds() mit const"

# Exception Anti-Patterns
CODE_SLOP_PATTERNS["throw\s+new\s+Exception\("]="Generic Exception statt spezifisch"
CODE_SLOP_FIXES["throw\s+new\s+Exception\("]="InvalidOperationException, ArgumentException, etc."

CODE_SLOP_PATTERNS["catch\s*\(\s*Exception\s*\)"]="Catch-all Exception (wenn nicht am top-level)"
CODE_SLOP_FIXES["catch\s*\(\s*Exception\s*\)"]="Spezifischere Exception fangen"

# Nullable Confusion (nur bei Assignments, nicht bei Logging)
CODE_SLOP_PATTERNS["=\s*[^;]*\?\?[^;]*\?\?[^;]*;"]="Doppelte null-coalescing in Assignment"
CODE_SLOP_FIXES["=\s*[^;]*\?\?[^;]*\?\?[^;]*;"]="Vereinfachen oder aufteilen"

# ============================================================================
# TEIL 3: MEHR CODE ANTI-PATTERNS
# ============================================================================

# Boolean Redundanz (nur non-nullable - bei bool? ist == true korrekt!)
# Diese sind oft false positives bei nullable bools, daher auskommentiert:
# CODE_SLOP_PATTERNS["==\s*true"]="== true ist redundant"
# CODE_SLOP_PATTERNS["==\s*false"]="== false - besser negieren"

# String Anti-Patterns
CODE_SLOP_PATTERNS["\.Equals\(\"\"\)"]="string.Equals(\"\") - use IsNullOrEmpty"
CODE_SLOP_FIXES["\.Equals\(\"\"\)"]="string.IsNullOrEmpty(x)"

CODE_SLOP_PATTERNS["\.Length\s*==\s*0"]="string.Length == 0 - use IsNullOrEmpty"
CODE_SLOP_FIXES["\.Length\s*==\s*0"]="string.IsNullOrEmpty(x)"

# string.Format ist OK für komplexe Formatierung, nur bei simplen Fällen warnen
# CODE_SLOP_PATTERNS["string\.Format\("]="string.Format - use interpolation"

# String.Concat mit separator (wie "|") ist OK für Dictionary-Keys
# CODE_SLOP_PATTERNS["String\.Concat\("]="String.Concat - use interpolation"

# Auskommentierter Code (verdächtige Patterns)
CODE_SLOP_PATTERNS["//\s*(public|private|internal|protected)\s+(class|void|async|static)"]="Auskommentierter Code"
CODE_SLOP_FIXES["//\s*(public|private|internal|protected)\s+(class|void|async|static)"]="[Löschen oder reaktivieren]"

CODE_SLOP_PATTERNS["//\s*var\s+\w+\s*="]="Auskommentierte Variable"
CODE_SLOP_FIXES["//\s*var\s+\w+\s*="]="[Löschen oder reaktivieren]"

CODE_SLOP_PATTERNS["/\*\s*(public|private|void|class)"]="Auskommentierter Code-Block"
CODE_SLOP_FIXES["/\*\s*(public|private|void|class)"]="[Löschen oder reaktivieren]"

# Debug/Console in Production
CODE_SLOP_PATTERNS["Console\.Write"]="Console.Write* in Production-Code"
CODE_SLOP_FIXES["Console\.Write"]="_logger.Log*()"

CODE_SLOP_PATTERNS["Debug\.Write"]="Debug.Write* in Production-Code"
CODE_SLOP_FIXES["Debug\.Write"]="_logger.LogDebug()"

CODE_SLOP_PATTERNS["Debug\.Assert"]="Debug.Assert - besser ArgumentException"
CODE_SLOP_FIXES["Debug\.Assert"]="ArgumentNullException.ThrowIfNull() etc."

# Schlechte Typisierung
CODE_SLOP_PATTERNS["\bobject\b\s+\w+\s*[=;,)]"]="'object' als Typ - zu generisch"
CODE_SLOP_FIXES["\bobject\b\s+\w+\s*[=;,)]"]="Spezifischer Typ oder Generic"

CODE_SLOP_PATTERNS["\bdynamic\b"]="'dynamic' usage - Typsicherheit verloren"
CODE_SLOP_FIXES["\bdynamic\b"]="Generics oder spezifischer Typ"

# Collection Initializers (C# 12)
CODE_SLOP_PATTERNS["new\s+List<[^>]+>\(\)"]="new List<T>() - use [] (C# 12)"
CODE_SLOP_FIXES["new\s+List<[^>]+>\(\)"]="List<T> list = [];"

CODE_SLOP_PATTERNS["new\s+Dictionary<[^>]+>\(\)"]="new Dictionary<K,V>() - use [] (C# 12)"  
CODE_SLOP_FIXES["new\s+Dictionary<[^>]+>\(\)"]="Dictionary<K,V> dict = [];"

CODE_SLOP_PATTERNS["Array\.Empty<"]="Array.Empty<T>() - use [] (C# 12)"
CODE_SLOP_FIXES["Array\.Empty<"]="[]"

# LINQ Identity Operations (sinnlos)
CODE_SLOP_PATTERNS["\.Select\(\s*x\s*=>\s*x\s*\)"]="Identity Select - sinnlos"
CODE_SLOP_FIXES["\.Select\(\s*x\s*=>\s*x\s*\)"]="[Entfernen]"

CODE_SLOP_PATTERNS["\.Where\(\s*x\s*=>\s*true\s*\)"]="Where(x => true) - sinnlos"
CODE_SLOP_FIXES["\.Where\(\s*x\s*=>\s*true\s*\)"]="[Entfernen]"

CODE_SLOP_PATTERNS["\.Where\(\s*_\s*=>\s*true\s*\)"]="Where(_ => true) - sinnlos"
CODE_SLOP_FIXES["\.Where\(\s*_\s*=>\s*true\s*\)"]="[Entfernen]"

CODE_SLOP_PATTERNS["\.OrderBy\([^)]+\)\.OrderBy\("]="Doppeltes OrderBy - nur letztes zählt!"
CODE_SLOP_FIXES["\.OrderBy\([^)]+\)\.OrderBy\("]="ThenBy() für sekundäre Sortierung"

# Async Anti-Patterns (erweitert)
# Task.FromResult ist OK für interface implementations - kein Slop
# CODE_SLOP_PATTERNS["return\s+Task\.FromResult"]="Task.FromResult - besser ValueTask"

CODE_SLOP_PATTERNS["async\s+Task\s+\w+\([^)]*\)\s*\{\s*return"]="Async ohne await - unnötig"
CODE_SLOP_FIXES["async\s+Task\s+\w+\([^)]*\)\s*\{\s*return"]="async entfernen, Task direkt returnen"

CODE_SLOP_PATTERNS["\.GetAwaiter\(\)\.GetResult\(\)"]="GetAwaiter().GetResult() - Deadlock!"
CODE_SLOP_FIXES["\.GetAwaiter\(\)\.GetResult\(\)"]="await"

# Ternary Abuse
CODE_SLOP_PATTERNS["\?\s*[^:]+\?\s*[^:]+:.*:"]="Nested ternary - schwer lesbar"
CODE_SLOP_FIXES["\?\s*[^:]+\?\s*[^:]+:.*:"]="if/else oder switch expression"

# Public Fields
CODE_SLOP_PATTERNS["public\s+\w+\s+\w+\s*;"]="Public field statt Property"
CODE_SLOP_FIXES["public\s+\w+\s+\w+\s*;"]="public T Name { get; set; }"

# Return am Ende von void
CODE_SLOP_PATTERNS["return;\s*\}"]="return; am Ende von void method - unnötig"
CODE_SLOP_FIXES["return;\s*\}"]="[Entfernen]"

# Redundante else nach return
CODE_SLOP_PATTERNS["return[^;]*;\s*\}\s*else\s*\{"]="else nach return - unnötig"
CODE_SLOP_FIXES["return[^;]*;\s*\}\s*else\s*\{"]="else weglassen"

# Resource Leaks
CODE_SLOP_PATTERNS["new\s+(StreamReader|StreamWriter|FileStream|HttpClient)\("]="IDisposable ohne using"
CODE_SLOP_FIXES["new\s+(StreamReader|StreamWriter|FileStream|HttpClient)\("]="using var x = new ...() oder DI"

# Potential Boxing
CODE_SLOP_PATTERNS["\.ToString\(\)\.GetHashCode\(\)"]="ToString().GetHashCode() - Boxing"
CODE_SLOP_FIXES["\.ToString\(\)\.GetHashCode\(\)"]="HashCode.Combine()"

# Lock Anti-Patterns
CODE_SLOP_PATTERNS["lock\s*\(\s*this\s*\)"]="lock(this) - schlecht isoliert"
CODE_SLOP_FIXES["lock\s*\(\s*this\s*\)"]="private readonly object _lock = new();"

CODE_SLOP_PATTERNS["lock\s*\(\s*typeof\("]="lock(typeof) - global lock!"
CODE_SLOP_FIXES["lock\s*\(\s*typeof\("]="private static readonly object _lock = new();"

# Event Handler Leaks
CODE_SLOP_PATTERNS["\+=\s*new\s+EventHandler"]="new EventHandler - method group reicht"
CODE_SLOP_FIXES["\+=\s*new\s+EventHandler"]="event += MethodName;"
echo -e "${BLUE}║${NC}           ${YELLOW}🔍 AI SLOP DETECTOR${NC}                             ${BLUE}║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# ============================================================================
# SCAN TEXT SLOP
# ============================================================================

echo -e "${YELLOW}📝 Teil 1: Text-Slop in Dokumentation...${NC}"
echo ""

MD_FILES=$(find . -type f -name "*.md" \
    -not -path "*/bin/*" \
    -not -path "*/obj/*" \
    -not -path "*/.git/*" \
    -not -name "*.legacy.md" 2>/dev/null || true)

for pattern in "${TEXT_SLOP_PATTERNS[@]}"; do
    matches=$(grep -rniE "$pattern" $MD_FILES 2>/dev/null || true)

    if [ -n "$matches" ]; then
        SLOP_FOUND=1
        count=$(echo "$matches" | wc -l)
        TOTAL_MATCHES=$((TOTAL_MATCHES + count))

        echo -e "  ${RED}✗${NC} \"$pattern\" (${count}x)"
        echo "$matches" | head -3 | sed 's/^/    /'
        [ $count -gt 3 ] && echo "    ..."
        echo ""
    fi
done

# ============================================================================
# SCAN CODE SLOP
# ============================================================================

echo -e "${YELLOW}💻 Teil 2: Code-Brainfarts in C#...${NC}"
echo ""

# Finde CS-Dateien, aber ignoriere Tests (dort ist mehr erlaubt)
CS_FILES=$(find . -type f -name "*.cs" \
    -not -path "*/bin/*" \
    -not -path "*/obj/*" \
    -not -path "*/.git/*" \
    -not -path "*.Tests/*" \
    -not -name "*.Tests.cs" \
    -not -name "*Test.cs" \
    -not -name "*.g.cs" \
    -not -name "AssemblyInfo.cs" 2>/dev/null || true)

for pattern in "${!CODE_SLOP_PATTERNS[@]}"; do
    desc="${CODE_SLOP_PATTERNS[$pattern]}"
    fix="${CODE_SLOP_FIXES[$pattern]}"

    matches=$(grep -rniE "$pattern" $CS_FILES 2>/dev/null || true)
    
    # Filter out false positives (comments containing keywords like "dynamic")
    if [[ "$pattern" == *"dynamic"* ]]; then
        matches=$(echo "$matches" | grep -v "///" | grep -v "/\*" || true)
    fi

    if [ -n "$matches" ]; then
        SLOP_FOUND=1
        count=$(echo "$matches" | wc -l)
        TOTAL_MATCHES=$((TOTAL_MATCHES + count))

        echo -e "  ${RED}✗${NC} ${desc} (${count}x)"
        echo -e "    ${GREEN}Fix:${NC} $fix"
        echo "$matches" | head -3 | sed 's/^/    /'
        [ $count -gt 3 ] && echo "    ..."
        echo ""
    fi
done

# ============================================================================
# SUMMARY
# ============================================================================

echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"

if [ $SLOP_FOUND -eq 0 ]; then
    echo -e "${GREEN}✅ Kein AI Slop gefunden! Code ist clean.${NC}"
    exit 0
else
    echo -e "${RED}❌ $TOTAL_MATCHES Slop-Matches gefunden${NC}"
    echo ""
    echo -e "${YELLOW}Quick Fixes:${NC}"
    echo "  Text:  'utilize' → 'use', 'in order to' → 'to'"
    echo "  Code:  .ToList().Count() → .Count()"
    echo "         .Result / .Wait() → await"
    echo "         catch {} → catch { log error }"
    
    if [ $WARN_ONLY -eq 1 ]; then
        echo ""
        echo -e "${GRAY}(--warn mode: exiting with 0)${NC}"
        exit 0
    fi
    exit 1
fi
