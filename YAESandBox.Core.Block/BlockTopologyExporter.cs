﻿using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using YAESandBox.Depend;

namespace YAESandBox.Core.Block;

public static class BlockTopologyExporter
{
    /// <summary>
    /// 用于序列化为 JSON 的内部节点表示。
    /// </summary>
    public record JsonBlockNode(string Id)
    {
        [JsonPropertyName("id")] public string Id { get; set; } = Id;

        [JsonPropertyName("children")] public List<JsonBlockNode> Children { get; } = new();
    }

    /// <summary>
    /// 从 Block 字典生成表示拓扑结构的嵌套 JSON 字符串。
    /// </summary>
    /// <param name="allBlocks">包含所有 Block 状态对象的字典 (BlockId -> BlockStatus)。</param>
    /// <param name="rootId">要开始构建树的根节点 ID (例如 "__WORLD__")。</param>
    /// <returns>表示嵌套拓扑结构的 JSON 字符串，如果根节点无效或发生错误则返回 null。</returns>
    public static JsonBlockNode? GenerateTopologyJson(
        IReadOnlyDictionary<string, IBlockNode> allBlocks, // 使用接口增加灵活性
        string rootId = BlockManager.WorldRootId // 使用常量默认值
    )
    {
        if (!allBlocks.Any())
        {
            Log.Warning("GenerateTopologyJson: 输入的 Block 字典为空。");
            return null;
        }

        if (!allBlocks.ContainsKey(rootId))
        {
            Log.Error($"GenerateTopologyJson: 根节点 ID '{rootId}' 在字典中不存在。");
            return null;
        }

        try
        {
            // 从指定的根节点开始递归构建节点树
            return BuildNodeRecursive(rootId, allBlocks, allBlocks.Count);
        }
        catch (Exception ex)
        {
            // 捕获任何在构建或序列化过程中发生的异常
            Log.Error(ex, $"GenerateTopologyJson: 生成 JSON 时发生错误: {ex.Message}");
            return null; // 返回 null 表示失败
        }
    }

    /// <summary>
    /// 递归辅助方法，用于构建单个节点及其子节点。
    /// </summary>
    /// <param name="currentBlockId">当前要处理的 Block ID。</param>
    /// <param name="allBlocks">包含所有 Block 状态对象的字典。</param>
    /// <param name="depth">最大递归深度，建议初始为allBlocks.Count</param>
    /// <returns>构建好的 JsonBlockNode，如果当前 ID 无效则返回 null。</returns>
    private static JsonBlockNode? BuildNodeRecursive(
        string currentBlockId,
        IReadOnlyDictionary<string, IBlockNode> allBlocks,
        int depth)
    {
        // 尝试从字典中获取当前 Block 的状态对象
        if (!allBlocks.TryGetValue(currentBlockId, out var currentBlockStatus))
        {
            // 如果在递归过程中发现引用的子节点 ID 无效，记录警告并跳过
            Log.Warning($"BuildNodeRecursive: 尝试访问的 Block ID '{currentBlockId}' 在字典中不存在 (可能数据不一致)。");
            return null; // 返回 null，调用者会忽略这个无效的子节点
        }

        // 创建当前节点的 JSON 表示
        var jsonNode = new JsonBlockNode(currentBlockStatus.BlockId);

        if (!currentBlockStatus.ChildrenList.Any())
            return jsonNode;
        if (depth == -1) //不太确定，可能应该是0才是“假设为单链情况下，最大深度”，不过这应该不影响任何东西，毕竟深度只是个保险。
            return jsonNode;

        // 递归处理所有子节点
        foreach (var childNode in currentBlockStatus.ChildrenList
                     .Select(childId => BuildNodeRecursive(childId, allBlocks, depth - 1)).OfType<JsonBlockNode>())
            jsonNode.Children.Add(childNode);
        // 返回构建好的当前节点（包含其所有有效的子节点）
        return jsonNode;
    }
}