// 网络层：真机预览用电脑局域网 IP；模拟器改回 http://localhost:5000；上线改为 https 域名
// 当前局域网 IP：192.168.3.100（换 Wi-Fi/网段时改此处，IP 会变）
const BASE = 'http://192.168.3.100:5000';
const TOKEN_KEY = 'dietplan_token';

function getToken() { return wx.getStorageSync(TOKEN_KEY) || ''; }
function setToken(t) { wx.setStorageSync(TOKEN_KEY, t); }

// 通用请求：自动带 JWT，401 时重新静默登录后重试一次
function request(path, method = 'GET', data) {
  return new Promise((resolve, reject) => {
    wx.request({
      url: BASE + path,
      method,
      data,
      header: { Authorization: 'Bearer ' + getToken() },
      success(res) {
        if (res.statusCode >= 200 && res.statusCode < 300) { resolve(res.data); return; }
        if (res.statusCode === 401) {
          login().then(() => request(path, method, data).then(resolve, reject)).catch(reject);
          return;
        }
        reject(new Error((res.data && (res.data.title || res.data.error)) || ('HTTP ' + res.statusCode)));
      },
      fail: reject
    });
  });
}

// 静默登录：wx.login code → /api/auth/wx-login
function login() {
  return new Promise((resolve, reject) => {
    wx.login({
      success(res) {
        wx.request({
          url: BASE + '/api/auth/wx-login',
          method: 'POST',
          data: { code: res.code },
          header: { 'Content-Type': 'application/json' },
          success(r) {
            if (r.statusCode === 200 && r.data.token) {
              setToken(r.data.token);
              getApp().globalData.userId = r.data.userId;
              getApp().globalData.nickname = r.data.nickname;
              resolve(r.data);
            } else { reject(new Error('登录失败')); }
          },
          fail: reject
        });
      },
      fail: reject
    });
  });
}

module.exports = { request, login, setToken, getToken, BASE };
