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
      const liked = !!wx.getStorageSync('liked_dish_' + this.dishId);
      this.setData({ liked });
    } catch (e) { wx.showToast({ title: String(e.message || e), icon: 'none' }); }
  },

  toggleLike() {
    const liked = !this.data.liked;
    const key = 'liked_dish_' + this.dishId;
    // 维护收藏索引（供「我的」页聚合列表读取，一期本地化）
    let ids = [];
    try { ids = JSON.parse(wx.getStorageSync('fav_dish_ids') || '[]'); } catch (e) { ids = []; }
    if (liked) {
      const cover = (this.data.dish.images && this.data.dish.images[0]) || '';
      wx.setStorageSync(key, JSON.stringify({ id: this.dishId, name: this.data.dish.name, image: cover, subtitle: this.data.dish.subtitle }));
      if (!ids.includes(this.dishId)) ids.push(this.dishId);
    } else {
      wx.removeStorageSync(key);
      ids = ids.filter(id => id !== this.dishId);
    }
    wx.setStorageSync('fav_dish_ids', JSON.stringify(ids));
    this.setData({ liked });
    wx.showToast({ title: liked ? '已收藏' : '已取消' });
  }
});
