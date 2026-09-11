// L3 菜品制作解析：大图 + 食材清单 + 制作步骤
const { request } = require('../../utils/request');

Page({
  data: { dish: null, liked: false },

  onLoad(options) { this.dishId = +options.id; this.loadDish(); },

  async loadDish() {
    try {
      const dish = await request('/api/dishes/' + this.dishId);
      this.setData({ dish });
      wx.setNavigationBarTitle({ title: dish.name });
      // 收藏状态本地记录（一期）
      const liked = wx.getStorageSync('liked_dish_' + this.dishId) === '1';
      this.setData({ liked });
    } catch (e) { wx.showToast({ title: String(e.message || e), icon: 'none' }); }
  },

  toggleLike() {
    const liked = !this.data.liked;
    wx.setStorageSync('liked_dish_' + this.dishId, liked ? '1' : '0');
    this.setData({ liked });
    wx.showToast({ title: liked ? '已收藏' : '已取消' });
  }
});
