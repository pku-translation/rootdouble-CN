using CSYetiTools.Base;
using CSYetiTools.Base.Branch;
using CSYetiTools.VnScripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSYetiTools.Commandlet;

public class RawGraph
{
    private readonly SortedDictionary<int, RawNode> _nodeTable = new();
    private readonly List<int> _entires = new();

    public int Index { get; }
    public string SceneTitle { get; }

    private RawGraph(int index, IList<string> sceneTitles, Script script, Script referenceScript)
    {
        Index = index;
        var scriptIndex = script.Footer.ScriptIndex;

        SceneTitle = scriptIndex >= 0 ? sceneTitles[scriptIndex] : "<no-title>";
        foreach (var offset in script.LabelOffsets) {
            _nodeTable.Add(offset, new RawNode { Offset = offset });
        }

        foreach (var entry in script.Header.Entries) {
            _entires.Add(entry.AbsoluteOffset);
        }

        var currentNode = new RawNode();
        var extraStart = false;
        var currentCharacter = (string?)null;
        foreach (var code in script.Codes) {
            if (extraStart) {
                extraStart = false;
                if (!_nodeTable.ContainsKey(code.Offset)) {
                    _nodeTable.Add(code.Offset, new RawNode { Offset = code.Offset });
                }
            }
            if (_nodeTable.TryGetValue(code.Offset, out var nextNode)) {
                if (currentNode.AutoNext) {
                    currentNode.Next = code.Offset;
                }
                currentNode = nextNode;
            }

            switch (code) {
                case JumpCode jump: {
                        currentNode.Next = null;
                        currentNode.AutoNext = false;
                        currentNode.Adjacents.Add(jump.TargetAddress.AbsoluteOffset);
                        break;
                    }
                case ScriptJumpCode scriptJump: {
                        currentNode.Next = null;
                        currentNode.AutoNext = false;
                        currentNode.Contents.Add(new JumpContent(code.Index, scriptJump.TargetScript,
                            scriptJump.TargetEntryIndex));
                        break;
                    }
                case CallCode /*call*/: {
                        //var targetNode = _nodeTable[call.TargetAddress.AbsoluteOffset];
                        //targetNode.IsCallTarget = true;
                        //currentNode.Contents.Add(new CallContent(code.Index, null, call.TargetAddress.AbsoluteOffset));
                        //Console.WriteLine($"[{Index:0000}] calling inner ref {targetNode.Offset}");
                        //break;
                        // Not found in text scripts
                        throw new InvalidDataException();
                    }
                case ScriptCallCode scriptCall: {
                        if (scriptCall.TargetScript <= 1) {
                            continue;
                        }
                        currentNode.Contents.Add(new CallContent(code.Index, scriptCall.TargetScript,
                            scriptCall.TargetEntryIndex));
                        break;
                    }
                case ReturnCode: {
                        //currentNode.IsReturn = true;
                        currentNode.Contents.Add(new ReturnContent());
                        currentNode.Next = null;
                        currentNode.AutoNext = false;
                        break;
                    }
                case CmpJumpCode cmpJumpCode: {
                        currentNode.Adjacents.Add(cmpJumpCode.TargetAddress.AbsoluteOffset);
                        extraStart = true;
                        break;
                    }
                case BoolJumpCode boolJumpCode: {
                        currentNode.Adjacents.Add(boolJumpCode.TargetAddress.AbsoluteOffset);
                        extraStart = true;
                        break;
                    }
                case SwitchCode switchCode: {
                        foreach (var branch in switchCode.Branches) {
                            currentNode.Adjacents.Add(branch.Offset.AbsoluteOffset);
                        }
                        extraStart = true;
                        break;
                    }
                case DialogCode dialog: {
                        var raw = (DialogCode)referenceScript.GetCodeAt(dialog.Index);
                        currentNode.Contents.Add(new TextContent(code.Index, currentCharacter, dialog.Content,
                            raw.Content));
                        currentCharacter = null;
                        break;
                    }
                case ExtraDialogCode extraDialog: {
                        if (extraDialog.IsCharacter) {
                            currentCharacter = extraDialog.Content;
                        }
                        else {
                            var raw = (ExtraDialogCode)referenceScript.GetCodeAt(extraDialog.Index);
                            currentNode.Contents.Add(new TextContent(code.Index, currentCharacter, extraDialog.Content,
                                raw.Content));
                            currentCharacter = null;
                        }
                        break;
                    }
                case TitleCode title: {
                        currentNode.Contents.Add(new TextContent(code.Index, null, $"<title: {title.Content}>", null));
                        break;
                    }
                case TextAreaCode textArea: {
                        currentNode.Contents.Add(new TextContent(code.Index, null,
                            $"<area: {textArea.X}, {textArea.Y}, {textArea.Width}, {textArea.Height}>", null));
                        break;
                    }
                case DirectiveMessageCode directiveMessage: {
                        var raw = (DirectiveMessageCode)referenceScript.GetCodeAt(directiveMessage.Index);
                        currentNode.Contents.Add(new TextContent(code.Index, null, directiveMessage.Content,
                            raw.Content));
                        break;
                    }
                case NovelCode novel: {
                        var raw = (NovelCode)referenceScript.GetCodeAt(novel.Index);
                        currentNode.Contents.Add(new TextContent(code.Index, null, novel.Content, raw.Content));
                        break;
                    }
                case SssInputCode /*sssInput*/: {
                        //currentNode.Contents.Add(new TextContent(code.Index, null, $"<sss-input {sssInput.Type.ToString().ToLowerInvariant()}>"));
                        break;
                    }
                case SssHideCode: {
                        //currentNode.Contents.Add(new TextContent(code.Index, null, "<sss-hide>"));
                        break;
                    }
                case SssFlagCode: {
                        //currentNode.Contents.Add(new TextContent(code.Index, null, "<sss-flag>"));
                        break;
                    }
            }
        }
        foreach (var entry in _entires) {
            _nodeTable[entry].IsEntry = true;
        }
        foreach (var node in _nodeTable.Values) {
            if (node.Next is { } next) {
                node.Adjacents.Add(next);
            }
        }
        int modified;
        do {
            var count0 = Simplify();
            var count1 = RemoveEmptyCircles();
            var count2 = MergeSingleFlow();
            modified = count0 + count1 + count2;
        } while (modified > 0);
    }

