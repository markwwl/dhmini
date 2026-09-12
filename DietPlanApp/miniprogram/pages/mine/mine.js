// 我的：打卡统计 + 基本信息 + 收藏菜品（一期本地化）
const { request } = require('../../utils/request');

Page({
  data: { nickname: '', avatar: '', summary: null, favs: [] },

  onShow() { this.load(); },

  async load() {
    const g = getApp().globalData;
    // 真实头像昵称（一期本地存储，待确认项 3 决定是否服务端化）
    let profile = {};
    try { profile = JSON.parse(wx.getStorageSync('wx_profile') || '{}'); } catch (e) { profile = {}; }
    this.setData({
      nickname: profile.nickname || g.nickname || '健康饮食用户',
      avatar: profile.avatar || ''
    });
    // 收藏列表：读取本地索引 + 明细
    const favs = this.readFavs();
    this.setData({ favs });
    try {
      const summary = await request('/api/checkins/summary');
      // 日历加工：截成 MM/DD 短日期 + 按当日打卡数分热度等级（0 无 / 1 少 / 2 多）
      if (summary && summary.calendar) {
        summary.calendar = summary.calendar.map(c => {
          const d = String(c.date || '');
          const md = d.length >= 10 ? d.slice(5, 10).replace('-', '/') : d;
          const lv = c.count <= 0 ? 0 : (c.count >= 3 ? 2 : 1);
          return { date: d, md, count: c.count, lv };
        });
      }
      this.setData({ summary });
    } catch (e) { console.error(e); }
  },

  readFavs() {
    let ids = [];
    try { ids = JSON.parse(wx.getStorageSync('fav_dish_ids') || '[]'); } catch (e) { ids = []; }
    const list = [];
    for (const id of ids) {
      let raw = {};
      try { raw = JSON.parse(wx.getStorageSync('liked_dish_' + id) || '{}'); } catch (e) { raw = {}; }
      if (raw && raw.name) list.push(raw);
    }
    return list;
  },

  // 微信头像昵称填写（需求 4.6 / 15）：头像用 chooseAvatar，昵称用 nickname input
  onChooseAvatar(e) {
    const avatar = e.detail.avatarUrl;
    this.saveProfile({ avatar });
  },
  onNicknameInput(e) {
    this.saveProfile({ nickname: e.detail.value });
  },
  saveProfile(patch) {
    let profile = {};
    try { profile = JSON.parse(wx.getStorageSync('wx_profile') || '{}'); } catch (e) { profile = {}; }
    profile = Object.assign(profile, patch);
    wx.setStorageSync('wx_profile', JSON.stringify(profile));
    this.setData({ nickname: profile.nickname || this.data.nickname, avatar: profile.avatar || '' });
  },

  // 进入收藏菜品 L3
  goFav(e) {
    const id = e.currentTarget.dataset.id;
    if (id) wx.navigateTo({ url: '/pages/dish/dish?id=' + id });
  }
});
