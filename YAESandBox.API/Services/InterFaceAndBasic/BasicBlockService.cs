﻿using YAESandBox.Core.Block;

namespace YAESandBox.API.Services;

public class BasicBlockService(INotifierService notifierService, IBlockManager blockManager)
{
    /// <summary>
    /// 通知服务
    /// </summary>
    protected INotifierService notifierService { get; } = notifierService;

    /// <summary>
    /// Block管理器
    /// </summary>
    protected IBlockManager blockManager { get; } = blockManager;

    /// <summary>
    /// 获得原始Block对象——不暴露给外部类
    /// </summary>
    /// <param name="blockId"></param>
    /// <returns></returns>
    protected Task<BlockStatus?> GetBlockAsync(string blockId) =>
        this.blockManager.GetBlockAsync(blockId);
}