    private int Simplify()
    {
        var importantAdjTable = _nodeTable.ToDictionary(kv => kv.Key, _ => new List<int>());
        foreach (var node in _nodeTable.Values) {
            node.Color = NodeColor.White;
        }

        void Dfs(RawNode node)
        {
            if (node.Color == NodeColor.Black) {
                return;
            }
            if (node.Color == NodeColor.Gray) {
                node.IsBackRef = true;
                return;
            }
            node.Color = NodeColor.Gray;
            var offsets = new HashSet<int>();
            foreach (var adjacent in node.Adjacents) {
                var otherNode = _nodeTable[adjacent];
                Dfs(otherNode);
                if (otherNode.IsImportant) {
                    offsets.Add(adjacent);
                }
                else {
                    foreach (var otherAdjacent in importantAdjTable[otherNode.Offset]) {
                        offsets.Add(otherAdjacent);
                    }
                }
            }
            importantAdjTable[node.Offset].AddRange(offsets.OrderBy(x => x));
            node.Color = NodeColor.Black;
        }
        foreach (var node in _nodeTable.Values) {
            Dfs(node);
        }

        var emptyOffsets = _nodeTable.Values
            .Where(x => !x.IsImportant)
            .Select(x => x.Offset)
            .ToList();
        var count = emptyOffsets.Count;
        if (count == 0) {
            return 0;
        }
        foreach (var offset in emptyOffsets) {
            _nodeTable.Remove(offset);
        }
        foreach (var node in _nodeTable.Values) {
            foreach (var adj in importantAdjTable[node.Offset]) {
                if (adj == node.Offset) {
                }
                else if (!_nodeTable.ContainsKey(adj)) {
                    Console.WriteLine($"[{Index:0000}] Missing offset {adj:X8}");
                }
            }
            node.Adjacents.Clear();
            node.Adjacents.AddRange(importantAdjTable[node.Offset]);
        }
        return count;
    }

