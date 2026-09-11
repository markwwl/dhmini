// 我的：打卡统计 + 基本信息
const { request } = require('../../utils/request');

Page({
  data: { nickname: '', summary: null },

  onShow() { this.load(); },

  async load() {
    const g = getApp().globalData;
    this.setData({ nickname: g.nickname || '健康饮食用户' });
    try {
      const summary = await request('/api/checkins/summary');
      this.setData({ summary });
    } catch (e) { console.error(e); }
  }
});
