// L1 方案列表：阶段侧边导航 + 周卡片
const { request } = require('../../utils/request');

Page({
  data: { plan: null, stages: [], activeStage: 0 },

  onShow() { this.loadData(); },

  async loadData() {
    try {
      const plans = await request('/api/plans');
      if (!plans || !plans.length) { this.setData({ plan: null }); return; }
      const tree = await request('/api/plans/' + plans[0].id);
      const stages = tree.stages.map(s => ({ ...s, weeks: s.weeks.map(w => ({ ...w, planId: tree.id, stageName: s.name + ' ' + s.theme })) }));
      this.setData({ plan: tree, stages, activeStage: 0 });
    } catch (e) {
      console.error(e);
      this.setData({ plan: null });
    }
  },

  // 点击阶段导航：滚动到对应锚点
  tapStage(e) {
    const idx = e.currentTarget.dataset.idx;
    this.setData({ activeStage: idx });
    wx.pageScrollTo({ selector: '#stage-' + idx, duration: 300 });
  },

  // 进入 L2 每日食谱
  goDay(e) {
    const { weekId, daycount, stage, label } = e.currentTarget.dataset;
    wx.navigateTo({
      url: `/pages/day/day?weekId=${weekId}&dayNo=1&dayCount=${daycount}&stage=${encodeURIComponent(stage)}&label=${encodeURIComponent(label)}`
    });
  }
});
