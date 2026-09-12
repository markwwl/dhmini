// 网络层：BASE 统一从 config.js 取（按环境自动切换开发/生产地址，避免硬编码进仓库）
const { BASE } = require('./config');
const TOKEN_KEY = 'dietplan_token';

function getToken() { return wx.getStorageSync(TOKEN_KEY) || ''; }
function setToken(t) { wx.setStorageSync(TOKEN_KEY, t); }

// 低频 GET 缓存（5 分钟），仅对显式传入 cacheMs 的接口生效，避免打卡状态等实时数据被缓存
const _cache = new Map();
const _cacheAt = new Map();

// 通用请求：自动带 JWT，401 时重新静默登录后重试一次
function request(path, method = 'GET', data, cacheMs) {
  const useCache = method === 'GET' && cacheMs && cacheMs > 0;
  if (useCache) {
    const hit = _cache.get(path);
    const at = _cacheAt.get(path) || 0;
    if (hit !== undefined && Date.now() - at < cacheMs) {
      return Promise.resolve(hit);
    }
  }
  return new Promise((resolve, reject) => {
    wx.request({
      url: BASE + path,
      method,
      data,
      header: { Authorization: 'Bearer ' + getToken() },
      success(res) {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          if (useCache) { _cache.set(path, res.data); _cacheAt.set(path, Date.now()); }
          resolve(res.data); return;
        }
        if (res.statusCode === 401) {
          login().then(() => request(path, method, data, cacheMs).then(resolve, reject)).catch(reject);
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
