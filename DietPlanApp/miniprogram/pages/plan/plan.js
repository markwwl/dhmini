// L1 方案列表：左侧阶段时间轴（随整页滚动）+ 右侧周卡片
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
      // 导航栏标题跟随方案名，避免与页面内容不一致
      wx.setNavigationBarTitle({ title: tree.name || '饮食方案' });
      // 渲染完成后测量各阶段锚点位置，供 onPageScroll 判定当前阶段
      wx.nextTick(() => this.measureStages());
    } catch (e) {
      console.error(e);
      this.setData({ plan: null });
    }
  },

  // 测量每个阶段块相对页面顶部的偏移（px），用于滚动高亮
  measureStages() {
    const q = wx.createSelectorQuery();
    q.selectAll('.stage-block').boundingClientRect();
    q.selectViewport().scrollOffset();
    q.exec((res) => {
      const rects = res[0] || [];
      const scrollTop = res[1] ? res[1].scrollTop : 0;
      this._stageTops = rects.map(r => r.top + scrollTop);
    });
  },

  // 页面级滚动：时间轴跟随内容滚动，同步高亮当前阶段
  onPageScroll(e) {
    if (!this._stageTops || !this._stageTops.length) { this.measureStages(); return; }
    const probe = e.scrollTop + 100; // 容差：以视口上方约 100px 为判定基准
    let active = 0;
    for (let i = 0; i < this._stageTops.length; i++) {
      if (this._stageTops[i] <= probe) active = i;
    }
    if (active !== this.data.activeStage) this.setData({ activeStage: active });
  },

  // 点击阶段导航：滚动到对应锚点
  tapStage(e) {
    const idx = Number(e.currentTarget.dataset.idx);
    this.setData({ activeStage: idx });
    wx.pageScrollTo({ selector: '#stage-' + idx, duration: 300 });
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
