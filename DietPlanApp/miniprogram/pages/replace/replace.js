// L4 更换菜品：候选列表搜索单选 + 确认更换
const { request } = require('../../utils/request');

Page({
  data: {
    slotId: 0, weekId: 0, dayNo: 1,
    keyword: '', candidates: [], selected: 0
  },

  onLoad(options) {
    this.setData({ slotId: +options.slotId, weekId: +options.weekId, dayNo: +options.dayNo });
    this.loadCandidates();
  },

  async loadCandidates() {
    try {
      const list = await request(`/api/dish-slots/${this.data.slotId}/candidates?keyword=` + encodeURIComponent(this.data.keyword));
      // 默认选中当前菜品
      const current = list.find(c => c.isCurrent);
      this.setData({ candidates: list, selected: current ? current.dishId : (list.length ? list[0].dishId : 0) });
    } catch (e) { wx.showToast({ title: String(e.message || e), icon: 'none' }); }
  },

  onSearch(e) { this.setData({ keyword: e.detail.value }); },
  doSearch() { this.loadCandidates(); },

  select(e) { this.setData({ selected: +e.currentTarget.dataset.id }); },

  // 预览制作方法
  goDish(e) { wx.navigateTo({ url: '/pages/dish/dish?id=' + e.currentTarget.dataset.id }); },

  async confirm() {
    if (!this.data.selected) { wx.showToast({ title: '请先选择菜品', icon: 'none' }); return; }
    try {
      await request(`/api/dish-slots/${this.data.slotId}/dish`, 'PUT', { slotId: this.data.slotId, dishId: this.data.selected });
      wx.showToast({ title: '更换成功' });
      setTimeout(() => wx.navigateBack(), 600);
    } catch (e) { wx.showToast({ title: String(e.message || e), icon: 'none' }); }
  }
});
