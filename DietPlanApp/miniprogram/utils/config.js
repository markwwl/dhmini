// 环境化配置：把会随部署环境变化的值集中到此文件，避免散落在各页面硬编码
// 开发用电脑局域网/本地 IP；正式发布替换为 https 域名（微信要求生产必须为 https）
const DEV_BASE = 'http://192.168.3.100:5000';
const PROD_BASE = 'https://your-domain.com'; // TODO 上线替换为真实 https 域名

// 自动按小程序环境区分：release=正式版用生产地址，其余（develop/trial）用开发地址
let env = 'develop';
try {
  env = (wx.getAccountInfoSync().miniProgram || {}).envVersion || 'develop';
} catch (e) {
  env = 'develop';
}

const BASE = env === 'release' ? PROD_BASE : DEV_BASE;

module.exports = { BASE, DEV_BASE, PROD_BASE, env };
