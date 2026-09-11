// 视频播放页
Page({
  data: { src: '' },
  onLoad(options) { this.setData({ src: decodeURIComponent(options.src || '') }); }
});
