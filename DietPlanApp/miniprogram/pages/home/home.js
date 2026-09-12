// 首页：运营 banner + 方案入口
const { request } = require('../../utils/request');

Page({
  data: { banners: [], plan: null, news: [] },

  onShow() { this.loadData(); },

  async loadData() {
    try {
      const [banners, news, plans] = await Promise.all([
        request('/api/content/blocks?type=banner', 'GET', null, 5 * 60 * 1000),
        request('/api/content/blocks?type=news', 'GET', null, 5 * 60 * 1000),
        request('/api/plans', 'GET', null, 5 * 60 * 1000)
      ]);
      const plan = plans && plans.length ? plans[0] : null;
      if (plan) plan.tree = await request('/api/plans/' + plan.id, 'GET', null, 5 * 60 * 1000);
      this.setData({ banners, news: news || [], plan });
    } catch (e) { console.error(e); }
  },

  goPlan() { wx.switchTab({ url: '/pages/plan/plan' }); }
});