    private int RemoveEmptyCircles()
    {
        var modified = 0;
        var nodes = _nodeTable.Values.Where(n => n.IsBackRef && n.Contents.Count == 0).ToList();
        var backrefSourceTable = nodes.ToDictionary(n => n.Offset, _ => new List<int>());
        foreach (var node in _nodeTable.Values) {
            foreach (var adj in node.Adjacents) {
                if (adj <= node.Offset && backrefSourceTable.TryGetValue(adj, out var backrefNode)) {
                    backrefNode.Add(node.Offset);
                }
            }
        }
        foreach (var node in nodes) {
            var backrefSources = backrefSourceTable[node.Offset];
            if (backrefSources.Count == 1 && backrefSources[0] == node.Offset) {
                if (node.Adjacents.RemoveAll(r => r == node.Offset) != 1) {
                    Console.WriteLine($"[{Index:0000}] Removing circle {node.Offset} failed.");
                }
                node.IsBackRef = false;
                ++modified;
            }
        }
        return modified;
    }

    private int MergeSingleFlow()
    {
        var modified = 0;
        var offsets = _nodeTable.Keys.ToList();
        var prevTable = _nodeTable.Values.ToDictionary(node => node.Offset, _ => new List<int>());
        foreach (var node in _nodeTable.Values) {
            foreach (var adj in node.Adjacents) {
                try {
                    prevTable[adj].Add(node.Offset);
                }
                catch (Exception exc) {
                    Console.WriteLine($"[{Index:0000}]: {exc}");
                }
            }
        }
        var i = 0;
        while (i < offsets.Count) {
            var offset = offsets[i];
            if (!_nodeTable.TryGetValue(offset, out var node)) {
                ++i;
                continue;
            }
            if (node.Adjacents.Count != 1) {
                ++i;
                continue;
            }
            var adj = node.Adjacents[0];
            var next = _nodeTable[adj];
            if (next.IsEntry || next.IsCallTarget) {
                ++i;
                continue;
            }
            if (prevTable[next.Offset].Count != 1) {
                ++i;
                continue;
            }
            node.Contents.AddRange(next.Contents);
            node.Adjacents.Clear();
            node.Adjacents.AddRange(next.Adjacents);
            _nodeTable.Remove(next.Offset);
            ++modified;
        }
        return modified;
    }

    public static List<Graph?> LoadPackage(SnPackage package, SnPackage referencePackage,
        IList<string> sceneTitles,
        bool parallel = true)
    {
        Graph? CreateGraph(int i)
        {
            var script = package.Scripts[i];
            var refScript = referencePackage.Scripts[i];
            if (script.Footer.ScriptIndex < 0) { return null; }
            var raw = new RawGraph(i, sceneTitles, script, refScript);
            return raw.ToGraph();
        }
        return parallel
            ? Utils.ParallelGenerateList(CreateGraph, package.Scripts.Count)
            : Utils.Generate(CreateGraph, package.Scripts.Count).ToList();
    }

    public Graph ToGraph()
    {
        var graph = new Graph { Index = Index, SceneTitle = SceneTitle };
        foreach (var (offset, node) in _nodeTable) {
            graph.NodeTable.Add(offset, new Node {
                Offset = node.Offset,
                Contents = node.Contents.ToList(),
                Adjacents = node.Adjacents.ToList(),
                DebugInfo = node.DebugInfo,
            });
        }
        foreach (var entry in _entires) {
            graph.Entries.Add(entry);
        }
        return graph;
    }
}
