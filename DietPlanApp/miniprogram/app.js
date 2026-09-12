// 应用入口：先确认隐私授权（微信合规要求），再静默登录拿 JWT，全局共享
const { login } = require('./utils/request');

App({
  globalData: {
    userId: 0,
    nickname: ''
  },

  onLaunch() {
    // 微信要求：调用 wx.login 等隐私接口前，必须先取得用户隐私授权，否则审核驳回
    this.ensurePrivacyThenLogin();
  },

  // 隐私授权：需要授权时弹官方隐私协议页，用户同意后再静默登录
  ensurePrivacyThenLogin() {
    const doLogin = () => {
      login().catch(err => {
        console.error('静默登录失败（本地开发请确认后端已启动）', err);
      });
    };

    if (typeof wx.getPrivacySetting !== 'function') {
      doLogin();
      return;
    }

    wx.getPrivacySetting({
      success(res) {
        if (res.needAuthorization && typeof wx.requirePrivacyAuthorize === 'function') {
          wx.requirePrivacyAuthorize({
            success: doLogin,
            fail: doLogin // 用户拒绝：先放行，后续调隐私接口时微信会再次拦截
          });
        } else {
          doLogin();
        }
      },
      fail: doLogin
    });
  }
});
