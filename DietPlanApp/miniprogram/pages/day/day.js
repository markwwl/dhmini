// L2 每日食谱：天选择器 + 三餐卡片 + 打卡 + 换菜入口
const { request } = require('../../utils/request');

Page({
  data: {
    weekId: 0, dayNo: 1, dayCount: 0, stage: '', label: '',
    dayList: [],            // 天选择器
    day: null,              // 当日三餐数据
    showDetail: false,      // 方案详情浮层
    detailText: ''
  },

  onLoad(options) {
    this.setData({
      weekId: +options.weekId || 0,
      dayNo: +options.dayNo || 1,
      dayCount: +options.dayCount || 1,
      stage: decodeURIComponent(options.stage || ''),
      label: decodeURIComponent(options.label || '')
    });
    this.setData({ dayList: Array.from({ length: this.data.dayCount }, (_, i) => i + 1) });
    wx.setNavigationBarTitle({ title: `${this.data.stage}（${this.data.label}）` });
    this.loadDay();
  },

  async loadDay() {
    try {
      const day = await request(`/api/weeks/${this.data.weekId}/days/${this.data.dayNo}/meals`);
      this.setData({ day });
    } catch (e) { wx.showToast({ title: String(e.message || e), icon: 'none' }); }
  },

  tapDay(e) { this.setData({ dayNo: +e.currentTarget.dataset.d }); this.loadDay(); },

  // 查看 L3 制作解析
  goDish(e) {
    wx.navigateTo({ url: '/pages/dish/dish?id=' + e.currentTarget.dataset.id });
  },

  // 进入 L4 更换菜品
  goReplace(e) {
    const { slotid } = e.currentTarget.dataset;
    wx.navigateTo({ url: `/pages/replace/replace?slotId=${slotid}&weekId=${this.data.weekId}&dayNo=${this.data.dayNo}` });
  },

  // 单菜打卡
  async checkin(e) {
    const slotId = e.currentTarget.dataset.slotid;
    try {
      await request('/api/checkins', 'POST', { weekId: this.data.weekId, dayNo: this.data.dayNo, slotId });
      wx.showToast({ title: '打卡成功' });
      this.loadDay();
    } catch (err) { wx.showToast({ title: String(err.message || err), icon: 'none' }); }
  },

  // 餐次一键打卡
  async checkinMeal(e) {
    const meal = this.data.day.meals.find(m => m.mealType === e.currentTarget.dataset.meal);
    if (!meal) return;
    try {
      for (const s of meal.slots) {
        if (!s.checked) {
          await request('/api/checkins', 'POST', { weekId: this.data.weekId, dayNo: this.data.dayNo, slotId: s.slotId });
        }
      }
      wx.showToast({ title: '本餐打卡成功' });
      this.loadDay();
    } catch (err) { wx.showToast({ title: String(err.message || err), icon: 'none' }); }
  },

  // 整日打卡（底部操作条）
  async checkinAll() {
    try {
      const r = await request('/api/checkins/day', 'POST', { weekId: this.data.weekId, dayNo: this.data.dayNo });
      wx.showToast({ title: r.count ? `打卡${r.count}项成功` : '今日已全部打卡' });
      this.loadDay();
    } catch (err) { wx.showToast({ title: String(err.message || err), icon: 'none' }); }
  },

  // 方案详情浮层
  async showPlanDetail() {
    try {
      const tree = await request('/api/plans/' + this.data.day.planId);
      const stage = tree.stages.find(s => s.id === this.data.day.stageId) || {};
      this.setData({ showDetail: true, detailText: stage.detailRichText || '暂无方案详情' });
    } catch (e) { wx.showToast({ title: String(e.message || e), icon: 'none' }); }
  },
  closeDetail() { this.setData({ showDetail: false }); }
});
