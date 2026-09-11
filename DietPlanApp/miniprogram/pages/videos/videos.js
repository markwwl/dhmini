// 视频秀：后台配置的视频列表
const { request, BASE } = require('../../utils/request');

Page({
  data: { videos: [] },

  onShow() { this.load(); },

  async load() {
    try {
      const videos = await request('/api/content/blocks?type=video');
      this.setData({ videos });
    } catch (e) { console.error(e); }
  },

  play(e) {
    const url = e.currentTarget.dataset.url;
    if (url) wx.navigateTo({ url: '/pages/videoplay/videoplay?src=' + encodeURIComponent(url) });
  }
});
