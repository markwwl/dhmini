// 首页：运营 banner + 方案入口
const { request } = require('../../utils/request');

Page({
  data: { banners: [], plan: null },

  onShow() { this.loadData(); },

  async loadData() {
    try {
      const banners = await request('/api/content/blocks?type=banner');
      const plans = await request('/api/plans');
      const plan = plans && plans.length ? plans[0] : null;
      if (plan) plan.tree = await request('/api/plans/' + plan.id);
      this.setData({ banners, plan });
    } catch (e) { console.error(e); }
  },

  goPlan() { wx.switchTab({ url: '/pages/plan/plan' }); }
});
