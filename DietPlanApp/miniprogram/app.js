// 应用入口：静默登录拿 JWT，全局共享
const { login } = require('./utils/request');

App({
  globalData: {
    userId: 0,
    nickname: ''
  },

  onLaunch() {
    // 微信静默登录（wx.login 的 code 换 token，用户无感知）
    login().catch(err => {
      console.error('静默登录失败（本地开发请确认后端已启动）', err);
    });
  }
});
