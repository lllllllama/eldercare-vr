# Pico 项目图标库

本目录包含从以下三个 HTML 文件中提取出的所有图标，已全部转换为 **SVG 矢量格式**：

- `pico_pingpang.html` – 乒乓球训练 MR 控制面板
- `pico_pingpang_2.html` – 乒乓球训练（含命中率进度条）
- `pico03.html` – 银龄·主动健康 MR 原型

## 📊 概览

- **共 46 个独立图标**
- 全部为 SVG 矢量文件，可任意缩放不失真
- 文件名格式：`U+<codepoint>_<语义名>.svg`
- 示例：`U+1F3D3_table_tennis_paddle_and_ball.svg` = 🏓

## 📂 目录结构

```
bundle/
├── index.html          # 图标预览页（浏览器打开查看全部图标）
├── README.md           # 本文件
└── svg_icons/          # 46 个 SVG 图标文件
    ├── U+1F3D3_table_tennis_paddle_and_ball.svg
    ├── U+1F3AF_direct_hit.svg
    └── ...
```

## 🎨 图标来源

- **彩色 emoji 图标**：来自 [Twemoji 14.0.2](https://github.com/twitter/twemoji)（Twitter 开源，CC-BY 4.0 授权）
- **几何符号**（○ ● ▶ ⏸ 🟢）：手工绘制的简洁 SVG

## 🚀 使用方法

### 方式 1：直接在 HTML 中引用
```html
<img src="svg_icons/U+1F3D3_table_tennis_paddle_and_ball.svg" width="24" height="24">
```

### 方式 2：内联 SVG（可用 CSS 改色）
```html
<div>
  <!-- 直接粘贴 SVG 内容 -->
</div>
```

### 方式 3：CSS 背景图
```css
.icon-paddle {
  background-image: url('svg_icons/U+1F3D3_table_tennis_paddle_and_ball.svg');
  background-size: contain;
}
```

### 方式 4：替换原 HTML 中的 emoji
把原本渲染 emoji 字符的位置改为 `<img>`，规避不同系统 emoji 字体不一致的问题，视觉效果统一。

## 📄 授权

Twemoji 图标遵循 CC-BY 4.0：可自由用于个人及商业项目，使用时请保留对 Twitter/Twemoji 的署名。
