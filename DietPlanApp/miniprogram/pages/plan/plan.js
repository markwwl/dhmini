// L1 方案列表：阶段侧边导航 + 周卡片
const { request } = require('../../utils/request');

Page({
  data: { plan: null, stages: [], activeStage: 0 },

  onShow() { this.loadData(); },

  async loadData() {
    try {
      const plans = await request('/api/plans', 'GET', null, 5 * 60 * 1000);
      if (!plans || !plans.length) { this.setData({ plan: null }); return; }
      const tree = await request('/api/plans/' + plans[0].id, 'GET', null, 5 * 60 * 1000);
      const stages = tree.stages.map(s => ({ ...s, weeks: s.weeks.map(w => ({ ...w, planId: tree.id, stageName: s.name + ' ' + s.theme })) }));
      this.setData({ plan: tree, stages, activeStage: 0 });
      // 数据就绪后再建立滚动监听，确保各阶段节点已渲染
      this.initScrollSpy();
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

  // 滚动高亮当前阶段：监听各阶段块进入视口，动态更新 activeStage（需求 4.1）
  initScrollSpy() {
    if (this._observer) { this._observer.disconnect(); }
    const that = this;
    const observer = wx.createIntersectionObserver(this, { observeAll: true });
    this._observer = observer;
    observer.relativeToViewport({ top: 0 })
      .observe('.stage-block', (res) => {
        if (res.intersectionRatio > 0) {
          const idx = Number(String(res.id).replace('stage-', ''));
          if (!isNaN(idx) && idx !== that.data.activeStage) {
            that.setData({ activeStage: idx });
          }
        }
      });
  },

  onUnload() {
    if (this._observer) { this._observer.disconnect(); this._observer = null; }
  },

  // 进入 L2 每日食谱
  goDay(e) {
    const { weekId, daycount, stage, label } = e.currentTarget.dataset;
    const planName = this.data.plan ? this.data.plan.name : '';
    wx.navigateTo({
      url: `/pages/day/day?weekId=${weekId}&dayNo=1&dayCount=${daycount}&stage=${encodeURIComponent(stage)}&label=${encodeURIComponent(label)}&planName=${encodeURIComponent(planName)}`
    });
  }
});
